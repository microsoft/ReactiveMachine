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
    public class ForkedUpdate<TState, TReturn> : IOrchestration<UnitType>
            where TState : IState
    {
        [DataMember]
        public IUpdate<TState, TReturn> Update;

        [DataMember]
        public TimeSpan Delay;

        public override string ToString()
        {
            return $"Scheduled-{Delay}-{Update}";
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.DelayBy(Delay);

            context.ForkUpdate(Update);

            return UnitType.Value;
        }
    }
}
