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
    public class GetResults : 
        IRead<ResultsState, List<long>>,
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


        public List<long> Execute(IReadContext<ResultsState> context)
        {            
            return context.State.Collisions;
        }
    }
}
