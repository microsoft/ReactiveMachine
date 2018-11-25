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
    public class ForkedLocalUpdate<TState, TReturn> : IOrchestration<UnitType>
            where TState : IState
    {
        [DataMember]
        public IUpdate<TState, TReturn> Update;

        [DataMember]
        public TimeSpan Delay;

        public override string ToString()
        {
            if (Delay != TimeSpan.Zero)
                return $"ForkedLocalOrchestration-{Update}";
            else
                return $"ForkedLocalOrchestration-{Delay}-{Update}";
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.DelayBy(Delay);

            context.ForkUpdate(Update);

            return UnitType.Value;
        }
    }
}
