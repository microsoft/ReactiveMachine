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
    public class TryFinalizeMatch :
        IOrchestration<TryFinalizeMatch.Response>,
        IDriverAffinity,
        IRiderAffinity
    {
        [DataMember]
        public DriverEvent AvailableDriver;

        [DataMember]
        public RiderEvent AvailableRider;

        public string DriverId => AvailableDriver.DriverId;

        public string RiderId => AvailableRider.RiderId;

        public enum Response
        {
            Ok, DriverRemainsUnmatched, RiderRemainsUnmatched
        }
       
        [Lock]
        public async Task<Response> Execute(IOrchestrationContext context)
        {            
            // (re-)check availability state of both
            var t1 = context.PerformRead(new GetDriverState() { DriverId = AvailableDriver.DriverId });
            var t2 = context.PerformRead(new GetRiderState() { RiderId = AvailableRider.RiderId });
            var t3 = context.NewGuid();

            var driverAvailability = (await t1).Availability;
            var riderAvailability = (await t2).Availability;
            var rideId = await t3;

            if (riderAvailability == null && driverAvailability != null)
            {
                return Response.DriverRemainsUnmatched;
            }
            else if (driverAvailability == null && riderAvailability != null)
            {
                return Response.RiderRemainsUnmatched;
            }
            else if (driverAvailability == null && riderAvailability == null)
            {
                return Response.Ok;
            }
            else
            {
                await context.PerformEvent(new RideMatchedEvent()
                {
                    DriverId = AvailableDriver.DriverId,
                    DriverLocation = AvailableDriver.CurrentAvailability.Value,
                    RiderId = AvailableRider.RiderId,
                    RiderLocation = AvailableRider.CurrentAvailability.Value,
                    RideId = rideId
                });

                return Response.Ok;
            }
        }
    }
}