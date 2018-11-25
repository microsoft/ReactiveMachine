// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using ReactiveMachine;

namespace ApplicationInsightsHelpers
{
    public class ApplicationInsightsTelemetryListener : ITelemetryListener
    {
        public ApplicationInsightsTelemetryListener(string instrumentationKey, string backend)
        {
            _instrumentationKey = TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;

            _telemetry = new TelemetryClient()
            {
                InstrumentationKey = _instrumentationKey
            };

            _metrics = new Dictionary<string, double>();

            _backend = backend;

            _properties = new Dictionary<string, string>();
        }

        String _backend;

        TelemetryClient _telemetry;

        Dictionary<string, string> _properties;

        Dictionary<string, double> _metrics;

        String _instrumentationKey;

        public void OnApplicationEvent(uint processId, string id, string name, string parent, OperationSide opSide, OperationType opType, double duration)
        {
            _telemetry.Context.Operation.Id = id;
            _telemetry.Context.Operation.Name = name;
            _telemetry.Context.Operation.ParentId = parent;

            _properties["backend"] = _backend;
            _properties["opside"] = opSide.ToString();
            _properties["optype"] = opType.ToString();

            _metrics["duration"] = duration;

            _telemetry.TrackEvent(name, _properties, _metrics);
        }
    }
}
