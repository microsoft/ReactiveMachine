// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using Harness;
using RideSharing.Service;

namespace RideSharing.Benchmark
{
    public class RideSharingBenchmark : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            builder.ScanThisDLL();
        }
    }
}