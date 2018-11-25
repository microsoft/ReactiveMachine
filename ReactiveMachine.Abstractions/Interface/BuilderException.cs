// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ReactiveMachine
{
    [Serializable]
    public class BuilderException : Exception
    {
        public BuilderException(string msg) : base(msg) { }
        public BuilderException(string msg, Exception inner) : base(msg, inner) { }

        protected BuilderException(SerializationInfo info, StreamingContext context) :
         base(info, context)
        { }

    }

    [Serializable]
    public class RuntimeException : Exception
    {
        public RuntimeException(string msg) : base(msg) { }
        public RuntimeException(string msg, Exception inner) : base(msg, inner) { }

        protected RuntimeException(SerializationInfo info, StreamingContext context) :
         base(info, context)
        { }

    }


    [Serializable]
    public class SynchronizationDisciplineException : RuntimeException
    {
        public SynchronizationDisciplineException(string msg) : base(msg) { }
        public SynchronizationDisciplineException(string msg, Exception inner) : base(msg, inner) { }

        protected SynchronizationDisciplineException(SerializationInfo info, StreamingContext context) :
         base(info, context)
        { }

    }

    [Serializable]
    public class TransactionException : Exception
    {
        public TransactionException(string msg) : base(msg) { }
        public TransactionException(string msg, Exception inner) : base(msg, inner) { }

        protected TransactionException(SerializationInfo info, StreamingContext context) :
         base(info, context)
        { }

    }
}
