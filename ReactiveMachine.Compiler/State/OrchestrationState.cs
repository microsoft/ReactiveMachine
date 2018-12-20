// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal interface IOrchestrationState : IRestorable
    {
        void Continue(ulong opid, ulong clock, MessageType type, object value);

        void SaveStateTo(Snapshot snapshot);

        Task<T> Activity<T>(Func<Guid, Task<T>> body, string name);

        KeyValuePair<ulong, string> WaitingFor { get; }
    }

    [DataContract]
    internal class OrchestrationState<TRequest, TReturn> : IOrchestrationState, IOrchestrationContext, IReadOrchestrationContext, ILogger
        where TRequest : IOrchestrationBase<TReturn>
    {
        public OrchestrationState(ForkOperation<TRequest> request)
        {
            this.Opid = request.Opid;
            this.Request = request.Request;
            this.Parent = request.Parent;
            this.StartingClock = request.Clock;
            this.ExpectsResponse = !request.MessageType.IsFork();
            this.LockedByCaller = request.LockedByCaller;
            this.History = new List<Record>();
            this.Continuations = new Dictionary<ulong, IContinuationInfo>();
        }

        [DataMember]
        public readonly ulong Opid;

        [DataMember]
        public readonly TRequest Request;

        [DataMember]
        public readonly bool ExpectsResponse;

        [DataMember]
        internal bool LockedByCaller;

        [DataMember]
        public readonly ulong Parent;

        [DataMember]
        public readonly ulong StartingClock;

        [DataMember]
        internal readonly List<Record> History;

        [DataMember]
        internal SortedSet<uint> ForkedDestinations;

        [DataContract]
        internal struct Record
        {
            [DataMember]
            public ulong opid;
            [DataMember]
            public ulong clock;
            [DataMember]
            public MessageType type;
            [DataMember]
            public object value;

            public override string ToString()
            {
                return $"o{opid:D10} {type}";
            }
        }

        [IgnoreDataMember]
        private Process process;

        [IgnoreDataMember]
        private OrchestrationInfo<TRequest,TReturn> info;

        [IgnoreDataMember]
        internal int HistoryPosition;

        [IgnoreDataMember]
        internal ulong Clock;

        [IgnoreDataMember]
        internal Task Task;

        [IgnoreDataMember]
        private Dictionary<ulong, IContinuationInfo> Continuations;

        [IgnoreDataMember]
        private List<IPartitionKey> LockSet;

        [IgnoreDataMember]
        private ulong LockOpid;


        private interface IContinuationInfo
        {
            void Continue(OrchestrationState<TRequest, TReturn> state, ulong opid, ulong clock, MessageType type, object value);
        }

        private class ContinuationInfo<TReturn2> : IContinuationInfo
        {
            public TaskCompletionSource<TReturn2> Tcs;

            public Stopwatch Stopwatch;

            public string OpName;

            public OperationType OpType;

            public ContinuationInfo(string opName, OperationType opType)
            {
                Tcs = new TaskCompletionSource<TReturn2>(TaskContinuationOptions.ExecuteSynchronously);
                this.OpName = opName;
                this.OpType = opType;
                Stopwatch = new Stopwatch();
                Stopwatch.Start();
            }

            public void Continue(OrchestrationState<TRequest, TReturn> state, ulong opid, ulong clock, MessageType type, object value)
            {
                state.OnResult(type, opid, clock, Tcs, OpName, OpType, Stopwatch.Elapsed.TotalMilliseconds, value);
            }

            public override string ToString()
            {
                return OpName;
            }
        }

        IExceptionSerializer IContext.ExceptionSerializer => process.Serializer;


        public void StartOrResume(Process process, OrchestrationInfo<TRequest,TReturn> info)
        {
            this.process = process;
            this.info = info;
            Clock = StartingClock;
            Continuations = new Dictionary<ulong, IContinuationInfo>();

            Task = Run();

            CheckIfDone();
        }

        public void CheckIfDone()
        {
            if (Continuations.Count == 0)
            {
                if (!Task.IsCompleted)
                    throw new RuntimeException($"task must not call async operations other than the ones offered by {nameof(IOrchestrationContext)}");


                process.RecordReplayTracer?.Invoke($"   [o{Opid:D10}-Record] Finished");
                process.OrchestrationStates.Remove(this.Opid);
            }
        }

        public void SaveStateTo(Snapshot snapshot)
        {
            snapshot.StatePieces.Add(this);
        }

        public void RestoreStateTo(Process process)
        {
            process.RecordReplayTracer?.Invoke($"[o{Opid:D10}-Replay] Replaying o{Opid:D10} {typeof(TRequest)}");

            process.OrchestrationStates[Opid] = this;
            StartOrResume(process, (OrchestrationInfo<TRequest,TReturn>) process.Orchestrations[typeof(TRequest)]);
            Debug.Assert(Continuations.Count != 0);

            while (HistoryPosition < History.Count)
            {
                var next = History[HistoryPosition];
                Debug.Assert(next.type.IsResponse());
                Continue(next.opid, next.clock, next.type, next.value);
                Debug.Assert(Continuations.Count != 0);
            }
        }


        public void Continue(ulong opid, ulong clock, MessageType type, object value)
        {
            Continuations.TryGetValue(opid, out var continuationInfo);

            if (continuationInfo != null)
            {
                continuationInfo.Continue(this, opid, clock, type, value);
            }
            else if (type != MessageType.RespondToActivity)
            {
                throw new Exception("internal error: missing continuation");
            }
        }

        public override string ToString()
        {
            return $"{Opid}-{Request.GetType().FullName}";
        }

        public KeyValuePair<ulong, string> WaitingFor
        {
            get
            {
                var kvp = Continuations.FirstOrDefault();
                if (kvp.Value != null)
                    return new KeyValuePair<ulong, string>(kvp.Key, kvp.Value.ToString());
                else
                    return default(KeyValuePair<ulong, string>);
            }
        }


        public async Task<Random> NewRandom()
        {             
            return new Random(await Determinize(new Random().Next()));
        }

        public Task<Guid> NewGuid()
        {
            return Determinize(Guid.NewGuid());
        }

        public Task<DateTime> ReadDateTimeUtcNow()
        {
             return Determinize(DateTime.UtcNow);
        }

        public Task<DateTime> ReadDateTimeNow()
        {
            return Determinize(DateTime.Now);
        }

        public Task<T> Determinize<T>(T value)
        {
            return Activity<T>((replayed) => Task.FromResult(value), $"Determinize<{typeof(T).Name}>");
        }

        public TConfiguration GetConfiguration<TConfiguration>()
        {
            return (TConfiguration)process.Configurations[typeof(TConfiguration)];
        }

        public void GlobalShutdown()
        {
            process.HostServices.GlobalShutdown?.Invoke();
        }

        public Task<T> Activity<T>(Func<Guid, Task<T>> body, string name)
        {
            // on send, always issue the activity (but pass recorded instance id, if replaying)
            RecordOrReplayCall(MessageType.RequestExternal, out var opid, out var clock, out var instanceId);
            process.AddActivity(opid, name, new Task(async () =>
            {
                var instanceIdOnStart = process.InstanceId;

                process.ActivityTracer?.Invoke($"   Starting activity o{opid:D10} {name} {instanceIdOnStart}");
                var stopwatch = process.Telemetry != null ? new Stopwatch() : null;
                stopwatch?.Start();

                object r;
                try
                {
                    r = await body(instanceId);
                }
                catch (Exception e)
                {
                    r = process.Serializer.SerializeException(e);
                }

                stopwatch?.Stop();
                process.ActivityTracer?.Invoke($"   Completed activity o{opid:D10} {name} {instanceIdOnStart}");

                process.Telemetry?.OnApplicationEvent(
                        processId: process.ProcessId,
                        id: opid.ToString(),
                        name: name,
                        parent: Opid.ToString(),
                        opSide: OperationSide.Caller,
                        opType: OperationType.Activity,
                        duration: stopwatch.Elapsed.TotalMilliseconds
                    );

                process.HostServices.Send(process.ProcessId, new RespondToActivity()
                {
                    Opid = opid,
                    Parent = this.Opid,
                    Clock = clock,
                    InstanceId = instanceIdOnStart,
                    Result = r
                });
            }));

            // replay the return or create a continuation
            if (!ReplayReturn(MessageType.RespondToActivity, opid, out var result))
            {
                var continuationInfo = new ContinuationInfo<T>(name, OperationType.Activity);
                Continuations[opid] = continuationInfo;
                return continuationInfo.Tcs.Task;
            }
            else
            {
                process.RemoveActivity(opid);
                if (process.Serializer.DeserializeException(result, out var e))
                {
                    return Task.FromException<T>(e);
                }
                else
                {
                    return Task.FromResult((T)result);
                }
            }
        }

      

        public async Task Run()
        {
            Stopwatch messageProcessingTimer = new Stopwatch();
            messageProcessingTimer?.Start();

            if (info.RequiresLocks(Request, out var locks))
            {
                LockSet = locks;
                if (!LockedByCaller)
                    await AcquireLocks();
            }

            object result;
            try
            {
                if (Request is IReadOrchestration<TReturn> ro)
                {
                    result = await ro.Execute(this);
                }
                else
                {
                    result = await ((IOrchestration<TReturn>)Request).Execute(this);
                }

                if (Continuations.Count > 0)
                {
                    throw new RuntimeException("Operation completed prematurely. Must await all spawned tasks, or use ForkXXX or ScheduleXXX for detached operations.");
                }
            }
            catch (Exception e)
            {
                result = process.Serializer.SerializeException(e);
            }

            if (LockSet != null && !LockedByCaller)
            {
                ReleaseLocks();
            }

            messageProcessingTimer.Stop();
            var elapsed = messageProcessingTimer.Elapsed.TotalMilliseconds;

            if (ExpectsResponse)
            {
                var response = new RespondToOperation()
                {
                    Opid = Opid,
                    Result = result,
                    Clock = Clock,
                    Parent = Parent,
                };
                process.Send(process.GetOrigin(Opid), response);
            }
            else if (process.FinishStates.TryGetValue(Parent, out var finishState))
            {
                finishState.RemovePending(Opid);
            }

            process.Telemetry?.OnApplicationEvent(
                    processId: process.ProcessId,
                    id: Opid.ToString(),
                    name: Request.ToString(),
                    parent: Parent.ToString(),
                    opSide: OperationSide.Callee,
                    opType: OperationType.Orchestration,
                    duration: elapsed
                );

            if (!ExpectsResponse && process.Serializer.DeserializeException(result, out var exceptionResult))
            {
                process.HandleGlobalException(exceptionResult);
            }
        }

        private bool RecordOrReplayCall(MessageType type, out ulong opid, out ulong clock, out Guid instanceId)
        {
            if (HistoryPosition < History.Count)
            {
                var entry = History[HistoryPosition++];
                System.Diagnostics.Debug.Assert(entry.type == type);
                System.Diagnostics.Debug.Assert(entry.clock == Clock);
                clock = entry.clock;
                opid = entry.opid;
                instanceId = (entry.type == MessageType.RequestExternal) ? (Guid)entry.value : process.InstanceId;
                process.RecordReplayTracer?.Invoke($"[o{Opid:D10}-Replay]    {entry}");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.Assert(HistoryPosition == History.Count);
                opid = process.NextOpid;
                clock = Clock;
                var entry = new Record() { type = type, opid = opid, clock = Clock };
                if (type == MessageType.RequestExternal) entry.value = instanceId = process.InstanceId;
                History.Add(entry);
                HistoryPosition++;
                process.RecordReplayTracer?.Invoke($"   [o{Opid:D10}-Record]    {entry}");
                return false;
            }
        }

        private bool ReplayReturn(MessageType type, ulong opid, out object value)
        {
            if (HistoryPosition < History.Count && History[HistoryPosition].opid == opid)
            {
                var entry = History[HistoryPosition++];
                System.Diagnostics.Debug.Assert(entry.type == type);
                System.Diagnostics.Debug.Assert(entry.clock >= Clock);
                value = entry.value;
                Clock = entry.clock;
                process.RecordReplayTracer?.Invoke($"[o{Opid:D10}-Replay] Continue {entry}");
                return true;
            }
            else
            {
                if(!ExpectsResponse && Continuations.Count == 0)
                {
                    if (!process.FinishStates.TryGetValue(Parent, out var finishState))
                        process.FinishStates[Parent] = finishState = new FinishState(process, Parent);
                    finishState.AddPending(Opid);
                }                     
                value = null;
                return false;
            }
        }

        public void OnResult<TReturn2>(MessageType type, ulong opid, ulong clock, TaskCompletionSource<TReturn2> tcs, string opName, OperationType opType, double elapsed, object value)
        {
            if (HistoryPosition < History.Count)
            {
                process.RecordReplayTracer?.Invoke($"[o{Opid:D10}-Replay] Continue {History[HistoryPosition]}");
            }
            else
            {
                var entry = new Record() { type = type, opid = opid, clock = clock, value = value };
                History.Add(entry);
                process.RecordReplayTracer?.Invoke($"   [o{Opid:D10}-Record] Continue {entry}");
            }

            Clock = Math.Max(Clock, clock);
            HistoryPosition++;
            Continuations.Remove(opid);

            if (type == MessageType.RespondToActivity)
                process.RemoveActivity(opid);

            process.Telemetry?.OnApplicationEvent(
                processId: process.ProcessId,
                id: opid.ToString(),
                name: opName,
                parent: Opid.ToString(),
                opSide: OperationSide.Caller,
                opType: opType,
                duration: elapsed
             );

            if (process.Serializer.DeserializeException(value, out var e))
            {
                tcs.SetException(e);
            }
            else
            {
                tcs.SetResult((TReturn2)value);
            }

            CheckIfDone();
        }

        Task AcquireLocks()
        {
            if (!RecordOrReplayCall(MessageType.AcquireLock, out var opid, out var clock, out var instanceId))
            {
                var destination = LockSet[0].Locate(process);
                var message = new AcquireLock();
                message.Opid = opid;
                message.Clock = clock;
                message.Parent = this.Opid;
                message.LockSet = LockSet;
                process.Send(destination, message);
            }
            LockOpid = opid;
            if (!ReplayReturn(MessageType.GrantLock, opid, out var ignoredResult))
            {
                var continuationInfo = new ContinuationInfo<UnitType>("Lock", OperationType.Event);
                Continuations[opid] = continuationInfo;
                return continuationInfo.Tcs.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        void ReleaseLocks()
        {
            if (!RecordOrReplayCall(MessageType.ReleaseLock, out var opid, out var clock, out var instanceId))
            {
                foreach (var key in LockSet)
                {
                    var destination = key.Locate(process);
                    var message = new ReleaseLock() { Key = key, LockOpid = LockOpid};
                    message.Opid = opid;
                    message.Clock = clock;
                    message.Parent = this.Opid;
                    process.Send(destination, message);
                }
            }
        }

        public void ForkOrchestration<TReturn2>(IOrchestration<TReturn2> orchestration)
        {
            if (!RecordOrReplayCall(MessageType.ForkOperation, out var opid, out var clock, out var instanceId))
            {
                if (!process.Orchestrations.TryGetValue(orchestration.GetType(), out var orchestrationInfo))
                    throw new BuilderException($"undefined orchestration type {orchestration.GetType().FullName}.");
                orchestrationInfo.CanExecuteLocally(orchestration, out var destination);
                (ForkedDestinations ?? (ForkedDestinations = new SortedSet<uint>())).Add(destination);
                var message = orchestrationInfo.CreateForkMessage(orchestration);
                message.Opid = opid;
                message.Clock = clock;
                message.Parent = this.Opid;
                message.LockedByCaller = false;

                process.Send(destination, message);

                process.Telemetry?.OnApplicationEvent(
                     processId: process.ProcessId,
                     id: opid.ToString(),
                     name: orchestration.ToString(),
                     parent: Opid.ToString(),
                     opSide: OperationSide.Fork,
                     opType: OperationType.Orchestration,
                     duration: 0
                 );
            }
        }
      
        Task<TReturn2> IReadContext.PerformRead<TState, TReturn2>(IRead<TState, TReturn2> read)
        {
            return PerformLocal<TState, TReturn2>(read);
        }
        Task<TReturn2> IWriteContext.PerformUpdate<TState, TReturn2>(IUpdate<TState, TReturn2> update)
        {
            return PerformLocal<TState, TReturn2>(update);
        }
        private  Task<TReturn2> PerformLocal<TState, TReturn2>(object localOperation)
        {
            if (!RecordOrReplayCall(MessageType.RequestLocal, out var opid, out var clock, out var instanceId))
            {
                if (!process.States.TryGetValue(typeof(TState), out var stateInfo))
                    throw new BuilderException($"undefined state {typeof(TState).FullName}.");
                if (LockSet != null)
                {
                    var partitionKey = stateInfo.GetPartitionKeyForLocalOperation(localOperation);
                    if (!LockSet.Contains(partitionKey))
                        throw new SynchronizationDisciplineException("to perform a local operation in a locked context, the caller must include its affinity");
                }
                var destination = stateInfo.AffinityInfo.LocateAffinity(localOperation);
                var message = stateInfo.CreateLocalMessage(localOperation, this.Opid, false);
                message.Opid = opid;
                message.Clock = clock;
                message.Parent = this.Opid;
                message.LockedByCaller = (LockSet != null);
                process.Send(destination, message);
            }
            if (!ReplayReturn(MessageType.RespondToLocal, opid, out var result))
            {
                var continuationInfo = new ContinuationInfo<TReturn2>(localOperation.ToString(), OperationType.Local);
                Continuations[opid] = continuationInfo;
                return continuationInfo.Tcs.Task;
            }
            else
            {
                if (process.Serializer.DeserializeException(result, out var e))
                {
                    return Task.FromException<TReturn2>(e);
                }
                else
                {
                    return Task.FromResult((TReturn2)result);
                }
            }
        }

        void IContextWithForks.ForkUpdate<TState, TReturn2>(IUpdate<TState, TReturn2> update)
        {
            if (!RecordOrReplayCall(MessageType.ForkLocal, out var opid, out var clock, out var instanceId))
            {
                if (!process.States.TryGetValue(typeof(TState), out var stateInfo))
                    throw new BuilderException($"undefined state {typeof(TState).FullName}.");
                var destination = stateInfo.AffinityInfo.LocateAffinity(update);
                (ForkedDestinations ?? (ForkedDestinations = new SortedSet<uint>())).Add(destination);
                var message = stateInfo.CreateLocalMessage(update, this.Opid, true);
                message.Opid = opid;
                message.Clock = clock;
                message.Parent = this.Opid;
                message.LockedByCaller = false;
                process.Send(destination, message);

                process.Telemetry?.OnApplicationEvent(
                     processId: process.ProcessId,
                     id: opid.ToString(),
                     name: update.ToString(),
                     parent: Opid.ToString(),
                     opSide: OperationSide.Fork,
                     opType: OperationType.Local,
                     duration: 0
                 );
            }
        }

        public Task<TReturn2> PerformOrchestration<TReturn2>(IReadOrchestration<TReturn2> orchestration)
        {
            return PerformOrchestration<TReturn2>((IOrchestration<TReturn2>) orchestration);
        }

        public Task<TReturn2> PerformOrchestration<TReturn2>(IOrchestration<TReturn2> orchestration)
        {
            if (!RecordOrReplayCall(MessageType.RequestOperation, out var opid, out var clock, out var instanceId))
            {
                if (!process.Orchestrations.TryGetValue(orchestration.GetType(), out var orchestrationInfo))
                    throw new BuilderException($"undefined orchestration type {orchestration.GetType().FullName}.");
                if (LockSet != null)
                {
                    if (! orchestrationInfo.RequiresLocks(orchestration, out var locks))
                        throw new SynchronizationDisciplineException("an orchestration called from a locked context must be locked also");
                    foreach (var l in locks)
                            if (!LockSet.Contains(l))
                                throw new SynchronizationDisciplineException("to perform an orchestration in a locked context, the caller's affinities must contain the callee's affinities");
                }
                orchestrationInfo.CanExecuteLocally(orchestration, out uint destination);
                var message = orchestrationInfo.CreateRequestMessage(orchestration);
                message.Opid = opid;
                message.Clock = clock;
                message.Parent = this.Opid;
                message.LockedByCaller = (LockSet != null);
                process.Send(destination, message);
            }
            if (!ReplayReturn(MessageType.RespondToOperation, opid, out var result))
            {
                var continuationInfo = new ContinuationInfo<TReturn2>(orchestration.ToString(), OperationType.Orchestration);
                Continuations[opid] = continuationInfo;
                return continuationInfo.Tcs.Task;
            }
            else
            {
                if (process.Serializer.DeserializeException(result, out var e))
                {
                    return Task.FromException<TReturn2>(e);
                }
                else
                {
                    return Task.FromResult((TReturn2) result);
                }
            }
        }

        Task IOrchestrationContext.PerformEvent<TEvent>(TEvent evt)
        {
            if (!process.Events.TryGetValue(evt.GetType(), out var eventInfo))
                throw new BuilderException($"undefined event type {evt.GetType().FullName}.");
           
            var effects = eventInfo.GetEffects(evt);
            if (effects.Count == 0)
                return Task.CompletedTask; // event has no effects

            if (LockSet != null)
                foreach (var e in effects)
                    if (!LockSet.Contains(e.UntypedKey))
                        throw new SynchronizationDisciplineException("to perform an event in a locked context, the caller's affinities must contain the event's affinities");

            if (!RecordOrReplayCall(MessageType.PerformEvent, out var opid, out var clock, out var instanceId))
            {
                var destination = effects[0].Locate(process);
                var message = eventInfo.CreateMessage(false, evt, 0);
                message.Opid = opid;
                message.Clock = clock;
                message.Parent = this.Opid;
                message.LockedByCaller = (LockSet != null);
                process.Send(destination, message);
            }
            if (!ReplayReturn(MessageType.AckEvent, opid, out var ignoredResult))
            {
                var continuationInfo = new ContinuationInfo<UnitType>(evt.ToString(), OperationType.Event);
                Continuations[opid] = continuationInfo;
                return continuationInfo.Tcs.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }
        void IContextWithForks.ForkEvent<TEvent>(TEvent evt)
        {
            if (!process.Events.TryGetValue(evt.GetType(), out var eventInfo))
                throw new BuilderException($"undefined event type {evt.GetType().FullName}.");
            var effects = eventInfo.GetEffects(evt);
 
            if (effects.Count != 0)
            {
                if (!RecordOrReplayCall(MessageType.ForkEvent, out var opid, out var clock, out var instanceId))
                {
                    var destination = effects[0].Locate(process);
                    (ForkedDestinations ?? (ForkedDestinations = new SortedSet<uint>())).Add(destination);
                    var message = eventInfo.CreateMessage(true, evt, 0);
                    message.Opid = opid;
                    message.Clock = clock;
                    message.Parent = this.Opid;
                    message.LockedByCaller = false;
                    process.Send(destination, message);
                }

                process.Telemetry?.OnApplicationEvent(
                     processId: process.ProcessId,
                     id: opid.ToString(),
                     name: evt.ToString(),
                     parent: Opid.ToString(),
                     opSide: OperationSide.Fork,
                     opType: OperationType.Event,
                     duration: 0
                );

            }
        }
 

        public Task<T> PerformActivity<T>(IActivityBase<T> activity)
        {
            if (!process.Activities.TryGetValue(activity.GetType(), out var activityInfo))
                throw new BuilderException($"undefined activity type {activity.GetType().FullName}.");

            if (LockSet != null)
            {
                //TODO : check if the activity declares any affinities 
            }

            return ((IActivityInfo<T>)activityInfo).Perform(activity, this);
        }

        public Task Finish()
        {
            if (LockSet != null)
            {
                throw new SynchronizationDisciplineException($"{nameof(Finish)} cannot be called in a locked context");
            }
            if (ForkedDestinations == null)
            {
                return Task.CompletedTask;
            }
            var opIds = new ulong[ForkedDestinations.Count];
            int opIdPos = 0;
            foreach (var destination in ForkedDestinations)
            {
                if (!RecordOrReplayCall(MessageType.RequestFinish, out opIds[opIdPos], out var clock, out var instanceId))
                {
                    var message = new RequestFinish();
                    message.Opid = opIds[opIdPos];
                    message.Clock = clock;
                    message.Parent = this.Opid;
                    process.Send(destination, message);
                }
                opIdPos++;
            }
            var finishAcks = new Task[opIds.Length];
            for(int i = 0; i < opIds.Length; i++)
            {
                if (!ReplayReturn(MessageType.AckEvent, opIds[i], out var ignoredResult))
                {
                    var continuationInfo = new ContinuationInfo<UnitType>("finish", OperationType.Finish);
                    Continuations[opIds[i]] = continuationInfo;
                    finishAcks[i] = continuationInfo.Tcs.Task;
                }
            }
            return Task.WhenAll(finishAcks);
        }

         
        public ILogger Logger => this;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (HistoryPosition < History.Count)
            {
                if (process.RecordReplayTracer != null && process.LoggingConfiguration.ApplicationEvents <= logLevel)
                    process.RuntimeLogger.Log<TState>(logLevel, eventId, state, exception, (s, e) => $"[o{Opid:D10}-Replay]    ==== {formatter(s, e)}");
            }
            else
            {
                process.ApplicationLogger?.Log(logLevel, eventId, state, exception, formatter);

                if (process.LoggingConfiguration.ApplicationEvents <= logLevel)
                    process.RuntimeLogger.Log<TState>(logLevel, eventId, state, exception, (s, e) => $"   ==== {formatter(s, e)}");
            }
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
