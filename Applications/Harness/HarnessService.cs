// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Harness;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Harness
{
    public class HarnessService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            // build this service automatically by scanning for declarations
            builder.ScanThisDLL();
        }
    }
}
