// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Compiler
{

    internal class HostServices : IHostServices
    {
        // services registered by host
        internal List<ITelemetryListener> TelemetryListeners = new List<ITelemetryListener>();
        internal ILogger RuntimeLogger;
        internal ILogger ApplicationLogger;
        internal Action<Exception> GlobalExceptionHandler;
        internal Action<uint, IMessage> Send;
        internal Action GlobalShutdown;

        internal HashSet<Type> SerializableTypeSet = new HashSet<Type>();

        public IEnumerable<Type> SerializableTypes => SerializableTypeSet;

        public void RegisterTelemetryListener(ITelemetryListener telemetryListener)
        {
            TelemetryListeners.Add(telemetryListener);
        }

        public void RegisterRuntimeLogger(ILogger logger)
        {
            RuntimeLogger = logger;
        }

        public void RegisterApplicationLogger(ILogger logger)
        {
            ApplicationLogger = logger;
        }

        public void RegisterGlobalExceptionHandler(Action<Exception> handler)
        {
            GlobalExceptionHandler = handler;
        }

        public void RegisterSend(Action<uint, IMessage> action)
        {
            Send = action;
        }

        public void RegisterSerializableType(Type type)
        {
            SerializableTypeSet.Add(type);
        }

        public void RegisterGlobalShutdown(Action globalShutdown)
        {
            GlobalShutdown = globalShutdown;
        }


        internal ITelemetryListener GetTelemetryListener()
        {
            if (TelemetryListeners.Count > 0)
                return new TelemetrySplitter() { TelemetryListeners = TelemetryListeners };
            else
                return null;
        }

        private class TelemetrySplitter : ITelemetryListener
        {
            public List<ITelemetryListener> TelemetryListeners;

            public void OnApplicationEvent(uint processId, string id, string name, string parent, OperationSide opSide, OperationType opType, double duration)
            {
                foreach (var tl in TelemetryListeners)
                {
                    tl.OnApplicationEvent(processId, id, name, parent, opSide, opType, duration);
                }
            }
        }

    }
}