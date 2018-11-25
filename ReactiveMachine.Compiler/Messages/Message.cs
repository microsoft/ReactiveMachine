// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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

        [IgnoreDataMember]
        public virtual bool Sequenced => true;
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


    [DataContract]
    internal abstract class QueuedMessage : RequestMessage
    {
        internal abstract IPartitionKey GetPartitionKey(Process process);

        internal abstract object Execute<TKey>(Process process, ulong opid);

        [IgnoreDataMember]
        internal abstract object Payload { get; }
    }


    /// <summary>
    ///  A message that will trigger a continuation when received
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
    }


    /// <summary>
    ///  A response message containing a result or exception
    /// </summary>
    [DataContract]
    internal abstract class ResultMessage : ResponseMessage
    {
        [DataMember]
        public object Result { get; set; }

        internal override void AntiAlias(IDeepCopier deepCopier)
        {
            if (! ((Result is Exception) || (Result is UnitType)))
                Result = deepCopier.DeepCopy(Result);
        }
    }




}
