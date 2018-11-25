// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Harness
{
    public interface IGeneratorAffinity : 
        IPartitionedAffinity<IGeneratorAffinity,uint>
    {
        [RoundRobinPlacement]
        uint GeneratorNumber { get; }
    }


   
}
