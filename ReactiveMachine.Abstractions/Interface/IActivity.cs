// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// An activity that returns a value of type {TReturn}. An activity executes at least once, and may (in rare failure cases) execute more than once.
    /// </summary>
    public interface IActivity<TReturn> : IActivity
    {
        TimeSpan TimeLimit { get; }

        Task<TReturn> Execute(IContext context);
    }
 
   

   
   
}
