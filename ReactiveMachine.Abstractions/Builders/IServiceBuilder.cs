// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    public interface IServiceBuilder
    {
        IServiceBuilder BuildService<TService>()
            where TService : IServiceBuildDefinition, new();

        IServiceBuilder ScanThisDLL();

        TConfiguration GetConfiguration<TConfiguration>();

        IServiceBuilder SetConfiguration<TConfiguration>(TConfiguration configuration);

        IServiceBuilder OnFirstStart(IOrchestration orchestration);

        IServiceBuilder RegisterSerializableType(Type type);

        IServiceBuilder OverridePlacement(Action<IPlacementBuilder> placement);

        IServiceBuilder DefinePartitionedAffinity<TAffinity, TKey>()
            where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        IServiceBuilder DefineSingletonAffinity<TAffinity>()
            where TAffinity : ISingletonAffinity<TAffinity>;

        IServiceBuilder DefineSingletonState<TState, TAffinity>()
            where TState : ISingletonState<TAffinity>, new()
            where TAffinity : ISingletonAffinity<TAffinity>;

        IServiceBuilder DefinePartitionedState<TState, TAffinity, TKey>()
                 where TState : IPartitionedState<TAffinity, TKey>, new()
                 where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        IServiceBuilder DefineEvent<TEvent>()
            where TEvent : IEvent;

        IServiceBuilder DefineOrchestration<TRequest, TReturn>()
            where TRequest : IOrchestration<TReturn>;

        IServiceBuilder DefineReadOrchestration<TRequest, TReturn>()
           where TRequest : IReadOrchestration<TReturn>;

        IServiceBuilder DefineUpdate<TState, TRequest, TReturn>()
            where TState : IState
            where TRequest : IUpdate<TState, TReturn>;

        IServiceBuilder DefineRead<TState, TRequest, TReturn>()
            where TState : IState
            where TRequest : IRead<TState, TReturn>;

        IServiceBuilder DefineAtLeastOnceActivity<TRequest, TReturn>()
            where TRequest : IAtLeastOnceActivity<TReturn>;

        IServiceBuilder DefineAtMostOnceActivity<TRequest, TReturn>()
            where TRequest : IAtMostOnceActivity<TReturn>;

    }


}
