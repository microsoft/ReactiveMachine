// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [DataMember]
        public Dictionary<ulong, QueuedMessage> Requests;

        [IgnoreDataMember]
        internal override string LabelForTelemetry => "Acquire";

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

        internal override void Enter<TKey>(Process process, TKey localkey, Stopwatch stopwatch, out bool exitImmediately)
        {
            var timestamp = process.NextOpid;

            // if we are not the last partition to lock send next lock message
            if (Position < LockSet.Count - 1)
            {
                var nextReq = NextMessage(timestamp);
                var destination = nextReq.LockSet[nextReq.Position].Locate(process);
                process.Send(destination, nextReq);
            }

            // if we are the last partition to lock return ack to orchestration
            else
            {
                process.Send(process.GetOrigin(Opid), new GrantLock()
                {
                    Opid = Opid,
                    Parent = Parent,
                    Clock = timestamp
                });
            }

            exitImmediately = false; // stays in the lock        
        }

        internal void Add(QueuedMessage request)
        {
            if (Requests == null)
                Requests = new Dictionary<ulong, QueuedMessage>();
            Requests.Add(request.Opid, request);
        }

        internal override void Update<TKey>(Process process, TKey localkey, ProtocolMessage protocolMessage, Stopwatch stopwatch, out bool exiting)
        {
            if (protocolMessage.OriginalOpid == this.Opid)
            {
                exiting = true;
            }
            else
            {
                exiting = false;
                Requests[protocolMessage.OriginalOpid].Update(process, localkey, protocolMessage, stopwatch, out var innerIsExiting);
                if (innerIsExiting)
                    Requests.Remove(protocolMessage.OriginalOpid);
            }
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

     }

    [DataContract]
    internal class ReleaseLock : ProtocolMessage
    {
        internal override MessageType MessageType => MessageType.ReleaseLock;

        public override string ToString()
        {
            return $"{base.ToString()} ReleaseLock";
        }
    }



}
