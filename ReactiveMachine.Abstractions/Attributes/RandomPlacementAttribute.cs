// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    /// <summary>
    /// If placed on an orchestration, each instance of that orchestration is executed on a randomly selected process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DistributeAttribute : System.Attribute
    {
    }
}
