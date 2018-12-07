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
    public class UpdateWrapper<TState, TReturn> : IOrchestration<TReturn>
            where TState : IState
    {
        [DataMember]
        public IUpdate<TState, TReturn> Update;

        public override string ToString()
        {
            return $"Wrapped-{Update}";
        }

        public Task<TReturn> Execute(IOrchestrationContext context)
        {
            return context.PerformUpdate(Update);
        }
    }

    [DataContract]
    public class ReadWrapper<TState, TReturn> : IOrchestration<TReturn>
           where TState : IState
    {
        [DataMember]
        public IRead<TState, TReturn> Read;

        public override string ToString()
        {
            return $"Wrapped-{Read}";
        }

        public Task<TReturn> Execute(IOrchestrationContext context)
        {
            return context.PerformRead(Read);
        }
    }

    [DataContract]
    public class EventWrapper : IOrchestration<UnitType>
    {
        [DataMember]
        public IEvent Event;

        public override string ToString()
        {
            return $"Wrapped-{Event}";
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.PerformEvent(Event);
            return UnitType.Value;
        }
    }

}
