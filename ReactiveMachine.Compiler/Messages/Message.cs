// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    [DataContract]
    internal abstract class Message : IMessage
    {      
        [DataMember]
        public ulong Opid { get; set; }

        [DataMember]
        public ulong Clock { get; set; }

        [DataMember]
        public ulong Parent { get; set; }

        internal abstract void Apply(Process process);

        [IgnoreDataMember]
        internal abstract MessageType MessageType { get; }
    }


    [DataContract]
    internal abstract class RequestMessage : Message
    {
        public override string ToString()
        {
            return $"o{Opid:D10}";
        }

        [DataMember]
        public bool LockedByCaller { get; set; }
    }



    /// <summary>
    ///  A message that can advance the state machine at a partition lock
    /// </summary>
    [DataContract]
    internal abstract class ProtocolMessage : Message
    {
        [DataMember]
        public IPartitionKey PartitionKey;

        [DataMember]
        public ulong OriginalOpid;

        internal override void Apply(Process process)
        {
            var PartitionLock = process.AffinityIndex[PartitionKey.Index].PartitionLock;
            PartitionLock.UpdateLock(this);
        }

    }
}
