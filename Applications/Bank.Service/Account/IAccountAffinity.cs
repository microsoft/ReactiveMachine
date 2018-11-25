// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ReactiveMachine;

namespace Bank.Service
{
    public interface IAccountAffinity :
        IPartitionedAffinity<IAccountAffinity,Guid>
    {
        Guid AccountId { get; }
    }


   
    
}
