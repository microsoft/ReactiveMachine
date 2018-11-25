// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal class ServiceBuilder : IServiceBuilder
    {
        private readonly Process process;
        internal readonly List<Action<IPlacementBuilder>> Placements = new List<Action<IPlacementBuilder>>();

        public ServiceBuilder(Process process) 
        {
            this.process = process;
        }

        IServiceBuilder IServiceBuilder.BuildService<TService>()
        {
            if (!process.Services.TryGetValue(typeof(TService), out var serviceInfo))
            {
                serviceInfo = new ServiceInfo<TService>(process, this);
            }
            else if (!serviceInfo.BuildComplete)
            {
                throw new BuilderException($"detected circular dependency of services");
            }
            return this;
        }

        IServiceBuilder IServiceBuilder.ScanThisDLL()
        {
            StackTrace stackTrace = new System.Diagnostics.StackTrace();
            StackFrame frame = stackTrace.GetFrames()[1];
            MethodBase method = frame.GetMethod();
            Type methodsClass = method.DeclaringType;
            Assembly assembly = methodsClass.Assembly;
            new ReflectionServiceBuilder(this).Build(assembly);
            return this;
        }

        TConfiguration IServiceBuilder.GetConfiguration<TConfiguration>()
        {
            return (TConfiguration) process.Configurations[typeof(TConfiguration)];
        }
        IServiceBuilder IServiceBuilder.SetConfiguration<TConfiguration>(TConfiguration configuration)
        {
            process.Configurations[typeof(TConfiguration)] = configuration;
            return this;
        }

        IServiceBuilder IServiceBuilder.OverridePlacement(Action<IPlacementBuilder> placement)
        {
            Placements.Add(placement);
            return this;
        }

        IServiceBuilder IServiceBuilder.OnFirstStart(IOrchestration orchestration)
        {
            process.StartupOrchestrations.Add(orchestration);
            return this;
        }

        IServiceBuilder IServiceBuilder.RegisterSerializableType(Type type)
        {
            process.HostServices.SerializableTypeSet.Add(type);
            return this;
        }

        IServiceBuilder IServiceBuilder.DefinePartitionedAffinity<TAffinity, TKey>() 
        {
            var type = typeof(TAffinity);
            if (process.Affinities.ContainsKey(type)) return this;
            var keyinfo = new PartitionedAffinityInfo<TAffinity,TKey>(process);
            new PartitionLock<TAffinity,TKey>(process, keyinfo);
            if (keyinfo.RoundRobinAttribute)
                Placements.Add((b) => b.PlaceByIndex<TAffinity, TKey>(1));
            else
                Placements.Add((b) => b.PlaceByJumpConsistentHash<TAffinity, TKey>());
            return this;
        }
        IServiceBuilder IServiceBuilder.DefineSingletonAffinity<TAffinity>()
        {
            var type = typeof(TAffinity);
            if (process.Affinities.ContainsKey(type)) return this;
            var keyinfo = new SingletonAffinityInfo<TAffinity>(process);
            new PartitionLock<TAffinity,UnitType>(process, keyinfo);
            Placements.Add((b) => b.PlaceOnProcess<TAffinity>(0));
            return this;
        }

        IServiceBuilder IServiceBuilder.DefineOrchestration<TRequest, TReturn>()
        {
            var type = typeof(TRequest);
            if (process.Orchestrations.ContainsKey(type)) return this;
            new OrchestrationInfo<TRequest, TReturn>(process);
            Placements.Add(ReflectionServiceBuilder.DefaultPlacement<TRequest>(process.Affinities.Keys));
            return this;
        }
        IServiceBuilder IServiceBuilder.DefineReadOrchestration<TRequest, TReturn>()
        {
            var type = typeof(TRequest);
            if (process.Orchestrations.ContainsKey(type)) return this;
            new OrchestrationInfo<TRequest, TReturn>(process);
            Placements.Add(ReflectionServiceBuilder.DefaultPlacement<TRequest>(process.Affinities.Keys));
            return this;
        }
     
        IServiceBuilder IServiceBuilder.DefineUpdate<TState, TRequest, TReturn>()
        {
            if (!process.States.TryGetValue(typeof(TState), out var stateInfo))
            {
                throw new BuilderException($"undefined state {typeof(TState).FullName}.");
            }
            if (!stateInfo.AffinityInfo.GetInterfaceType().IsAssignableFrom(typeof(TRequest)))
            {
                throw new BuilderException($"class {typeof(TRequest).Name} must implement {stateInfo.AffinityInfo.GetInterfaceType().Name}.");
            }
            stateInfo.RegisterOperation<TRequest, TReturn>(false);
            return this;
        }

        IServiceBuilder IServiceBuilder.DefineRead<TState, TRequest, TReturn>()
        {
            if (!process.States.TryGetValue(typeof(TState), out var stateInfo))
            {
                throw new BuilderException($"undefined state {typeof(TState).FullName}.");
            }
            if (!stateInfo.AffinityInfo.GetInterfaceType().IsAssignableFrom(typeof(TRequest)))
            {
                throw new BuilderException($"class {typeof(TRequest).Name} must implement {stateInfo.AffinityInfo.GetInterfaceType().Name}.");
            }
            stateInfo.RegisterOperation<TRequest, TReturn>(true);
            return this;
        }

        IServiceBuilder IServiceBuilder.DefineAtLeastOnceActivity<TRequest, TReturn>()
        {
            var type = typeof(TRequest);
            if (process.Activities.ContainsKey(type)) return this;
            new ActivityInfo<TRequest, TReturn>(process, ActivityType.AtLeastOnce);
            Placements.Add(ReflectionServiceBuilder.DefaultPlacement<TRequest>(process.Affinities.Keys));
            return this;
        }
        IServiceBuilder IServiceBuilder.DefineAtMostOnceActivity<TRequest, TReturn>()
        {
            var type = typeof(TRequest);
            if (process.Activities.ContainsKey(type)) return this;
            new ActivityInfo<TRequest, TReturn>(process, ActivityType.AtMostOnce);
            Placements.Add(ReflectionServiceBuilder.DefaultPlacement<TRequest>(process.Affinities.Keys));
            return this;
        }
       

        IServiceBuilder IServiceBuilder.DefineEvent<TEvent>()
        {
            if (process.Events.ContainsKey(typeof(TEvent))) return this;
            new EventInfo<TEvent>(process);
            return this;
        }

        IServiceBuilder IServiceBuilder.DefineSingletonState<TState, TAffinity>()
        {
            var type = typeof(TState);
            if (process.States.ContainsKey(type)) return this;
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined affinity {typeof(TAffinity).FullName}.");
            }
            ((SingletonAffinityInfo<TAffinity>)keyInfo).MakeStateInfo<TState>();
            return this;
        }

        IServiceBuilder IServiceBuilder.DefinePartitionedState<TState, TAffinity,TKey>()
        {
            var type = typeof(TState);
            if (process.States.ContainsKey(type)) return this;
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined affinity {typeof(TAffinity).FullName}.");
            }
            ((PartitionedAffinityInfo<TAffinity,TKey>)keyInfo).MakeStateInfo<TState>();
            return this;
        }
    }
}
