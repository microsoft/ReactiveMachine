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
    internal abstract class EventMessage : QueuedMessage
    {
        [DataMember]
        public IEvent Event;

        [DataMember]
        public int Position;

        [DataMember]
        public int PendingAcks;

        [IgnoreDataMember]
        protected List<IPartitionEffect> Effects; // we cache this because it can be a bit heavy to compute

        [IgnoreDataMember]
        internal override string LabelForTelemetry => Event.ToString();

        public List<IPartitionEffect> GetEffects(Process process)
        {
            return Effects ?? (Effects = process.Events[Event.GetType()].GetEffects(Event));
        }

        public IPartitionEffect GetCurrent(Process process)
        {
            return GetEffects(process)[Position];
        }

        internal override IPartitionKey GetPartitionKey(Process process)
        {
            return GetCurrent(process).UntypedKey;
        }
 
        public abstract EventMessage NextMessage(ulong timestamp);

        internal override void Apply(Process process)
        {
            var affinityInfo = process.AffinityIndex[GetCurrent(process).UntypedKey.Index];
            affinityInfo.PartitionLock.EnterLock(this);
        }

        internal override void Enter<TKey>(Process process, TKey localkey, Stopwatch stopwatch, out bool isExiting)
        {
            var effects = GetEffects(process);
            var timestamp = process.NextOpid;
            bool isCoordinator = Position == effects.Count - 1;

            // kick off any initializations if needed, counting how many acks we must wait for
            var peff = (PartitionEffect<TKey>)GetCurrent(process);
            foreach (var s in peff.AffectedStates)
                if (!((IStateInfoWithKey<TKey>)s).TryGetInstance(peff.Key.Key, out var instance, true, Opid))
                {
                    PendingAcks++;
                }

            // if we are not the last partition with an effect, forward to next
            if (! isCoordinator)
            {
                PendingAcks++;
                var nextReq = NextMessage(timestamp);
                var destination = nextReq.GetCurrent(process).Locate(process);
                process.Send(destination, nextReq);
                isExiting = false; // stays in the lock
            }
            else
            {
                // send commit messages to remote participants
                // so they can apply the effects and then exit
                for (int i = 0; i < effects.Count - 1; i++)
                {
                    var key = effects[i].UntypedKey;
                    var destination = key.Locate(process);
                    var message = new CommitEvent() { PartitionKey = key, OriginalOpid = Opid };
                    message.Clock = timestamp;
                    message.Parent = Parent;
                    message.Opid = Opid;
                    process.Send(destination, message);
                }

                // return ack to orchestration
                if (MessageType == MessageType.PerformEvent)
                {
                    process.Send(process.GetOrigin(Opid), new AckEvent()
                    { Opid = Opid, Parent = Parent, Clock = process.NextOpid });
                }

                TryCommit<TKey>(process, stopwatch, out isExiting);
            }
        }

        private void TryCommit<TKey>(Process process, Stopwatch stopwatch, out bool isExiting)
        {
            if (PendingAcks > 0)
            {
                isExiting = false;
                return;
            }

            ApplyEffects<TKey>(process);

            process.Telemetry?.OnApplicationEvent(
                processId: process.ProcessId,
                id: Opid.ToString(),
                name: LabelForTelemetry.ToString(),
                parent: Parent.ToString(),
                opSide: OperationSide.Callee,
                opType: OperationType.Event,
                duration: stopwatch.Elapsed.TotalMilliseconds
            );

            isExiting = true;
        }

        internal override void Update<TKey>(Process process, TKey localkey, ProtocolMessage protocolMessage, Stopwatch stopwatch, out bool exiting)
        {
            PendingAcks--;     
            TryCommit<TKey>(process, stopwatch, out exiting);
        }

        private void ApplyEffects<TKey>(Process process)
        {
            // apply the event to all the affected states
            var peff = (PartitionEffect<TKey>)GetCurrent(process);
            foreach (var s in peff.AffectedStates)
            {
                ((IStateInfoWithKey<TKey>)s).TryGetInstance(peff.Key.Key, out var instance, false);
                instance.Execute(Event, Opid, StateOperation.Event);
            }
        }
    }

    [DataContract]
    internal class PerformEvent : EventMessage
    {
        public override string ToString()
        {
            return $"{base.ToString()} PerformEvent({Position})";
        }

        internal override MessageType MessageType => MessageType.PerformEvent;

        public override EventMessage NextMessage(ulong timestamp)
        {
            return new PerformEvent()
            {   Clock = timestamp, Effects = Effects, Event = Event,
                LockedByCaller = LockedByCaller, PendingAcks = 0,
                Opid = Opid, Parent = Parent, Position = Position + 1 };
        }
    }

    [DataContract]
    internal class ForkEvent : EventMessage
    {
        public override string ToString()
        {
            return $"{base.ToString()} ForkEvent({Position})";
        }

        internal override MessageType MessageType => MessageType.ForkEvent;

        public override EventMessage NextMessage(ulong timestamp)
        {
            return new ForkEvent()
            { Clock = timestamp, Effects = Effects, Event = Event,
                Opid = Opid, Parent = Parent, Position = Position + 1 };
        }
    }

   
    [DataContract]
    internal class AckEvent : ResponseMessage
    {
        internal override MessageType MessageType => MessageType.AckEvent;

        public override string ToString()
        {
            return $"{base.ToString()} AckEvent";
        }
    }

    [DataContract]
    internal class CommitEvent : ProtocolMessage
    {
        internal override MessageType MessageType => MessageType.CommitEvent;

        public override string ToString()
        {
            return $"{base.ToString()} CommitEvent";
        }
    }


}
