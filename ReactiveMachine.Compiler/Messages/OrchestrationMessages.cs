﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class ForkOrchestration<TOrchestration> : RequestMessage
    {
        [DataMember]
        public TOrchestration Request;

        internal override MessageType MessageType => MessageType.ForkOrchestration;

        public override string ToString()
        {
            return $"{base.ToString()} ForkOrchestration<{typeof(TOrchestration).Name}>";
        }

        internal override void Apply(Process process)
        {
            var orchestrationInfo = process.Orchestrations[typeof(TOrchestration)];
            orchestrationInfo.ProcessRequest(this, OrchestrationType.Fork);
        }
    }

    [DataContract]
    internal class RequestOrchestration<TOperation> : ForkOrchestration<TOperation>
    {
        internal override MessageType MessageType => MessageType.RequestOrchestration;


        public override string ToString()
        {
            return $"{base.ToString()} RequestOrchestration<{typeof(TOperation).Name}>";
        }

        internal override void Apply(Process process)
        {
            var orchestrationInfo = process.Orchestrations[typeof(TOperation)];
            orchestrationInfo.ProcessRequest(this, OrchestrationType.Perform);
        }
    }

    /// <summary>
    ///  A response message from an orchestration
    /// </summary>
    [DataContract]
    internal class RespondToOrchestration : ResultMessage
    {
        internal override MessageType MessageType => MessageType.RespondToOrchestration;

        public override string ToString()
        {
            return $"{base.ToString()} RespondToOrchestration";
        }
    }

}
