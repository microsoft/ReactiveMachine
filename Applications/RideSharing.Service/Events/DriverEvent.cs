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
    public class DriverEvent :
        IEvent,
        IDriverAffinity,
        IMultiple<IGeoAffinity,GeoLocation>
    {
        [DataMember]
        public string DriverId { get; set; }

        [DataMember]
        public GeoLocation? CurrentAvailability { get; set; }

        [DataMember]
        public GeoLocation? PreviousAvailability { get; set; }

        public IEnumerable<GeoLocation> DeclareAffinities()
        {
            if (CurrentAvailability != null)
                yield return CurrentAvailability.Value;

            if (PreviousAvailability != null)
                yield return PreviousAvailability.Value;
        }
    }

 
  

}