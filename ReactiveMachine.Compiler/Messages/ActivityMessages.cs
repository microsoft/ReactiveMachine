using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    /// <summary>
    ///  A response message from an activity
    /// </summary>
    [DataContract]
    internal class RespondToActivity : ResultMessage
    {
        [DataMember]
        public Guid InstanceId;

        internal override MessageType MessageType => MessageType.RespondToActivity;

        public override string ToString()
        {
            return $"{base.ToString()} RespondToActivity {InstanceId}";
        }
    }

}
