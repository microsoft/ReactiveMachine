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

        bool CanExecuteLocally(object request, out uint destination);

        void ProcessRequest(RequestMessage request);

        RequestMessage CreateForkMessage(IOrchestration orchestration);

        RequestMessage CreateRequestMessage(object orchestration);

        IAffinityInfo PlacementAffinity { set; }
        bool RequiresLocks(object request, out List<IPartitionKey> list);

        void DefineExtensions(IServiceBuilder serviceBuilder);
    }

    class OrchestrationInfo<TRequest, TReturn> : IOrchestrationInfo
        where TRequest : IOrchestrationBase<TReturn>
    {
        private readonly Process process;
        private readonly bool requirelocks;

        public IAffinityInfo PlacementAffinity { private get; set; }

        public List<IAffinityInfo> Affinities;

        public OrchestrationInfo(Process process)
        {
            this.process = process;
            process.Orchestrations[typeof(TRequest)] = this;


            var canRouteToPrefix = ReflectionServiceBuilder.GetGenericTypeNamePrefix(typeof(ICanRouteTo<>));
            foreach (var i in typeof(TRequest).GetInterfaces())
                if (ReflectionServiceBuilder.GetGenericTypeNamePrefix(i) == canRouteToPrefix)
                {
                    var gt = i.GenericTypeArguments;
                    var affinityInfo = process.Affinities[gt[0]];
                    (Affinities ?? (Affinities = new List<IAffinityInfo>())).Add(affinityInfo);
                }

            var method = typeof(TRequest).GetMethod("Execute");
            requirelocks = method.GetCustomAttributes(typeof(LockAttribute), false).Count() > 0;

            if (requirelocks && (Affinities == null || Affinities.Count == 0))
                throw new BuilderException($"To use {nameof(LockAttribute)} on Execute function of {typeof(TRequest).FullName}, you must define at least one affinity.");
        }

        public IEnumerable<Type> SerializableTypes()
        {
            yield return typeof(TRequest);
            yield return typeof(TReturn);
            yield return typeof(ForkOperation<TRequest>);
            yield return typeof(RequestOperation<TRequest>);
            yield return typeof(RespondToOperation);
            yield return typeof(OrchestrationState<TRequest,TReturn>);
        }

        public void DefineExtensions(IServiceBuilder serviceBuilder)
        {
            Extensions.Register.DefineOrchestrationExtensions<TRequest, TReturn>(serviceBuilder);
        }

        public bool CanExecuteLocally(object request, out uint destination)
        {
            if (PlacementAffinity != null)
            {
                destination = PlacementAffinity.LocateAffinity(request);
            }
            else
            {
                destination = process.ProcessId;
            }
            return destination == process.ProcessId;
        }

        public void ProcessRequest(RequestMessage request)
        {
            var state = new OrchestrationState<TRequest, TReturn>((ForkOperation<TRequest>)request);
            process.OrchestrationStates[request.Opid] = state;
            state.StartOrResume(process, this);
        }

        public RequestMessage CreateForkMessage(IOrchestration orchestration)
        {
            return new ForkOperation<TRequest>()
            {
                Request = (TRequest) orchestration
            };
        }

        public RequestMessage CreateRequestMessage(object orchestration)
        {
            return new RequestOperation<TRequest>()
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
            else
            {
                if (Affinities.Count == 1)
                    list = Affinities[0].GetAffinityKeys(request).ToList();
                else
                {
                    list = new List<IPartitionKey>();
                    foreach (var a in Affinities)
                        foreach (var k in a.GetAffinityKeys(request))
                            list.Add(k);
                }
                return true;
            }
        }
    }
}
