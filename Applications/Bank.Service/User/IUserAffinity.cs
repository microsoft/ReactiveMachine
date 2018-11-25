// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bank.Service
{
    public interface IUserAffinity : 
        IPartitionedAffinity<IUserAffinity,string>
    {
        string UserId { get; }
    }


  
}
