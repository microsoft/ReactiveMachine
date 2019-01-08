using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal abstract class QueuedMessage : RequestMessage
    {
        internal abstract IPartitionKey GetPartitionKey(Process process);

        internal abstract void Enter<TKey>(Process process, TKey localkey, Stopwatch stopwatch, out bool exitImmediately);

        internal virtual void Update<TKey>(Process process, TKey localkey, ProtocolMessage protocolMessage, Stopwatch stopwatch, out bool exiting)
        {
            throw new NotImplementedException();
        }

        [IgnoreDataMember]
        internal abstract string LabelForTelemetry { get; }

        internal void OnEnter(Process process)
        {
            // trace enter
            process.LockTracer?.Invoke($"p{process.ProcessId:D3} {GetPartitionKey(process)} Enter {Opid}");

            // if this is a fork, add it to list of pending forked operations by this parent
            if (MessageType.IsFork())
            {
                if (!process.FinishStates.TryGetValue(Parent, out var finishState))
                    process.FinishStates[Parent] = finishState = new FinishState(process, Parent);
                finishState.AddPending(Opid);
            }
        }
        internal void OnExit(Process process)
        {
            // trace exit
            process.LockTracer?.Invoke($"p{process.ProcessId:D3} {GetPartitionKey(process)} Exit {Opid}");

            // if this was a fork, remove it from the list of pending forked operations
            if (MessageType.IsFork())
            {
                process.FinishStates[Parent].RemovePending(Opid);
            }
        }
    }
}
