using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

   

    [DataContract]
    internal class AckInitialization : ProtocolMessage
    {
        internal override MessageType MessageType => MessageType.AckInitialization;

        public override string ToString()
        {
            return $"{base.ToString()} AckInitialization";
        }
    }



}
