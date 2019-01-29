// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// A specification that determines how to place affinites and operations
    /// on a cluster with a given number of processes. A placement builder can be used to override 
    /// the automatic default placement.
    /// </summary>
    public interface IPlacementBuilder
    {
        uint NumberProcesses { get;  }

        // affinities can be placed on a single process, or a range of processes
        // by default:
        // - singleton affinities are placed on process 0
        // - partitioned affinities are placed on range [0 .. NumberProcesses)

        IPlacementBuilder PlaceOnProcess<TAffinity>(uint process)
            where TAffinity : IAffinitySpec<TAffinity>;

        IPlacementBuilder PlaceOnSubrange<TAffinity>(uint firstProcess, uint numberProcesses)
            where TAffinity : IAffinitySpec<TAffinity>;

        // partitioned affinities can be placed using a hash, or using an index range
        // by default:
        // - affinities marked with [RoundRobinPlacement] are placed by index, with chunksize 1
        // - all others are placed using a jump-consistent hash

        IPlacementBuilder PlaceByJumpConsistentHash<TAffinity, TKey>()
            where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        IPlacementBuilder PlaceByJumpConsistentHash<TAffinity, TKey>(Func<TKey, ulong> preHash)
            where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        IPlacementBuilder PlaceByIndex<TAffinity, TKey>(Func<TKey, uint> index, uint chunksize)
            where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        IPlacementBuilder PlaceByIndex<TAffinity, TKey>(uint chunksize)
            where TAffinity : IPartitionedAffinity<TAffinity, TKey>;

        // orchestrations, activities, and transactions can be placed on affinities they implement
        // by default:
        // - an operation that has exactly one affinity is placed according to that affinity
        // - all other operations are placed on the caller's process

        IPlacementBuilder PlaceByAffinity<TOperation, TAffinity>()
            where TOperation : INonAffineOperation, TAffinity
            where TAffinity : IAffinitySpec<TAffinity>;

        IPlacementBuilder PlaceOnCaller<TOperation>()
            where TOperation : INonAffineOperation;

        IPlacementBuilder PlaceRandomly<TOperation>()
            where TOperation : INonAffineOperation;
    }
}
