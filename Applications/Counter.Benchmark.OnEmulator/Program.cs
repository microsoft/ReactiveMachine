// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Counter.Benchmark;
using Microsoft.Extensions.Logging;

namespace Counter.Benchmark.OnEmulator
{
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Building Application...");

            var appConfig = new CounterBenchmarkConfiguration()
            {
                NumberGeneratorProcesses = 4,
                NumberGenerators = 4,
                NumberCounterProcesses = 4,
                NumberCounters = 4,
                Duration = TimeSpan.FromSeconds(10),
                Cooldown = TimeSpan.FromSeconds(5),
                Rate = 4,
                Implementation = CounterImplementation.UpdateBased,
            };

            //var appConfig = new CounterBenchmarkConfiguration()
            //{
            //    NumberGeneratorProcesses = 2,
            //    NumberGenerators = 1000,
            //    NumberCounterProcesses = 2,
            //    NumberCounters = 1000,
            //    NumberRequests = 5000,
            //    Stagger = TimeSpan.FromSeconds(1),
            //    Implementation = CounterImplementation.UpdateBased,
            //};

            //var appConfig = new CounterBenchmarkConfiguration()
            //{
            //    NumberGeneratorProcesses = 1,
            //    NumberGenerators = 10000,
            //    NumberCounterProcesses = 1,
            //    NumberCounters = 1,
            //    NumberRequests = 200000,
            //    Stagger = TimeSpan.FromSeconds(1),
            //    Implementation = CounterImplementation.UpdateBased,
            //};

            var hostConfig = new EmulatorHost.Configuration()
            {
                MultiThreaded = true,

                RoundTripMessages = true,
                RoundTripProcessStateEvery = int.MaxValue,
                DeliverStaleExternalsOneOutOf = 1,

                ConsoleLogLevel = LogLevel.Information,
                FileLogLevel = Debugger.IsAttached ? LogLevel.Trace : LogLevel.None,
                LocalLogDirectory = "C:\\logs\\",

                ApplicationLogLevel = LogLevel.Trace, // log through runtime
                HostLogLevel = LogLevel.Trace,
                RuntimeLogLevel = LogLevel.Trace
            };

            var telemetryConfig = new TelemetryBlobWriter.Configuration()
            {
                CollectHostEvents = false,
                CollectApplicationEvents = (System.Diagnostics.Debugger.IsAttached || appConfig.IsFixedRateExperiment),
                CollectThroughput = (System.Diagnostics.Debugger.IsAttached || appConfig.IsLoadLoopsExperiment),
            };


            var application = new ApplicationCompiler()
              .SetConfiguration(appConfig)
              .SetConfiguration(telemetryConfig)
              .SetConfiguration(hostConfig)
              .AddService<CounterBenchmarkService>()
              .Compile(appConfig.NumberCounterProcesses + appConfig.NumberGeneratorProcesses);


            var experimentAndHost = "counter/emulator/";

            var localOrCloudDeployment =
                Environment.GetEnvironmentVariable("REACTIVE_MACHINE_DIR") == null ? "cloud" : "local";

            var deploymentTimestamp = DateTime.UtcNow;

            var deploymentId = string.Format("{1}{2}/{0:o}", deploymentTimestamp, experimentAndHost, localOrCloudDeployment);

            var emulator = new EmulatorHost.Emulator(deploymentId, deploymentTimestamp);

            Console.WriteLine("Starting Host...");
            emulator.Run(application);

            Console.WriteLine("Done (hit enter to terminate)...");
            Console.ReadLine();

            emulator.Shutdown();
        }
    }
}
