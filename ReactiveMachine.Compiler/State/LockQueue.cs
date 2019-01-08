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

        [IgnoreDataMember]
        public bool IsEmpty => head == null;

        public QueuedMessage Holder => head.Request;

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

        public void Update<TKey>(TKey localkey, ProtocolMessage protocolMessage)
        {
            if (head == null)
                throw new Exception("internal error: received update for request not holding lock");

            head.Request.Update(Process, localkey, protocolMessage, head.Stopwatch, out var exiting);

            while (exiting)
            {
                head.Request.OnExit(Process);
                head = head.Next;

                if (head == null)
                    return;

                head.Request.OnEnter(Process);
                head.Request.Enter(Process, localkey, head.Stopwatch, out exiting);
            }
        }
    }
}
