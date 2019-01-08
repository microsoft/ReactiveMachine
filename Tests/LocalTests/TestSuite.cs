// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using LocalTests.Counter;
using LocalTests.Locks;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LocalTests
{
    [DataContract]
    public class TestSuite : IStartupOrchestration
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation($"Counter.TestSuite Start");

            await context.PerformOrchestration(new TestStates());
            await context.PerformOrchestration(new TestEvents());
            await context.PerformOrchestration(new TestEventsWithInit());
            await context.PerformOrchestration(new TestLocks());

            await Task.WhenAll(
                context.PerformOrchestration(new TestRandomActivityCount()),
                context.PerformOrchestration(new TestRandomActivityCount()),
                context.PerformOrchestration(new TestRandomActivityCount())
                );

            await context.PerformOrchestration(new TestForks());

            context.Logger.LogInformation($"Counter.TestSuite End");

            return UnitType.Value;
        }


       
        
    }
}
