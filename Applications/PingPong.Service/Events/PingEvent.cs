// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PingPong.Service
{                
    [DataContract]
    public class PingEvent :
        IEvent,
        IServerAffinity
    {
        [DataMember]
        public string Message;
    }
}
