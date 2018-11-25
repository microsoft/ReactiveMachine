// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Harness
{
    [DataContract]
    public class RequestSequence : 
        IOrchestration<UnitType>, 
        IGeneratorAffinity
    {
        [DataMember]
        public uint GeneratorNumber { get; set; }

        [DataMember]
        public IWorkloadGenerator OperationFactory;

        [DataMember]
        public uint NumberRequests;

        [DataMember]
        public DateTime StartTime { get; set; }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogTrace($"Harness.RequestSequence {GeneratorNumber} Start");

            await context.DelayUntil(StartTime);

            for (uint i = 0; i < NumberRequests; i++)
            {
                await context.PerformOrchestration(OperationFactory.GenerateRequest(GeneratorNumber, i));
            }

            context.Logger.LogTrace($"Harness.RequestSequence {GeneratorNumber} Done");

            return UnitType.Value;
        }
    }
}
