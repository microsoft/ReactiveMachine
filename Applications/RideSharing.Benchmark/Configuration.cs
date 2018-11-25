// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace RideSharing.Benchmark
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        public uint NumberGeneratorProcesses;

        [DataMember]
        public uint NumberGenerators;

        [DataMember]
        public uint NumberServiceProcesses;

        [DataMember]
        public uint NumberRiders;

        [DataMember]
        public uint NumberDrivers;

        [DataMember]
        public TimeSpan Duration;

        [DataMember]
        public TimeSpan Cooldown;

        [DataMember]
        public double Rate;

    }
}
