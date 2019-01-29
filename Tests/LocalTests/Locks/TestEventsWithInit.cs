// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LocalTests.Locks
{

    public interface IAccountAffinity : IPartitionedAffinity<IAccountAffinity, int>
    {
        int AccountId { get; }
    }

    public class AccountState1 :
        IPartitionedState<IAccountAffinity, int>,
        IInitialize<int>,
        ISubscribe<MultiEventThree,IAccountAffinity,int>
    {
        public int Balance;

        public void On(ISubscriptionContext<int> context, MultiEventThree evt)
        {
            Balance++;
        }

        public Task OnInitialize(IInitializationContext context, int key)
        {
            Balance = 100;
            return Task.CompletedTask;
        }
    }

    public class InitializationActivity : IActivity<int>
    {
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(20);

        public Task<int> Execute(IContext context)
        {
            return Task.FromResult(100);
        }
    }

    public class ReadBalance1 : IRead<AccountState1, int>, IAccountAffinity
    {
        public int AccountId { get; set; }
        public int Execute(IReadContext<AccountState1> context)
        {
            return context.State.Balance;
        }
    }

    public class AccountState2 :
    IPartitionedState<IAccountAffinity, int>,
    ISubscribe<MultiEventThree, IAccountAffinity, int>
    {
        public int Balance = 100;

        public void On(ISubscriptionContext<int> context, MultiEventThree evt)
        {
            Balance++;
        }
    }

    public class ReadBalance2 : IRead<AccountState2, int>, IAccountAffinity
    {
        public int AccountId { get; set; }
        public int Execute(IReadContext<AccountState2> context)
        {
            return context.State.Balance;
        }
    }



    public class MultiEventThree : IEvent,
        IMultiple<IPlaceAffinity, Places>,
        IGlobalCounterAffinity,
        IMultiple<IAccountAffinity, int>
    {
        public Places[] Places;
        public int[] AccountIds;

        IEnumerable<int> IMultiple<IAccountAffinity, int>.DeclareAffinities()
        {
            return AccountIds;
        }

        IEnumerable<Places> IMultiple<IPlaceAffinity, Places>.DeclareAffinities()
        {
            return Places;
        }
    }

    public class LockedMultiEventOrchestration : LockedTestOrchestration,
        IMultiple<IPlaceAffinity, Places>,
        IGlobalCounterAffinity,
        IMultiple<IAccountAffinity, int>
    {
        public Places[] Places;
        public int[] AccountIds;

        IEnumerable<int> IMultiple<IAccountAffinity, int>.DeclareAffinities()
        {
            return AccountIds;
        }

        IEnumerable<Places> IMultiple<IPlaceAffinity, Places>.DeclareAffinities()
        {
            return Places;
        }

        protected override async Task Run(IOrchestrationContext context)
        {

            await context.PerformEvent(new MultiEventThree() { Places = Places, AccountIds = AccountIds });

            foreach (var a in AccountIds)
            {
                var result1 = await context.PerformRead(new ReadBalance1() { AccountId = a });
                var result2 = await context.PerformRead(new ReadBalance2() { AccountId = a });
                Assert.Equal(101, result1);
                Assert.Equal(101, result2);
            }

            foreach (var p in Places)
            {
                var result = await context.PerformUpdate(new SetBalance() { Place = p , NewBalance = 1000});
            }
            foreach (var p in Places)
            {
                var result = await context.PerformRead(new GetBalance() { Place = p });
                Assert.Equal(1000, result);
            }
            foreach (var p in Places)
            {
                var result = await context.PerformUpdate(new SetBalance() { Place = p, NewBalance = 0 });
            }
        }
    }

    public class TestEventsWithInit : TestOrchestration
    {
        public IEnumerable<Places> AllPlaces()
        {
            yield return Places.A;
            yield return Places.B;
            yield return Places.C;
        }


        protected override async Task Run(IOrchestrationContext context)
        {
            await context.PerformOrchestration(new LockedMultiEventOrchestration()
            {
                AccountIds = new int[] { 0, 1, 2 },
                Places = new Places[] { Places.B, Places.C }
            });
            await context.PerformOrchestration(new LockedMultiEventOrchestration()
            {
                AccountIds = new int[] { 3, 4, 5, 6, 7, 8, 9, 10 },
                Places = new Places[] { Places.B }
            });
        }
    }
}
