// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{

    [DataContract]
    internal class FinishState : IRestorable
    {
        public FinishState(Process process, ulong parent)
        {
            this.process = process;
            this.Parent = parent;
        }

        [DataMember]
        public ulong Parent;


        [DataMember]
        public SortedDictionary<ulong, EntryType> Entries = new SortedDictionary<ulong, EntryType>();

        internal enum EntryType
        {
            Pending,
            Request
        }

        [IgnoreDataMember]
        private Process process;

        public void SaveStateTo(Snapshot snapshot)
        {
            snapshot.StatePieces.Add(this);
        }

        public void RestoreStateTo(Process process)
        {
            this.process = process;
            process.FinishStates[Parent] = this;
        }

        public void AddRequest(ulong opid)
        {
            Entries.Add(opid, EntryType.Request);
            ReleaseFront();
        }

        public void AddPending(ulong opid)
        {
            Entries[opid] = EntryType.Pending;
        }

        public void RemovePending(ulong opid)
        {
            if (Entries.Remove(opid))
                ReleaseFront();
        }

        private void ReleaseFront()
        {
            while (true)
            {
                using (var e = Entries.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        process.FinishStates.Remove(Parent);
                        return;
                    }
                    if (e.Current.Value == EntryType.Pending)
                    {
                        return; // still waiting for this one
                    }
                    // the front-most finish request is satisfied
                    var opid = e.Current.Key;
                    process.Send(process.GetOrigin(opid), new AckFinish()
                    {
                        Clock = process.OperationCounter,
                        Opid = opid,
                        Parent = this.Parent
                    });


                    Entries.Remove(e.Current.Key);
                }
            }
        }
    }
}
