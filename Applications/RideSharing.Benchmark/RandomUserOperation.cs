// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
 

namespace RideSharing.Benchmark
{
    [DataContract]
    public class RandomUserOperation : IOrchestration<UnitType>, Harness.IWorkloadGenerator
    {

        [DataMember]
        public Configuration Configuration;

        public IOrchestration<UnitType> GenerateRequest(uint generatorNumber, uint iterationNumber)
        {
            return new RandomUserOperation()
            {
                Configuration = Configuration
            };
        }

        public IOrchestration<UnitType> GenerateInitialization(uint generatorNumber)
        {
            return new RandomUserOperation()
            {
                Configuration = Configuration
            };
        }
     

        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            return UnitType.CompletedTask;
        }

    }
}
