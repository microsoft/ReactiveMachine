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
    public class Counter2 :
        IPartitionedState<ICounterAffinity,uint>
    {
        [DataMember]
        public int Count;
    }

 
    [DataContract]
    public class IncrementUpdate :
       IUpdate<Counter2, UnitType>,
       ICounterAffinity
    {
        [DataMember]
        public uint CounterId { get; set; }

        public UnitType Execute(IUpdateContext<Counter2> context)
        {
            context.State.Count++;
            return UnitType.Value;
        }
    }

}
