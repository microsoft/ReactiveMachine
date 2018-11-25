// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miner.Service
{

    public class MinerService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder serviceBuilder)
        {
            // build this service by automatically scanning for declarations
            serviceBuilder.ScanThisDLL();
        }
    }
}
