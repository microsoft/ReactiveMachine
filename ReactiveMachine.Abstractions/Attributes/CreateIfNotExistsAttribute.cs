// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    /// <summary>
    /// If placed on the Execute method of an orchestration, the orchestration acuqires
    /// locks on all of its affinities prior to executing the body. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CreateIfNotExistsAttribute : Attribute
    {
    }
}
