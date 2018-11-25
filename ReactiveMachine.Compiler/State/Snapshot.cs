// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class Snapshot : IRestorable
    {
        [DataMember]
        public uint ProcessId;

        [DataMember]
        public ClockState ClockState;

        [DataMember]
        public List<IRestorable> StatePieces;

        public void RestoreStateTo(Process process)
        {
            if (process.ProcessId != ProcessId)
                throw new InvalidOperationException("attempt to restore a snapshot with different process id");

            ClockState.RestoreStateTo(process);

            foreach (var p in StatePieces)
                p.RestoreStateTo(process);
        }

        public override string ToString()
        {
            // unique label for the snapshot
            return $"Snapshot-p{ProcessId:D3}-{ClockState.DeliveryCounter:D15}";
        }
    }

    internal interface IRestorable
    {
        void RestoreStateTo(Process process);
    }

    internal interface ISaveable
    {
        void SaveStateTo(Snapshot snapshot);

        void ClearState();
    }

    [DataContract]
    internal class LockState : IRestorable
    {
        [DataMember]
        public int AffinityIndex;
        [DataMember]
        public object KeyValue;
        [DataMember]
        public LockQueue LockQueue;

        public void RestoreStateTo(Process process)
        {
            process.AffinityIndex[AffinityIndex].PartitionLock.RestoreLockQueue(KeyValue, LockQueue);
        }
    }

    [DataContract]
    internal class StateState<TState> : IRestorable
    {
        [DataMember]
        public object KeyValue;
        [DataMember]
        public TState State;
        // [DataMember]
        //public ulong Clock;

        public void RestoreStateTo(Process process)
        {
           ((IStateInfo<TState>) process.States[typeof(TState)]).Restore(KeyValue, State);
        }
    }

    [DataContract]
    internal class ClockState : IRestorable
    {
        [DataMember]
        public ulong DeliveryCounter;

        [DataMember]
        public ulong OperationCounter;

        [DataMember]
        public ulong[] LowerBounds;

        public void RestoreStateTo(Process process)
        {
            process.DeliveryCounter = DeliveryCounter;
            process.OperationCounter = OperationCounter;
            process.LowerBounds = LowerBounds;
        }
    }
}
