// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Service
{
    [DataContract]
    public class GetDriverState :
        IRead<DriverState, DriverState>,
        IDriverAffinity
    {
        [DataMember]
        public string DriverId { get; set; }


        public DriverState Execute(IReadContext<DriverState> context)
        {
            return context.State;
        }
    }
}
