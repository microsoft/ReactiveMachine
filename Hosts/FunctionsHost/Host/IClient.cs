using ReactiveMachine;
using ReactiveMachine.Compiler;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    /// <summary>
    /// Provides access to the a reactive machine service running on the functions host.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Fork an orchestration.
        /// </summary>
        /// <param name="orchestration">The orchestration to perform.</param>
        /// <returns>a task that completes when the orchestration has been durably queued.</returns>
        Task ForkOrchestration(IOrchestration orchestration);

        /// <summary>
        /// Fork an update.
        /// </summary>
        /// <param name="update">The update to perform.</param>
        /// <returns>a task that completes when the update has been durably queued.</returns>
        Task ForkUpdate<TState, TReturn>(IUpdate<TState, TReturn> update) where TState:IState;

        /// <summary>
        /// Perform an orchestration.
        /// </summary>
        /// <typeparam name="TReturn">The type of the result</typeparam>
        /// <param name="orchestration">The orchestration to perform</param>
        /// <returns>a task that completes after the orchestration has finished, with the result.</returns>
        Task<TReturn> PerformOrchestration<TReturn>(IOrchestration<TReturn> orchestration);

        /// <summary>
        /// Perform an update operation.
        /// </summary>
        /// <param name="update">The update to perform.</param>
        /// <returns>a task that completes after the update has finished, with the result.</returns>
        Task<TReturn> PerformUpdate<TState, TReturn>(IUpdate<TState, TReturn> update) where TState : IState;

        /// <summary>
        /// Peform a read operation.
        /// </summary>
        /// <param name="update">The read to perform.</param>
        /// <returns>a task that returns the result of the read operation</returns>
        Task<TReturn> PerformRead<TState, TReturn>(IRead<TState, TReturn> read) where TState : IState;
    }


    internal interface IClientInternal
    {
        void ProcessResult<TResult>(Guid clientRequestId, TResult result, ExceptionResult exceptionResult);
    }
}