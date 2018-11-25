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
    public class ForkedActivity<TReturn> : IOrchestration<UnitType>
    {
        [DataMember]
        public IActivityBase<TReturn> Activity;

        [DataMember]
        public TimeSpan Delay;

        public override string ToString()
        {
            if (Delay != TimeSpan.Zero)
                return $"ForkedActivity-{Activity}";
            else
                return $"ForkedActivity-{Delay}-{Activity}";
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.DelayBy(Delay);

            await context.PerformActivity(Activity);

            return UnitType.Value;
        }
    }
}
