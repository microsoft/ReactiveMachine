using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Counter.Tests
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
