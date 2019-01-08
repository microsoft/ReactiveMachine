// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests.Locks
{
    public enum Places
    {
        A, B, C
    }

    public interface IPlaceAffinity : IPartitionedAffinity<IPlaceAffinity, Places>
    {
        [RoundRobinPlacement]
        Places Place { get; }
    }

    public class State : 
        IPartitionedState<IPlaceAffinity, Places>, 
        ISubscribe<IncrementEvent, IPlaceAffinity, Places>
    {
        public int Balance;

        public void On(ISubscriptionContext<Places> context, IncrementEvent evt)
        {
            Balance++;
            context.Logger.LogInformation($"Increment {context.Key} to {Balance} in response to {evt}");
        }
    }

    public class GetBalance : IRead<State, int>, IPlaceAffinity
    {
        public Places Place { get; set; }

        public int Execute(IReadContext<State> context)
        {
            return context.State.Balance;
        }
    }

    public class SetBalance : IUpdate<State, UnitType>, IPlaceAffinity
    {
        public Places Place { get; set; }

        public int NewBalance;

        [CreateIfNotExists]
        public UnitType Execute(IUpdateContext<State> context)
        {
            context.State.Balance = NewBalance;
            return UnitType.Value;
        }
    }

    public class IncrementBalance : IUpdate<State, UnitType>, IPlaceAffinity
    {
        public Places Place { get; set; }

      
        public UnitType Execute(IUpdateContext<State> context)
        {
            context.State.Balance++;
            return UnitType.Value;
        }
    }

    public class IncrementEvent : IEvent, IMultiple<IPlaceAffinity,Places>
    {
        public Places[] Places;

        public IEnumerable<Places> DeclareAffinities()
        {
            return Places;
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            foreach (var x in Places)
                result.Append(x);
            return result.ToString();
        }
    }

}
