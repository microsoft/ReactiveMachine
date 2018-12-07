// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using ReactiveMachine.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace EmulatorHost
{
    public class Emulator : ILogger
    {
        public Emulator(string deploymentId, DateTime deploymentTimestamp)
        {
            this.deploymentId = deploymentId;
            this.deploymentTimestamp = deploymentTimestamp;
        }
        private readonly string deploymentId;
        private readonly DateTime deploymentTimestamp;
        private Configuration configuration;
        private StreamWriter streamWriter;
        private ILogger runtimeLogger;
        private ILogger applicationLogger;
        private ILogger hostLogger;

        public void Run(ICompiledApplication application)
        {
            if (application.Configurations.TryGetValue(typeof(Configuration), out var c))
            {
                configuration = (Configuration)c;
            }
            else
            {
                configuration = new Configuration();
            }

            if (configuration.FileLogLevel != LogLevel.None)
            {
                streamWriter = new StreamWriter(Path.Combine(configuration.LocalLogDirectory, "emulatorhost.log"));
            }

            hostLogger = new LoggerWrapper(this, "[host] ", configuration.HostLogLevel);
            applicationLogger = new LoggerWrapper(this, "[application] ", configuration.ApplicationLogLevel);
            runtimeLogger = new LoggerWrapper(this, "[runtime] ", configuration.RuntimeLogLevel);

            application.HostServices.RegisterRuntimeLogger(runtimeLogger);
            application.HostServices.RegisterApplicationLogger(applicationLogger);

            if (!configuration.MultiThreaded)
            {
                new SingleThreadSimulation(configuration, application, deploymentId, deploymentTimestamp, hostLogger).Run(application);
            }
            else
            {
                new MultiThreadedSimulation(configuration, application, deploymentId, deploymentTimestamp, hostLogger).Run(application);
            }

            if (Debugger.IsAttached)
                ReactiveMachine.TelemetryBlobWriter.TaskoMeterLauncher.Launch(application, deploymentId);

            if (streamWriter != null)
            {
                var x = streamWriter;
                streamWriter = null;
                x.Close();
            }
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string s = formatter(state, exception);
            if (logLevel >= configuration.ConsoleLogLevel)
                Console.WriteLine(s);
            if (streamWriter != null)
                lock (streamWriter)
                    streamWriter.WriteLine(s);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return logLevel >= configuration.ConsoleLogLevel || logLevel >= configuration.FileLogLevel;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return null;
        }

        public void Shutdown()
        {
            Environment.Exit(0);
        }
    }
}
