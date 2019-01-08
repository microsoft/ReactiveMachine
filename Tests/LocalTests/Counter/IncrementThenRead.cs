// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Counter;
using Counter.Service;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LocalTests.Counter
{
    [DataContract]
    public class IncrementThenRead : 
        IUpdate<Counter2, UnitType>, 
        ICounterAffinity
    {
        [DataMember]
        public uint CounterId { get; set; }

        [CreateIfNotExists]
        public UnitType Execute(IUpdateContext<Counter2> context)
        {
            context.Logger.LogDebug($"IncrementThenRead({CounterId}) Start");
            {
                {
                    Assert.Equal(0, context.State.Count);
                }

                context.PerformUpdate(new IncrementUpdate() { CounterId = CounterId });

                {
                    Assert.Equal(1, context.State.Count);
                }
            }

            context.Logger.LogDebug($"IncrementThenRead({CounterId}) End");

            return UnitType.Value;
        }
    }
}
