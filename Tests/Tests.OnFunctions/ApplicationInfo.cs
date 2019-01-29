// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using ReactiveMachine;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Collections.Generic;
using FunctionsHost;

namespace Tests.OnFunctions
{
    public class ApplicationInfo : IStaticApplicationInfo
    {
        public ICompiledApplication Build(IApplicationCompiler compiler)
        {
            var telemetryConfig = new ReactiveMachine.TelemetryBlobWriter.Configuration()
            {
                CollectHostEvents = true,
                CollectApplicationEvents = true,
                CollectThroughput = true,
            };

            compiler
               .SetConfiguration(telemetryConfig)
               .AddService<SimpleLoadTest.Service.SimpleLoadTestService>();

            return compiler.Compile(numberOfProcesses: 1);
        }

        public string GetDeploymentId(DateTime deploymentTimestamp)
        {
            String ExperimentAndHost = "simpleloadtest/functions/";

            String LocalOrCloudDeployment =
                Environment.GetEnvironmentVariable("REACTIVE_MACHINE_DIR") == null ? "cloud" : "local";

            return $"{ExperimentAndHost}{LocalOrCloudDeployment}/{deploymentTimestamp:o}";
        }

        public FunctionsHostConfiguration GetHostConfiguration()
        {
            return new FunctionsHostConfiguration()
            {
                // connection strings
                StorageConnectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                EventHubsConnectionString = System.Environment.GetEnvironmentVariable("EVENTHUBS_CONNECTION_STRING"),
                AppInsightsInstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"),

                // logging sources : specify levels to be generated
                ApplicationLogLevel = LogLevel.Information,
                HostLogLevel = LogLevel.Trace,
                RuntimeLogLevel = LogLevel.Debug,

                // logging destinations: specify levels to be saved 
                FunctionsLoggerLogLevel = LogLevel.Information,
                LocalDevelopmentFileLogLevel = LogLevel.Trace,
                LocalDevelopmentLogDirectory = "C:\\logs\\",

                // parameters for behavior
                CheckpointInterval = 10000,
                ReceiveWaitTime = TimeSpan.FromSeconds(15),
                TimeLimit = TimeSpan.FromMinutes(4.5),
                MaxReceiveBatchSize = 10000,
            };
        }
    }
}