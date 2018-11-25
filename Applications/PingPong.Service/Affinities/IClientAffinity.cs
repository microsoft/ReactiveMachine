// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace PingPong.Service
{
    public interface IClientAffinity 
        : ISingletonAffinity<IClientAffinity>
    {
    }

}
