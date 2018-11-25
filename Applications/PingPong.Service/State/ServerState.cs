// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
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
    public class ServerState :
        ISingletonState<IServerAffinity>,
        ISubscribe<PingEvent, IServerAffinity>
    {
        [DataMember]
        int count;

        public void On(ISubscriptionContext context, PingEvent pingEvent)
        {
            count++;

            context.Logger.LogInformation($"Received: {pingEvent.Message}");

            context.ForkEvent(new PongEvent()
            {
                Message = $"Echo {pingEvent.Message}",
            });
        }
         
    }

}
