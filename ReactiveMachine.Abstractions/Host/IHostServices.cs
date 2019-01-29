// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ReactiveMachine
{
    public interface IHostServices
    {
        void RegisterTelemetryListener(ITelemetryListener telemetryListener);

        void RegisterRuntimeLogger(ILogger logger);

        void RegisterApplicationLogger(ILogger logger);

        void RegisterGlobalExceptionHandler(Action<Exception> handler);

        void RegisterSend(Action<uint, IMessage> action);

        void RegisterSerializableType(Type type);

        void RegisterGlobalShutdown(Action globalShutdown);

        IEnumerable<Type> SerializableTypes { get; }
    }
}
