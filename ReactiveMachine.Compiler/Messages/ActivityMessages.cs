using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    /// <summary>
    ///  A request message for an activity
    /// </summary>
    [DataContract]
    internal class PerformActivity<TActivity> : RequestMessage
    {
        [DataMember]
        public TActivity Request;

        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.PerformActivity;

        public override string ToString()
        {
            return $"{base.ToString()} PerformActivity<{typeof(TActivity).Name}>";
        }

        internal override void Apply(Process process)
        {
            var activityInfo = process.Activities[typeof(TActivity)];
            activityInfo.ProcessRequest(this);
        }
    }

    /// <summary>
    ///  A response message from an activity
    /// </summary>
    [DataContract]
    internal class RespondToActivity : ResultMessage
    {
        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.RespondToActivity;

        public override string ToString()
        {
            return $"{base.ToString()} RespondToActivity";
        }
    }


    [DataContract]
    internal class RecordActivityResult : Message
    {
        [DataMember]
        public object Result;

        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.RecordActivityResult;

        public override string ToString()
        { 
            return $"o{Opid:D10} RecordActivityResult";
        }

        internal override void Apply(Process process)
        {
            if (process.PendingActivities.TryGetValue(Opid, out var activityState))
            {
                activityState.RecordResult(Result);
            }
        }
    }

}
