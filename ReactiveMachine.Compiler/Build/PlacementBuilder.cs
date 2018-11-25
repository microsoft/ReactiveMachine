// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal class PlacementBuilder : IPlacementBuilder
    {
        private readonly Process process;

        public uint NumberProcesses => process.NumberProcesses;

        public PlacementBuilder(Process process, uint NumberProcesses)
        {
            this.process = process;
            process.NumberProcesses = NumberProcesses;
        }

        private static void CheckProcessNumber(Process process, uint processId)
        {
            if (processId >= process.NumberProcesses)
            {
                throw new BuilderException($"invalid process id");
            }
        }
        private static void CheckSubrange(Process process, uint firstProcess, uint numberProcesses)
        {
            if (numberProcesses == 0)
            {
                throw new BuilderException($"empty process subrange");
            }
            if (firstProcess + numberProcesses > process.NumberProcesses)
            {
                throw new BuilderException($"invalid process subrange");
            }
        }

        // range

        IPlacementBuilder IPlacementBuilder.PlaceOnProcess<TAffinity>(uint processId)
        {
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
            }
            keyInfo.SetPlacementRange(processId, 1);
            return this;
        }

        IPlacementBuilder IPlacementBuilder.PlaceOnSubrange<TAffinity>(uint firstProcess, uint numberProcesses)
        {
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
            }
            keyInfo.SetPlacementRange(firstProcess, numberProcesses);
            return this;
        }

        // placement

        IPlacementBuilder IPlacementBuilder.PlaceByJumpConsistentHash<TAffinity, TKey>()
        {
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
            }
            ((PartitionedAffinityInfo<TAffinity, TKey>)keyInfo).SetPlacementFunction(
                (Func<TKey, uint, uint>)
                KeyFunctions.GetJumpConsistentHashFor<TKey>(process.Serializer));
            return this;
        }

        IPlacementBuilder IPlacementBuilder.PlaceByJumpConsistentHash<TAffinity, TKey>(Func<TKey, ulong> preHash)
        {
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
            }
            ((PartitionedAffinityInfo<TAffinity, TKey>)keyInfo).SetPlacementFunction(
                new Func<TKey, uint, uint>
                ((key, n) => JumpConsistentHash.Compute(preHash(key), n)));
            return this;
        }

        IPlacementBuilder IPlacementBuilder.PlaceByIndex<TAffinity, TKey>(uint chunksize)
        {
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
            }
             ((PartitionedAffinityInfo<TAffinity, TKey>)keyInfo).SetPlacementFunction(
                 (Func<TKey, uint, uint>)
                 KeyFunctions.GetRoundRobinFor<TKey>(chunksize));
            return this;
        }

        IPlacementBuilder IPlacementBuilder.PlaceByIndex<TAffinity, TKey>(Func<TKey, uint> index, uint chunksize)
        {
            if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
            {
                throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
            }
            ((PartitionedAffinityInfo<TAffinity,TKey>) keyInfo).SetPlacementFunction(
                new Func<TKey, uint, uint>
                ((x,n) => (index(x) / chunksize) % n));
            return this;
        }

        // operations

        IPlacementBuilder IPlacementBuilder.PlaceByAffinity<TOperation, TAffinity>()
        {
            if (typeof(IOrchestration).IsAssignableFrom(typeof(TOperation)))
            {
                if (!process.Orchestrations.TryGetValue(typeof(TOperation), out var orchestrationInfo))
                {
                    throw new BuilderException($"undefined orchestration {typeof(TOperation).FullName}");
                }
                if (!process.Affinities.TryGetValue(typeof(TAffinity), out var affinityInfo))
                {
                    throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
                }
                orchestrationInfo.PlacementAffinity = affinityInfo;
            }
            else if (typeof(IActivity).IsAssignableFrom(typeof(TOperation)))
            {
                if (!process.Activities.TryGetValue(typeof(TOperation), out var activityInfo))
                {
                    throw new BuilderException($"undefined activity {typeof(TOperation).FullName}");
                }
                if (!process.Affinities.TryGetValue(typeof(TAffinity), out var keyInfo))
                {
                    throw new BuilderException($"undefined key {typeof(TAffinity).FullName}");
                }
                activityInfo.AffinitizationKey = keyInfo;
            }
            else
                throw new NotImplementedException();
            return this;
        }

        IPlacementBuilder IPlacementBuilder.PlaceOnCaller<TOperation>()
        {
            if (typeof(IOrchestration).IsAssignableFrom(typeof(TOperation)))
            {
                if (!process.Orchestrations.TryGetValue(typeof(TOperation), out var orchestrationInfo))
                {
                    throw new BuilderException($"undefined orchestration {typeof(TOperation).FullName}");
                }
               orchestrationInfo.PlacementAffinity = null;
            }
            else if (typeof(IActivity).IsAssignableFrom(typeof(TOperation)))
            {
                if (!process.Activities.TryGetValue(typeof(TOperation), out var activityInfo))
                {
                    throw new BuilderException($"undefined activity {typeof(TOperation).FullName}");
                }
                activityInfo.AffinitizationKey = null;
            }
            else
                throw new NotImplementedException();
            return this;
        }
    }
}