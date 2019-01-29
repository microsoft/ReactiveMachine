// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Extensions
{
    [DataContract]
    public class ForkedOrchestration<TReturn> : IOrchestration<UnitType>
    {
        [DataMember]
        public IOrchestration<TReturn> Orchestration;

        [DataMember]
        public TimeSpan Delay;

        public override string ToString()
        {
            return $"Scheduled-{Delay}-{Orchestration}";
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.DelayBy(Delay);

            context.ForkOrchestration(Orchestration);

            return UnitType.Value;
        }
    }
}
