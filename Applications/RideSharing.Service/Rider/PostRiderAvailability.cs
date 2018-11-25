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
    public class PostDriverAvailability : 
        IOrchestration<UnitType>, 
        IDriverAffinity
    {
        [DataMember]
        public GeoLocation? CurrentAvailability { get; set; }

        [DataMember]
        public string DriverId { get; set; }

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var state = await context.PerformRead(new GetDriverState() { DriverId = DriverId });

            if (state.CurrentRide != null)
            {
                throw new InvalidOperationException("currently doing a ride - must finish ride first");
            }

            if (! GeoLocation.Equals(CurrentAvailability, state.Availability))
            {
                await context.PerformEvent(new DriverEvent()
                {
                    DriverId = DriverId,
                    PreviousAvailability = state.Availability,
                    CurrentAvailability = CurrentAvailability
                });
            }

            return UnitType.Value;
        }
    }
}
