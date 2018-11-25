// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// An orchestration that returns a value of type {TReturn}.
    /// </summary>
    /// 
    public interface IOrchestrationBase<TReturn> : IOrchestration
    {
    }

    public interface IOrchestration<TReturn> : IOrchestrationBase<TReturn>
    {
        Task<TReturn> Execute(IOrchestrationContext context);
    }

    public interface IReadOrchestration<TReturn> : IOrchestrationBase<TReturn>
    {
        Task<TReturn> Execute(IReadOrchestrationContext context);
    }

    public interface IUpdate<TState, TReturn> : ILocalOperation<TState>
        where TState : IState
    {
        TReturn Execute(IUpdateContext<TState> context);
    }

    public interface IRead<TState, TReturn> : ILocalOperation<TState>
        where TState : IState
    {
        TReturn Execute(IReadContext<TState> context);
    }


    public interface IStartupOrchestration : IOrchestration<UnitType>
    {
    }

}
