// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FunctionsHost
{
    internal class CombinedLogger : ILogger, IDisposable
    {
        private FunctionsHostConfiguration config;
        private ILogger logger;
        private StreamWriter streamWriter;

        public CombinedLogger(ILogger logger, FunctionsHostConfiguration config, uint processId, bool cloud)
        {
            this.logger = logger;
            this.config = config;

            if (!cloud && config.LocalDevelopmentFileLogLevel != LogLevel.None)
            {
                try {
                    streamWriter = new StreamWriter(Path.Combine(config.LocalDevelopmentLogDirectory, $"p{processId:d3}-{DateTime.UtcNow:yyyy-MM-dd--hh-mm-ss}.log"));
                }
                catch(Exception e)
                {
                    logger.LogWarning($"could not create log file in {config.LocalDevelopmentLogDirectory}, caught {e}");
                }
            }
        }

        public void Flush()
        {
            streamWriter?.Flush();
        }

        public void Dispose()
        {
            streamWriter?.Close();
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string s = formatter(state, exception);
            if (logLevel >= config.FunctionsLoggerLogLevel)
                logger.Log(logLevel, eventId, state, exception, formatter);
            if (streamWriter != null && logLevel >= config.LocalDevelopmentFileLogLevel)
                lock (streamWriter)
                    streamWriter.WriteLine(s);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel) || logLevel >= config.LocalDevelopmentFileLogLevel;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return logger?.BeginScope(state);
        }
    }

}
