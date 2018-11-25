// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LocalTests
{
    [Serializable]
    public class TestFailureException : Exception
    {
        public TestFailureException(string msg) : base(msg) { }
        public TestFailureException(string msg, Exception inner) : base(msg, inner) { }

        protected TestFailureException(SerializationInfo info, StreamingContext context) :
                base(info, context)
        { }
    }

}
