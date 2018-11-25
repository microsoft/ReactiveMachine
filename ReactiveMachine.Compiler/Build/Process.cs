// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ReactiveMachine.Compiler
{
    internal class Process : IProcess
    {
        // service
        public Dictionary<Type, IServiceInfo> Services = new Dictionary<Type, IServiceInfo>();
        public Dictionary<Type, IAffinityInfo> Affinities = new Dictionary<Type, IAffinityInfo>();
        public Dictionary<Type, IPartitionLock> PartitionLocks = new Dictionary<Type, IPartitionLock>();
        public Dictionary<Type, IStateInfo> States = new Dictionary<Type, IStateInfo>();
        public Dictionary<Type, IOrchestrationInfo> Orchestrations = new Dictionary<Type, IOrchestrationInfo>();
        public Dictionary<Type, IActivityInfo> Activities = new Dictionary<Type, IActivityInfo>();
        public Dictionary<Type, IEventInfo> Events = new Dictionary<Type, IEventInfo>();
        public List<IOrchestration> StartupOrchestrations = new List<IOrchestration>();

        // indexes
        public List<IAffinityInfo> AffinityIndex = new List<IAffinityInfo>();

        // topology
        public uint ProcessId;
        public uint NumberProcesses { get; internal set; }
        private uint Pad;

        // orchestrations in progress
        public Dictionary<ulong, IOrchestrationState> OrchestrationStates = new Dictionary<ulong, IOrchestrationState>();
        public bool IsPrimary;
        public Dictionary<ulong, ActivityState> PendingActivities = new Dictionary<ulong, ActivityState>();
        public Dictionary<ulong, FinishState> FinishStates = new Dictionary<ulong, FinishState>();

        internal struct ActivityState
        {
            public Task Task;
            public string Name;
        }

        // clocks
        public ulong DeliveryCounter;
        public ulong OperationCounter;
        public ulong[] LowerBounds;
        public Guid InstanceId { get; private set; }

        // context during execution of Process method
        private List<Message> loopback = new List<Message>();
        private List<Message> incoming = new List<Message>();

        // runtime services
        internal HostServices HostServices;
        internal Serializer Serializer;
        internal IDeepCopier DeepCopier;
        internal ITelemetryListener Telemetry;
        internal Dictionary<Type, object> Configurations;

        // logging
        internal LoggingConfiguration LoggingConfiguration;
        internal ILogger RuntimeLogger;
        internal ILogger ApplicationLogger;

        // internal runtime logging
        internal Action<String> SnapshotTracer;
        internal Action<String> RecordReplayTracer;
        internal Action<String> SendTracer;
        internal Action<String> ReceiveTracer;
        internal Action<String> ActivityTracer;
        internal Action<String> ProgressTracer;
        internal Action<String> LockTracer;

        public Process(uint ProcessId, HostServices hostServices)
        {
            this.ProcessId = ProcessId;
            this.HostServices = hostServices;
            this.Configurations = new Dictionary<Type, object>();
        }

        public ulong NextOpid => ++OperationCounter * Pad + ProcessId;

        public bool RequestsOutstanding()
        {
            if (PendingActivities.Count > 0)
            {
                var a = PendingActivities.First();
                ProgressTracer?.Invoke($"Waiting for activity o{a.Key:d10}-{a.Value.Name}");
                return true;
            }
            if (OrchestrationStates.Count > 0)
            {
                var op = OrchestrationStates.First().Value.WaitingFor;
                while (OrchestrationStates.ContainsKey(op.Key))
                    op = OrchestrationStates[op.Key].WaitingFor;
                ProgressTracer?.Invoke($"Waiting for o{op.Key:d10}-{op.Value}");
                return true;
            }
            return false;
        }
 
        public uint GetOrigin(ulong opid)
        {
            return (uint) (opid % Pad);
        }
        public void LamportMerge(ulong opid)
        {
            if (opid / Pad > OperationCounter)
                OperationCounter = (opid / Pad) + 1;
        }

        public void LowerBoundUpdate(ulong opid)
        {
            var origin = GetOrigin(opid);
            if (LowerBounds[origin] < opid)
                LowerBounds[origin] = opid;
        }
  
        public void HandleGlobalException(Exception e)
        {
            if (HostServices.GlobalExceptionHandler != null)
            {
                HostServices.GlobalExceptionHandler(e);
            }
            else
            {
                RuntimeLogger.LogError($"!!! Unhandled Exception: {e}");
            }
        }

        internal void ConfigureLogging()
        {
            RuntimeLogger = HostServices.RuntimeLogger;
            ApplicationLogger = HostServices.ApplicationLogger;

            if (!Configurations.TryGetValue(typeof(LoggingConfiguration), out var c))
                c = new LoggingConfiguration();
            LoggingConfiguration = (LoggingConfiguration)c;

            if (RuntimeLogger != null)
            {
                SnapshotTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.SnapshotsLogLevel);
                RecordReplayTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.RecordReplayLogLevel);
                SendTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.SendLogLevel);
                ReceiveTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.ReceiveLogLevel);
                ActivityTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.ActivitiesLogLevel);
                ProgressTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.ProgressLogLevel);
                LockTracer = GetTracerFor(RuntimeLogger, LoggingConfiguration.LockLogLevel);
            }
        }
        private static Action<string> GetTracerFor(ILogger logger, LogLevel level)
        {
            if (level != LogLevel.None && logger.IsEnabled(level))
                return (msg) => logger.Log(level, msg);
            else
                return null;
        }

        public void FinalizePlacement()
        {
            // add serializable types

            foreach (var k in Affinities)
                foreach (var c in k.Value.SerializableTypes())
                    HostServices.SerializableTypeSet.Add(c);
            foreach (var t in PartitionLocks)
                foreach (var c in t.Value.SerializableTypes())
                    HostServices.SerializableTypeSet.Add(c);
            foreach (var v in States)
                foreach (var c in v.Value.SerializableTypes())
                    HostServices.SerializableTypeSet.Add(c);
            foreach (var o in Orchestrations)
                foreach (var c in o.Value.SerializableTypes())
                    HostServices.SerializableTypeSet.Add(c);
            foreach (var x in Activities)
                foreach (var c in x.Value.SerializableTypes())
                    HostServices.SerializableTypeSet.Add(c);
            foreach (var e in Events)
                foreach (var c in e.Value.SerializableTypes())
                    HostServices.SerializableTypeSet.Add(c);
           

            HostServices.SerializableTypeSet.Add(typeof(DataContractSerializedExceptionResult));
            HostServices.SerializableTypeSet.Add(typeof(ClassicallySerializedExceptionResult));
            HostServices.SerializableTypeSet.Add(typeof(NonserializedExceptionResult));
            HostServices.SerializableTypeSet.Add(typeof(List<IRestorable>));
            HostServices.SerializableTypeSet.Add(typeof(UnitType));
            HostServices.SerializableTypeSet.Add(typeof(RequestFinish));
            HostServices.SerializableTypeSet.Add(typeof(FinishState));
            HostServices.SerializableTypeSet.Add(typeof(AckFinish));
            HostServices.SerializableTypeSet.Add(typeof(RespondToActivity));

            // finalize partition placement
            foreach (var keyInfo in Affinities.Values)
                keyInfo.FinalizePlacement();

            // determine padding for operations
            Pad = 1000;
            while (Pad < NumberProcesses)
                Pad = Pad * 10;
        }

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            foreach (var o in Orchestrations.ToList())
                o.Value.DefineExtensions(serviceBuilder);
            foreach (var a in Activities.ToList())
                a.Value.DefineExtensions(serviceBuilder);
            foreach (var u in States.ToList())
                u.Value.DefineExtensions(serviceBuilder);
        }

        public void SaveState(out byte[] snapshot, out string label)
        {
            List<byte[]> serialized = new List<byte[]>();

            SnapshotTracer?.Invoke($"Saving Process");
            var s = new Snapshot();
            SaveToSnapshot(s);
            label = s.ToString();
            snapshot = Serializer.SerializeSnapshot(s);
            SnapshotTracer?.Invoke($"Saved snapshot {s} as byte[{snapshot.Length}]");
        }

        public void FirstStart()
        {
            if (ProcessId == 0 && DeliveryCounter == 0)
                ProcessMessage(new EnqueueStartup());
        }

        public void Restore(byte[] bytes, out string label)
        {
            ClearState();
            SnapshotTracer?.Invoke($"Restoring from byte[{bytes.Length}], instanceid={InstanceId}");
            try
            {
                var snapshot = Serializer.DeserializeSnapshot(bytes);
                snapshot.RestoreStateTo(this);
                SnapshotTracer?.Invoke($"Restored snapshot {snapshot}");
                label = snapshot.ToString();
                return;
            }
            catch
            {
                RuntimeLogger.LogCritical($"!!! Reactive Machine: Restore snapshot failed");
                throw;
            }
        }

        public void BecomePrimary()
        {
            if (!IsPrimary)
            {
                SnapshotTracer?.Invoke($"Becoming Primary");

                IsPrimary = true;
                foreach (var t in PendingActivities)
                {
                    ActivityTracer?.Invoke($"   Restoring activity o{t.Key:D10}");
                    t.Value.Task.Start();
                }
            }
        }

        // TODO avoid memory leak on secondaries

        internal void AddActivity(ulong opid, string name, Task task)
        {
            PendingActivities.Add(opid, new ActivityState() { Name = name, Task = task });
            if (IsPrimary)
            {
                task.Start();
            }
        }

        internal void RemoveActivity(ulong opid)
        {
            PendingActivities.Remove(opid);
        }

        public void SaveToSnapshot(Snapshot s)
        {
            s.ProcessId = ProcessId;
            s.ClockState = new ClockState()
            {
                DeliveryCounter = DeliveryCounter,
                OperationCounter = OperationCounter,
                LowerBounds = LowerBounds
            };
            s.StatePieces = new List<IRestorable>();
            foreach (var x in States.Values)
                x.SaveStateTo(s);
            foreach (var x in PartitionLocks.Values)
                x.SaveStateTo(s);
            foreach (var x in OrchestrationStates.Values)
                x.SaveStateTo(s);
            foreach (var x in FinishStates.Values)
                x.SaveStateTo(s);
        }

        public void ClearState()
        {
            foreach (var x in States.Values)
                x.ClearState();
            foreach (var x in PartitionLocks.Values)
                x.ClearState();

            OrchestrationStates.Clear();
            IsPrimary = false;
            PendingActivities.Clear();

            DeliveryCounter = 0;
            OperationCounter = 0;
            LowerBounds = new ulong[NumberProcesses];
            InstanceId = Guid.NewGuid();
        }

       
        public void Send(uint destination, Message m)
        {
            SendTracer?.Invoke($"   {m} -->p{destination:D3}");

            if (destination == ProcessId)
            {
                loopback.Add(m);
            }
            else
            {
                HostServices.Send(destination, m);
            }
        }

        public void ProcessMessage(IMessage message)
        {
            DeliveryCounter++;
            incoming.Add((Message)message);
           
            while (true)
            {
                foreach (var msg in incoming)
                {
                    ReceiveTracer?.Invoke($"p{ProcessId:D3} {msg.ToString()}");


                    try
                    {
                        //LowerBoundUpdate(msg.Opid);
                        LamportMerge(msg.Clock);
                        msg.Apply(this);
                    }
                    catch (Exception e)
                    {
                        RuntimeLogger.LogCritical($"!!! Reactive Machine: internal exception {e}");
                    }
                }

                incoming.Clear();

                if (loopback.Count > 0)
                {
                    // read responses must be copied, otherwise we get aliasing problems
                    // that break application invariants and replay
                    foreach (var msg in loopback)
                        if (msg is ResponseMessage responseMessage)
                            responseMessage.AntiAlias(DeepCopier);

                    // swap
                    var temp = loopback;
                    loopback = incoming;
                    incoming = temp;
                    continue;
                }
                else
                {
                    return;
                }
            }
        }

    }
}
