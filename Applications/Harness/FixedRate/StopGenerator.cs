// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Harness
{
    [DataContract]
    public class StopGenerator :
        IUpdate<GeneratorState, UnitType>,
        IGeneratorAffinity
    {
        [DataMember]
        public uint GeneratorNumber { get; set; }

        [CreateIfNotExists]
        public UnitType Execute(IUpdateContext<GeneratorState> context)
        {
            context.State.ExperimentIsFinished = true;
      
            return UnitType.Value;
        }
    }
}
