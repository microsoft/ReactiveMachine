// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ReactiveMachine;

namespace HelloWorld.Service
{
    /// <summary>
    /// Provides the building instructions for the hello world service
    /// </summary>
    public class HelloWorldService : ReactiveMachine.IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            // we build this service by automatically scanning the project for declarations
            builder.ScanThisDLL();
        }
    }
}
