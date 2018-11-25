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
    public class TryMatchRider :
        IOrchestration<UnitType>,
        IGeoAffinity
    {
        public RiderEvent AvailableRider;
        public GeoLocation Location => AvailableRider.CurrentAvailability.Value;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var nearbyArea = AvailableRider.CurrentAvailability.Value.GetNearbyAreas();
 
            DriverEvent candidate = null;

            foreach (var location in nearbyArea)
            {
                candidate = await context.PerformRead(new GetAvailableDriver() { Location = location });

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
                    AvailableRider = AvailableRider,
                    AvailableDriver = candidate
                });

                if (result == TryFinalizeMatch.Response.RiderRemainsUnmatched)
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
