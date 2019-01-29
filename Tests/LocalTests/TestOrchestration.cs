// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LocalTests
{
    [DataContract]
    public abstract class TestOrchestration : TestBase, IOrchestration<UnitType>
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation($"{this.GetType().Name} Start");

            try
            {
                await Run(context);
            }
            catch (TestFailureException e)
            {
                context.Logger.LogInformation($"{this.GetType().Name} Test Failure {e}");
            }
            catch (Exception e)
            {
                context.Logger.LogInformation($"{this.GetType().Name} Unknown Exception {e}");
            }

            context.Logger.LogInformation($"{this.GetType().Name} End");

            return UnitType.Value;
        }
    }

    [DataContract]
    public abstract class LockedTestOrchestration : TestBase, IOrchestration<UnitType>
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation($"{this.GetType().Name} Start");

            try
            {
                await Run(context);
            }
            catch (TestFailureException e)
            {
                context.Logger.LogInformation($"{this.GetType().Name} Test Failure {e}");
            }
            catch (Exception e)
            {
                context.Logger.LogInformation($"{this.GetType().Name} Unknown Exception {e}");
            }

            context.Logger.LogInformation($"{this.GetType().Name} End");

            return UnitType.Value;
        }
    }

    [DataContract]
    public abstract class TestBase
    {

        public static void AssertEqual<T>(T expected, T actual) where T : IEquatable<T>
        {
            if (!expected.Equals(actual))
            {
                throw new TestFailureException($"expected: {expected} actual: {actual}");
            }
        }


        protected abstract Task Run(IOrchestrationContext context);
    }
}
