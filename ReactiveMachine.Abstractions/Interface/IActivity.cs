// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// An activity that returns a value of type {TReturn}.
    /// </summary>
    public interface IActivityBase<TReturn> : IActivity
    {
        TimeSpan TimeLimit { get; }

        Task<TReturn> Execute(IContext context);
    }

    /// <summary>
    /// An activity that executes at least once, and may (in rare failure cases) execute more than once.
    /// </summary>
    public interface IAtLeastOnceActivity<TReturn> : IActivityBase<TReturn>
    {
    }

    /// <summary>
    /// An activity that executes either once, or (in the rare case of uncertainty) executes a custom handler
    /// </summary>
    public interface IAtMostOnceActivity<TReturn> : IActivityBase<TReturn>
    {
        Task<TReturn> AfterFault(IContext context);
    }

   
   
}
