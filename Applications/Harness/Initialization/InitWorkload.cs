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
    /// <summary>
    /// Performs the initialization operation, with generator affinity. 
    /// </summary> 
    [DataContract]
    public class InitWorkload :
        IOrchestration<UnitType>,
        IGeneratorAffinity
    {
        [DataMember]
        public uint GeneratorNumber { get; set; }

        [DataMember]
        public IWorkloadGenerator Workload { get; set; }

        public Task<UnitType> Execute(IOrchestrationContext context)
        {        
            var toRun = Workload.GenerateInitialization(GeneratorNumber);

            return context.PerformOrchestration(toRun);
        }
    }
}
