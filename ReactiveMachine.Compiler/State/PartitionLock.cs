// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal interface IPartitionLock : ISaveable
    {
        IEnumerable<Type> SerializableTypes();   

        void RestoreLockQueue(object keyValue, LockQueue queue);

        IAffinityInfo KeyInfo { get; }

        void EnterLock(QueuedMessage request);

        void UpdateLock(ProtocolMessage message);
    }

    internal class PartitionLock<TAffinity, TKey> : 
        IPartitionLock
        where TAffinity : IAffinitySpec<TAffinity>
    {
        private readonly Process process;
        private readonly AffinityInfo<TAffinity, TKey> keyInfo;
        private readonly Dictionary<TKey, LockQueue> lockqueues = new Dictionary<TKey, LockQueue>();

        public PartitionLock(Process process, AffinityInfo<TAffinity, TKey> keyInfo)
        {
            this.process = process;
            process.PartitionLocks[typeof(TAffinity)] = this;
            this.keyInfo = keyInfo;
            keyInfo.PartitionLock = this;
        }

        public IAffinityInfo KeyInfo => this.keyInfo;

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(LockState);
            yield return typeof(ForkEvent);
            yield return typeof(PerformEvent);
            yield return typeof(AckEvent);
            yield return typeof(CommitEvent);
            yield return typeof(AcquireLock);
            yield return typeof(GrantLock);
            yield return typeof(ReleaseLock);
            yield break;
        }

        public void SaveStateTo(Snapshot snapshot)
        {
            foreach (var kvp in lockqueues)
            {
                snapshot.StatePieces.Add(new LockState()
                {
                    AffinityIndex = keyInfo.Index,
                    KeyValue = kvp.Key,
                    LockQueue = kvp.Value
                });
            }
        }

        public void ClearState()
        {
            lockqueues.Clear();
        }

        public void RestoreLockQueue(object keyValue, LockQueue queue)
        {
            queue.Process = process;
            lockqueues[(TKey)keyValue] = queue;
        }

        public void EnterLock(QueuedMessage request)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool exitsImmediately = false;
            var opid = request.Opid;
            var parent = request.Parent;
            var messageType = request.MessageType;
            var partitionkey = request.GetPartitionKey(process);
            TKey localkey = ((PartitionKey<TKey>)partitionkey).Key;

            if (request.LockedByCaller)
            {
                request.OnEnter(process);
                request.Enter(process, localkey, stopwatch, out exitsImmediately);
                if (exitsImmediately)
                {
                    request.OnExit(process);
                }
                else
                {
                    ((AcquireLock)lockqueues[localkey].Holder).Add(request);
                }
            }
            else
            {
                if (!lockqueues.TryGetValue(localkey, out var queue)
                    || queue.IsEmpty)
                {
                    request.OnEnter(process);
                    request.Enter(process, localkey, stopwatch, out exitsImmediately);
                    if (exitsImmediately)
                    {
                        request.OnExit(process);
                    }
                    else
                    {
                        if (queue == null)
                        {
                            queue = new LockQueue() { Process = process };
                            lockqueues[localkey] = queue;
                        }
                        queue.Enqueue(request, stopwatch);
                    }
                }
                else
                {
                    queue.Enqueue(request, stopwatch);
                }
            }
            if (!exitsImmediately && messageType.IsFork())
            {
                if (!process.FinishStates.TryGetValue(parent, out var finishState))
                    process.FinishStates[parent] = finishState = new FinishState(process, parent);
                finishState.AddPending(opid);
            }
        }

        public void UpdateLock(ProtocolMessage message)
        {
            var localkey = ((PartitionKey<TKey>)message.PartitionKey).Key;
            var queue = lockqueues[localkey];
            queue.Update(localkey, message);
        }
    }
}
