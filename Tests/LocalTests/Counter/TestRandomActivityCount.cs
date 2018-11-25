// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LocalTests.Counter
{
    [DataContract]
    public class TestRandomActivityCount : TestTemplate
    {

        protected override async Task Run(IOrchestrationContext context)
        {
            var random = await context.NewRandom();
            var offset = (uint) random.Next();

            await context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 0 });

            await context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 1 });

            var numactivities = 2 + random.Next(3);
            var activities = new List<Task>(); ;
            for (int i = 0; i < numactivities; i++)
            {
                activities.Add(context.NewGuid());
            }
            await Task.WhenAll(activities);

            context.ForkUpdate(new IncrementThenRead() { CounterId = offset + 2 });

            context.ForkUpdate(new IncrementThenRead() { CounterId = offset + 3 });

            await Task.WhenAll(
                context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 4 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 5 })
            );

            await Task.WhenAll(
                context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 6 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 7 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 8 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = offset + 9 })
            );

        }
    }
}
