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
                 .DefineOrchestration<EventWrapper, UnitType>()
                 ;
        }
        public static void DefineInternalExtensions(IServiceBuilder builder)
        {
            builder
                 .DefineActivity<StableDelay, UnitType>()
                 .DefineOrchestration<ForkedEvent, UnitType>()
                 .DefineActivity<DeterminizationActivity<Guid>, Guid>()
                 .DefineActivity<DeterminizationActivity<int>, int>()
                 .DefineActivity<DeterminizationActivity<DateTime>, DateTime>()
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

        public static void DefineOperationExtensions<TState, TRequest, TReturn>(bool isRead, IServiceBuilder builder)
            where TState : IState
        {
            if (isRead)
            {
                builder.DefineOrchestration<ReadWrapper<TState, TReturn>, TReturn>();
            }
            else
            {
                builder.DefineOrchestration<UpdateWrapper<TState, TReturn>, TReturn>();
                builder.DefineOrchestration<ForkedUpdate<TState, TReturn>, UnitType>();
            }
        }
    }
}
