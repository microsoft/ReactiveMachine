// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;

namespace EmulatorHost
{
    internal class MultiThreadedSimulation
    {
        private class ProcessInfo
        {
            public List<IMessage> Inbox;
            public IProcess Process;
            public long Received;
        }

        ProcessInfo[] processes;
        private readonly string deploymentId;
        private readonly DateTime deploymentTimestamp;
        private readonly Configuration configuration;
        private readonly ICompiledApplication application;
        private ILogger logger;

        public MultiThreadedSimulation(Configuration configuration, ICompiledApplication application, string deploymentId, DateTime deploymentTimestamp, ILogger logger)
        {
            this.configuration = configuration;
            this.application = application;
            this.deploymentId = deploymentId;
            this.deploymentTimestamp = deploymentTimestamp;
            this.logger = logger;
        }

        DataContractSerializer _serializer;

        static List<IMessage> empty = new List<IMessage>();

        private Random random = new Random(0);

        private CancellationTokenSource shutdown = new CancellationTokenSource(); 

        public void Run(ICompiledApplication application)
        {
            TelemetryListenerArray telemetry = null;
            if (application.Configurations.TryGetValue(typeof(ReactiveMachine.TelemetryBlobWriter.Configuration), out var c))
            {
                telemetry = new TelemetryListenerArray((ReactiveMachine.TelemetryBlobWriter.Configuration)c, application, this.GetType(), deploymentId, deploymentTimestamp);
                application.HostServices.RegisterTelemetryListener(telemetry);
            }

            application.HostServices.RegisterSend(Send);
            application.HostServices.RegisterGlobalExceptionHandler(HandleGlobalException);
            application.HostServices.RegisterGlobalShutdown(() => shutdown.Cancel());
            _serializer = new DataContractSerializer(typeof(IMessage), application.SerializableTypes);


            processes = new ProcessInfo[application.NumberProcesses];
            for (uint i = 0; i < application.NumberProcesses; i++)
            {
                processes[i] = new ProcessInfo()
                {
                    Inbox = new List<IMessage>(),
                };
            }
            for (uint i = 0; i < application.NumberProcesses; i++)
            {
                var info = processes[i];

                logger.LogDebug($"Recovering process {i}.");

                info.Process = application.MakeProcess(i);
                info.Process.FirstStart();
            }
            for (uint i = 0; i < application.NumberProcesses; i++)
            {
                uint processId = i;
                new Thread(() => RunProcess(processId, processes[processId], application)).Start();
            }

            bool someoneBusy = true;

            while (!shutdown.IsCancellationRequested)
            {
                someoneBusy = false;
                for (uint i = 0; i < application.NumberProcesses; i++)
                {
                    var info = processes[i];
                    long received;
                    bool busy;
                    lock (info)
                    {
                        received = info.Received;
                        busy = (info.Inbox.Count > 0) || (info.Process?.RequestsOutstanding() ?? true);
                    }
                    someoneBusy = busy || someoneBusy;
                    logger.LogInformation($"Process {i}: Received={received:D12} busy={busy}");
                }
                if (!someoneBusy)
                    break;

                shutdown.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
            }

            telemetry?.Shutdown().Wait();
        }

        private void RunProcess(uint processId, ProcessInfo info, ICompiledApplication application)
        {
            logger.LogInformation($"Starting process {processId}.");

            info.Process.BecomePrimary();

            while (true)
            {
                List<IMessage> deliver = empty;
                lock (info)
                {
                    if (info.Inbox.Count > 0)
                    {
                        deliver = info.Inbox;
                        info.Received += deliver.Count;
                        info.Inbox = new List<IMessage>();
                    }
                }
                if (deliver.Count > 0)
                {
                    foreach (var m in deliver)
                    {
                        //// chaos monkey:
                        //// lose 1/2 of activity responses that originated on older instances
                        //if (configuration.DeliverStaleExternalsOneOutOf != 1
                        //    && m.MessageType == ReactiveMachine.Implementation.MessageType.RespondToActivity 
                        //    && rte.InstanceId != info.Process.InstanceId
                        //    && (random.Next(configuration.DeliverStaleExternalsOneOutOf) != 0))
                        //{
                        //    continue;
                        //}

                        info.Process.ProcessMessage(m);

                        if (configuration.RoundTripProcessStateEvery < int.MaxValue
                                && random.Next(configuration.RoundTripProcessStateEvery) == 0) // for debugging process state serialization
                        {
                            info.Process.SaveState(out var bytes, out var label1);
                            info.Process.Restore(bytes, out var label2);
                            info.Process.BecomePrimary();
                        }
                    }
                }
            }
        }

        public void Send(uint processId, IMessage message)
        {
            var destinationprocess = processes[processId];

            if (configuration.RoundTripMessages) // for debugging message serialization
            {
                var stream = new MemoryStream();
                using (var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
                {
                    _serializer.WriteObject(binaryDictionaryWriter, message);
                    stream.Flush();
                }
                var bytes = stream.ToArray();

                stream = new MemoryStream(bytes);
                using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                {
                    message = (IMessage)_serializer.ReadObject(binaryDictionaryReader);
                }
            }

            // need lock since ephemeral sends can execute on threadpool
            lock (destinationprocess)
            {
                destinationprocess.Inbox.Add(message);
            }
        }


        public void HandleGlobalException(Exception e)
        {
            System.Diagnostics.Debugger.Break();
            logger.LogError($"global exception was unhandled: {e}");       
        }
    }
}
