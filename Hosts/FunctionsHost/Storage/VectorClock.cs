// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    [Serializable]
    public class VectorClock : Dictionary<uint, long>
    { 
        public void Increment(uint processId)
        {
            if (TryGetValue(processId, out var value))
            {
                this[processId] = value + 1;
            }
            else
            {
                this[processId] = 1; ;
            }
        }

        public void Set(uint processId, long count)
        {
            this[processId] = count;
        }

        public long Get(uint processId)
        {
            if (TryGetValue(processId, out var value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        public bool HasSeen(uint processId, long count)
        {
            if(TryGetValue(processId, out var value))
            {
                return value >= count;
            }
            else
            {
                return false;
            }
        }

        public override String ToString()
        {
            var pairs = string.Join(",", this.Select(kvp => $"{kvp.Key}=>{kvp.Value}"));
            return $"[{pairs}]";
        }
    }
}
