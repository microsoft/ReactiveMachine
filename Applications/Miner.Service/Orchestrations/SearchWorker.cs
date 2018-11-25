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
    /// <summary>
    /// Operation that searches a given range for collisions. Executed by a worker.
    /// Breaks the range into smaller search portions that are executed as activities.
    /// </summary>
    [DataContract]
    public class SearchWorker : IOrchestration<UnitType>, IWorkerAffinity
    {
        [DataMember]
        public int Target;

        [DataMember]
        public long Start;

        [DataMember]
        public long Count;

        [DataMember]
        public uint WorkerNumber { get; set; }

        const int PortionSize = 10000000;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            Console.WriteLine($"Starting worker for [{Start},{Start+Count})");

            var pos = Start;
            while (pos < Start + Count)
            {
                var nextportionsize = Math.Min(PortionSize, (Start + Count) - pos);
                var results = await context.PerformActivity(new SearchPortion()
                {
                    Target = Target,
                    Start = pos,
                    Count = nextportionsize,
                });
                foreach (var c in results)
                {
                    context.ForkEvent(new CollisionFoundEvent() { Collision = c });
                }
                pos += nextportionsize;
            }

            await context.Finish();

            Console.WriteLine($"Worker finished [{Start},{Start + Count})");

            return UnitType.Value;
        }
    }
}
