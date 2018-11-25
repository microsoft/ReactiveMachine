// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using HelloWorld.Service;

namespace HelloWorld.Test
{
    /// <summary>
    /// An orchestration that tests the hello world service,
    /// and runs automatically when the service is started for the first time
    /// </summary>
    public class HelloWorldTestOrchestration : ReactiveMachine.IStartupOrchestration
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var config = context.GetConfiguration<HelloWorldTestConfiguration>() 
                ?? new HelloWorldTestConfiguration();

            for (int i = 0; i < config.NumberRepetitions; i++)
                await context.PerformOrchestration(new HelloWorldOrchestration());

            return UnitType.Value;
        }
    }
}
