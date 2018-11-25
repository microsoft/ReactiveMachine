// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Counter.Benchmark
{
    [DataContract]
    public class CounterBenchmarkConfiguration
    {
        // parameters for the experiment

        [DataMember]
        public uint NumberGeneratorProcesses;

        [DataMember]
        public uint NumberGenerators;

        [DataMember]
        public uint NumberCounterProcesses;

        [DataMember]
        public uint NumberCounters;


        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public CounterImplementation Implementation;


        // used only for fixed-rate experiment

        [IgnoreDataMember]
        public bool IsFixedRateExperiment => Rate > 0;

        [DataMember]
        public double Rate;

        [DataMember]
        public TimeSpan Duration;

        [DataMember]
        public TimeSpan Cooldown;


        // used only for load-loops experiment

        [IgnoreDataMember]
        public bool IsLoadLoopsExperiment => NumberRequests > 0;

        [DataMember]
        public uint NumberRequests;

        [DataMember]
        public TimeSpan Stagger;

    }

    public enum CounterImplementation
    {
        UpdateBased,
        EventBased
    }

}
