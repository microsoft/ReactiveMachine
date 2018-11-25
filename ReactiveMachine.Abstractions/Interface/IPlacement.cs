// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
   
    public interface IUseConsistentHash 
    {
        ulong GetHashInput();
    }

    public interface IUseCustomPlacement
    {
        uint GetProcess(uint NumberProcesses);
    }


 
}
