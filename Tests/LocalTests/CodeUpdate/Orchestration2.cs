using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LocalTests.CodeUpdate
{
    [AddedInVersionAttribute(1)]
    public class Orchestration2 : 
        IOrchestration<UnitType>
    {
        public int IterationCount;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation($"Orchestration2 iteration={IterationCount}");

            await context.PerformActivity(new Activity1());

            context.ForkOrchestration(new Orchestration2() { IterationCount = IterationCount + 1 });

            return UnitType.Value;
        }
    }


}
