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
    public class TryMatchDriver : 
        IOrchestration<UnitType>, 
        IGeoAffinity
    {
        public DriverEvent AvailableDriver;

        public GeoLocation Location => AvailableDriver.CurrentAvailability.Value;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var nearbyArea = AvailableDriver.CurrentAvailability.Value.GetNearbyAreas();

            RiderEvent candidate = null;

            foreach (var location in nearbyArea)
            {
                candidate = await context.PerformRead(new GetAvailableRider() { Location = location });

                if (candidate != null)
                    break;
            }

            if (candidate == null)
            {
                // there are no matches. So we just wait until someone joins and gets matched to us.
                return UnitType.Value;
            }

            try
            {
                var result = await context.PerformOrchestration(new TryFinalizeMatch()
                {
                    AvailableDriver = AvailableDriver,
                    AvailableRider = candidate
                });

                if (result == TryFinalizeMatch.Response.DriverRemainsUnmatched)
                {
                    // retry in order to find another match
                    context.ForkOrchestration(this);
                }
            }
            catch (TransactionException)
            {
                // transaction ran into trouble for some reason... retry this orchestration
                context.ForkOrchestration(this);
            }          

            return UnitType.Value;
        }
    }
}
