// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Counter;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Counter.Service
{

    public class CounterService : IServiceBuildDefinition
    { 
        public void Build(IServiceBuilder builder)
        {
            // build this service by automatically scanning for declarations
            builder.ScanThisDLL();
        }
    }

}
