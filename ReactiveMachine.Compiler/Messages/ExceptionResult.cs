// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine.Compiler
{
    [DataContract]
    internal abstract class ExceptionResult
    {
        [DataMember]
        public string Description;
    }

    [DataContract]
    internal class DataContractSerializedExceptionResult : ExceptionResult
    {
        [DataMember]
        public byte[] Content;
    }

    [DataContract]
    internal class ClassicallySerializedExceptionResult : ExceptionResult
    {
        [DataMember]
        public byte[] Content;
    }

    [DataContract]
    internal class NonserializedExceptionResult : ExceptionResult
    {
    }
}
