// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Service
{
    [DataContract]
    public class GetAvailableDriver :
        IRead<AvailabilityState, DriverEvent>,
        IGeoAffinity
    {
        public GeoLocation Location { get; set; }

        public DriverEvent Execute(IReadContext<AvailabilityState> context)
        {
            return context.State.AvailableDrivers.Values.FirstOrDefault();
        }
    }
}
