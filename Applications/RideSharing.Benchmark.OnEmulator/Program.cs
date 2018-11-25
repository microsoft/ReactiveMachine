// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveMachine;
using RideSharing.Benchmark;

namespace RideSharing.Benchmark.OnEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
  
            var applicationConfiguration = new RideSharing.Benchmark.Configuration()
            {
                NumberGeneratorProcesses = 1,
                NumberRiders = 10,
                NumberDrivers = 10,
                NumberServiceProcesses = 1,
                Duration = TimeSpan.FromSeconds(10),
                Cooldown = TimeSpan.FromSeconds(5),
                Rate = 0.333333333333,
            };

            var hostConfiguration = new EmulatorHost.Configuration()
            {
                RoundTripMessages = true,
                RoundTripProcessStateEvery = int.MaxValue,
                DeliverStaleExternalsOneOutOf = 10000
            };

            Console.WriteLine("Building Application...");

            var application = new ApplicationCompiler()
              .SetConfiguration(applicationConfiguration)
              .AddService<RideSharingBenchmark>()
              .Compile(applicationConfiguration.NumberServiceProcesses + applicationConfiguration.NumberGeneratorProcesses);

            var experimentAndHost = "ridesharing/emulator/";

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