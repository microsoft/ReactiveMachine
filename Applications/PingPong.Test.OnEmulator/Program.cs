// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using PingPong.Service;
using ReactiveMachine;
using System;

namespace PingPong.Test.OnEmulator
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Building Application...");
            var compiler = new ApplicationCompiler();
            compiler.AddService<PingPongService>();                
            var application = compiler.Compile(2);

            Console.WriteLine("Starting Host...");
            var deploymentTimestamp = DateTime.UtcNow;
            var emulator = new EmulatorHost.Emulator($"pingpong/emulator/{deploymentTimestamp:o}", deploymentTimestamp);
            emulator.Run(application);

            Console.WriteLine("Done (hit enter to terminate)...");
            Console.ReadLine();

            emulator.Shutdown();
        }
    }
}
