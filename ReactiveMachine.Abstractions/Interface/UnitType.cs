// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    /// <summary>
    /// Type with a single value, used for operations that return only an "ack"
    /// </summary>
    [DataContract]
    public struct UnitType : IEquatable<UnitType>
    {
        public static UnitType Value = new UnitType();

        public static Task<UnitType> CompletedTask = Task.FromResult(new UnitType());

        public bool Equals(UnitType other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitType;
        }

        public override int GetHashCode()
        {
            return 1234;
        }
    }
}
