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
    public class GetRiderState :
        IRead<RiderState,RiderState>,
        IRiderAffinity
    {
        [DataMember]
        public string RiderId { get; set; }


        public RiderState Execute(IReadContext<RiderState> context)
        {
            return context.State;
        }
    }
}
