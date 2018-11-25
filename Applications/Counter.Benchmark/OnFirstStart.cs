// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Counter.Benchmark
{
    [DataContract]
    public class OnFirstStart : IStartupOrchestration
    {
        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            var c = context.GetConfiguration<CounterBenchmarkConfiguration>();

            var workload = new Workload()
            {
                TotalNumberOfCounters = c.NumberCounters,
                Implementation = c.Implementation,
            };

            if (c.NumberRequests == 0)
            {
                return context.PerformOrchestration(new Harness.FixedRateExperiment()
                {
                    NumberGeneratorProcesses = c.NumberGeneratorProcesses,
                    NumberGenerators = c.NumberGenerators,
                    Workload = workload,

                    Duration = c.Duration,
                    Cooldown = c.Cooldown,
                    Rate = c.Rate,
                 });
            }
            else
            {
                return context.PerformOrchestration(new Harness.LoadLoopsExperiment()
                {
                    NumberProcesses = c.NumberGeneratorProcesses,
                    NumberGenerators = c.NumberGenerators,
                    Workload = workload,


                    NumberRequests = c.NumberRequests,
                    Stagger = c.Stagger,
                });
            }

        }
    }
}
