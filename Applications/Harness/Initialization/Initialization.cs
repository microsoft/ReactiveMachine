// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Harness
{
    [DataContract]
    public class Initialization : IOrchestration<UnitType>
    {
        [DataMember]
        public IWorkloadGenerator Workload;

        [DataMember]
        public uint NumberGeneratorProcesses;

        [DataMember]
        public uint NumberGenerators;


        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            // do an initial ping to all processes to make sure they are running
            var tasks = new List<Task>();
            for (uint i = 0; i < NumberGeneratorProcesses; i++)
            {
                tasks.Add(context.PerformOrchestration(new InitialPing()
                {
                    ProcessId = i,
                }));
            }
            await Task.WhenAll(tasks);


            // initialize workload
            tasks = new List<Task>();
            for (uint i = 0; i < NumberGenerators; i++)
            {
                tasks.Add(context.PerformOrchestration(new InitWorkload()
                {
                    GeneratorNumber = i,
                    Workload = Workload
                }));
            }
            await Task.WhenAll(tasks);

            return UnitType.Value;
        }
    }
}
