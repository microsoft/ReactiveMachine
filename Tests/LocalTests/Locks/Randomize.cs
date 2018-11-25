// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LocalTests.Locks
{
    public class RandomizeOrchestration : IOrchestration<UnitType>
    {

        public IOrchestration<UnitType> Request;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var random = await context.NewRandom();

            return await context.PerformOrchestration(new ExecuteAt()
            {
                Place = (Places)random.Next(3),
                Request = Request
            });

        }
    }

   

    public class ExecuteAt : IOrchestration<UnitType>, IPlaceAffinity
    {
        public IOrchestration<UnitType> Request;

        public Places Place { get; set; }

        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            return context.PerformOrchestration(Request);
        }
    }


    public class RandomizeEvent : IOrchestration<UnitType>
    {

        public IEvent Event;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var random = await context.NewRandom();

            return await context.PerformOrchestration(new ExecuteEventAt()
            {
                Place = (Places)random.Next(3),
                Event = Event
            });

        }
    }



    public class ExecuteEventAt : IOrchestration<UnitType>, IPlaceAffinity
    {
        public IEvent Event;

        public Places Place { get; set; }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.PerformEvent(Event);
            return UnitType.Value;
        }
    }
}
