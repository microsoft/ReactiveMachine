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
    public class RideMatchedEvent :
        IEvent,
        IDriverAffinity,
        IRiderAffinity,
        IMultiple<IGeoAffinity,GeoLocation>
    {
        [DataMember]
        public string DriverId { get; set; }

        [DataMember]
        public string RiderId { get; set; }

        [DataMember]
        public GeoLocation DriverLocation { get; set; }

        [DataMember]
        public GeoLocation RiderLocation { get; set; }

        [DataMember]
        public Guid RideId { get; set; }

        public IEnumerable<GeoLocation> DeclareAffinities()
        {
            yield return DriverLocation.Location;
            yield return RiderLocation.Location;
        }
    }


    [DataContract]
    public class RideFinishedEvent :
        IEvent,
        IRiderAffinity,
        IDriverAffinity
    {
        [DataMember]
        public string DriverId { get; set; }

        [DataMember]
        public string RiderId { get; set; }

        [DataMember]
        public Guid RideId { get; set; }
    }
}
