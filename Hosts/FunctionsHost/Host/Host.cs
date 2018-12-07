// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using ReactiveMachine.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ReactiveMachine.TelemetryBlobWriter;

namespace FunctionsHost
{
    internal class Host
    {
        private readonly IStaticApplicationInfo applicationInfo;
        private readonly FunctionsHostConfiguration configuration;
        public readonly EventHubsConnections Connections;

        private readonly uint processId;
        private readonly string hostName;
        private long ourClock;
        private VectorClock seenClock;
        private DateTime deploymentTimestamp;
        private string deploymentId;
        private long lastCheckpointedSequenceNumber = -1;

        private readonly ICompiledApplication application;

        private TelemetryCollector blobTelemetryListener;
        private readonly Stopwatch stopwatch;
        private readonly Guid invocationId;
        private bool collectHostEvents;

        public CombinedLogger CombinedLogger;
        public ILogger HostLogger;
        public ILogger ApplicationLogger;
        public ILogger RuntimeLogger;

        private LoopbackSender loopbackSender;
        private readonly Dictionary<uint, RemoteSender> remoteSenders = new Dictionary<uint, RemoteSender>();

        private readonly DataContractSerializer payloadSerializer;
        private readonly DataContractSerializer payloadSerializerLoopback;

        public Host(IStaticApplicationInfo applicationInfo, FunctionsHostConfiguration configuration, ILogger logger, uint processId, Stopwatch stopwatch, Guid invocationId)
        {
            this.processId = processId;
            this.stopwatch = stopwatch;
            this.applicationInfo = applicationInfo;
            this.configuration = configuration;
            this.hostName = Environment.MachineName;
            bool cloud = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REACTIVE_MACHINE_DIR"));

            CombinedLogger = new CombinedLogger(logger, configuration, processId, cloud);
            HostLogger = new LoggerWrapper(CombinedLogger, $"[p{processId:d3} host] ", configuration.HostLogLevel);
            ApplicationLogger = new LoggerWrapper(CombinedLogger, $"[p{processId:d3} application] ", configuration.ApplicationLogLevel);
            RuntimeLogger = new LoggerWrapper(CombinedLogger, $"[p{processId:d3} runtime] ", configuration.RuntimeLogLevel);

            this.application = applicationInfo.Build(new ReactiveMachine.ApplicationCompiler().SetConfiguration(configuration));

            this.invocationId = invocationId;
            this.payloadSerializer = new DataContractSerializer(typeof(List<KeyValuePair<long,IMessage>>), application.SerializableTypes);
            this.payloadSerializerLoopback = new DataContractSerializer(typeof(List<IMessage>), application.SerializableTypes);
            this.Connections = new EventHubsConnections(processId, HostLogger, configuration.ehConnectionString);

            if (application.TryGetConfiguration<ReactiveMachine.TelemetryBlobWriter.Configuration>(out var config))
            {
                this.collectHostEvents = config.CollectHostEvents;
                this.blobTelemetryListener = new TelemetryCollector(config, application, processId, this.GetType());
            }
        }

        public void SetDeploymentTimestamp(DateTime deploymentTimestamp)
        {
            this.deploymentTimestamp = deploymentTimestamp;
            this.deploymentId = applicationInfo.GetDeploymentId(deploymentTimestamp);
        }

        public uint NumberProcesses => application.NumberProcesses;
      
    
        public async Task<long> GetLastEnqueuedSequenceNumber(uint processId)
        {
            var client = Connections.GetEventHubClient(processId);

            var partitionInfo = await client.GetPartitionRuntimeInformationAsync((processId / 8).ToString());

            return partitionInfo.LastEnqueuedSequenceNumber;
        }
     
        public void Send(uint processId, IMessage message)
        {
            if (processId == this.processId)
            {
                var sender = GetLoopbackSender();
                sender.Add(message);
            }
            else
            {
                var sender = GetRemoteSender(processId);
                sender.Add(new KeyValuePair<long,IMessage>(++ourClock, message));
            }
        }

        public LoopbackSender GetLoopbackSender()
        {
            lock (remoteSenders)
            {
                if (loopbackSender == null)
                {
                    loopbackSender =  new LoopbackSender(processId, Connections, HostLogger, payloadSerializerLoopback, configuration, deploymentTimestamp);
                }
                return loopbackSender;
            }
        }

        public RemoteSender GetRemoteSender(uint destination)
        {
            lock (remoteSenders)
            {
                if (!remoteSenders.TryGetValue(destination, out var sender))
                {
                    sender = remoteSenders[destination] = new RemoteSender(processId, destination, Connections, HostLogger, payloadSerializer, configuration, deploymentTimestamp);
                }
                return sender;
            }
        }

        public async Task<bool> ResumeFromCheckpoint(LeaseManager leaseManager)
        {
            var start = stopwatch.Elapsed;

            try
            {
                // kick off the load
                var storageConnectionString = configuration.StorageConnectionString;
                var loadtask = AzureBlobStorageStateManager.Load(storageConnectionString, HostLogger, processId);

                // register host services
                application.HostServices.RegisterSend(Send);
                application.HostServices.RegisterApplicationLogger(ApplicationLogger);
                application.HostServices.RegisterRuntimeLogger(RuntimeLogger);

                //          if (_configuration.AppInsightsInstrumentationKey != null)
                //                _application.HostServices.RegisterTelemetryListener(new ApplicationInsightsTelemetryListener(_configuration.AppInsightsInstrumentationKey, "eventhubs"));

                if (blobTelemetryListener != null)
                {
                    application.HostServices.RegisterTelemetryListener(blobTelemetryListener);
                }

                // read the checkpoint
                var checkpointedState = await loadtask;

                SetDeploymentTimestamp(checkpointedState.DeploymentTimestamp);
                seenClock = checkpointedState.SeenClock;
                ourClock = checkpointedState.OurClock;
                var state = checkpointedState.State;
                lastCheckpointedSequenceNumber = checkpointedState.Version;

                HostLogger.LogDebug($"Resuming at position {lastCheckpointedSequenceNumber}");

                Connections.ResumeFrom(lastCheckpointedSequenceNumber);

                // build the process

                IProcess process = application.MakeProcess(processId);

                if (state == null)
                    process.FirstStart();
                else
                    process.Restore(state, out var label);

                process.BecomePrimary();

                // start the message receiving loop

                long lastProcessedPosition = 0;

                int iterationCount = 0;
                int dedupCount = 0;
                int receiveCount = 0;
                int loopbackCount = 0;
                int clientCount = 0;

                TimeSpan lastReport = stopwatch.Elapsed;
                long lastReportedPosition = 0;

                while (true)
                {
                    iterationCount++;


                    if (lastProcessedPosition > lastReportedPosition && stopwatch.Elapsed - lastReport > TimeSpan.FromSeconds(15))
                    {
                        HostLogger.LogInformation($"progress to v{lastProcessedPosition} after {stopwatch.Elapsed.TotalSeconds:f2}s, {receiveCount + dedupCount + loopbackCount + clientCount} messages ({receiveCount} new, {loopbackCount} loopback, {clientCount} client, {dedupCount} deduped) in {iterationCount} batches");
                        lastReportedPosition = lastProcessedPosition;
                        lastReport = stopwatch.Elapsed;
                        CombinedLogger.Flush();
                    }

                    try
                    {
                        bool outOfTime = stopwatch.Elapsed > configuration.TimeLimit;

                        IEnumerable<EventData> eventData = outOfTime ? null :
                              await Connections.Receiver.ReceiveAsync(configuration.MaxReceiveBatchSize, iterationCount == 1 ? TimeSpan.FromSeconds(10) : configuration.ReceiveWaitTime);

                        if (eventData == null)
                        {
                            HostLogger.LogTrace($"{ DateTime.UtcNow:o} Received nothing. {Connections.Receiver.RuntimeInfo.LastSequenceNumber}");
                            
                            if (process.RequestsOutstanding() && !outOfTime)
                            {
                                HostLogger.LogDebug($"continue for outstanding requests.");
                                CombinedLogger.Flush();
                                continue;
                            }
                            else if (lastProcessedPosition > lastCheckpointedSequenceNumber)
                            {
                                await Task.WhenAll(Senders.Select(s => s.WaitForCurrentWorkToBeServiced()).ToList());
                                lastCheckpointedSequenceNumber = lastProcessedPosition;
                                await Save(storageConnectionString, process, leaseManager);
                                HostLogger.LogDebug($"continue for saving snapshot.");
                                continue;
                            }
                            else
                            {
                                // no more work to do here
                                HostLogger.LogInformation($"{(outOfTime ? "out of time" : "done")} after {stopwatch.Elapsed.TotalSeconds}s, {receiveCount + dedupCount + loopbackCount + clientCount} messages ({receiveCount} new, {loopbackCount} loopback, {clientCount} client, {dedupCount} deduped) in {iterationCount} batches");
                                CombinedLogger.Flush();
                                return !outOfTime;
                            }
                        }

                        foreach (var ed in eventData)
                        {
                            var body = ed.Body;
                            var message = ProcessMessage.Deserialize(body.Array);

                            HostLogger.LogTrace($"{DateTime.UtcNow:o} Received {message}");

                            if (!message.IsExternalRequest && message.DeploymentTimestamp < deploymentTimestamp)
                            {
                                HostLogger.LogDebug($"Ignoring message from earlier deployment {message.DeploymentTimestamp}");
                            }
                            else if (!message.IsExternalRequest && message.DeploymentTimestamp > deploymentTimestamp)
                            {
                                HostLogger.LogError($"****** MISSING STATE ERROR process state is from older deployment, should have been initialized for new one! {message.DeploymentTimestamp} != {deploymentTimestamp}");
                            }
                            else if (message.IsSequenced
                                 && seenClock.HasSeen(message.Source, message.LastClock.Value))
                            {
                                dedupCount++;
                                HostLogger.LogTrace($"Deduping: {message}");
                            }
                            else if (message.IsExternalRequest)
                            {
                                clientCount++;
                                HostLogger.LogTrace($"Processing: {message}");

                                // deserialize content
                                List<IMessage> payload;
                                MemoryStream stream = new MemoryStream(message.Payload);
                                using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                                {
                                    payload = (List<IMessage>)payloadSerializerLoopback.ReadObject(binaryDictionaryReader);
                                }

                                // Iterate 
                                foreach (var m in payload)
                                    process.ProcessMessage(m);
                            }
                            else if (message.IsLoopback)
                            {
                                loopbackCount++;
                                HostLogger.LogTrace($"Processing: {message}");

                                // deserialize content
                                List<IMessage> payload;
                                MemoryStream stream = new MemoryStream(message.Payload);
                                using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                                {
                                    payload = (List<IMessage>)payloadSerializerLoopback.ReadObject(binaryDictionaryReader);
                                }

                                // Iterate 
                                foreach (var m in payload)
                                    process.ProcessMessage(m);
                            }
                            else
                            {
                                receiveCount++;
                                HostLogger.LogTrace($"Processing: {message}");

                                // deserialize content
                                List<KeyValuePair<long, IMessage>> payload;
                                MemoryStream stream = new MemoryStream(message.Payload);
                                using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                                {
                                    payload = (List<KeyValuePair<long, IMessage>>)payloadSerializer.ReadObject(binaryDictionaryReader);
                                }

                                // Iterate
                                foreach (var kvp in payload)
                                    if (!seenClock.HasSeen(message.Source, kvp.Key))
                                        process.ProcessMessage(kvp.Value);

                                // Update seen clock.
                                seenClock.Set(message.Source, message.LastClock.Value);
                            }

                            lastProcessedPosition = ed.SystemProperties.SequenceNumber;

                            // Checkpoint and commit every X.
                            if (lastProcessedPosition > lastCheckpointedSequenceNumber
                                && lastProcessedPosition % configuration.CheckpointInterval == configuration.CheckpointInterval - 1)
                            {
                                await Task.WhenAll(Senders.Select(s => s.WaitForCurrentWorkToBeServiced()).ToList());

                                // Checkpoint state. 
                                lastCheckpointedSequenceNumber = lastProcessedPosition;
                                await Save(storageConnectionString, process, leaseManager);
                            }
                        }

                    }
                    catch (System.OperationCanceledException)
                    {
                        HostLogger.LogDebug($"receive returned OperationCanceledException, scheduling retry!");
                        continue;
                    }
                }
            }
            finally
            {
                if (this.collectHostEvents)
                {
                    this.blobTelemetryListener.OnApplicationEvent(processId, invocationId.ToString(), $"Process {processId}", "", OperationSide.Caller, OperationType.Host, (stopwatch.Elapsed - start).TotalMilliseconds);
                }
            }
        }

        private IEnumerable<IBatchSender> Senders {
            get
            {
                if (loopbackSender != null)
                    yield return loopbackSender;
                foreach (var kvp in remoteSenders)
                    yield return kvp.Value;
            }
        }

        public async Task FinalRecheck()
        {
            var eventData = await Connections.Receiver.ReceiveAsync(1, TimeSpan.FromMilliseconds(1));
            if (eventData != null)
            {
                var eventEnumerator = eventData.GetEnumerator();
                if (eventEnumerator.MoveNext())
                {
                    // there was a race... we missed a message 
                    var msg = ProcessMessage.Deserialize(eventEnumerator.Current.Body.Array);

                    // send a ping to ourselves before shutting down
                    // so we will wake up and resume
                    HostLogger.LogDebug($"{DateTime.UtcNow:o} detected a new message {msg}->{processId} at position {eventEnumerator.Current.SystemProperties.SequenceNumber} after already releasing lease - ring my own doorbell");
                    await RingMyself();
                }
            }
        }

        public async Task RingMyself()
        {
            var message = new DoorbellMessage()
            {
                ProcessId = processId,
                Guid = Guid.NewGuid()
            };
            var messageBytes = DoorbellMessage.Serialize(message);
            var sender = Connections.GetDoorbellSender(processId);
            await sender.SendAsync(new EventData(messageBytes));
        }

        public async Task Cleanup(bool isBootstrap)
        {
            await Task.WhenAll(Senders.Select(s => s.WaitForCurrentWorkToBeServiced()).ToList());

            await Connections.Close();

            if(collectHostEvents)
                blobTelemetryListener.OnApplicationEvent(processId, invocationId.ToString(), $"Host {hostName}", "", OperationSide.Caller, OperationType.Host, stopwatch.Elapsed.TotalMilliseconds);

            var blobname = await blobTelemetryListener.PushTelemetry(deploymentId, deploymentTimestamp, isBootstrap);

            if (blobname != null)
                HostLogger.LogInformation($"telemetry written to {blobname}");

            CombinedLogger.Dispose();
        }

        public async Task Save(String storageConnectionString, IProcess process, LeaseManager leaseManager)
        {
            process.SaveState(out var serialized, out var label);

            var cloudBlobContainer = await AzureBlobStorageStateManager.GetCloudBlobContainer(storageConnectionString, HostLogger, initialize:false);

            await AzureBlobStorageStateManager
                .Save(cloudBlobContainer, deploymentId, HostLogger, processId,
                    new ProcessState(deploymentTimestamp, lastCheckpointedSequenceNumber, serialized, seenClock, ourClock), leaseManager.LeaseId);
        }
    }
}