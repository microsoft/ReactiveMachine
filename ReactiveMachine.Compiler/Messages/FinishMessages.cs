// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class RequestFinish : RequestMessage
    {
        internal override MessageType MessageType => MessageType.RequestFinish;

        internal override void Apply(Process process)
        {
            if (process.FinishStates.TryGetValue(Parent, out var finishState))
            {
                finishState.AddRequest(Opid);
            }
            else // send response immediately - there are no pending children here
            {
                process.Send(process.GetOrigin(Opid), new AckFinish()
                {
                    Clock = process.OperationCounter,
                    Opid = Opid,
                    Parent = Parent
                });
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()} RequestFinish";
        }
    }

   


    [DataContract]
    internal class AckFinish : ResponseMessage
    {
        internal override MessageType MessageType => MessageType.AckFinish;

        public override string ToString()
        {
            return $"{base.ToString()} AckFinish";
        }
    }



}
