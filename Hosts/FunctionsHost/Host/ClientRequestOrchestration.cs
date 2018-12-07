// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using ReactiveMachine.Compiler;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{


    [DataContract]
    internal class ClientRequestOrchestration<TStaticApplicationInfo,TResult> : IOrchestration<UnitType>
        where TStaticApplicationInfo: IStaticApplicationInfo, new()
    {
        [DataMember]
        public IOrchestration<TResult> Orchestration;

        [DataMember]
        public Guid ClientRequestId;

        [DataMember]
        public uint ResponsePartition;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var response = new ResponseMessage<TResult>()
            {            
                 ClientRequestId = ClientRequestId,
            };

            try
            {
                response.Result = await context.PerformOrchestration(Orchestration);
            }
            catch (Exception e)
            {
                response.ExceptionResult = (ExceptionResult)context.ExceptionSerializer.SerializeException(e);
            }

            await context.PerformActivity(new ClientRequestResponseNotification<TStaticApplicationInfo, TResult>()
            {
                ResponsePartition = ResponsePartition,
                ResponseMessage = response
            });

            return UnitType.Value;
        }
    }
}
   