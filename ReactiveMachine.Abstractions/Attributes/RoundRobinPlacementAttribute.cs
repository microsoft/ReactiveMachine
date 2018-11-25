// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    /// <summary>
    /// If placed in an interface that declares an affinity, that affinity uses round-robin placement.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RoundRobinPlacementAttribute : Attribute
    {
        public uint ChunkSize { get; set; } = 1;
    }

}
