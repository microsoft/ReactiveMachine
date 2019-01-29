// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Bank.Service;
using Bank.Tests;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Diagnostics;

namespace Bank.OnEmulator
{
    class Program 
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || !uint.TryParse(args[0], out var numNodes))
                numNodes = 4;

            var configuration = new EmulatorHost.Configuration()
            {
                MultiThreaded = true,
                ApplicationLogLevel = LogLevel.Trace,
                RuntimeLogLevel = LogLevel.Trace,
                FileLogLevel = LogLevel.Trace,
                LocalLogDirectory = "C:\\logs\\",
            };
            var loggingConfig = new ReactiveMachine.LoggingConfiguration()
            {
                //SendLogLevel = LogLevel.Trace,
                //LockLogLevel = LogLevel.Trace
            };

            Console.WriteLine("Building Application...");
            var compiler = new ApplicationCompiler();
            compiler.AddService<BankTestsService>();
            compiler.AddBuildStep(sb => sb
                .SetConfiguration(configuration)
                .SetConfiguration(loggingConfig));
            var compiled = compiler.Compile(numNodes);

            Console.WriteLine("Building Host...");

            var deploymentTimestamp = DateTime.UtcNow;
            var emulator = new EmulatorHost.Emulator($"bank/emulator/{deploymentTimestamp:o}", deploymentTimestamp);

            Console.WriteLine("Starting Host...");
            emulator.Run(compiled);

            Console.WriteLine("Done (hit enter to terminate)...");
            Console.ReadLine();

            emulator.Shutdown();
        }
    }
}
