// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsHost
{
    public interface IStaticApplicationInfo
    {
        string GetDeploymentId(DateTime deploymentTimestamp);

        FunctionsHostConfiguration GetHostConfiguration();

        ICompiledApplication Build(IApplicationCompiler compiler);
    }
}
