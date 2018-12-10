// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ReactiveMachine.TelemetryBlobWriter
{
    public static class TaskoMeterLauncher
    {
        public static void Launch(Configuration configuration, string deploymentId)
        {
            var taskoMeter = new Process();
            taskoMeter.StartInfo.Arguments = $"{configuration.ConnectionString} {configuration.BlobContainer} {deploymentId}/events/";

            var executable = Path.Combine(
                Environment.GetEnvironmentVariable("REACTIVE_MACHINE_DIR"),
                "Telemetry",
                "TaskoMeter",
#if DEBUG
                "Debug",
#else
                "Release",
#endif
                "ReactiveMachine.Tools.Taskometer.exe");

            taskoMeter.StartInfo.FileName = executable;
            taskoMeter.StartInfo.UseShellExecute = false;
            taskoMeter.Start();

        }

        public static void Launch(ICompiledApplication application, string deploymentId)
        {
            if (application.TryGetConfiguration<Configuration>(out var config))
            {
                Launch(config, deploymentId);
            }
        }
    }
}
