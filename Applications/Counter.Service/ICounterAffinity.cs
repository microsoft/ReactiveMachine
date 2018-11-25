// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Counter.Service
{
    public interface ICounterAffinity :
         IPartitionedAffinity<ICounterAffinity, uint>
    {
        [RoundRobinPlacement]
        uint CounterId { get; }
    }

}
