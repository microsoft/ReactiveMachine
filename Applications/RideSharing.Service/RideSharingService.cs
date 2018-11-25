// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Service
{

    public class RideSharingService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            builder.ScanThisDLL();
        }

    }
}
