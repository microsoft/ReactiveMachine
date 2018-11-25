// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace RideSharing
{
    public interface IDriverAffinity : 
        IPartitionedAffinity<IDriverAffinity,string>
    {
        string DriverId { get; }
    }
}
