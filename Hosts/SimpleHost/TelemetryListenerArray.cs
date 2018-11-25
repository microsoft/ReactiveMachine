// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TelemetryBlobWriter;

namespace EmulatorHost
{
    public class TelemetryListenerArray : ITelemetryListener
    {
        private TelemetryCollector[] listeners;
        private string deploymentId;
        private DateTime deploymentTimestamp;

        public TelemetryListenerArray(TelemetryBlobWriter.Configuration config, ICompiledApplication application, Type hosttype, string deploymentId, DateTime deploymentTimestamp)
        {
            listeners = new TelemetryCollector[application.NumberProcesses];
            for (uint i = 0; i < application.NumberProcesses; i++)
                listeners[i] = new TelemetryCollector(config, application, i, hosttype);
            this.deploymentId = deploymentId;
            this.deploymentTimestamp = deploymentTimestamp;
        }

        public void OnApplicationEvent(uint processId, string id, string name, string parent, OperationSide opSide, OperationType opType, double duration)
        {
            listeners[processId].OnApplicationEvent(processId, id, name, parent, opSide, opType, duration);
        }

        public Task Shutdown()
        {
            var tasks = new List<Task>();
            foreach (var l in listeners)
                tasks.Add(l.PushTelemetry(deploymentId, deploymentTimestamp, false));
            return Task.WhenAll(tasks);
        }
    }
}
