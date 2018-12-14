// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal interface IActivityInfo : ISchemaElement
    {
        IEnumerable<Type> SerializableTypes();

        IAffinityInfo AffinitizationKey { set; }

        void DefineExtensions(IServiceBuilder serviceBuilder);
    }

    internal interface IActivityInfo<TReturn>
    {
        Task<TReturn> Perform(IActivityBase<TReturn> request, IOrchestrationState orchestrationState);
    }

    internal enum ActivityType
    {
        AtLeastOnce,
        AtMostOnce,
    }

    internal class ActivityInfo<TRequest, TReturn> : SchemaElement<TRequest>, IActivityInfo, IActivityInfo<TReturn>, IContext, ILogger
        where TRequest : IActivityBase<TReturn>
    {
        private readonly Process process;
        private readonly ActivityType type;

        //TODO: honor this when launching activities
        public IAffinityInfo AffinitizationKey { private get; set; }

        public ActivityInfo(Process process, ActivityType type)
        {
            this.process = process;
            this.type = type;
            process.Activities[typeof(TRequest)] = this;
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TRequest);
            yield return typeof(TReturn);
        }

        public override bool AllowVersionReplace => true;

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            Extensions.Register.DefineActivityExtensions<TRequest, TReturn>(serviceBuilder);
        }

        public Task<TReturn> Perform(IActivityBase<TReturn> r, IOrchestrationState orchestrationState)
        {
            var request = (TRequest)r;
            return orchestrationState.Activity(
                (originalInstanceId) => RunWithTimeout(request, originalInstanceId == process.InstanceId),
                request.ToString());
        }

        private async Task<TReturn> Execute(TRequest request, bool guaranteedFirst)
        {
            if (guaranteedFirst
                || type == ActivityType.AtLeastOnce)
            {
                return await request.Execute(this);
            }
            else if (type == ActivityType.AtMostOnce)
            {
                var a = (IAtMostOnceActivity<TReturn>)request;
                return await a.AfterFault(this);
            }
            else
            {
                throw new Exception("internal error: unmatched activity type");
            }
        }

        private async Task<TReturn> RunWithTimeout(TRequest request, bool guaranteedFirst)
        {
            var timelimit = request.TimeLimit;

            var taskToComplete = Task.Run(() => Execute(request, guaranteedFirst));

            var timeoutCancellationTokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(taskToComplete, Task.Delay(timelimit, timeoutCancellationTokenSource.Token));

            // We got done before the timeout, or were able to complete before this code ran, return the result
            if (taskToComplete == completedTask)
            {
                timeoutCancellationTokenSource.Cancel();
                // Await this so as to propagate the exception correctly
                return await taskToComplete;
            }

            // We did not complete before the timeout
            throw new TimeoutException(String.Format($"Activity {request} has timed out after {0}.", timelimit));
        }

        ILogger IContext.Logger => this;


        void ILogger.Log<TState1>(LogLevel logLevel, EventId eventId, TState1 state, Exception exception, Func<TState1, Exception, string> formatter)
        {
            if (process.LoggingConfiguration.ApplicationEvents <= logLevel)
            {
                process.RuntimeLogger.Log<TState1>(logLevel, eventId, state, exception, (s, e) => "   ==== " + formatter(s, e));
            }
            process.ApplicationLogger?.Log(logLevel, eventId, state, exception, formatter);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return process.ApplicationLogger?.IsEnabled(logLevel) ?? false;
        }

        IDisposable ILogger.BeginScope<TState1>(TState1 state)
        {
            return process.ApplicationLogger?.BeginScope<TState1>(state);
        }

        TConfiguration IContext.GetConfiguration<TConfiguration>()
        {
            return (TConfiguration)process.Configurations[typeof(TConfiguration)];
        }
    }
}