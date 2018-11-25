// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmulatorHost
{
    public class Configuration
    {
        /// <summary>
        /// If true, each application process runs on a separate thread
        /// </summary>
        public bool MultiThreaded = true;

        /// <summary>
        /// If set to true, all messages undergo serialization roundtrip
        /// </summary>
        public bool RoundTripMessages = true;

        /// <summary>
        /// If set to x, process state will do serialization round trip with probability (1/x)
        /// </summary>
        public int RoundTripProcessStateEvery = int.MaxValue;

        /// <summary>
        /// If set to x, stale externals will be lost with a probability (1/x)
        /// </summary>
        public int DeliverStaleExternalsOneOutOf = 2;

        /// <summary>
        /// The log level limit used for the console
        /// </summary>
        public LogLevel ConsoleLogLevel;

        /// <summary>
        /// The log level limit used for the log file
        /// </summary>
        public LogLevel FileLogLevel;

        /// <summary>
        /// Directory path for log file
        /// </summary>
        public string LocalLogDirectory = "";

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
