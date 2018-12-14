// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal interface IStateInfo : ISaveable, ISchemaElement
    {
        IEnumerable<Type> SerializableTypes();

        void ProcessSubscriptions();

        RequestMessage CreateLocalMessage(object local, ulong parent, bool fork);

        IAffinityInfo AffinityInfo { get; }

        void RegisterOperation<TRequest,TReturn>(bool isRead);

        IPartitionKey GetPartitionKeyForLocalOperation(object localOperation);

        void DefineExtensions(IServiceBuilder serviceBuilder);

    }

    internal interface IStateInfoWithKey<TKey>
    {
        IStateInstance GetInstance(TKey key);
    }

    internal interface IStateInfo<TState>
    {
        void Restore(object keyValue, TState state);

    }

    internal class StateInfo<TState, TAffinity, TKey> : SchemaElement<TState>, IStateInfo, IStateInfo<TState>, IStateInfoWithKey<TKey>
       where TState : IState<TAffinity>, new()
       where TAffinity : IAffinitySpec<TAffinity>
    {
        private readonly Process process;
        private readonly Dictionary<TKey, StateContext<TState, TAffinity,TKey>> states = new Dictionary<TKey, StateContext<TState, TAffinity, TKey>>();       
        private readonly AffinityInfo<TAffinity,TKey> keyInfo;
        private readonly List<IOperationInfo> operations = new List<IOperationInfo>();

        public StateInfo(Process process, AffinityInfo<TAffinity, TKey> keyInfo)
        {
            this.process = process;
            process.States[typeof(TState)] = this;
            this.keyInfo = keyInfo;
            ProcessSubscriptions();
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TState);
            yield return typeof(StateState<TState>);
            yield return typeof(ForkLocal<TState>);
            yield return typeof(RequestLocal<TState>);
            yield return typeof(RespondToLocal);
            foreach (var o in operations)
                foreach (var t in o.SerializableTypes())
                    yield return t;
        }

        public override bool AllowVersionReplace => true;


        public void SaveStateTo(Snapshot s)
        {
            foreach (var kvp in states)
            {
                s.StatePieces.Add(new StateState<TState>()
                {
                    KeyValue = kvp.Key,
                    State = kvp.Value.State,
                });
            }
        }

        public void ClearState()
        {
            states.Clear();
        }

        public StateContext<TState, TAffinity, TKey> GetInstance(TKey key)
        {
            if (!states.TryGetValue(key, out var instance))
            {
                states[key] = instance = CreateStateInstance(key);
            }
            return instance;
        }

        IStateInstance IStateInfoWithKey<TKey>.GetInstance(TKey key)
        {
            return GetInstance(key);
        }

        public StateContext<TState, TAffinity, TKey> CreateStateInstance(TKey key)
        {
            return new StateContext<TState, TAffinity, TKey>(process, key);
        }


        public IAffinityInfo AffinityInfo => keyInfo;

        public void ProcessSubscriptions()
        {
            // use reflection to find the interfaces, check the types, and subscribe
            var singletonSubscribeName = typeof(ISubscribe<,>).Name;
            var partitionedSubscribeName = typeof(ISubscribe<,,>).Name;
            foreach (var i in typeof(TState).GetInterfaces())
                if (i.Name == singletonSubscribeName || i.Name == partitionedSubscribeName)
                {
                    var eventType = i.GenericTypeArguments[0];
                    if (!process.Events.TryGetValue(eventType, out var eventInfo))
                    {
                        throw new BuilderException($"invalid subscription on state {typeof(TState).Name}: could not find event {eventType.Name}");
                    }
                    var affinityType = i.GenericTypeArguments[1];
                    if (affinityType != typeof(TAffinity))
                    {
                        throw new BuilderException($"invalid subscription on state {typeof(TState).Name}: must use affinity {typeof(TAffinity).Name}");
                    }
                    if (i.Name == singletonSubscribeName)
                    {
                        if (!keyInfo.Singleton)
                            throw new BuilderException($"invalid subscription on state {typeof(TState).Name}: must use {partitionedSubscribeName} with 3 generic parameters");
                    }
                    else if (i.Name == partitionedSubscribeName)
                    {
                        if (keyInfo.Singleton)
                            throw new BuilderException($"invalid subscription on state {typeof(TState).Name}: must use {singletonSubscribeName} with 2 generic parameters");         
                        var keytype = i.GenericTypeArguments[2];
                        if (keytype != typeof(TKey))
                        {
                            throw new BuilderException($"invalid subscription on state {typeof(TState).Name}: must use key type {keytype.Name} for affinity {affinityType.Name}");
                        }
                    }
                    eventInfo.Subscribe(this);
                }
        }

        public IPartitionKey GetPartitionKeyForLocalOperation(object localOperation)
        {
            return new PartitionKey<TKey>()
            {
                Index = keyInfo.Index,
                Key = keyInfo.GetAffinityKey(localOperation),
                Comparator = keyInfo.Comparator
            };
        }

        public void Restore(object keyValue, TState state)
        {
            var instance = GetInstance((TKey) keyValue);
            instance.Restore(state);
        }

        public RequestMessage CreateForkLocalMessage(object op, ulong parent)
        {
            return new ForkLocal<TState>()
            {
                LocalOperation = op,
                Parent = parent
            };
        }

        public RequestMessage CreateLocalMessage(object op, ulong parent, bool fork)
        {
            if (fork)
            {
                return new ForkLocal<TState>()
                {
                    LocalOperation = op,
                    Parent = parent
                };
            }
            else
            {
                return new RequestLocal<TState>()
                {
                    LocalOperation = op,
                    Parent = parent
                };
            }
        }

        public void RegisterOperation<TRequest, TReturn>(bool isRead)
        {
            operations.Add(new OperationInfo<TRequest,TReturn>()
            {
                IsRead = isRead
            });
        }

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            foreach (var op in operations)
                op.DefineExtensions(serviceBuilder);
        }

        public interface IOperationInfo
        {
            IEnumerable<Type> SerializableTypes();

            void DefineExtensions(IServiceBuilder serviceBuilder);
        }

        public class OperationInfo<TRequest, TReturn> : IOperationInfo
        {
            public bool IsRead;

            public IEnumerable<Type> SerializableTypes()
            {
                yield return typeof(TRequest);
                yield return typeof(TReturn);
            }

            public void DefineExtensions(IServiceBuilder serviceBuilder)
            {
                Extensions.Register.DefineUpdateExtensions<TState, TRequest, TReturn>(IsRead, serviceBuilder);
            }
        }      
    }
}
