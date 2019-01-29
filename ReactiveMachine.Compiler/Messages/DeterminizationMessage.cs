using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    [DataContract]
    internal class PerformDeterminize : RequestMessage
    {
        internal override MessageType MessageType => MessageType.PerformDeterminize;

        internal override void Apply(Process process)
        {
            throw new NotImplementedException(); // is never sent to process
        }

        public override string ToString()
        {
            return $"{base.ToString()} PerformDeterminize";
        }
    }




}
