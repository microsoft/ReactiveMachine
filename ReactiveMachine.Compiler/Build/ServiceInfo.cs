// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal interface IServiceInfo  
    {
        bool BuildComplete { get;  }

    }

    internal class ServiceInfo<TService> : IServiceInfo
        where TService: IServiceBuildDefinition, new()
    {
        private readonly Process process;
        private readonly TService instance;

        public bool BuildComplete { get; private set; }

        public ServiceInfo(Process process, ServiceBuilder serviceBuilder)
        {
            this.process = process;
            serviceBuilder.Services[typeof(TService)] = this;
            instance = new TService();

            BuildComplete = false;
            instance.Build(serviceBuilder);
            BuildComplete = true;
        }
 
  
    }
    
}
