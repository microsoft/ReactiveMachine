using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    /// <summary>
    ///  A message that will trigger a continuation in an orchestration waiting for a response
    /// </summary>
    [DataContract]
    internal abstract class ResponseMessage : Message
    {
        // we call this on messages that are not already serialized/deserialized
        internal virtual void AntiAlias(IDeepCopier deepCopier)
        {
        }

        public override string ToString()
        {
            return $"o{Parent:D10}.o{Opid:D10}";
        }

        [IgnoreDataMember]
        internal virtual object ResultForContinuation => UnitType.Value;

        internal override void Apply(Process process)
        {
            if (process.OrchestrationStates.TryGetValue(Parent, out var orchestrationState))
            {
                orchestrationState.Continue(Opid, Clock, MessageType, ResultForContinuation);
            }
            else
            {
                if (MessageType == MessageType.RespondToActivity)
                {
                    // activities can receive duplicate responses, leading to missing continuations
                    // in that case the second response should simply be ignored
                    return;
                }
                else
                {
                    throw new Exception("internal error: missing continuation");
                }
            }
        }
    }


    /// <summary>
    ///  A response message containing a result or exception
    /// </summary>
    [DataContract]
    internal abstract class ResultMessage : ResponseMessage
    {
        [DataMember]
        public object Result { get; set; }

        [IgnoreDataMember]
        internal override object ResultForContinuation => Result;

        internal override void AntiAlias(IDeepCopier deepCopier)
        {
            if (!((Result is Exception) || (Result is UnitType)))
                Result = deepCopier.DeepCopy(Result);
        }

        internal override void Apply(Process process)
        {
            process.OrchestrationStates[Parent].Continue(Opid, Clock, MessageType, Result);
        }
    }




}
