// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Counter;
using Harness;

namespace Counter.Benchmark
{
    [DataContract]
    public class Workload : IWorkloadGenerator
    {
        [DataMember]
        public uint TotalNumberOfCounters;

        [DataMember]
        public CounterImplementation Implementation;

        public IOrchestration<UnitType> GenerateInitialization(uint generatorNumber)
        {
            return new Increment()
            {
                GeneratorNumber = generatorNumber,
                IterationNumber = null,
                Implementation = Implementation,
                TotalNumberOfCounters = TotalNumberOfCounters
            };
        }

        public IOrchestration<UnitType> GenerateRequest(uint generatorNumber, uint iterationNumber)
        {
            return new Increment()
            {
                GeneratorNumber = generatorNumber,
                IterationNumber = iterationNumber,
                Implementation = Implementation,
                TotalNumberOfCounters = TotalNumberOfCounters
            };
        }
    }
}
