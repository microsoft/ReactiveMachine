// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
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
    public class SearchPortion : IAtLeastOnceActivity<List<long>>
    {
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);


        [DataMember]
        public int Target;

        [DataMember]
        public long Start;

        [DataMember]
        public long Count;


        public Task<List<long>> Execute(IContext context)
        {
            context.Logger.LogInformation($"Starting portion [{Start},{Start + Count})");

            var results = new List<long>();
            // search given range for a hash collision
            for (var i = Start; i < Start + Count; i++)
            {
                if (i.GetHashCode() == Target)
                    results.Add(i);
            }

            context.Logger.LogInformation($"Finished portion [{Start},{Start + Count})");

            return Task.FromResult(results);
        }
    }
}
