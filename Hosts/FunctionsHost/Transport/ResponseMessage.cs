// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.EventHubs;
using ReactiveMachine.Compiler;

namespace FunctionsHost
{

    internal interface IResponseMessage
    {
        void Process(IClientInternal client);
    }

    [DataContract]
    internal class ResponseMessage<TResult> : IResponseMessage
    {
        [DataMember]
        public Guid ClientRequestId;

        [DataMember]
        public TResult Result;

        [DataMember]
        public ExceptionResult ExceptionResult;

        public void Process(IClientInternal client)
        {
            client.ProcessResult(ClientRequestId, Result, ExceptionResult);
        }

        public override string ToString()
        {
            return $"resp.{ClientRequestId}";
        }

    }

}
