// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Miner.Service;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miner.Test.OnEmulator
{

    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Building Application...");
            var compiler = new ApplicationCompiler();

            uint numberProcesses = 5;
            uint workersPerProcess = 2;

            compiler
                .AddService<MinerService>()
                .AddBuildStep(serviceBuilder => serviceBuilder.OnFirstStart(
                    new SearchJob()
                    {
                        Target = 100007394059441.GetHashCode(),
                        Start = 100000000000000,
                        Count = 10000000000,
                        NumberWorkers = numberProcesses * workersPerProcess
                    }))
                ;

            var application = compiler.Compile(numberProcesses);

            Console.WriteLine("Building Host...");
            var deploymentTimestamp = DateTime.UtcNow;
            var emulator = new EmulatorHost.Emulator($"miner/emulator/{deploymentTimestamp:o}", deploymentTimestamp);

            Console.WriteLine("Starting Host...");
            emulator.Run(application);

            Console.WriteLine("Done (hit enter to terminate)...");
            Console.ReadLine();

            emulator.Shutdown();
        }
    }
}
