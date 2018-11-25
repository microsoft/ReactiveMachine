// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    
    public interface IPartitionedAffinity<TAffinity,TKey> : IAffinitySpec<TAffinity>
    {
    }

    public interface ISingletonAffinity<TAffinity> : IAffinitySpec<TAffinity>
    {
    }

    public interface IAffinitySpec<TAffinity> : ICanRouteTo<TAffinity>
    {
    }

    public interface ICanRouteTo<TAffinity>
    {
    }

    public interface IMultiple<TAffinity,TKey> : ICanRouteTo<TAffinity>
        where TAffinity : IPartitionedAffinity<TAffinity,TKey>
    {
        IEnumerable<TKey> DeclareAffinities();
    }


 
}
