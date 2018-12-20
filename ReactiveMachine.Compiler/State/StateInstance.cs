// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal interface IStateInstance
    {
        object Execute(dynamic op, ulong opid, bool evt);
    }

    internal interface IStateInstance<TState, TAffinity>   
       where TState : IState<TAffinity>, new()
       where TAffinity : IAffinitySpec<TAffinity>
    {
        void Restore(TState state);

        TState State { get; } 
    }

    internal class StateContext<TState, TAffinity, TKey> :
        IStateInstance,
        IStateInstance<TState, TAffinity>,
        ISubscriptionContext<TKey>,
        IUpdateContext<TState>,
        ILogger
        where TState : IState<TAffinity>, new()
        where TAffinity : IAffinitySpec<TAffinity>

    {
        protected Process process;
        protected TState state;
        protected TKey key;

        public ulong CurrentOpid = 0;

        public StateContext(Process process, TKey key)
        {
            this.process = process;
            this.state = new TState();
            this.key = key;
        }

        public void Restore(TState state)
        {
            this.state = state;
        }

        public TState State => state;

        TKey ISubscriptionContext<TKey>.Key => key;

        TState IReadContext<TState>.State => state;

        IExceptionSerializer IContext.ExceptionSerializer => process.Serializer;

        void IContextWithForks.ForkEvent<TEvent>(TEvent evt)
        {
            if (!process.Events.TryGetValue(typeof(TEvent), out var eventInfo))
                throw new BuilderException($"undefined event type {typeof(TEvent).FullName}.");
            var effects = eventInfo.GetEffects(evt);
            var opid = process.NextOpid;
            if (effects.Count != 0)
            {
                var destination = effects[0].Locate(process);
                var message = eventInfo.CreateMessage(true, evt, 0);
                message.Opid = opid;
                message.Parent = CurrentOpid;
                process.Send(destination, message);

                process.Telemetry?.OnApplicationEvent(
                         processId: process.ProcessId,
                         id: opid.ToString(),
                         name: evt.ToString(),
                         parent: CurrentOpid.ToString(),
                         opSide: OperationSide.Fork,
                         opType: OperationType.Event,
                         duration: 0
                     );
            }
        }

        void IContextWithForks.ForkOrchestration<TReturn>(IOrchestration<TReturn> orchestration)
        {
            if (!process.Orchestrations.TryGetValue(orchestration.GetType(), out var orchestrationInfo))
                throw new BuilderException($"undefined orchestration type {orchestration.GetType().FullName}.");
            orchestrationInfo.CanExecuteLocally(orchestration, out uint destination);
            var opid = process.NextOpid;
            var message = orchestrationInfo.CreateForkMessage(orchestration);
            message.Opid = opid;
            message.Parent = CurrentOpid;
            process.Send(destination, message);

            process.Telemetry?.OnApplicationEvent(
                     processId: process.ProcessId,
                     id: opid.ToString(),
                     name: orchestration.ToString(),
                     parent: CurrentOpid.ToString(),
                     opSide: OperationSide.Fork,
                     opType: OperationType.Orchestration,
                     duration: 0
                 );
        }

        void IContextWithForks.ForkUpdate<TState1, TReturn>(IUpdate<TState1, TReturn> update)
        {
            if (!process.States.TryGetValue(typeof(TState1), out var stateInfo))
                throw new BuilderException($"undefined state {typeof(TAffinity).FullName}.");
            var destination = stateInfo.AffinityInfo.LocateAffinity(update);
            var opid = process.NextOpid;
            var message = stateInfo.CreateLocalMessage(update, CurrentOpid, true);
            message.Opid = opid;
            message.Parent = CurrentOpid;
            process.Send(destination, message);

            process.Telemetry?.OnApplicationEvent(
                 processId: process.ProcessId,
                 id: opid.ToString(),
                 name: update.ToString(),
                 parent: CurrentOpid.ToString(),
                 opSide: OperationSide.Fork,
                 opType: OperationType.Local,
                 duration: 0
             );
        }

        void IContextWithForks.GlobalShutdown()
        {
            process.HostServices.GlobalShutdown();
        }

        public object Execute(dynamic op, ulong opid, bool evt)
        {
            CurrentOpid = opid;
            try
            {
                if (evt)
                {
                    dynamic s = State;
                    s.On(this, op);
                    return null;
                }
                else
                {
                    return op.Execute(this);
                }
            }
            catch (Exception e)
            {
                // TODO for events, what with that?
                return process.Serializer.SerializeException(e);
            }
            finally
            {
                CurrentOpid = 0;
            }
        }

        TReturn IUpdateContext<TState>.PerformUpdate<TReturn>(IUpdate<TState, TReturn> update)
        {
            return update.Execute(this);
        }

        TReturn IReadContext<TState>.PerformRead<TReturn>(IRead<TState, TReturn> read)
        {
            return read.Execute(this);
        }

        TConfiguration IContext.GetConfiguration<TConfiguration>()
        {
            return (TConfiguration)process.Configurations[typeof(TConfiguration)];
        }


        public ILogger Logger => this;

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

    }

}
