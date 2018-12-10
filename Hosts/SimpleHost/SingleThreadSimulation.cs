// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace EmulatorHost
{
    internal class SingleThreadSimulation
    {
        private class ProcessInfo
        {
            public List<IMessage> Inbox;
            public IProcess Process;
        }

        ProcessInfo[] processes;
        private readonly string deploymentId;
        private readonly DateTime deploymentTimestamp;
        private readonly Configuration configuration;
        private readonly ICompiledApplication application;
        private ILogger logger;

        public SingleThreadSimulation(Configuration configuration, ICompiledApplication application, string deploymentId, DateTime deploymentTimestamp, ILogger logger)
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
                processes[i].Process = application.MakeProcess(i);
                processes[i].Process.FirstStart();
                processes[i].Process.BecomePrimary();
            }

            Console.WriteLine($"=========================== START SIMULATION ===========================");
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            while (processes[0].Process.RequestsOutstanding())
            {
                for (uint i = 0; i < application.NumberProcesses; i++)
                {
                    var info = processes[i];
                    List<IMessage> deliver = empty;
                    lock (sgl)
                    {
                        if (info.Inbox.Count > 0)
                        {
                            deliver = info.Inbox;
                            info.Inbox = new List<IMessage>();
                        }
                    }
                    if (deliver.Count > 0)
                    {
                        foreach (var m in deliver)
                        {
                            //// lose 1/2 of activity responses that originated on older instances
                            //if (configuration.DeliverStaleExternalsOneOutOf != 1
                            //    && m is ReactiveMachine.Implementation.RespondToActivity rte 
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

            Console.WriteLine($"=========================== END SIMULATION ===========================");
            stopwatch.Stop();
            Console.WriteLine($"elapsed = {stopwatch.Elapsed.TotalSeconds:f2}s  #messages={messageCount}");

            telemetry?.Shutdown().Wait();

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
            lock (sgl)
            {
                messageCount++;
                destinationprocess.Inbox.Add(message);
            }
        }

        private object sgl = new object();
        private int messageCount;

        public void HandleGlobalException(Exception e)
        {
            System.Diagnostics.Debugger.Break();

            Console.WriteLine($"!!! Unhandled Exception: {e}");
        }
    }
}
