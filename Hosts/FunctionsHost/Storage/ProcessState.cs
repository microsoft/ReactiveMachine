// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    [DataContract]
    public class ProcessState
    {
        [DataMember]
        public DateTime DeploymentTimestamp { get; private set; }

        [DataMember]
        public byte[] State { get; private set; }

        [DataMember]
        public VectorClock SeenClock { get; private set; }

        [DataMember]
        public long OurClock { get; private set; }

        [DataMember]
        public long Version { get; private set; }

        public override String ToString()
        {
            if (State == null)
                return $"Checkpoint(v{Version} initial state)";
            else
                return $"Checkpoint(v{Version} sent:{OurClock} seen:{SeenClock} size:{State.Length/1024}kB)";
        }

        // create an empty checkpoint
        public ProcessState(DateTime deploymentTimestamp, long version)
        {
            this.DeploymentTimestamp = deploymentTimestamp;
            this.Version = version;
            SeenClock = new VectorClock();
        }
      
        // create a checkpoint with a state
        public ProcessState(DateTime deploymentTimestamp, long version, byte[] state, VectorClock seenClock, long ourClock)
        {
            this.DeploymentTimestamp = deploymentTimestamp;
            this.Version = version;
            this.State = state;
            this.SeenClock = seenClock;
            this.OurClock = ourClock;
        }
    }
}
