// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReactiveMachine.Tools.Taskometer
{
    static class Program
    {        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var connectionString = args.Length > 0 ? args[0] : System.Environment.GetEnvironmentVariable("REACTIVE_MACHINE_TELEMETRY");
            var containerName = args.Length > 1 ? args[1] : "reactive-machine-results";
            var folderName = args.Length > 2 ? args[2] : "folderName";

            if (args.Length > 2)
               TaskGroup.ReadFromFile(connectionString, containerName, folderName, null);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Stats(connectionString, containerName, folderName));
        }
    }
}
