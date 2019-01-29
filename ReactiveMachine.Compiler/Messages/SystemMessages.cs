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
                var opid = process.NextOpid;
                opInfo.CanExecuteLocally(orchestration, opid, out var dest);
                var msg = opInfo.CreateForkMessage(orchestration);
                msg.Opid = opid;
                msg.Clock = 0;
                msg.Parent = 0;
                process.Send(dest, msg);
            }
        }
    }

    [DataContract]
    internal class ExternalRequest : RequestMessage
    {
        [DataMember]
        public IOrchestration Orchestration;

        public override string ToString()
        {
            return $"{base.ToString()} ExternalRequest";
        }

        internal override MessageType MessageType => MessageType.ExternalRequest;

        internal override void Apply(Process process)
        {
            var opInfo = process.Orchestrations[Orchestration.GetType()];
            var opid = process.NextOpid;
            opInfo.CanExecuteLocally(Orchestration, opid, out var dest);
            var msg = opInfo.CreateForkMessage(Orchestration);
            msg.Opid = opid;
            msg.Clock = 0;
            msg.Parent = 0;
            process.Send(dest, msg);
        }
    }

}
