// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Harness
{
    [DataContract]
    public class LoadLoopsExperiment : IOrchestration<UnitType>
    {
        [DataMember]
        public IWorkloadGenerator Workload;

        [DataMember]
        public uint NumberGenerators;

        [DataMember]
        public uint NumberRequests;

        [DataMember]
        public uint NumberProcesses;

        [DataMember]
        public TimeSpan Stagger;


        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.PerformOrchestration(new Initialization()
            {
                NumberGenerators = NumberGenerators,
                NumberGeneratorProcesses = NumberProcesses,
                Workload = Workload
            });

            var time = await context.ReadDateTimeUtcNow();

            context.Logger.LogInformation($"Starting workload.");

            var tasks = new List<Task>();
            for (uint i = 0; i < NumberGenerators; i++)
            {
                tasks.Add(context.PerformOrchestration(new RequestSequence()
                {
                    GeneratorNumber = i,
                    NumberRequests = (NumberRequests + NumberGenerators - 1) / NumberGenerators,
                    OperationFactory = Workload,
                    StartTime = time + TimeSpan.FromSeconds(Stagger.TotalSeconds * i/ NumberGenerators),
                }));
            }
            await Task.WhenAll(tasks);

            context.Logger.LogInformation($"Experiment complete.");

            context.GlobalShutdown();

            return UnitType.Value;
        }
    }
}
