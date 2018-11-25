// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Miner.Service
{
    [DataContract]
    public class ResultsState : 
        ISingletonState<IJobAffinity>, 
        IJobAffinity, 
        ISubscribe<CollisionFoundEvent, IJobAffinity>
    {
        [DataMember]
        public List<long> Collisions = new List<long>();

        public void On(ISubscriptionContext context, CollisionFoundEvent e)
        {
            Collisions.Add(e.Collision);
        }

    }
}
