// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miner.Service
{
    // this affinity is used to spread the workers over different processes
    public interface IWorkerAffinity : 
        IPartitionedAffinity<IWorkerAffinity,uint>
    {
        [RoundRobinPlacement]
        uint WorkerNumber { get; }
    }

}
