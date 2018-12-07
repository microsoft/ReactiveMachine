// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.TelemetryBlobWriter
{

    public class BlobFormat
    {
        public string deploymentId;

        public Dictionary<string,Object> configurations;
    }

    public class EventsBlobFormat : BlobFormat
    {
        public uint processId;

        public TelemetryEvent[] Events;
    }

    public struct TelemetryEvent
    {
        public string id;

        public string name;

        public string parent;

        [JsonConverter(typeof(StringEnumConverter))]
        public OperationSide opSide;

        [JsonConverter(typeof(StringEnumConverter))]
        public OperationType opType;

        public double duration;

        public double time;
    }

    public class ThroughputBlobFormat : BlobFormat
    {
        public uint processId;

        public List<ThroughputEvent> events;
    }


    public struct ThroughputEvent
    {
        public int time;

        public string name;

        public int count;
    }
}
