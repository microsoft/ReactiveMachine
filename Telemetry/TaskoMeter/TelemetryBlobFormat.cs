// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Tools.Taskometer
{

    public class EventsBlobFormat
    {
        public string deploymentId;

        public Dictionary<string, Object> configurations;

        public uint processId;

        public List<TelemetryEvent> Events;
    }

    public struct TelemetryEvent
    {
        public string id;

        public string name;

        public string parent;

        public string opSide;

        public string opType;

        public double duration;

        public double time;
    }

 
}
