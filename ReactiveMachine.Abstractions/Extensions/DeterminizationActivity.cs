// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Extensions
{
    [DataContract]
    public class DeterminizationActivity<TReturn> : IActivity<TReturn>
    {
        [DataMember]
        public TReturn Value;

        public TimeSpan TimeLimit => TimeSpan.FromHours(1); // irrelevant - executes instantly

        public Task<TReturn> Execute(IContext context)
        {
            return Task.FromResult(Value);
        }

        public override string ToString()
        {
            return $"Determinize<{typeof(TReturn).Name}>";
        }
    }
}
