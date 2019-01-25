// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Counter;
using Counter.Service;
using ReactiveMachine;
using System;

namespace LocalTests
{
    public class TestsService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            builder.BuildService<CounterService>();
            builder.BuildService<Bank.BankTestsService>();
            builder.BuildService<SimpleLoadTest.Service.SimpleLoadTestService>();
            builder.ScanThisDLL();
        }
    }
}