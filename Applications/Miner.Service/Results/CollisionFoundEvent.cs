// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveMachine;

namespace Miner.Service
{
    public class CollisionFoundEvent :
        IEvent,
        IJobAffinity
    {
        public long Collision;
    }
}
