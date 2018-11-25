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
    public class PostFinishRide :
        IOrchestration<UnitType>,
        IDriverAffinity
    {
        [DataMember]
        public string DriverId { get; set; }

        [DataMember]
        public Guid RideId;

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var state = await context.PerformRead(new GetDriverState() { DriverId = DriverId });

            if (state.CurrentRide == null || state.CurrentRide.RideId != RideId)
            {
                return UnitType.Value;  // this request is not applicable in the current state, no such ride
            }

            await context.PerformEvent(new RideFinishedEvent() {
                DriverId = DriverId,
                RiderId = state.CurrentRide.RiderId,
                RideId = state.CurrentRide.RideId,
            });

            return UnitType.Value;
        }
    }
}
