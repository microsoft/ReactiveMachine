// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal interface IEventInfo : ISchemaElement
    {
        IEnumerable<Type> SerializableTypes();

        void Subscribe(IStateInfo subscriber);

        List<IPartitionEffect> GetEffects(object evt);

        RequestMessage CreateMessage(bool fork, IEvent evt, int position);
    }


    internal class EventInfo<TEvent> : SchemaElement<TEvent>, IEventInfo
        where TEvent : IEvent
    {
        private readonly Process process;
        private readonly List<IStateInfo> subscribers = new List<IStateInfo>();

        public EventInfo(Process process)
        {
            this.process = process;
            process.Events[typeof(TEvent)] = this;
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TEvent);
        }

        public override bool AllowVersionReplace => false;

        public void Subscribe(IStateInfo subscriber)
        {
            subscribers.Add(subscriber);
        }

        public List<IPartitionEffect> GetEffects(object evt)
        {
            return GetKeys(evt)
                .GroupBy(kvp => kvp.Key)
                .OrderBy(g => g.Key)
                .Select(g => g.Key.MakePartitionEffect(g.Select(kvp => kvp.Value)))
                .ToList();
        }

        public IEnumerable<KeyValuePair<IPartitionKey, IStateInfo>> GetKeys(object evt)
        {
            foreach (var s in subscribers)
                foreach (var pk in s.AffinityInfo.GetAffinityKeys(evt))
                    yield return new KeyValuePair<IPartitionKey, IStateInfo>(pk, s);
        }

        public RequestMessage CreateMessage(bool fork, IEvent evt, int position)
        {
            if (fork)
                return new ForkEvent()
                {
                    Event = evt,
                    Position = position
                };
            else
                return new PerformEvent()
                {
                    Event = evt,
                    Position = position
                };

        }
    }
}