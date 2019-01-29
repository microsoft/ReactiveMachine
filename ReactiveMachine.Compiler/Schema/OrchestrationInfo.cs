// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{
    interface IOrchestrationInfo
    {
        IEnumerable<Type> SerializableTypes();

        void CallDerivedDefiner(IDerivedDefiner definer, IServiceBuilder builder);

        bool CanExecuteLocally(object request, ulong opid, out uint destination);

        void ProcessRequest(RequestMessage request, OrchestrationType orchestrationType);

        RequestMessage CreateForkMessage(IOrchestration orchestration);

        RequestMessage CreateRequestMessage(object orchestration);

        IAffinityInfo PlacementAffinity { set; }

        bool DistributeRandomly { set; }

        bool RequiresLocks(object request, out List<IPartitionKey> list);

        void DefineExtensions(IServiceBuilder serviceBuilder);
    }

    class OrchestrationInfo<TRequest, TReturn> : IOrchestrationInfo
        where TRequest : IOrchestrationBase<TReturn>
    {
        public readonly Process Process;
        private readonly bool requirelocks;
        private readonly bool IsInitialization;

        public IAffinityInfo PlacementAffinity { private get; set; }
        public bool DistributeRandomly { private get; set; }

        public List<IAffinityInfo> AffinityList;

        // constructor for user-defined orchestration
        public OrchestrationInfo(Process process)
        {
            this.Process = process;
            process.Orchestrations[typeof(TRequest)] = this;

            if (typeof(IInitializationRequest).IsAssignableFrom(typeof(TRequest)))
            {
                // this is an initialization orchestration
                IsInitialization = true;
                requirelocks = true;
            }
            else
            {
                IsInitialization = false;
                
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
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TRequest);
            yield return typeof(TReturn);
            yield return typeof(ForkOrchestration<TRequest>);
            yield return typeof(PerformOrchestration<TRequest>);
            yield return typeof(RespondToOrchestration);
            yield return typeof(AckInitialization);
            yield return typeof(OrchestrationState<TRequest,TReturn>);
        }

        public void CallDerivedDefiner(IDerivedDefiner definer, IServiceBuilder builder)
        {
            definer.DefineForEachOrchestration<TRequest, TReturn>(builder);
        }

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            Extensions.Register.DefineOrchestrationExtensions<TRequest, TReturn>(serviceBuilder);
        }

        public bool CanExecuteLocally(object request, ulong opid, out uint destination)
        {
            if (PlacementAffinity != null)
            {
                destination = PlacementAffinity.LocateAffinity(request);
            }
            else if (DistributeRandomly)
            {
                destination = (uint)(FNVHash.ComputeHash(opid) % Process.NumberProcesses);
            }
            else
            {
                destination = Process.ProcessId;
            }
            return destination == Process.ProcessId;
        }

        public void ProcessRequest(RequestMessage request, OrchestrationType orchestrationType)
        {
            new OrchestrationState<TRequest, TReturn>(
                Process,
                this,
                request.Opid,
                ((OrchestrationMessage<TRequest>)request).Request,
                orchestrationType,
                request.LockedByCaller,
                request.Parent,
                request.Clock);
        }

        public RequestMessage CreateForkMessage(IOrchestration orchestration)
        {
            return new ForkOrchestration<TRequest>()
            {
                Request = (TRequest) orchestration
            };
        }

        public RequestMessage CreateRequestMessage(object orchestration)
        {
            return new PerformOrchestration<TRequest>()
            {
                Request = (TRequest) orchestration,
            };
        }

        public bool RequiresLocks(object request, out List<IPartitionKey> list)
        {
            if (!requirelocks)
            {
                list = null;
                return false;
            }
            else if (IsInitialization)
            {
                list = new List<IPartitionKey>() {
                    ((IInitializationRequest)request).GetPartitionKey()
                };
                return true;
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
    }
}
