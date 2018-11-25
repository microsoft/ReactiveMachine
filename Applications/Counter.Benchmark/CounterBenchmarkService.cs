// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Counter.Service;
using Harness;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Counter.Benchmark
{
    public class CounterBenchmarkService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            var configuration = builder.GetConfiguration<CounterBenchmarkConfiguration>();

            builder
                .BuildService<CounterService>()
                .BuildService<HarnessService>()
                .ScanThisDLL()

                .OverridePlacement(placementBuilder => placementBuilder
                    .PlaceOnSubrange<IGeneratorAffinity>(0, configuration.NumberCounterProcesses)
                    .PlaceOnSubrange<ICounterAffinity>(configuration.NumberGeneratorProcesses, configuration.NumberCounterProcesses)
                 )
             ;
        }

      
    }
}
