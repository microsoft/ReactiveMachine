// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public interface IApplicationCompiler
    {
        IApplicationCompiler AddService<TService>()
            where TService : IServiceBuildDefinition, new();

        IApplicationCompiler AddBuildStep(Action<IServiceBuilder> buildStep);

        IApplicationCompiler SetConfiguration<TConfiguration>(TConfiguration configuration);

        IApplicationCompiler OverridePlacement(Action<IPlacementBuilder> placement);

        ICompiledApplication Compile(uint numberOfProcesses);
    }

}
