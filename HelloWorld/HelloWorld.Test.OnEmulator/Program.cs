// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.


using HelloWorld.Test;
using ReactiveMachine;
using System;

namespace HelloWorld.Emulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var compiler = new ApplicationCompiler();

            Console.WriteLine("Setting configuration objects...");

            compiler.SetConfiguration(new HelloWorldTestConfiguration()
            {
                NumberRepetitions = 100
            });
            compiler.SetConfiguration(new ReactiveMachine.TelemetryBlobWriter.Configuration()
            {
                CollectHostEvents = true,
                CollectApplicationEvents = true,
                CollectThroughput = false,
            });

            Console.WriteLine("Building Application...");

            compiler.AddService<HelloWorldTestService>();

            Console.WriteLine("Compiling Application...");

            var compiledApplication = compiler.Compile(numberProcesses: 1);

            Console.WriteLine("Building Host...");

            var deploymentTimestamp = DateTime.UtcNow;
            var deploymentId = $"helloworld/emulator/{deploymentTimestamp:o}";

            var emulator = new EmulatorHost.Emulator(deploymentId, deploymentTimestamp);

            Console.WriteLine("Starting Host...");

            emulator.Run(compiledApplication);

            Console.WriteLine("Done (hit enter to terminate)...");
            Console.ReadLine();

            emulator.Shutdown();
        }
    }
}
