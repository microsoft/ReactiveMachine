// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miner.Service
{
    // The job runs on a single process
    public interface IJobAffinity : ISingletonAffinity<IJobAffinity>
    {
    }

}
