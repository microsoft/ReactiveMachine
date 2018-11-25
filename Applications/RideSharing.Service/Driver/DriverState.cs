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
    public class DriverState :
        IPartitionedState<IDriverAffinity, string>,
        ISubscribe<DriverEvent, IDriverAffinity, string>,
        ISubscribe<RideMatchedEvent, IDriverAffinity, string>,
        ISubscribe<RideFinishedEvent, IDriverAffinity, string>

    {
        [DataMember]
        public GeoLocation? Availability;

        [DataMember]
        public RideMatchedEvent CurrentRide;

        public void On(ISubscriptionContext<string> context, DriverEvent evt)
        {
            Availability = evt.CurrentAvailability;
        }

        public void On(ISubscriptionContext<string> context, RideMatchedEvent evt)
        {
            Availability = null;
            CurrentRide = evt;
        }

        public void On(ISubscriptionContext<string> context, RideFinishedEvent evt)
        {
            CurrentRide = null;
        }
    }


    


}
