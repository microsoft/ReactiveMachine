// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveMachine.Compiler
{
    internal interface IStateInfo : ISaveable
    {
        IEnumerable<Type> SerializableTypes();

        void ProcessSubscriptions();

        RequestMessage CreateLocalMessage(object local, ulong parent, MessageType mtype);

        IAffinityInfo AffinityInfo { get; }

        void RegisterOperation<TRequest,TReturn>(bool isRead);

        IPartitionKey GetPartitionKeyForLocalOperation(object localOperation);

        void DefineExtensions(IServiceBuilder serviceBuilder);      
    }

    internal interface IStateInfoWithKey<TKey>
    {
        bool TryGetInstance(TKey key, out IStateInstance instance, bool createIfNotExist, ulong parent = 0);
        bool IsCreateIfNotExists(Type operationType);
    }

    internal interface IStateInfo<TState>
    {
        void Restore(object keyValue, TState state);

    }

    internal interface IStateInfoWithStateAndKey<TState, TKey>
    {
        void SetInitializationResult(TKey key, TState state);
    }

    internal class StateInfo<TState, TAffinity, TKey> : IStateInfo, IStateInfo<TState>, IStateInfoWithKey<TKey>, IStateInfoWithStateAndKey<TState, TKey>
       where TState : IState<TAffinity>, new()
       where TAffinity : IAffinitySpec<TAffinity>
    {
        private readonly Process process;
        private readonly Dictionary<TKey, StateContext<TState, TAffinity,TKey>> states = new Dictionary<TKey, StateContext<TState, TAffinity, TKey>>();       
        private readonly AffinityInfo<TAffinity,TKey> keyInfo;
        private readonly Dictionary<Type,IOperationInfo> operations = new Dictionary<Type, IOperationInfo>();

        private bool HasInitializer;

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
            yield return typeof(RequestPing<TState>);
            yield return typeof(RespondToLocal);
            foreach (var kvp in operations)
                foreach (var t in kvp.Value.SerializableTypes())
                    yield return t;
        }

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

        public void Restore(object keyValue, TState state)
        {
            var key = (TKey)keyValue;
            if (!states.TryGetValue(key, out var instance))
            {
                states[key] = instance = new StateContext<TState, TAffinity, TKey>(process, key);
            }
            instance.Restore(state);
        }

        bool IStateInfoWithKey<TKey>.TryGetInstance(TKey key, out IStateInstance instance, bool createIfNotExist, ulong parent)
        {
            if (states.TryGetValue(key, out var entry))
            {
                instance = entry;
                return true;
            }
            else
            {
                if (createIfNotExist)
                {
                    instance = states[key] = new StateContext<TState, TAffinity, TKey>(process, key);

                    if (!HasInitializer)
                    {
                        return true;
                    }
                    else
                    {
                        var initialization = new Initialization<TState, TKey>()
                        {
                            PartitionKey = keyInfo.MakePartitionKey(key),
                            Singleton = keyInfo.Singleton,
                        };

                        var orchestrationInfo =
                            (OrchestrationInfo<Initialization<TState, TKey>, UnitType>)
                            process.Orchestrations[typeof(Initialization<TState, TKey>)];
                        var clock = process.OperationCounter;
                        var orchestrationState = 
                            new OrchestrationState<Initialization<TState, TKey>, UnitType>(
                                process,
                                orchestrationInfo,
                                process.NextOpid,
                                initialization,
                                OrchestrationType.Initialize,
                                true,
                                parent,
                                clock);

                        return false;
                    }
                }
                else
                {
                    instance = null;
                    return false;
                }
            }
        }


        public IAffinityInfo AffinityInfo => keyInfo;

        public void ProcessSubscriptions()
        {
            // use reflection to find the interfaces, check the types, and subscribe
            var singletonSubscribeName = typeof(ISubscribe<,>).Name;
            var partitionedSubscribeName = typeof(ISubscribe<,,>).Name;
            var singletonInitialization = typeof(IInitialize).Name;
            var partitionedInitialization = typeof(IInitialize<>).Name;

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
                else if (i.Name == singletonInitialization || i.Name == partitionedInitialization)
                {
                    if (i.Name == singletonInitialization)
                    {
                        if (!keyInfo.Singleton)
                            throw new BuilderException($"invalid initialization interface on state {typeof(TState).Name}: must use {partitionedInitialization} with keytype parameter");
                    }
                    else if (i.Name == partitionedInitialization)
                    {
                        if (keyInfo.Singleton)
                            throw new BuilderException($"invalid initialization interface on state {typeof(TState).Name}: must use {singletonInitialization}");
                        var keytype = i.GenericTypeArguments[0];
                        if (keytype != typeof(TKey))
                        {
                            throw new BuilderException($"invalid initialization interface on state {typeof(TState).Name}: must use key type {keytype.Name}");
                        }
                    }
                    HasInitializer = true;
                }

            if (HasInitializer)
                new OrchestrationInfo<Initialization<TState, TKey>,UnitType>(process);
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

        public RequestMessage CreateForkLocalMessage(object op, ulong parent)
        {
            return new ForkLocal<TState>()
            {
                Operation = op,
                Parent = parent
            };
        }

        public RequestMessage CreateLocalMessage(object payload, ulong parent, MessageType mtype)
        {
            switch (mtype)
            {
                case MessageType.ForkLocal:
                    return new ForkLocal<TState>()
                    {
                        Operation = payload,
                        Parent = parent
                    };

                case MessageType.RequestLocal:
                    return new RequestLocal<TState>()
                    {
                        Operation = payload,
                        Parent = parent
                    };

                case MessageType.RequestPing:
                    return new RequestPing<TState>()
                    {
                        Key = (IPartitionKey) payload,
                        Parent = parent
                    };

                default: throw new Exception("unhandled case");
            }
        }

        public void RegisterOperation<TRequest, TReturn>(bool isRead)
        {
            operations.Add(typeof(TRequest), new OperationInfo<TRequest, TReturn>(isRead));
        }

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            foreach (var kvp in operations)
                kvp.Value.DefineExtensions(serviceBuilder);
        }

        public bool IsCreateIfNotExists(Type operationType)
        {
            return operations[operationType].CreateIfNotExists;
        }

        public void SetInitializationResult(TKey key, TState state)
        {
            states[key].SetInitializationResult(state);
        }

        public interface IOperationInfo
        {
            IEnumerable<Type> SerializableTypes();

            void DefineExtensions(IServiceBuilder serviceBuilder);

            bool CreateIfNotExists { get; }
        }

        public class OperationInfo<TRequest, TReturn> : IOperationInfo
        {
            public bool IsRead;
            public bool CreateIfNotExists { get; private set; }

            public OperationInfo(bool isRead)
            {
                this.IsRead = isRead;

                var method = typeof(TRequest).GetMethod("Execute");
                CreateIfNotExists = method.GetCustomAttributes(typeof(CreateIfNotExistsAttribute), false).Count() > 0;

                if (isRead && CreateIfNotExists)
                    throw new BuilderException($"The attribute {nameof(CreateIfNotExistsAttribute)} is not allowed on read operations. Consider making this an update operation instead.");
            }

            public IEnumerable<Type> SerializableTypes()
            {
                yield return typeof(TRequest);
                yield return typeof(TReturn);
            }

            public void DefineExtensions(IServiceBuilder serviceBuilder)
            {
                Extensions.Register.DefineOperationExtensions<TState, TRequest, TReturn>(IsRead, serviceBuilder);
            }
        }      
    }
}
