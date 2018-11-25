// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class ForkOperation<TOperation> : RequestMessage
    {
        [DataMember]
        public TOperation Request;

        internal override MessageType MessageType => MessageType.ForkOperation;

        public override string ToString()
        {
            return $"{base.ToString()} ForkOperation<{typeof(TOperation).Name}>";
        }

        internal override void Apply(Process process)
        {
            var orchestrationInfo = process.Orchestrations[typeof(TOperation)];
            orchestrationInfo.ProcessRequest(this);
        }
    }

    [DataContract]
    internal class RequestOperation<TOperation> : ForkOperation<TOperation>
    {
        internal override MessageType MessageType => MessageType.RequestOperation;


        public override string ToString()
        {
            return $"{base.ToString()} RequestOperation<{typeof(TOperation).Name}>";
        }

        internal override void Apply(Process process)
        {
            var orchestrationInfo = process.Orchestrations[typeof(TOperation)];
            orchestrationInfo.ProcessRequest(this);
        }
    }

    [DataContract]
    internal class RespondToOperation : ResultMessage
    {
        internal override MessageType MessageType => MessageType.RespondToOperation;

        public override string ToString()
        {
            return $"{base.ToString()} RespondToOperation";
        }

        internal override void Apply(Process process)
        {
            process.OrchestrationStates[Parent].Continue(Opid, Clock, MessageType.RespondToOperation, Result);
        }
    }


}
