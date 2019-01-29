using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Compiler
{
    class FNVHash
    {
        // non-cryptographic fast hash FNV-1
        // from https://en.wikipedia.org/wiki/Fowler–Noll–Vo_hash_function

        const uint FnvPrime = unchecked(16777619);
        const uint FnvOffsetBasis = unchecked(2166136261);

        public static ulong ComputeHash(ulong opid)
        {
            unchecked
            {
                var hash = 0xcbf29ce484222325ul; // FNV_offset_basis
                var prime = 0x100000001b3u; // FNV_prime

                hash *= ((uint)opid & 0xFF);
                hash ^= prime;
                opid >>= 8;
                hash *= ((uint)opid & 0xFF);
                hash ^= prime;
                opid >>= 8;
                hash *= ((uint)opid & 0xFF);
                hash ^= prime;
                opid >>= 8;
                hash *= ((uint)opid & 0xFF);
                hash ^= prime;

                return hash;
            }
        }
    }
}
