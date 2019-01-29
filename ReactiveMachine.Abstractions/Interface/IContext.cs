// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// Common context for executing application code without side effects.
    /// </summary>
    public interface IContext
    {
        ILogger Logger { get; }

        TConfiguration GetConfiguration<TConfiguration>();

        IExceptionSerializer ExceptionSerializer { get; }
    }

    /// <summary>
    /// Common context for executing application code without synchronous side effects.
    /// </summary>
    public interface IContextWithForks
    {
        void ForkOrchestration<TReturn>(IOrchestration<TReturn> orchestration);

        void ForkEvent<TEvent>(TEvent evt)
            where TEvent : IEvent;

        void ForkUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
            where TState : IState;

        void GlobalShutdown();
    }

    /// <summary>
    /// Context for executing user code that updates state in response to an event.
    /// </summary>
    public interface ISubscriptionContext : IContext, IContextWithForks
    {
    }

    /// <summary>
    /// Context for initializing state when it is first accessed
    /// </summary>
    public interface IInitializationContext : IOrchestrationContext
    {
    }

    /// <summary>
    /// Context for executing user code that updates state in response to an event.
    /// </summary>
    public interface ISubscriptionContext<TKey> : ISubscriptionContext
    {
        TKey Key { get; }
    }

    /// <summary>
    /// Context for executing user code that may require determinization
    /// </summary>
    public interface IDeterminizationContext
    {
        Task<Guid> NewGuid();

        Task<Random> NewRandom();

        Task<DateTime> ReadDateTimeNow();

        Task<DateTime> ReadDateTimeUtcNow();

        Task<T> Determinize<T>(T value);
    }

    public interface IWriteContext : IDeterminizationContext, IContextWithForks
    {
        Task<TReturn> PerformUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
            where TState : IState;

    }

    public interface IReadContext 
    {
        Task<TReturn> PerformRead<TState, TReturn>(IRead<TState, TReturn> read)
            where TState : IState;

    }
     
    /// <summary>
    /// Context for executing application code that is part of a readonly orchestration.
    /// </summary>
    public interface IReadOrchestrationContext : IContext, IReadContext
    {
        Task<TReturn> PerformOrchestration<TReturn>(IReadOrchestration<TReturn> orchestration);
    }

    /// <summary>
    /// Context for executing application code that is part of an orchestration.
    /// </summary>
    public interface IReadWriteContext : IWriteContext, IReadContext
    {
    }

    public interface IOrchestrationContext : IContext, IReadWriteContext
    {
        Task<TReturn> PerformOrchestration<TReturn>(IOrchestration<TReturn> orchestration);

        Task<TReturn> PerformActivity<TReturn>(IActivity<TReturn> activity);

        Task PerformEvent<TEvent>(TEvent evt) where TEvent : IEvent;

        Task<bool> StateExists<TState, TAffinity, TKey>(TKey key)
            where TState : IPartitionedState<TAffinity, TKey>
            where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        Task Finish();
    }

    /// <summary>
    /// Context for executing application code that is localized by an affinity and has no side effects.
    /// </summary>
    public interface IReadContext<TState> : IContext, IContextWithForks
        where TState : IState
    {
        TState State { get; }

        TReturn PerformRead<TReturn>(IRead<TState, TReturn> read);
    }

    /// <summary>
    /// Context for executing application code that is localized by an affinity.
    /// </summary>
    public interface IUpdateContext<TState> : IReadContext<TState>
        where TState : IState
    {
        TReturn PerformUpdate<TReturn>(IUpdate<TState, TReturn> read);
    }
}
