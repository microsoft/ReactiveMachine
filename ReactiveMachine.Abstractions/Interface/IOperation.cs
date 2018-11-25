// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// An operation.
    /// </summary>
    public interface IOperation
    {
    }

    /// <summary>
    /// An operation whose placement can be assigned.
    /// </summary>
    public interface INonAffineOperation
    {
    }

    /// <summary>
    /// An activity is an operation that executes outside of the reliable substrate.
    /// </summary>
    public interface IActivity : IOperation, INonAffineOperation
    {
    }

    /// <summary>
    /// An orchestration, or workflow, is a composition of operations that executes reliably from beginning to end.
    /// </summary>
    public interface IOrchestration : IOperation, INonAffineOperation
    {
    }

    /// <summary>
    /// A saga is a reversible orchestration: if aborted, all component steps are undone.
    /// </summary>
    public interface ISaga : IOperation, INonAffineOperation
    {
    }

    /// <summary>
    /// A transaction is a saga that locks all affinities, thereby achieving serializability.
    /// </summary>
    public interface ITransaction : IOperation, INonAffineOperation
    {
    }

    /// <summary>
    /// A local operation reads and/or updates state.
    /// </summary>
    public interface ILocalOperation<TState> : IOperation
    where TState : IState
    {
    }


}