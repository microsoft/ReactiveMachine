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
    public class AvailabilityState :
        IPartitionedState<IGeoAffinity,GeoLocation>,
        // subscribes to the following events
        ISubscribe<DriverEvent, IGeoAffinity, GeoLocation>,
        ISubscribe<RiderEvent, IGeoAffinity, GeoLocation>,
        ISubscribe<RideMatchedEvent, IGeoAffinity, GeoLocation>
    {

        [DataMember]
        public Dictionary<string, RiderEvent> AvailableRiders 
            = new Dictionary<string, RiderEvent>();

        [DataMember]
        public Dictionary<string, DriverEvent> AvailableDrivers 
            = new Dictionary<string, DriverEvent>();

        public void On(ISubscriptionContext<GeoLocation> context, DriverEvent evt)
        {
            if (GeoLocation.Equals(context.Key, evt.PreviousAvailability))
            {
                AvailableDrivers.Remove(evt.DriverId);
            }
            else if (GeoLocation.Equals(context.Key, evt.CurrentAvailability))
            {
                AvailableDrivers.Add(evt.DriverId, evt);
                context.ForkOrchestration(new TryMatchDriver()
                {
                    AvailableDriver = evt
                });
            }
        }

        public void On(ISubscriptionContext<GeoLocation> context, RiderEvent evt)
        {
            if (GeoLocation.Equals(context.Key, evt.PreviousAvailability))
            {
                AvailableRiders.Remove(evt.RiderId);
            }
            else if (GeoLocation.Equals(context.Key, evt.CurrentAvailability))
            {
                AvailableRiders.Add(evt.RiderId, evt);
                context.ForkOrchestration(new TryMatchRider()
                {
                    AvailableRider = evt
                });
            }
        }

        public void On(ISubscriptionContext<GeoLocation> context, RideMatchedEvent evt)
        {
            AvailableDrivers.Remove(evt.DriverId);
            AvailableRiders.Remove(evt.RiderId);
        }

    }
}
