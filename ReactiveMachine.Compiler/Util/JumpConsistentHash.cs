// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Util
{
    public static class JumpConsistentHash
    {
        public static uint Compute(ulong x, uint num_buckets)
        {
            return ComputeJumpConsistentHash(x, num_buckets);
        }

        public static uint Compute(Guid guid, uint num_buckets)
        {
            var b = guid.ToByteArray();
            // this is the same as Guid.ToHashCode() but we copy it for stability

            unchecked
            {
                ulong key = ((((((((((((((((ulong)b[3]
                                    ) << 8) | b[2]
                                    ) << 8) | b[1]
                                    ) << 8) | b[0]
                                    ) << 8) | b[5]
                                    ) << 8) | b[4]
                                    ) << 8) | b[7]
                                    ) << 8) | b[6])
                           ^ ((ulong)b[10] << 24 | b[15]);

                return ComputeJumpConsistentHash(key, num_buckets);
            }
        }

        public static uint Compute(string s, uint num_buckets)
        {
            byte[] bytesToHash = Encoding.UTF8.GetBytes(s);
            var hash = ComputeJenkinsHash(bytesToHash);
            return ComputeJumpConsistentHash(hash, num_buckets);
        }

        public static uint Compute(byte[] bytes, uint num_buckets)
        {
            var hash = ComputeJenkinsHash(bytes);
            return ComputeJumpConsistentHash(hash, num_buckets);
        }

        private static uint ComputeJumpConsistentHash(ulong key, uint num_buckets)
        {
            unchecked
            {
                long j = 0;
                long b;
                while (true)
                {
                    b = j;
                    key = key * 2862933555777941757UL + 1;
                    j = (long)((b + 1) * ((1L << 31) / (double)((key >> 33) + 1)));
                    if (j >= num_buckets)
                        return (uint)b;
                }
            }
        }

        private static uint ComputeJenkinsHash(byte[] data)
        {
            int len = data.Length;
            uint a = 0x9e3779b9;
            uint b = a;
            uint c = 0;
            int i = 0;

            unchecked
            {
                while (i + 12 <= len)
                {
                    a += (uint)data[i++] |
                        ((uint)data[i++] << 8) |
                        ((uint)data[i++] << 16) |
                        ((uint)data[i++] << 24);
                    b += (uint)data[i++] |
                        ((uint)data[i++] << 8) |
                        ((uint)data[i++] << 16) |
                        ((uint)data[i++] << 24);
                    c += (uint)data[i++] |
                        ((uint)data[i++] << 8) |
                        ((uint)data[i++] << 16) |
                        ((uint)data[i++] << 24);
                    JenkinsMix(ref a, ref b, ref c);
                }
                c += (uint)len;
                if (i < len)
                    a += data[i++];
                if (i < len)
                    a += (uint)data[i++] << 8;
                if (i < len)
                    a += (uint)data[i++] << 16;
                if (i < len)
                    a += (uint)data[i++] << 24;
                if (i < len)
                    b += (uint)data[i++];
                if (i < len)
                    b += (uint)data[i++] << 8;
                if (i < len)
                    b += (uint)data[i++] << 16;
                if (i < len)
                    b += (uint)data[i++] << 24;
                if (i < len)
                    c += (uint)data[i++] << 8;
                if (i < len)
                    c += (uint)data[i++] << 16;
                if (i < len)
                    c += (uint)data[i++] << 24;
                JenkinsMix(ref a, ref b, ref c);
                return c;
            }
        }

        private static void JenkinsMix(ref uint a, ref uint b, ref uint c)
        {
            unchecked
            {
                a -= b; a -= c; a ^= (c >> 13);
                b -= c; b -= a; b ^= (a << 8);
                c -= a; c -= b; c ^= (b >> 13);
                a -= b; a -= c; a ^= (c >> 12);
                b -= c; b -= a; b ^= (a << 16);
                c -= a; c -= b; c ^= (b >> 5);
                a -= b; a -= c; a ^= (c >> 3);
                b -= c; b -= a; b ^= (a << 10);
                c -= a; c -= b; c ^= (b >> 15);
            }
        }
    }
}
