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

        void ExitLock(IPartitionKey key, ulong opid);
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
            bool freshlyCreated = false;
            bool alreadyProcessed = false;
            var opid = request.Opid;
            var localkey = (PartitionKey<TKey>) request.GetPartitionKey(process);
            var parent = request.Parent;
            var messageType = request.MessageType;

            if (request.LockedByCaller)
            {
                // lock has already been acquired - so we need not touch the queue
                Entering(opid, localkey.Key, request, stopwatch, messageType, parent);
            }
            else
            {
                if (!lockqueues.TryGetValue(localkey.Key, out var queue))
                {
                    queue = new LockQueue();
                    queue.Process = process;
                    freshlyCreated = true;
                }
                if (queue.IsEmpty)
                {
                    if (Entering(opid, localkey.Key, request, stopwatch, messageType, parent))
                    {
                        queue.Enqueue(request, stopwatch);
                        if (freshlyCreated)
                            lockqueues[localkey.Key] = queue;
                    }
                    else
                    {
                        alreadyProcessed = true;
                    }
                }
                else
                {
                    queue.Enqueue(request, stopwatch);
                }
                if (!alreadyProcessed && messageType.IsFork())
                {
                    if (!process.FinishStates.TryGetValue(parent, out var finishState))
                        process.FinishStates[parent] = finishState = new FinishState(process, parent);
                    finishState.AddPending(opid);
                }
            }
        }

        private bool Entering(ulong opid, TKey localkey, QueuedMessage payload, Stopwatch stopwatch, MessageType messageType, ulong parent)
        {
            process.LockTracer?.Invoke($"p{process.ProcessId:D3} {payload.GetPartitionKey(process)} Enter {payload.Opid}");
            var timestamp = process.NextOpid;
            switch (messageType)
            {             
                case (MessageType.RequestLocal):
                case (MessageType.ForkLocal):
                   
                    var result = payload.Execute<TKey>(process, opid);
                    if (messageType == MessageType.RequestLocal)
                        process.Send(process.GetOrigin(opid), new RespondToLocal() { Opid = opid, Parent = parent, Result = result });

                    process.Telemetry?.OnApplicationEvent(
                        processId: process.ProcessId,
                        id: opid.ToString(),
                        name: payload.Payload.ToString(),
                        parent: parent.ToString(),
                        opSide: OperationSide.Callee,
                        opType: OperationType.Local,
                        duration: stopwatch.Elapsed.TotalMilliseconds
                    );

                    process.LockTracer?.Invoke($"p{process.ProcessId:D3} {payload.GetPartitionKey(process)} Exit {payload.Opid}");
                    return false; // in and out of the lock

                case (MessageType.PerformEvent):
                case (MessageType.ForkEvent):
                    {
                        var req = (PerformEvent)payload;
                        var effects = req.GetEffects(process);

                        // apply the event to all the affected states
                        req.Execute<TKey>(process, opid);

                        // if we are not the last partition with an effect, forward to next
                        if (req.Position < effects.Count - 1)
                        {
                            var nextReq = req.NextMessage(timestamp);
                            var destination = nextReq.GetCurrent(process).Locate(process);
                            process.Send(destination, nextReq);
                            return true; // stays in the lock
                        }

                        // if we are the last partition
                        else
                        {
                            // if we are not running in locked orchestration,
                            // release all previously locked partitions now
                            if (! payload.LockedByCaller && effects.Count > 1)
                                for (int i = 0; i < effects.Count - 1; i++)
                                {
                                    var key = effects[i].UntypedKey;
                                    var destination = key.Locate(process);
                                    var message = new ReleaseLock() { Key = key, LockOpid = req.Opid };
                                    message.Clock = timestamp;
                                    message.Parent = req.Parent;
                                    message.Opid = opid;
                                    process.Send(destination, message);
                                }

                            // return ack to orchestration
                            if (messageType == MessageType.PerformEvent)
                            {
                                process.Send(process.GetOrigin(opid), new AckEvent()
                                { Opid = opid, Parent = parent, Clock = timestamp });
                            }

                            process.Telemetry?.OnApplicationEvent(
                                processId: process.ProcessId,
                                id: opid.ToString(),
                                name: payload.Payload.ToString(),
                                parent: parent.ToString(),
                                opSide: OperationSide.Callee,
                                opType: OperationType.Event,
                                duration: stopwatch.Elapsed.TotalMilliseconds
                            );

                            process.LockTracer?.Invoke($"p{process.ProcessId:D3} {payload.GetPartitionKey(process)} Exit {payload.Opid}");
                            return false; // in and out of the lock
                        }
                    }
                case (MessageType.AcquireLock):
                    {
                        var req = (AcquireLock)payload;
                         
                        // if we are not the last partition to lock send next lock message
                        if (req.Position < req.LockSet.Count - 1)
                        {
                            var nextReq = req.NextMessage(timestamp);
                            var destination = nextReq.LockSet[nextReq.Position].Locate(process);
                            process.Send(destination, nextReq);
                        }

                        // if we are the last partition to lock return ack to orchestration
                        else
                        {
                           process.Send(process.GetOrigin(opid), new GrantLock()
                           { Opid = opid, Parent = parent, Clock = timestamp });
                        }

                        return true; // stays in the lock
                    }

                default:
                    throw new InvalidOperationException("unhandled case for messageType");
            }
        }

        public void ExitLock(IPartitionKey key, ulong exitingOpid)
        {
            var localkey = ((PartitionKey<TKey>)key).Key;
            var queue = lockqueues[localkey];
            queue.Remove(exitingOpid, out var msg);
            if (msg.MessageType.IsFork())
            {
                process.FinishStates[msg.Parent].RemovePending(exitingOpid);
            }

            if (!queue.IsEmpty)
            {
                queue.EnterNextHolder(localkey, Entering);
            }
        }

      
    }
}
