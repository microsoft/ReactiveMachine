using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Counter.Tests
{
    [DataContract]
    public class TestSuite : IStartupOrchestration
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation($"Counter.TestSuite Start");

            //await context.PerformOrchestration(new TestRandomActivityCount());
            await context.PerformOrchestration(new TestForks());
            await context.PerformOrchestration(new TestForks());

            context.Logger.LogInformation($"Counter.TestSuite End");

            return UnitType.Value;
        }


       
        
    }
}
