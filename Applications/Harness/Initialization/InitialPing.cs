// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Harness
{
    /// <summary>
    /// The initial ping is executed on each process before we start the experiment.
    /// This helps to avoid experiment distortions caused by processes starting at different times.
    /// </summary>
    [DataContract]
    public class InitialPing : 
        IOrchestration<UnitType>, 
        IProcessAffinity
    {
        public uint ProcessId { get; set; }

        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            context.Logger.LogInformation($"Received Initial Ping");
            
            return UnitType.CompletedTask;
        }
    }

    

}
