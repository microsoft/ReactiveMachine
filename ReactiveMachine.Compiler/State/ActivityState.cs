using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    internal interface IActivityState
    {
        void RecordResult(object result);

        void BecomePrimary();

        void SaveStateTo(Snapshot snapshot);

        string Name { get; }
    }



    [DataContract]
    internal class ActivityState<TRequest, TReturn> : IActivityState, IRestorable
        where TRequest : IActivity<TReturn>
    {
        [DataMember]
        private PerformActivity<TRequest> message;

        [IgnoreDataMember]
        private Process process;

        [IgnoreDataMember]
        private Task task;

        public string Name => message.Request.ToString();


        public ActivityState(Process process, PerformActivity<TRequest> message)
        {
            this.process = process;
            this.message = message;

            process.PendingActivities.Add(message.Opid, this);
    
            if (process.IsPrimary)
            {
                task = Start();
            }
        }

        public void BecomePrimary()
        {
            task = Start();
        }

        public void SaveStateTo(Snapshot snapshot)
        {
            snapshot.StatePieces.Add(this);
        }

        public void RestoreStateTo(Process process)
        {
            this.process = process;
        }

        private async Task Start()
        {
            process.ActivityTracer?.Invoke($"   Starting activity o{message.Opid:D10} {message.Request}");
            var stopwatch = process.Telemetry != null ? new Stopwatch() : null;
            stopwatch?.Start();

            var info = (ActivityInfo<TRequest, TReturn>)process.Activities[typeof(TRequest)];

            object r;
            try
            {
                r = await info.RunWithTimeout(message.Request);
            }
            catch (Exception e)
            {
                r = process.Serializer.SerializeException(e);
            }

            stopwatch?.Stop();
            process.ActivityTracer?.Invoke($"   Completed activity o{message.Opid:D10} {message.Request}");

            process.Telemetry?.OnApplicationEvent(
                    processId: process.ProcessId,
                    id: message.Opid.ToString(),
                    name: message.Request.ToString(),
                    parent: message.Parent.ToString(),
                    opSide: OperationSide.Callee,
                    opType: OperationType.Activity,
                    duration: stopwatch.Elapsed.TotalMilliseconds
                );

            process.HostServices.Send(process.ProcessId, new RecordActivityResult()
            {
                Opid = message.Opid,
                Parent = message.Parent,
                Clock = message.Clock,
                Result = r
            });
        }

        public virtual void RecordResult(object result)
        {
            process.HostServices.Send(process.GetOrigin(message.Opid), new RespondToActivity()
            {
                Opid = message.Opid,
                Parent = message.Parent,
                Clock = message.Clock,
                Result = result
            });

            process.PendingActivities.Remove(message.Opid);
        }
    }
}

