// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Counter;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using ReactiveMachine.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new EmulatorHost.Configuration()
            {
                MultiThreaded = true,
                RoundTripMessages = true,
                RoundTripProcessStateEvery = int.MaxValue,
                DeliverStaleExternalsOneOutOf = 1,
                ApplicationLogLevel = LogLevel.Trace,
                RuntimeLogLevel = LogLevel.Trace,
                ConsoleLogLevel = LogLevel.Trace,
                FileLogLevel = LogLevel.Trace,
            };

            var loggingConfig = new ReactiveMachine.LoggingConfiguration()
            {
                SendLogLevel = LogLevel.Trace,
                LockLogLevel = LogLevel.Trace
            };

            Console.WriteLine("Building Application...");

            var application = new ApplicationCompiler()
                                   .AddService<TestsService>()
                                   .SetConfiguration<EmulatorHost.Configuration>(configuration)
                                   .SetConfiguration<ReactiveMachine.LoggingConfiguration>(loggingConfig)
                                   .Compile(5);

            Console.WriteLine("Building Host...");
            var deploymentTimestamp = DateTime.UtcNow;
            var emulator = new EmulatorHost.Emulator($"localtests/{deploymentTimestamp:o}", deploymentTimestamp);

            Console.WriteLine($"Starting Test {deploymentTimestamp}...");
            emulator.Run(application);

            Console.WriteLine("Done (hit enter to terminate)...");
            Console.ReadLine();

            emulator.Shutdown();
        }
    }

    
}
  