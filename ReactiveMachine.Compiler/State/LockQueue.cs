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
    internal class LockQueue
    {
        [IgnoreDataMember]
        internal Process Process;

        [DataMember]
        private RInfo head;
        [DataMember]
        private RInfo tail;

        [DataContract]
        private class RInfo
        {
            [DataMember]
            public QueuedMessage Request;
            [DataMember]
            public RInfo Next;
            [IgnoreDataMember]
            public Stopwatch Stopwatch;
        }

        public bool IsEmpty => head == null;

       
        public void Enqueue(QueuedMessage request, Stopwatch stopwatch)
        {
            Process.LockTracer?.Invoke($"p{Process.ProcessId:D3} {request.GetPartitionKey(Process)} Enqueue {request.Opid}");
            var rinfo = new RInfo()
            {
                Request = request,
                Stopwatch = stopwatch,
            };        
            if (head == null)
            {
                head = tail = rinfo;
            }
            else
            {
                tail.Next = rinfo;
                tail = rinfo;
            }
        }

        public void Remove(ulong opid, out QueuedMessage msg)
        {
            if (head.Request.Opid == opid)
            {
                msg = head.Request;
                head = head.Next;
            }
            else
            {
                var pos = head;
                while (pos.Next.Request.Opid != opid)
                {
                    pos = pos.Next;
                }
                msg = pos.Next.Request;
                if (tail == pos.Next)
                    tail = pos;
                pos.Next = pos.Next.Next;
            }
            Process.LockTracer?.Invoke($"p{Process.ProcessId:D3} {msg.GetPartitionKey(Process)} Removed {opid}");
        }

        public void EnterNextHolder<TKey>(TKey localkey, Func<ulong,TKey,QueuedMessage,Stopwatch,MessageType,ulong,bool> entering)
        {
            // move head forward past all entries that immediately leave the lock after entering
            while (head != null && !entering(head.Request.Opid, localkey, head.Request, head.Stopwatch, head.Request.MessageType, head.Request.Parent))
            {
                if (head.Request.MessageType.IsFork())
                    Process.FinishStates[head.Request.Parent].RemovePending(head.Request.Opid);
                head = head.Next;
                continue;
            }
        }
    }
}
