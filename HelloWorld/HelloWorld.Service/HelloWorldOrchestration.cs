// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveMachine;

namespace HelloWorld.Service
{
    /// <summary>
    /// Defines an orchestration that does nothing other than returning the string "Hello World"
    /// </summary>
    public class HelloWorldOrchestration : ReactiveMachine.IOrchestration<string>
    {
        public Task<string> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation("Hello World was called");

            return Task.FromResult("Hello World");
        }
    }
}
