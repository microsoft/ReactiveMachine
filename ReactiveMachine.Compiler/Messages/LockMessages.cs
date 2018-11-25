// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class AcquireLock : QueuedMessage
    {
        [DataMember]
        public List<IPartitionKey> LockSet;

        [DataMember]
        public int Position;

        [IgnoreDataMember]
        internal override object Payload => null;

        public AcquireLock NextMessage(ulong timestamp)
        {
            return new AcquireLock()
            { Clock = timestamp, LockSet = LockSet, Opid = Opid, Parent = Parent, Position = Position + 1 };
        }

        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.AcquireLock;

        internal override IPartitionKey GetPartitionKey(Process process)
        {
            return LockSet[Position];
        }

        internal override void Apply(Process process)
        {
            var affinityInfo = process.AffinityIndex[LockSet[Position].Index];
            affinityInfo.PartitionLock.EnterLock(this);
        }

        internal override object Execute<TKey>(Process process, ulong opid)
        {
            return null;
        }

        public override string ToString()
        {
            return $"{base.ToString()} AcquireLock<{Position}>";
        }
    }


    [DataContract]
    internal class GrantLock : ResponseMessage
    {
        internal override MessageType MessageType => MessageType.GrantLock;

        public override string ToString()
        {
            return $"{base.ToString()} GrantLock";
        }

        internal override void Apply(Process process)
        {
            process.OrchestrationStates[Parent].Continue(Opid, Clock, MessageType.GrantLock, UnitType.Value);
        }
    }

    [DataContract]
    internal class ReleaseLock : Message
    {
        internal override MessageType MessageType => MessageType.ReleaseLock;

        [DataMember]
        public IPartitionKey Key;

        [DataMember]
        public ulong LockOpid;

        internal override void Apply(Process process)
        {
            var PartitionLock = process.AffinityIndex[Key.Index].PartitionLock;
            PartitionLock.ExitLock(Key, LockOpid);
        }
        public override string ToString()
        {
            return $"{base.ToString()} ReleaseLock";
        }
    }



}
