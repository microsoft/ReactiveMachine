// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Util
{
    public class LoggerWrapper : ILogger
    {
        private readonly ILogger logger;

        private readonly LogLevel limit;

        private readonly string prefix;

        public LoggerWrapper(ILogger logger, string prefix, LogLevel limit = LogLevel.Trace )
        {
            this.logger = logger;
            this.limit = limit;
            this.prefix = prefix;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= limit)
            {
                logger.Log<TState>(logLevel, eventId, state, exception, (s, e) => prefix + formatter(s, e));
            }
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel) && logLevel >= limit;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
