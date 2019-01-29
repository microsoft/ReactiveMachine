// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Harness
{
    [DataContract]
    public class GeneratorIteration : 
        IUpdate<GeneratorState, UnitType>, 
        IGeneratorAffinity
    {
        [DataMember]
        public uint GeneratorNumber { get; set; }

        [DataMember]
        public uint Iteration { get; set; }

        [DataMember]
        public TimeSpan DelayBetweenRequests;

        [DataMember]
        public IWorkloadGenerator Workload;

        [CreateIfNotExists]
        public UnitType Execute(IUpdateContext<GeneratorState> context)
        {
            if (!context.State.ExperimentIsFinished)
            {
                var thingToRun = Workload.GenerateRequest(GeneratorNumber, Iteration);

                context.Logger.LogTrace($"Harness.Generator{GeneratorNumber} issues {thingToRun} ");

                // fork the payload orchestration
                context.ForkOrchestration(thingToRun);

                // schedule next iteration
                context.ScheduleLocalUpdate(DelayBetweenRequests, 
                    new GeneratorIteration()
                    {
                        GeneratorNumber = GeneratorNumber,
                        Iteration = Iteration + 1,
                        DelayBetweenRequests = DelayBetweenRequests,
                        Workload = Workload
                    });
            }
            else
            {
                context.Logger.LogTrace($"Harness.Generator{GeneratorNumber} is done ");
            }

            return UnitType.Value;
        }
    }
}
