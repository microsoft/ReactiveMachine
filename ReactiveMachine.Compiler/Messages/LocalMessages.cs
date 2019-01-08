// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{  
    [DataContract]
    internal abstract class LocalMessage<TState> : QueuedMessage
    {
        [IgnoreDataMember]
        private IStateInfo stateInfo;

        protected IStateInfo GetStateInfo(Process process)
        {
            return stateInfo ?? (stateInfo = process.States[typeof(TState)]);
        }

        internal override void Apply(Process process)
        {
            GetStateInfo(process).AffinityInfo.PartitionLock.EnterLock(this);
        }

        internal override void Enter<TKey>(Process process, TKey localkey, Stopwatch stopwatch, out bool isExiting)
        {
            var completed = Execute<TKey>(process, localkey, out var result);

            if (!completed)
            { // we did not actually execute, but are waiting for initialization ack
                isExiting = false;
                return; // keep lock
            }

            if (MessageType != MessageType.ForkLocal)
                process.Send(process.GetOrigin(Opid), new RespondToLocal() { Opid = Opid, Parent = Parent, Result = result });

            process.Telemetry?.OnApplicationEvent(
                processId: process.ProcessId,
                id: Opid.ToString(),
                name: LabelForTelemetry,
                parent: Parent.ToString(),
                opSide: OperationSide.Callee,
                opType: OperationType.Local,
                duration: stopwatch.Elapsed.TotalMilliseconds
            );

            isExiting = true; 
        }

        internal abstract bool Execute<TKey>(Process process, TKey localkey, out object result);
    }

    [DataContract]
    internal abstract class LocalOperation<TState> : LocalMessage<TState>
    {
        [DataMember]
        public object Operation;

        internal override string LabelForTelemetry => Operation.ToString();

        internal override IPartitionKey GetPartitionKey(Process process)
        {
            return GetStateInfo(process).GetPartitionKeyForLocalOperation(Operation);
        }

        internal override bool Execute<TKey>(Process process, TKey localkey, out object result)
        {
            var state = (IStateInfoWithKey<TKey>)process.States[typeof(TState)];
            bool createIfNotExist = state.IsCreateIfNotExists(Operation.GetType());
            var success = state.TryGetInstance(localkey, out var instance, createIfNotExist, Opid);
            if (success)
            {
                result = instance.Execute(Operation, Opid, StateOperation.ReadOrUpdate);
                return true;
            }
            else
            {
                if (!createIfNotExist)
                {
                    result = process.Serializer.SerializeException(new KeyNotFoundException("no state for this partition key"));
                    return true;
                }
                else
                {
                    // we kicked off an initialization
                    result = null;
                    return false;
                }
            }
        }

        internal override void Update<TKey>(Process process, TKey localkey, ProtocolMessage protocolMessage, Stopwatch stopwatch, out bool exiting)
        {
            // try again now that initialization is done
            Enter(process, localkey, stopwatch, out exiting);
        }

    }

    [DataContract]
    internal class ForkLocal<TState> : LocalOperation<TState>
    {
        internal override MessageType MessageType => MessageType.ForkLocal;

        public override string ToString()
        {
            return $"{base.ToString()} ForkLocal<{typeof(TState).Name}>";
        }
    }


    [DataContract]
    internal class RequestLocal<TState> : LocalOperation<TState>
    {
        internal override MessageType MessageType => MessageType.RequestLocal;

        public override string ToString()
        {
            return $"{base.ToString()} RequestLocal<{typeof(TState).Name}>";
        }
    }

    [DataContract]
    internal class RequestPing<TState> : LocalMessage<TState>
    {
        [DataMember]
        public IPartitionKey Key;

        [IgnoreDataMember]
        internal override string LabelForTelemetry => Key.ToString();

        [IgnoreDataMember]
        internal override MessageType MessageType => MessageType.RequestPing;


        public override string ToString()
        {
            return $"{base.ToString()} RequestPing<{typeof(TState).Name}>";
        }

        internal override IPartitionKey GetPartitionKey(Process process)
        {
            return Key;
        }

        internal override bool Execute<TKey>(Process process, TKey localkey, out object result)
        {
            var state = (IStateInfoWithKey<TKey>)process.States[typeof(TState)];
            if (state.TryGetInstance(localkey, out _, false))
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return true;
        }
    }


    /// <summary>
    ///  A response message from a local operation
    /// </summary>
    [DataContract]
    internal class RespondToLocal : ResultMessage
    {
        internal override MessageType MessageType => MessageType.RespondToLocal;

        public override string ToString()
        {
            return $"{base.ToString()} RespondToLocal";
        }
    }

}
