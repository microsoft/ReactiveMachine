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
    public class SearchJob : 
        IOrchestration<UnitType>, 
        IJobAffinity
    {
        [DataMember]
        public int Target;

        [DataMember]
        public long Start;

        [DataMember]
        public long Count;

        [DataMember]
        public uint NumberWorkers;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            Console.WriteLine($"Starting job for [{Start},{Start + Count})");

            // divide the search space evenly among workers
            var portionSize = ((ulong)Count + (NumberWorkers - 1)) / NumberWorkers;

            // create one task per worker
            var workertasks = new List<Task>();
            var pos = Start;
            for (uint i = 0; i < NumberWorkers; i++)
            {
                var nextportionsize = Math.Min((long)portionSize, (Start + Count) - pos);
                workertasks.Add(context.PerformOrchestration(new SearchWorker()
                {
                    Target = Target,
                    Start = pos,
                    Count = nextportionsize,
                    WorkerNumber = i
                }));
                pos += nextportionsize;
            }

            await Task.WhenAll(workertasks);

            // read the results
            var collisions = await context.PerformRead(new GetResults());

            collisions.Sort();

            Console.WriteLine($"Finished job for [{Start},{Start + Count}), {collisions.Count()} collisions found: {string.Join(", ", collisions)}");

            return UnitType.Value;
        }


    }
}
