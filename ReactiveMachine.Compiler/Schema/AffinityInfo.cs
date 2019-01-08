// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal interface IAffinityInfo
    {
        IEnumerable<Type> SerializableTypes();

        void FinalizePlacement();

        uint LocateAffinity(object affinity);

        Type GetInterfaceType();

        bool Singleton { get; }

        IEnumerable<IPartitionKey> GetAffinityKeys(object evt);

        int Index { get; }

        IPartitionLock PartitionLock { get; set;}

        void SetPlacementRange(uint firstProcess, uint numberProcesses);
    }

 
    internal interface IAffinityInfoByKeyType<TKey> 
    {
        TKey GetAffinityKey(object obj);

        uint LocateKey(TKey key);

        PartitionKey<TKey> MakePartitionKey(TKey key);
    }

    internal abstract class AffinityInfo<TAffinity, TKey> : IAffinityInfo, IAffinityInfoByKeyType<TKey>
        where TAffinity : IAffinitySpec<TAffinity>
    {
        protected readonly Process process;

        public int Index { get; private set; }

        public uint NumberProcesses { get; private set; } = 0;

        public uint FirstProcess { get; private set; } = 0;

        public Func<TKey,TKey,int> Comparator { get; protected set; }

        public AffinityInfo(Process process)
        {
            this.process = process;
            process.Affinities[typeof(TAffinity)] = this;
            Index = process.AffinityIndex.Count;
            process.AffinityIndex.Add(this);      
        }

        public PartitionKey<TKey> MakePartitionKey(TKey key)
        {
            return new PartitionKey<TKey>()
            {
                Key = key,
                Index = Index,
                Comparator = Comparator
            };
        }

        public IPartitionLock PartitionLock { get; set; }

        public abstract bool Singleton { get; }

        public Type GetInterfaceType()
        {
            return typeof(TAffinity);
        }

        public void SetPlacementRange(uint firstProcess, uint numberProcesses)
        {
            FirstProcess = firstProcess;
            NumberProcesses = numberProcesses;
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TKey);
            yield return typeof(PartitionKey<TKey>);
        }

        public abstract TKey GetAffinityKey(object obj);

        public abstract uint LocateAffinity(object key);

      
        public abstract uint LocateKey(TKey key);

        public abstract IEnumerable<IPartitionKey> GetAffinityKeys(object evt);

        public void FinalizePlacement()
        {
            if (process.NumberProcesses == 0)
                throw new BuilderException("placement builder must specify the number of processes");

            if (NumberProcesses == 0)
            {
                NumberProcesses = process.NumberProcesses;
            }
            else if (NumberProcesses == 1)
            {
                if (FirstProcess >= process.NumberProcesses)
                {
                    throw new BuilderException($"key {typeof(TAffinity).FullName} was placed on invalid process id: {FirstProcess}");
                }
            }
            else
            {
                if (FirstProcess + NumberProcesses > process.NumberProcesses)
                {
                    throw new BuilderException($"key {typeof(TAffinity).FullName} was placed on invalid process range: [{FirstProcess},{FirstProcess + NumberProcesses})");
                }
            }
        }

    }

    internal class SingletonAffinityInfo<TAffinity> : AffinityInfo<TAffinity, UnitType>
      where TAffinity : ISingletonAffinity<TAffinity>
    {
        public SingletonAffinityInfo(Process process) : base(process)
        {
            Comparator = new Func<UnitType, UnitType, int>((a, b) => 0);
        }

        public override bool Singleton => true;

        public override IEnumerable<IPartitionKey> GetAffinityKeys(object evt)
        {
            yield return new PartitionKey<UnitType>()
            {
                Index = this.Index,
                Comparator = this.Comparator,
                Key = UnitType.Value
            };
        }

        public override UnitType GetAffinityKey(object affinity)
        {
            return UnitType.Value;
        }

        public override uint LocateAffinity(object affinity)
        {
            return FirstProcess;
        }

        public override uint LocateKey(UnitType key)
        {
            return FirstProcess;
        }

        public IStateInfo MakeStateInfo<TState>()
            where TState : IState<TAffinity>, new()
        {
            return new StateInfo<TState, TAffinity, UnitType>(process, this);
        }

    }

    internal class PartitionedAffinityInfo<TAffinity, TKey> : AffinityInfo<TAffinity, TKey>
       where TAffinity : IPartitionedAffinity<TAffinity,TKey>
    {
        public readonly Func<TAffinity, TKey> Selector;
        public bool RoundRobinAttribute { get; private set; }

        public PartitionedAffinityInfo(Process process) : base(process)
        {
            var properties = typeof(TAffinity).GetProperties();
            if (properties.Length != 1
                || properties[0].GetGetMethod().ReturnType != typeof(TKey))
            {
                throw new BuilderException($"invalid affinity {typeof(TAffinity).Name} : interface must define a single property of type {typeof(TKey).Name}");
            }
            var method = properties[0].GetGetMethod();
            RoundRobinAttribute = properties[0].GetCustomAttributes(typeof(RoundRobinPlacementAttribute), false).Count() > 0;
            Selector = (x) => (TKey)method.Invoke(x, emptyArgs);
            Comparator = (Func<TKey,TKey,int>) KeyFunctions.GetComparatorFor<TKey>();
        }
        private static object[] emptyArgs = new object[0];

        private Func<TKey, uint, uint> PlacementFunction;


        public override bool Singleton => false;

        public override IEnumerable<IPartitionKey> GetAffinityKeys(object evt)
        {
            if (evt is TAffinity a)
            {
                yield return new PartitionKey<TKey>()
                {
                    Index = this.Index,
                    Comparator = this.Comparator,
                    Key = Selector(a)
                };
            }
            else if (evt is IMultiple<TAffinity,TKey> m)
            {
                foreach (var x in m.DeclareAffinities())
                {
                    yield return new PartitionKey<TKey>()
                    {
                        Index = this.Index,
                        Comparator = this.Comparator,
                        Key = x
                    };
                }
            }
        }

        public override TKey GetAffinityKey(object obj)
        {
           return Selector((TAffinity)obj);
        }

        public override uint LocateAffinity(object affinity)
        {
            if (NumberProcesses == 1)
                return FirstProcess;
            else
                return FirstProcess + PlacementFunction(Selector((TAffinity)affinity), NumberProcesses);
        }

        public override uint LocateKey(TKey key)
        {
            if (NumberProcesses == 1)
                return FirstProcess;
            else
                return FirstProcess + PlacementFunction(key, NumberProcesses);
        }

        public void SetPlacementFunction(Func<TKey, uint, uint> placementFunction)
        { 
            PlacementFunction = placementFunction;
        }

        public IStateInfo MakeStateInfo<TState>()
            where TState : IState<TAffinity>, new()
        {
            return new StateInfo<TState, TAffinity, TKey>(process, this);
        }

    }
}
