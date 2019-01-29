// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal interface IActivityInfo
    {
        IEnumerable<Type> SerializableTypes();

        IAffinityInfo PlacementAffinity { set; }

        bool DistributeRandomly { set; }

        bool RequiresLocks(object request, out List<IPartitionKey> list);

        bool CanExecuteLocally(object request, ulong opid, out uint destination);

        void DefineExtensions(IServiceBuilder serviceBuilder);

        void ProcessRequest(RequestMessage message);

        RequestMessage CreateRequestMessage(object activity);
    }

    internal class ActivityInfo<TRequest, TReturn> : IActivityInfo, IContext, ILogger
        where TRequest : IActivity<TReturn>
    {
        private readonly Process process;

        public IAffinityInfo PlacementAffinity { private get; set; }

        public bool DistributeRandomly { private get; set; }

        private readonly bool requirelocks;

        public List<IAffinityInfo> AffinityList;


        public ActivityInfo(Process process)
        {
            this.process = process;
            process.Activities[typeof(TRequest)] = this;

            // use reflection to obtain affinity and locking information
            var canRouteToPrefix = ReflectionServiceBuilder.GetGenericTypeNamePrefix(typeof(ICanRouteTo<>));
            foreach (var i in typeof(TRequest).GetInterfaces())
                if (ReflectionServiceBuilder.GetGenericTypeNamePrefix(i) == canRouteToPrefix)
                {
                    var gt = i.GenericTypeArguments;
                    var affinityInfo = process.Affinities[gt[0]];
                    (AffinityList ?? (AffinityList = new List<IAffinityInfo>())).Add(affinityInfo);
                }

            var method = typeof(TRequest).GetMethod("Execute");
            requirelocks = method.GetCustomAttributes(typeof(LockAttribute), false).Count() > 0;

            if (requirelocks && (AffinityList == null || AffinityList.Count == 0))
                throw new BuilderException($"To use {nameof(LockAttribute)} on Execute function of {typeof(TRequest).FullName}, you must define at least one affinity.");
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TRequest);
            yield return typeof(TReturn);
            yield return typeof(PerformActivity<TRequest>);
        }

        IExceptionSerializer IContext.ExceptionSerializer => process.Serializer;

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            Extensions.Register.DefineActivityExtensions<TRequest, TReturn>(serviceBuilder);
        }

        public bool CanExecuteLocally(object request, ulong opid, out uint destination)
        {
            if (PlacementAffinity != null)
            {
                destination = PlacementAffinity.LocateAffinity(request);
            }
            else if (DistributeRandomly)
            {
                destination = (uint)(FNVHash.ComputeHash(opid) % process.NumberProcesses);
            }
            else
            {
                destination = process.ProcessId;
            }
            return destination == process.ProcessId;
        }

        public RequestMessage CreateRequestMessage(object activity)
        {
            return new PerformActivity<TRequest>()
            {
                Request = (TRequest)activity,
            };
        }

        public void ProcessRequest(RequestMessage req)
        {
            var request = (PerformActivity<TRequest>)req;

            new ActivityState<TRequest, TReturn>(process, request);
        }

        public async Task<TReturn> RunWithTimeout(TRequest request)
        {
            var timelimit = request.TimeLimit;

            var taskToComplete = Task.Run(() => request.Execute(this));

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

        public bool RequiresLocks(object request, out List<IPartitionKey> list)
        {
            if (!requirelocks)
            {
                list = null;
                return false;
            }
            else
            {
                if (AffinityList.Count == 1)
                    list = AffinityList[0].GetAffinityKeys(request).ToList();
                else
                {
                    list = new List<IPartitionKey>();
                    foreach (var a in AffinityList)
                        foreach (var k in a.GetAffinityKeys(request))
                            list.Add(k);
                }
                return true;
            }
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