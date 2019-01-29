using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public interface ICustomKeyType<TKey>
    {
        Func<TKey, uint, uint> RoundRobinPlacer { get; }

        Func<TKey, TKey, int> Comparator { get; }

        Func<TKey, uint, uint> JumpConsistentHasher { get; }
    }
}
