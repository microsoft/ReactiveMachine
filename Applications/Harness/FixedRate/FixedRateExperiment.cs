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
    public class FixedRateExperiment : IOrchestration<UnitType>
    {
        [DataMember]
        public IWorkloadGenerator Workload;

        [DataMember]
        public TimeSpan Duration;

        [DataMember]
        public TimeSpan Cooldown;

        [DataMember]
        public double Rate;

        [DataMember]
        public uint NumberGenerators;

        [DataMember]
        public uint NumberGeneratorProcesses;


        public async Task<UnitType> Execute(IOrchestrationContext context)
        {

            await context.PerformOrchestration(new Initialization()
            {
                NumberGenerators = NumberGenerators,
                NumberGeneratorProcesses = NumberGeneratorProcesses,
                Workload = Workload
            });
         
            for (uint i = 0; i < NumberGenerators; i++)
            {
                // schedule events to stop load generation after the test duration
                context.ScheduleLocalUpdate(Duration, new StopGenerator()
                {
                    GeneratorNumber = i
                });
            }

            var DelayBetweenRequests = TimeSpan.FromSeconds(NumberGenerators / Rate);

            context.Logger.LogInformation($"Starting workload.");

            for (uint i = 0; i < NumberGenerators; i++)
            {
                var stagger = TimeSpan.FromSeconds(i / Rate);
                context.ScheduleLocalUpdate(stagger, new GeneratorIteration()
                {
                    GeneratorNumber = i,
                    Iteration = 0,
                    DelayBetweenRequests = DelayBetweenRequests,
                    Workload = Workload,
                });
            }

            await context.DelayBy(Duration);

            context.Logger.LogInformation($"Entering Cooldown.");

            await context.DelayBy(Cooldown);

            context.Logger.LogInformation($"Experiment complete.");

            context.GlobalShutdown();

            return UnitType.Value;
        }
    }
}
