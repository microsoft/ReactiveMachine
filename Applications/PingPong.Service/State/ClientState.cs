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
    public class ClientState :
        ISingletonState<IClientAffinity>,
        ISubscribe<PongEvent, IClientAffinity>
    {
        [DataMember]
        int count;

        public void On(ISubscriptionContext context, PongEvent pongEvent)
        {
            count++;
   
            context.Logger.LogInformation($"Received: {pongEvent.Message}");

            // Send a ping event unless we have reach max count
            if (count < SendFirstPing.NumberOfEvents)
            {
                context.ForkEvent(new PingEvent()
                {
                    Message = $"Ping #{count + 1}",
                });
            }
        }
    }

}
