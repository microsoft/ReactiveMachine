// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal static class KeyFunctions
    {
        internal static object GetJumpConsistentHashFor<T>(Serializer serializer)
        {
            if (typeof(T) == typeof(sbyte))
            {
                return new Func<sbyte, uint, uint>((o,n) => JumpConsistentHash.Compute((ulong)o, n));
            }
            else if (typeof(T) == typeof(short))
            {
                return new Func<short, uint, uint>((o, n) => JumpConsistentHash.Compute((ulong)o, n));
            }
            else if (typeof(T) == typeof(int))
            {
                return new Func<int, uint, uint>((o, n) => JumpConsistentHash.Compute((ulong)o, n));
            }
            else if (typeof(T) == typeof(long))
            {
                return new Func<long, uint, uint>((o, n) => JumpConsistentHash.Compute((ulong)o, n));
            }
            else if (typeof(T) == typeof(byte))
            {
                return new Func<byte, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else if (typeof(T) == typeof(ushort))
            {
                return new Func<ushort, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else if (typeof(T) == typeof(uint))
            {
                return new Func<uint, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else if (typeof(T) == typeof(ulong))
            {
                return new Func<ulong, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else if (typeof(T) == typeof(char))
            {
                return new Func<char, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else if (typeof(T) == typeof(bool))
            {
                return new Func<bool, uint, uint>((o, n) => JumpConsistentHash.Compute(o ? 1ul : 0ul, n));
            }
            else if (typeof(T).IsEnum)
            {
                return new Func<T, uint, uint>((o, n) => JumpConsistentHash.Compute(Convert.ToUInt64(o), n));
            }
            else if (typeof(T) == typeof(double))
            {
                return new Func<double, uint, uint>((o, n) => JumpConsistentHash.Compute(BitConverter.GetBytes((double)o), n));
            }
            else if (typeof(T) == typeof(float))
            {
                return new Func<float, uint, uint>((o, n) => JumpConsistentHash.Compute(BitConverter.GetBytes((float)o), n));
            }
            else if (typeof(T) == typeof(Guid))
            {
                return new Func<Guid, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else if (typeof(T) == typeof(string))
            {
                return new Func<string, uint, uint>((o, n) => JumpConsistentHash.Compute(o, n));
            }
            else
            {
                throw new BuilderException($"type {typeof(T)} cannot be used as a key");
                //return new Func<sbyte, uint>((o) => ConsistentHashing.Compute(serializer.SerializeObject(o), num_buckets));
            }
        }

        internal static object GetRoundRobinFor<T>(uint chunksize)
        {
            if (typeof(T) == typeof(sbyte))
            {
                return new Func<sbyte, uint, uint>((o, n) => (uint) Math.Abs(o) / chunksize % n);
            }
            else if (typeof(T) == typeof(short))
            {
                return new Func<short, uint, uint>((o, n) => (uint) Math.Abs(o) / chunksize % n);
            }
            else if (typeof(T) == typeof(int))
            {
                return new Func<int, uint, uint>((o, n) => (uint) Math.Abs(o) / chunksize % n);
            }
            else if (typeof(T) == typeof(long))
            {
                return new Func<long, uint, uint>((o, n) => (uint) Math.Abs(o) / chunksize % n);
            }
            else if (typeof(T) == typeof(byte))
            {
                return new Func<byte, uint, uint>((o, n) => o / chunksize % n);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return new Func<ushort, uint, uint>((o, n) => o / chunksize % n);
            }
            else if (typeof(T) == typeof(uint))
            {
                return new Func<uint, uint, uint>((o, n) => o / chunksize % n);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return new Func<ulong, uint, uint>((o, n) => (uint) (o / chunksize % n));
            }
            else if (typeof(T) == typeof(char))
            {
                return new Func<char, uint, uint>((o, n) => o / chunksize % n);
            }
            else if (typeof(T) == typeof(bool))
            {
                return new Func<bool, uint, uint>((o, n) => (o ? 1u : 0u) / chunksize % n);
            }
            else if (typeof(T).IsEnum)
            {
                return new Func<T, uint, uint>((o, n) =>  Convert.ToUInt32(o) / chunksize % n);
            }
            else
            {
                throw new BuilderException($"type {typeof(T)} does not allow round-robin placement");
            }
        }

        internal static object GetComparatorFor<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return new Func<string, string, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(double))
            {
                return new Func<double, double, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(float))
            {
                return new Func<float, float, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(Guid))
            {
                return new Func<Guid, Guid, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return new Func<sbyte, sbyte, int>((a,b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(short))
            {
                return new Func<short, short, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(int))
            {
                return new Func<int, int, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(long))
            {
                return new Func<long, long, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(byte))
            {
                return new Func<byte, byte, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(ushort))
            {
                return new Func<ushort, ushort, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(uint))
            {
                return new Func<uint, uint, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(ulong))
            {
                return new Func<ulong, ulong, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(char))
            {
                return new Func<char, char, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T) == typeof(bool))
            {
                return new Func<bool, bool, int>((a, b) => a.CompareTo(b));
            }
            else if (typeof(T).IsEnum)
            {
                return new Func<T, T, int>((a, b) => Convert.ToUInt32(a).CompareTo(Convert.ToUInt32(b)));
            }
            else
            {
                throw new BuilderException($"type {typeof(T)} cannot be used as a key");
            }
        }
    }
}
