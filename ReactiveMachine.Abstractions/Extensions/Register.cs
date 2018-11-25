// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Extensions
{
    public static class Register
    {
        public static void DefineVisibleExtensions (IServiceBuilder builder)
        {
            builder
                 .DefinePartitionedAffinity<IProcessAffinity, uint>()
                 ;
        }
        public static void DefineInternalExtensions(IServiceBuilder builder)
        {
            builder
                 .DefineAtLeastOnceActivity<StableDelay, UnitType>()
                 .DefineOrchestration<ForkedRaise, UnitType>()
                 ;
        }

        public static void DefineOrchestrationExtensions<TRequest, TReturn>(IServiceBuilder builder)
        {
            builder.DefineOrchestration<ForkedOrchestration<TReturn>, UnitType>();
        }

        public static void DefineActivityExtensions<TRequest, TReturn>(IServiceBuilder builder)
        {
            builder.DefineOrchestration<ForkedActivity<TReturn>, UnitType>();
        }

        public static void DefineUpdateExtensions<TState, TRequest, TReturn>(bool isRead, IServiceBuilder builder)
            where TState : IState
        {
            if (!isRead)
                builder.DefineOrchestration<ForkedLocalUpdate<TState, TReturn>, UnitType>();
        }

    }
}
