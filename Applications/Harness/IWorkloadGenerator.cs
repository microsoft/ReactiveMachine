// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Harness
{
    public interface IWorkloadGenerator 
    {

        IOrchestration<UnitType> GenerateInitialization(uint generatorNumber);

        IOrchestration<UnitType> GenerateRequest(uint generatorNumber, uint iterationNumber);

    }
}
