// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Runtime.Serialization;
using Microsoft.Azure.EventHubs;
using ReactiveMachine;
using System.Collections.Generic;
using ReactiveMachine.TelemetryBlobWriter;
using Microsoft.Extensions.Logging;
using ReactiveMachine.Util;

namespace FunctionsHost
{
    public static class HostManager<TStaticApplicationInfo>
            where TStaticApplicationInfo: IStaticApplicationInfo, new()
    {
        public static async Task<string> InitializeService(Microsoft.Azure.WebJobs.ExecutionContext executionContext, ILogger logger)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var applicationInfo = new TStaticApplicationInfo();

            logger = new LoggerWrapper(logger, "[initialize] ");

            try
            {
                var configuration = applicationInfo.GetHostConfiguration();

                var host = new Host<TStaticApplicationInfo>(applicationInfo, configuration, logger, 0, stopwatch, executionContext.InvocationId);

                var deploymentTimestamp = DateTime.UtcNow;
                var deploymentId = applicationInfo.GetDeploymentId(deploymentTimestamp);
                host.SetDeploymentTimestamp(deploymentTimestamp);

                // generate blobs
                var cloudBlobContainer = await AzureBlobStorageStateManager.GetCloudBlobContainer(
                     storageConnectionString: configuration.StorageConnectionString,
                     logger: logger,
                     initialize: true
                );

                // check the current position in all the queues, and start from there
                var initTasks = new List<Task>();
                for (uint i = 0; i < host.NumberProcesses; i++)
                    StartTask(initTasks, async () =>
                    {
                        uint processId = i;
                        var lastEnqueued = await host.GetLastEnqueuedSequenceNumber(processId);
                        await AzureBlobStorageStateManager.Save(
                            cloudBlobContainer,
                            deploymentId,
                            logger,
                            processId,
                            new ProcessState(deploymentTimestamp, lastEnqueued));
                    });
                await Task.WhenAll(initTasks);

                // send ping message to process 0
                var guid = Guid.NewGuid();
                var message = new DoorbellMessage()
                {
                    ProcessId = 0,
                    Guid = guid
                };
                var messageBytes = DoorbellMessage.Serialize(message);
                await host.Connections.GetDoorbellSender(0).SendAsync(new EventData(messageBytes));

                await host.Cleanup(true);

                return deploymentId;
            }
            catch(Exception e)
            {
                logger.LogError($"Initialize failed: {e}");
                throw;
            }
        }

        private static void StartTask(List<Task> list, Func<Task> body)
        {
            list.Add(body());
        }

        public static async Task Doorbell(Microsoft.Azure.WebJobs.ExecutionContext executionContext, ILogger logger, EventData[] messages)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var msg = DoorbellMessage.Deserialize(messages[0].Body.Array);
            
            var processId = msg.ProcessId;

            var applicationInfo = new TStaticApplicationInfo();

            var configuration = applicationInfo.GetHostConfiguration();

            var hostlogger = new LoggerWrapper(logger, $"[p{processId:d3} doorbell] ");

            var lastRing = messages[messages.Length - 1].SystemProperties.SequenceNumber;


            try
            {
                var cloudBlobContainer = await AzureBlobStorageStateManager.GetCloudBlobContainer(configuration.StorageConnectionString, hostlogger);

                var stateBlob = cloudBlobContainer.GetBlockBlobReference(AzureBlobStorageStateManager.BlobName(processId));

                var leaseManager = new LeaseManager(stateBlob);

                if (await leaseManager.TryGetLease(hostlogger))
                {
                    hostlogger.LogInformation($"{DateTime.UtcNow:o} Rings x{messages.Length} on {Environment.MachineName}, through #{lastRing}, lease acquired");
                    await RunHost(applicationInfo, configuration, processId, hostlogger, logger, leaseManager, stopwatch, executionContext.InvocationId);
                }
                else
                {
                    hostlogger.LogInformation($"{DateTime.UtcNow:o} Rings x{messages.Length} on {Environment.MachineName}, through #{lastRing}, ignored");
                }
            }
            catch (Exception e)
            {
                hostlogger.LogError($"Doorbell failed: {e}");
            }
        }

        private static async Task RunHost(IStaticApplicationInfo applicationInfo, FunctionsHostConfiguration configuration, uint processId,
                                          ILogger hostlogger, ILogger logger, LeaseManager leaseManager, System.Diagnostics.Stopwatch stopwatch, Guid invocationId)
        {
            try
            {
                var host = new Host<TStaticApplicationInfo>(applicationInfo, configuration, logger, processId, stopwatch, invocationId);

                var done = await host.ResumeFromCheckpoint(leaseManager);

                // if we get here, there is no more work for this process
                await leaseManager.Release();

               hostlogger.LogInformation($"Lease released");

                if (!done)
                    await host.RingMyself();
                else
                    await host.FinalRecheck();

                await host.Cleanup(false);
            }
            catch (Exception e)
            {
                hostlogger.LogError($"RunHost Failed: {e}");

                // throw exception, so event hub delivers doorbell message again
                // TODO think about poison messages
                throw; 
            }
            finally
            {
                hostlogger.LogDebug($"Control returned");
            }
        }

     

    }
}
