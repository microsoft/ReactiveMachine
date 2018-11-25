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
    public class GetAvailableRider :
        IRead<AvailabilityState, RiderEvent>,
        IGeoAffinity
    {

        [DataMember]
        public GeoLocation Location { get; set; }


        public RiderEvent Execute(IReadContext<AvailabilityState> context)
        {
            return context.State.AvailableRiders.Values.FirstOrDefault();
        }
    }
}
