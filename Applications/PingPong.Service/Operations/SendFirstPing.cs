// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingPong.Service
{
    public class SendFirstPing : 
        IStartupOrchestration,
        IClientAffinity
    {
        public static int NumberOfEvents = 100;

        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            var startTime = DateTime.Now;

            context.ForkEvent(new PingEvent()
            {
                Message = $"Ping!",
            });

            return UnitType.CompletedTask;
        }
    }
}
