using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Counter.Tests
{
    [DataContract]
    public class IncrementThenRead : 
        IUpdate<Counter2, UnitType>, 
        ICounterAffinity
    {
        [DataMember]
        public uint CounterId { get; set; }


        public void AssertEqual<T>(T expected, T actual) where T : IEquatable<T>
        {
            if (!expected.Equals(actual))
            {
                throw new TestFailureException($"expected: {expected} actual: {actual}");
            }
        }

        public UnitType Execute(IUpdateContext<Counter2> context)
        {
            context.Logger.LogDebug($"IncrementThenRead({CounterId}) Start");
            {
                {
                    AssertEqual(0, context.State.Count);
                }

                context.PerformUpdate(new IncrementUpdate() { CounterId = CounterId });

                {
                    AssertEqual(1, context.State.Count);
                }
            }

            context.Logger.LogDebug($"IncrementThenRead({CounterId}) End");

            return UnitType.Value;
        }
    }
}
