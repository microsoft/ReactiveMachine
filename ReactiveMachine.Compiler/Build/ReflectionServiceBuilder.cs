// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal class ReflectionServiceBuilder
    {
        private readonly IServiceBuilder serviceBuilder;

        // scan results
        private readonly List<Type> affinities = new List<Type>();
        private readonly List<Type> states = new List<Type>();
        private readonly List<Type> events = new List<Type>();
        private readonly List<Type> operations = new List<Type>();
        private readonly List<Type> serializableTypes = new List<Type>();


        public ReflectionServiceBuilder(IServiceBuilder serviceBuilder)
        {
            this.serviceBuilder = serviceBuilder;
        }

        public void Build(Assembly assembly)
        {
            var stateType = typeof(IState);
            var eventType = typeof(IEvent);
            var affinityType = typeof(IAffinitySpec<>);
            var operationType = typeof(IOperation);

            foreach (var type in assembly.GetTypes())
            {
                if (affinityType.MakeGenericType(type).IsAssignableFrom(type))
                    affinities.Add(type);
                if (eventType.IsAssignableFrom(type))
                    events.Add(type);
                if (stateType.IsAssignableFrom(type))
                    states.Add(type);
                if (operationType.IsAssignableFrom(type))
                    operations.Add(type);
                var a = type.GetCustomAttribute(typeof(DataContractAttribute));
                var b = type.GetCustomAttribute(typeof(SerializableAttribute));
                if (a != null || b != null)
                    serializableTypes.Add(type);
            }

            BuildAffinities();
            BuildEvents();
            BuildStates();
            BuildExceptions();
            BuildOperations();
        }

        public static string GetGenericTypeNamePrefix(Type type)
        {
            return $"{type.Namespace}.{type.Name}";
        }
 
        public void BuildAffinities()
        {
            var partitionedPrefix = GetGenericTypeNamePrefix(typeof(IPartitionedAffinity<,>));
            var singletonPrefix = GetGenericTypeNamePrefix(typeof(ISingletonAffinity<>));

            foreach (var a in affinities)
            {
                if (a.ContainsGenericParameters)
                    throw new BuilderException($"invalid affinity {a.Name} : generic parameters not supported");

                Type spec = null;
                foreach (var i in a.GetInterfaces())
                {
                    var name = GetGenericTypeNamePrefix(i);
                    if (name == partitionedPrefix || name == singletonPrefix)
                    {
                        if (spec != null)
                            throw new BuilderException($"invalid affinity {a.Name} : multiple specifiers : {spec.FullName}, {i.FullName}");
                        spec = i;
                    }
                }

                if (spec == null)
                    throw new BuilderException($"invalid affinity {a.Name} : missing interface {typeof(IPartitionedAffinity<,>).Name} or {typeof(ISingletonAffinity<>).Name}");

                if (GetGenericTypeNamePrefix(spec) == singletonPrefix)
                {
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineSingletonAffinity));
                    var mg = m.MakeGenericMethod(a);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
                else
                {
                    var keytype = spec.GenericTypeArguments[1];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefinePartitionedAffinity));
                    var mg = m.MakeGenericMethod(a, keytype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
            }
        }
        public void BuildStates()
        {
            var partitionedPrefix = GetGenericTypeNamePrefix(typeof(IPartitionedState<,>));
            var singletonPrefix = GetGenericTypeNamePrefix(typeof(ISingletonState<>));


            foreach (var s in states)
            {
                Type spec = null;
                foreach (var i in s.GetInterfaces())
                {
                    var name = GetGenericTypeNamePrefix(i);
                    if (name == partitionedPrefix || name == singletonPrefix)
                    {
                        if (spec != null)
                            throw new BuilderException($"invalid state {s.Name} : multiple specifiers : {spec.FullName}, {i.FullName}");
                        spec = i;
                    }
                }

                if (spec == null)
                    throw new BuilderException($"invalid state {s.Name} : missing interface {typeof(ISingletonState<>).Name} or {typeof(IPartitionedState<,>).Name}");

                var affinity = spec.GenericTypeArguments[0];

                if (GetGenericTypeNamePrefix(spec) == singletonPrefix)
                {
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineSingletonState));
                    var mg = m.MakeGenericMethod(s, affinity);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
                else
                {
                    var keytype = spec.GenericTypeArguments[1];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefinePartitionedState));
                    var mg = m.MakeGenericMethod(s, affinity, keytype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
            }
        }

        public void BuildEvents()
        {
            foreach (var e in events)
            {
                var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineEvent));
                var mg = m.MakeGenericMethod(e);
                mg.Invoke(serviceBuilder, new object[0]);
            }
        }

        public void BuildExceptions()
        {
            foreach (var e in serializableTypes)
            {
                serviceBuilder.RegisterSerializableType(e);
            }
        }

        public void BuildOperations()
        {
            var orchestrationPrefix = GetGenericTypeNamePrefix(typeof(IOrchestration<>));
            var readOrchestrationPrefix = GetGenericTypeNamePrefix(typeof(IReadOrchestration<>));
            var atLeastOnceActivityPrefix = GetGenericTypeNamePrefix(typeof(IAtLeastOnceActivity<>));
            var atMostOnceActivityPrefix = GetGenericTypeNamePrefix(typeof(IAtMostOnceActivity<>));
            var updatePrefix = GetGenericTypeNamePrefix(typeof(IUpdate<,>));
            var readPrefix = GetGenericTypeNamePrefix(typeof(IRead<,>));

            var startups = new List<IStartupOrchestration>();

            foreach (var o in operations)
            {
                Type spec = null;
                foreach (var i in o.GetInterfaces())
                {
                    var name = GetGenericTypeNamePrefix(i);
                    if (name == orchestrationPrefix || name == readOrchestrationPrefix ||
                        name == atLeastOnceActivityPrefix || name == atMostOnceActivityPrefix ||
                        name == updatePrefix || name == readPrefix)
                    {
                        if (spec != null)
                            throw new BuilderException($"invalid operation {o.Name} : multiple specifiers : {spec.FullName}, {i.FullName}");
                        spec = i;
                    }
                }

                if (spec == null)
                    throw new BuilderException($"invalid operation {o.Name} : missing interface IActivity, IOrchestration, or ITransaction");

                var specname = GetGenericTypeNamePrefix(spec);

                if (specname == orchestrationPrefix)
                {
                    var returntype = spec.GenericTypeArguments[0];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineOrchestration));
                    var mg = m.MakeGenericMethod(o, returntype);
                    mg.Invoke(serviceBuilder, new object[0]);

                    if (typeof(IStartupOrchestration).IsAssignableFrom(o))
                        serviceBuilder.OnFirstStart((IOrchestration) Activator.CreateInstance(o));
                }
                else if (specname == readOrchestrationPrefix)
                {
                    var returntype = spec.GenericTypeArguments[0];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineReadOrchestration));
                    var mg = m.MakeGenericMethod(o, returntype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
                else if (specname == atLeastOnceActivityPrefix)
                {
                    var returntype = spec.GenericTypeArguments[0];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineAtLeastOnceActivity));
                    var mg = m.MakeGenericMethod(o, returntype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
                else if (specname == atMostOnceActivityPrefix)
                {
                    var returntype = spec.GenericTypeArguments[0];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineAtMostOnceActivity));
                    var mg = m.MakeGenericMethod(o, returntype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
                else if (specname == updatePrefix)
                {
                    var statetype = spec.GenericTypeArguments[0];
                    var returntype = spec.GenericTypeArguments[1];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineUpdate));
                    var mg = m.MakeGenericMethod(statetype, o, returntype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
                else if (specname == readPrefix)
                {
                    var statetype = spec.GenericTypeArguments[0];
                    var returntype = spec.GenericTypeArguments[1];
                    var m = typeof(IServiceBuilder).GetMethod(nameof(IServiceBuilder.DefineRead));
                    var mg = m.MakeGenericMethod(statetype, o, returntype);
                    mg.Invoke(serviceBuilder, new object[0]);
                }
            }
        }

        public static Action<IPlacementBuilder> DefaultPlacement<TOperation>(IEnumerable<Type> affinities)
          where TOperation : INonAffineOperation
        {
            Type singleAffinity = null;
            foreach(var a in affinities)
                if (a.IsAssignableFrom(typeof(TOperation)))
                {
                    if (singleAffinity != null)
                    {
                        singleAffinity = null;
                        break;
                    }
                    else
                    {
                        singleAffinity = a;
                    }
                }

            if (singleAffinity == null)
            {
                return (p) => p.PlaceOnCaller<TOperation>();
            }
            else
            {
                var m = typeof(IPlacementBuilder).GetMethod(nameof(IPlacementBuilder.PlaceByAffinity));
                var mg = m.MakeGenericMethod(typeof(TOperation), singleAffinity);
                return (b) => mg.Invoke(b, new object[0]);
            }
        }
    }
}
