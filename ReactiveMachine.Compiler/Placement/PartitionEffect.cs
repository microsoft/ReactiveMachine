// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal interface IPartitionEffect
    {
        IEnumerable<IStateInfo> AffectedStates { get; }

        uint Locate(Process process);

        IPartitionKey UntypedKey { get; }
    }

    internal class PartitionEffect<TKey> : IPartitionEffect
    {
        public PartitionKey<TKey> Key;

        public IEnumerable<IStateInfo> States;

        IPartitionKey IPartitionEffect.UntypedKey => Key;

        public IEnumerable<IStateInfo> AffectedStates => States;

        public uint Locate(Process process)
        {
            return Key.Locate(process);
        }
    }


   

}
