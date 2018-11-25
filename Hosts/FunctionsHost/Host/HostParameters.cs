// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    public class GlobalParameters
    {
        public FunctionsHostConfiguration HostConfiguration { get; set; }

        public ICompiledApplication Application { get; set; }

        public Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        public string LocalLogFileDirectory { get; set; }

        public string HostName;

        public Guid InvocationId;

        public Stopwatch Stopwatch;
    }

    public class ResumeParameters
    {

        public uint ProcessId { get; set; }

        public string DeploymentId { get; set; }

    }
}
