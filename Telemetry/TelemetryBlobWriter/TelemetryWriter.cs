// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using ReactiveMachine;

namespace ReactiveMachine.TelemetryBlobWriter
{
    public class TelemetryCollector : ITelemetryListener
    {
        private readonly Configuration configuration;

        private readonly Dictionary<string, Object> configurations;

        private readonly uint processId;

        private readonly Type hostType;

        private readonly Stopwatch stopwatch = new Stopwatch();

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private readonly DateTime starttime;

        public CancellationToken TerminationToken => cts.Token;

        public TelemetryCollector(Configuration configuration, ICompiledApplication application,
            uint processId, Type hostType)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(configuration.ConnectionString))
            {
                configuration.ConnectionString = 
                    System.Environment.GetEnvironmentVariable("REACTIVE_MACHINE_TELEMETRY")
                    ??  "UseDevelopmentStorage=true;";
            }
            this.configurations = new Dictionary<string, object>();
            this.processId = processId;
            this.hostType = hostType;
            
            foreach(var kvp in application.Configurations)
            {
                configurations[kvp.Key.FullName] = kvp.Value;
            }

            starttime = DateTime.UtcNow;
            stopwatch.Start();
        }

        public List<TelemetryEvent> TelemetryEvents = new List<TelemetryEvent>();

        public void OnApplicationEvent(uint processId, string id, string name, string parent, OperationSide opSide, OperationType opType, double duration)
        {
            if (opSide == OperationSide.Caller
                && (   configuration.CollectApplicationEvents 
                    || configuration.CollectThroughput  
                    || configuration.CollectHostEvents && opType == OperationType.Host))
            {
                TimeSpan time = stopwatch.Elapsed;

                TelemetryEvents.Add(
                    new TelemetryEvent()
                    {
                        id = id,
                        name = name,
                        parent = parent,
                        duration = duration,
                        opSide = opSide,
                        opType = opType,
                        time = time.TotalMilliseconds
                    }
                );
            }
        }

        public async Task<string> PushTelemetry(string deploymentId, DateTime deploymentTimestamp, bool includeParameters)
        {
            if (TelemetryEvents.Count > 0)
            {
                await WriteThroughput(deploymentId, deploymentTimestamp);

                await WriteEvents(deploymentId, deploymentTimestamp);

                // reset the list if there are more later
                TelemetryEvents = new List<TelemetryEvent>();

            }

            return await WriteParameters(deploymentId);
        }

        private async Task<CloudBlobContainer> GetContainer()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(configuration.ConnectionString);
            CloudBlobClient serviceClient = account.CreateCloudBlobClient();
            var container = serviceClient.GetContainerReference("reactive-machine-results");
            await container.CreateIfNotExistsAsync();
            return container;
        }

        public async Task<string> WriteParameters(string deploymentId)
        {
            var container = await GetContainer();

            // Serialize results.
            object content = new BlobFormat()
                {
                    deploymentId = deploymentId,
                    configurations = configurations
                };
            var serialized = JsonConvert.SerializeObject(content, Formatting.Indented);

            // write a blob to the container
            CloudBlobDirectory dir = container.GetDirectoryReference(deploymentId);
            CloudBlockBlob blob = dir.GetBlockBlobReference("parameters.json");
            await blob.UploadTextAsync(serialized);

            return dir.Uri.ToString();
        }


        public async Task WriteEvents(string deploymentId, DateTime deploymentTimestamp)
        {
            if (!(configuration.CollectApplicationEvents || configuration.CollectHostEvents))
                return;

            var container = await GetContainer();

            var eventsToWrite = configuration.CollectApplicationEvents ? TelemetryEvents.ToArray()
                : TelemetryEvents.Where(e => e.opType == OperationType.Host).ToArray();

            var timeoffset = (starttime - deploymentTimestamp).TotalMilliseconds;
            for (int i = 0; i < eventsToWrite.Length; i++)
            {
                eventsToWrite[i].time += timeoffset;
            }                                 

            // Serialize results.
            object content = new EventsBlobFormat()
                {
                    deploymentId = deploymentId,
                    processId = processId,
                    configurations = configurations,
                    Events = eventsToWrite
                };
            var serialized = JsonConvert.SerializeObject(content, Formatting.Indented);

            // write a blob to the container
            String blobReference = $"{deploymentId}/events/{processId:D3}-{Guid.NewGuid()}.json";
            CloudBlockBlob blob = container.GetBlockBlobReference(blobReference);
            await blob.UploadTextAsync(serialized);
        }

        public async Task WriteThroughput(string deploymentId, DateTime deploymentTimestamp)
        {
            if (!configuration.CollectThroughput)
                return;

            var container = await GetContainer();

            var counters = new SortedDictionary<int, SortedDictionary<string, int>>();

            var timeoffset = (starttime - deploymentTimestamp).TotalMilliseconds;

            foreach (var x in TelemetryEvents)
                if (x.opSide == OperationSide.Caller)
                {
                    var second = (int)Math.Floor(x.time + timeoffset) / 1000;
                    if (!counters.TryGetValue(second, out var countermap))
                        counters[second] = countermap = new SortedDictionary<string, int>();
                    var eventname = x.name;
                    if (!countermap.TryGetValue(eventname, out var count))
                        count = 0;
                    countermap[x.name] = count + 1;
                }

            var events = new List<ThroughputEvent>();
            foreach (var kvp1 in counters)
                foreach (var kvp2 in kvp1.Value)
                    events.Add(new ThroughputEvent()
                    {
                        time = kvp1.Key,
                        name = kvp2.Key,
                        count = kvp2.Value
                    });

            // Serialize results.
            object content = new ThroughputBlobFormat()
            {
                deploymentId = deploymentId,
                processId = processId,
                configurations = configurations,
                events = events
            };
            var serialized = JsonConvert.SerializeObject(content, Formatting.Indented);

            // write a blob to the container
            String blobReference = $"{deploymentId}/throughput/{processId:D3}-{Guid.NewGuid()}.json";
            CloudBlockBlob blob = container.GetBlockBlobReference(blobReference);
            await blob.UploadTextAsync(serialized);
        }

    }
}
