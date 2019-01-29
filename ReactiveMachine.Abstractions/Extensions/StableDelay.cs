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
    public class StableDelay : IActivity<UnitType>
    {
        [DataMember]
        public DateTime TargetTime;

        public override string ToString()
        {
            return $"StableDelay";
        }

        public TimeSpan TimeLimit => TimeSpan.FromMilliseconds(int.MaxValue);

        public async Task<UnitType> Execute(IContext context)
        {
            var now = DateTime.UtcNow;

            if (TargetTime > now)
            {
                await Task.Delay(TargetTime - now);
            }
            
            return UnitType.Value;
        }
    }
}
