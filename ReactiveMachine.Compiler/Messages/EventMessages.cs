// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class PerformEvent : QueuedMessage
    {
        [DataMember]
        public IEvent Event;

        [DataMember]
        public int Position;

        [IgnoreDataMember]
        protected List<IPartitionEffect> Effects; // we cache this because it can be a bit heavy to compute

        [IgnoreDataMember]
        internal override object Payload => Event;

        public virtual PerformEvent NextMessage(ulong timestamp)
        {
            return new PerformEvent()
            { Clock = timestamp, Effects = Effects, Event = Event, Opid = Opid, Parent = Parent, Position = Position + 1 };
        }

        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.PerformEvent;

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

        internal override void Apply(Process process)
        {
            var affinityInfo = process.AffinityIndex[GetCurrent(process).UntypedKey.Index];
            affinityInfo.PartitionLock.EnterLock(this);
        }

        internal override object Execute<TKey>(Process process, ulong opid)
        {
            var peff = (PartitionEffect<TKey>) GetCurrent(process);
            foreach (var s in peff.AffectedStates)
                ((IStateInfoWithKey<TKey>)s).GetInstance(peff.Key.Key).Execute(Event, opid, true);
            return null;
        }

        public override string ToString()
        {
            return $"{base.ToString()} PerformEvent({Position})";
        }
    }

    [DataContract]
    internal class ForkEvent : PerformEvent
    {
        public override string ToString()
        {
            return $"{base.ToString()} ForkEvent({Position})";
        }

        internal override MessageType MessageType => MessageType.ForkEvent;

        public override PerformEvent NextMessage(ulong timestamp)
        {
            return new ForkEvent()
            { Clock = timestamp, Effects = Effects, Event = Event, Opid = Opid, Parent = Parent, Position = Position + 1 };
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

        internal override void Apply(Process process)
        {
            process.OrchestrationStates[Parent].Continue(Opid, Clock, MessageType.AckEvent, UnitType.Value);
        }
    }




}
