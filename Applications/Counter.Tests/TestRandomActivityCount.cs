using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Counter.Tests
{
    [DataContract]
    public class TestRandomActivityCount : TestTemplate
    {

        protected override async Task Run(IOrchestrationContext context)
        {
            var random = await context.NewRandom();

            await context.PerformUpdate(new IncrementThenRead() { CounterId = 0 });

            await context.PerformUpdate(new IncrementThenRead() { CounterId = 1 });

            var numactivities = 2 + random.Next(3);
            var activities = new List<Task>(); ;
            for (int i = 0; i < numactivities; i++)
            {
                activities.Add(context.NewGuid());
            }
            await Task.WhenAll(activities);

            context.ForkUpdate(new IncrementThenRead() { CounterId = 2 });

            context.ForkUpdate(new IncrementThenRead() { CounterId = 3 });

            await Task.WhenAll(
                context.PerformUpdate(new IncrementThenRead() { CounterId = 4 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = 5 })
            );

            await Task.WhenAll(
                context.PerformUpdate(new IncrementThenRead() { CounterId = 6 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = 7 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = 8 }),
                context.PerformUpdate(new IncrementThenRead() { CounterId = 9 })
            );

        }
    }
}
