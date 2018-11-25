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
    public class ForkedRaise : IOrchestration<UnitType>
    {
        [DataMember]
        public IEvent Event;

        [DataMember]
        public TimeSpan Delay;

        public override string ToString()
        {
            if (Delay != TimeSpan.Zero)
                return $"ForkedRaise-{Event}";
            else
                return $"ForkedRaise-{Delay}-{Event}";
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.DelayBy(Delay);

            context.ForkEvent(Event);

            return UnitType.Value;
        }
    }

  
}
