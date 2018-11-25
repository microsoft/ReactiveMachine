// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{  

    [DataContract]
    internal class ForkLocal<TState> : QueuedMessage 
    {
        [DataMember]
        public object LocalOperation;

        [IgnoreDataMember]
        private IStateInfo stateInfo;

        private IStateInfo GetStateInfo(Process process)
        {
            return stateInfo ?? (stateInfo = process.States[typeof(TState)]);
        }

        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.ForkLocal;

        [IgnoreDataMember]
        internal override object Payload => LocalOperation;

        internal override void Apply(Process process)
        {
            GetStateInfo(process).AffinityInfo.PartitionLock.EnterLock(this);
        }

        public override string ToString()
        {
            return $"{base.ToString()} ForkLocal<{typeof(TState).Name}>";
        }

        internal override IPartitionKey GetPartitionKey(Process process)
        {
            return GetStateInfo(process).GetPartitionKeyForLocalOperation(LocalOperation);
        }

        internal override object Execute<TKey>(Process process, ulong opid)
        {
            var state = (IStateInfoWithKey<TKey>)process.States[typeof(TState)];
            var partitionKey = (PartitionKey<TKey>)GetPartitionKey(process);
            var instance = state.GetInstance(partitionKey.Key);
            return instance.Execute(LocalOperation, opid, false);
        }
    }

    [DataContract]
    internal class RequestLocal<TState> : ForkLocal<TState>
    {
        internal override MessageType MessageType => MessageType.RequestLocal;

        public override string ToString()
        {
            return $"{base.ToString()} RequestLocal<{typeof(TState).Name}>";
        }
    }

    [DataContract]
    internal class RespondToLocal : ResultMessage
    {
        internal override MessageType MessageType => MessageType.RespondToLocal;

        internal override void Apply(Process process)
        {
            process.OrchestrationStates[Parent].Continue(Opid, Clock, MessageType.RespondToLocal, Result);
        }

        public override string ToString()
        {
            return $"{base.ToString()} RespondToLocal";
        }
    }


}
