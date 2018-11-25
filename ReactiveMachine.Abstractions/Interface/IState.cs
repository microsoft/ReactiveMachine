// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
  
    public interface ISingletonState<TAffinity> : IState<TAffinity>
        where TAffinity : ISingletonAffinity<TAffinity>
    {
    }
    public interface IPartitionedState<TAffinity,TKey> : IState<TAffinity>
        where TAffinity: IPartitionedAffinity<TAffinity,TKey>
    {
    }

    public interface IState<TAffinity> : IState { }
    public interface IState { }

    public interface ISubscribe<TEvent, TAffinity>
        where TAffinity : ISingletonAffinity<TAffinity>
        where TEvent : TAffinity
    {
        void On(ISubscriptionContext context, TEvent evt);
    }

    public interface ISubscribe<TEvent, TAffinity, TKey>
       where TAffinity : IPartitionedAffinity<TAffinity, TKey>
       where TEvent : ICanRouteTo<TAffinity>
    {
        void On(ISubscriptionContext<TKey> context, TEvent evt);
    }


 
}
