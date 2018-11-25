using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Counter.Tests
{
    [DataContract]
    public class TestForks : TestTemplate
    {
        protected override async Task Run(IOrchestrationContext context)
        {
            await context.Finish();

            context.ForkUpdate(new IncrementThenRead() { CounterId = 0 });

            await context.Finish();

            context.ForkUpdate(new IncrementThenRead() { CounterId = 1 });
            context.ForkUpdate(new IncrementThenRead() { CounterId = 2 });

            await context.Finish();

            for (uint i = 0; i < 100; i++)
                context.ForkUpdate(new IncrementThenRead() { CounterId = 100 + i });

            var f1 = context.Finish();

            for (uint i = 0; i < 100; i++)
                context.ForkUpdate(new IncrementThenRead() { CounterId = 200 + i });

            var f2 = context.Finish();

            for (uint i = 0; i < 100; i++)
                context.ForkUpdate(new IncrementThenRead() { CounterId = 300 + i });

            await Task.WhenAll(f1,f2);

            await context.Finish();
        }
    }
}
