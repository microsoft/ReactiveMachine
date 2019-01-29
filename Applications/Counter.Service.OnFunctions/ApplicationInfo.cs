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
using FunctionsHost;
using System.Threading.Tasks;
using Counter.Benchmark;
using ReactiveMachine;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Collections.Generic;

namespace Counter.Service.OnFunctions
{
    public class ApplicationInfo : IStaticApplicationInfo
    {
        public ICompiledApplication Build(IApplicationCompiler compiler)
        {
            var runtimeLoggingConfig = new ReactiveMachine.LoggingConfiguration()
            {
                ReceiveLogLevel = LogLevel.Debug,
                SendLogLevel = LogLevel.Debug,
            };

            var telemetryConfig = new ReactiveMachine.TelemetryBlobWriter.Configuration()
            {
                CollectHostEvents = true,
                CollectApplicationEvents = true,
                CollectThroughput = true,
                //CollectApplicationEvents = (System.Diagnostics.Debugger.IsAttached || applicationConfig.IsFixedRateExperiment),
                //CollectThroughput = (System.Diagnostics.Debugger.IsAttached || applicationConfig.IsLoadLoopsExperiment),
            };

            compiler
               .SetConfiguration(telemetryConfig)
               .SetConfiguration(runtimeLoggingConfig)
               .AddService<CounterService>();

            return compiler.Compile(10);
        }

        public IEnumerable<Type> GetResultTypes()
        {
            yield return typeof(int);
        }

        public string GetDeploymentId(DateTime deploymentTimestamp)
        {
            String ExperimentAndHost = "counter/service/";

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