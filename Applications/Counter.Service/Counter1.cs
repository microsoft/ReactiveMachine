// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Counter.Service
{

 

    [DataContract]
    public class Counter1 :
        IPartitionedState<ICounterAffinity, uint>,
        ISubscribe<IncrementEvent, ICounterAffinity, uint>
    {
        [DataMember]
        public int Count;

        public void On(ISubscriptionContext<uint> context, IncrementEvent evt)
        {
            Count++;
        }
    }


    [DataContract]
    public class IncrementEvent :
       IEvent,
       ICounterAffinity
    {

        [DataMember]
        public uint CounterId { get; set; }

    }

}
