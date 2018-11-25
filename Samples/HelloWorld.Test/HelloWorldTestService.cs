// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ReactiveMachine;
using HelloWorld.Service;

namespace HelloWorld.Test
{
    /// <summary>
    /// Provides the building instructions for the hello world service
    /// </summary>
    public class HelloWorldTestService : ReactiveMachine.IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            // first we import the HelloWorld service
            builder.BuildService<HelloWorldService>();

            // we build this service by automatically scanning the project for declarations
            builder.ScanThisDLL();
        }
    }
}
