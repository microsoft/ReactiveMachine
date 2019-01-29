// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public class LoggingConfiguration
    {
        /// <summary>
        /// the log level of application events to include in runtime log
        /// </summary>
        public LogLevel ApplicationEvents { get; set; } = LogLevel.None;

        /// <summary>
        /// the log level used for snapshot save and restore
        /// </summary>
        public LogLevel SnapshotsLogLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// the log level used for recording and replaying orchestration states
        /// </summary>
        public LogLevel RecordReplayLogLevel { get; set; } = LogLevel.None;

        /// <summary>
        /// the log level used for sending messages
        /// </summary>
        public LogLevel SendLogLevel { get; set; } = LogLevel.None;

        /// <summary>
        /// the log level used for receiving messages
        /// </summary>
        public LogLevel ReceiveLogLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        /// the log level used for activities
        /// </summary>
        public LogLevel ActivitiesLogLevel { get; set; } = LogLevel.Trace;

         /// <summary>
        /// the log level used for information on locks
        /// </summary>
        public LogLevel LockLogLevel { get; set; } = LogLevel.None;

       /// <summary>
        /// the log level used for information on progress
        /// </summary>
        public LogLevel ProgressLogLevel { get; set; } = LogLevel.Debug;
    }
}
