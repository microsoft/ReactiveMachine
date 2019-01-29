// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReactiveMachine;
using ReactiveMachine.TelemetryBlobWriter;

namespace FunctionsHost
{

    public class FunctionsHostConfiguration
    {

        [JsonIgnore] // we want no connection strings in logs
        public String StorageConnectionString { get; set; }

        [JsonIgnore] // we want no connection strings in logs
        public String EventHubsConnectionString { get; set; }

        [JsonIgnore] // we want no connection strings in logs
        public String AppInsightsInstrumentationKey { get; set; }

        public int CheckpointInterval { get; set; }

        public int MaxReceiveBatchSize { get; set; }

        public TimeSpan ReceiveWaitTime { get; set; }

        public TimeSpan TimeLimit { get; set; }

        /// <summary>
        /// The log level limit used for log file during local development
        /// </summary>
        public LogLevel LocalDevelopmentFileLogLevel;

        /// <summary>
        /// The log level used for logging into the functions-provided logger
        /// </summary>
        public LogLevel FunctionsLoggerLogLevel;


        /// <summary>
        /// Directory path for log file to use during local development
        /// </summary>
        public string LocalDevelopmentLogDirectory = "";

        /// <summary>
        /// An additional log level limit for host
        /// </summary>
        public LogLevel RuntimeLogLevel;

        /// <summary>
        /// An additional log level limit for the reactive machine runtime
        /// </summary>
        public LogLevel HostLogLevel;

        /// <summary>
        /// An additional log level limit for application
        /// </summary>
        public LogLevel ApplicationLogLevel;
    }

}
