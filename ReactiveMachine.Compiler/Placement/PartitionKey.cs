// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal interface IPartitionKey : IEquatable<IPartitionKey>
    {
        IPartitionEffect MakePartitionEffect(IEnumerable<IStateInfo> States);

        int Index { get; }

        uint Locate(Process process);
    }

    [DataContract]
    internal struct PartitionKey<TKey> : IPartitionKey, IComparable<PartitionKey<TKey>>, IComparable
    {
        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public TKey Key;

        // we need not restore this, as it is only used by events and locked orchestrations
        // that sort partition keys; and those are re-creating the keys when loaded back from storage
        [IgnoreDataMember]
        public Func<TKey, TKey, int> Comparator;

        public override string ToString()
        {
            return $"{Index}:{Key}";
        }

        public IAffinityInfo GetKeyInfo(Process process)
        {
            return process.AffinityIndex[Index];
        }

        public IPartitionEffect MakePartitionEffect(IEnumerable<IStateInfo> States)
        {
            return new PartitionEffect<TKey>()
            {
                Key = this,
                States = States
            };
        }

        public uint Locate(Process process)
        {
            return ((IAffinityInfoByKeyType<TKey>)process.AffinityIndex[Index]).LocateKey(Key);
        }

        public int CompareTo(PartitionKey<TKey> other)
        {
            int x = Index.CompareTo(other.Index);
            if (x == 0) x = Comparator(Key, other.Key);
            return x;
        }

        public int CompareTo(object obj)
        {
            int x = Index.CompareTo(((IPartitionKey)obj).Index);
            if (x == 0) x = CompareTo((PartitionKey<TKey>)obj);
            return x;
        }

        public bool Equals(IPartitionKey other)
        {
            return this.Index == other.Index
                && this.Key.Equals(((PartitionKey<TKey>)other).Key);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode() * 777 ^ Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is IPartitionKey other) && Equals(other);
        }
    }

}
