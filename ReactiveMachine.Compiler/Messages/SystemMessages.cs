// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class EnqueueStartup : RequestMessage
    {
        public override string ToString()
        {
            return $"{base.ToString()} EnqueueStartup";
        }

        internal override MessageType MessageType => MessageType.EnqueueStartup;

        internal override void Apply(Process process)
        {
            foreach (var orchestration in process.StartupOrchestrations)
            {
                var opInfo = process.Orchestrations[orchestration.GetType()];
                var opid2 = process.NextOpid;
                opInfo.CanExecuteLocally(orchestration, out var dest);
                var msg2 = opInfo.CreateForkMessage(orchestration);
                msg2.Opid = opid2;
                msg2.Clock = 0;
                msg2.Parent = 0;
                process.Send(dest, msg2);
            }
        }
    }

    [DataContract]
    internal class RespondToActivity : ResultMessage
    {
        [DataMember]
        public Guid InstanceId;

        internal override MessageType MessageType => MessageType.RespondToActivity;

        public override bool Sequenced => false;

        public override string ToString()
        {
            return $"{base.ToString()} RespondToActivity {InstanceId}";
        }

        internal override void Apply(Process process)
        {
            if (process.OrchestrationStates.TryGetValue(Parent, out var orchestrationState))
                orchestrationState.Continue(Opid, Clock, MessageType.RespondToActivity, Result);
        }
    }


   
}
