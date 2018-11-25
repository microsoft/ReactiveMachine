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
    public class Increment : IOrchestration<UnitType>
    {
        [DataMember]
        public uint TotalNumberOfCounters;

        [DataMember]
        public CounterImplementation Implementation { get; set; }

        [DataMember]
        public uint GeneratorNumber { get; set; }

        [DataMember]
        public uint? IterationNumber { get; set; }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var targetCounter = GeneratorNumber % TotalNumberOfCounters;

            switch(Implementation)
            {
                case CounterImplementation.EventBased:
                    await context.PerformEvent(new Counter.Service.IncrementEvent()
                    {
                        CounterId = targetCounter,
                    });
                    break;

                case CounterImplementation.UpdateBased:
                    await context.PerformUpdate(new Counter.Service.IncrementUpdate()
                    {
                        CounterId = targetCounter,
                    });
                    break;
            }
         
            return UnitType.Value;
        }

    }
}
