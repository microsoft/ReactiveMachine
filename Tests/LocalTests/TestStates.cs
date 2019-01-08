// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LocalTests
{
    public interface IAccountAffinity : IPartitionedAffinity<IAccountAffinity, Guid>
    {
        Guid AccountId { get; }
    }

    public class AccountState :
        IPartitionedState<IAccountAffinity, Guid>,
        IInitialize<Guid>
    {
        public int Balance;

        public Task OnInitialize(IInitializationContext context, Guid key)
        {
            context.Logger.LogInformation($"initializing account {key}");
            Balance = 10;
            return Task.CompletedTask;
        }
    }

    public class ReadBalance : IRead<AccountState, int>, IAccountAffinity
    {
        public Guid AccountId { get; set; }
        public int Execute(IReadContext<AccountState> context)
        {
            return context.State.Balance;
        }
    }

    public class TryWithdraw : IUpdate<AccountState, bool>, IAccountAffinity
    {
        public Guid AccountId { get; set; }

        public int Amount;

        public bool Execute(IUpdateContext<AccountState> context)
        {
            if (context.State.Balance < Amount)
            {
                context.Logger.LogInformation($"insufficient funds in {AccountId}");
                return false;
            }
            else
            {
                context.Logger.LogInformation($"withdrawing {Amount} from {AccountId}");
                context.State.Balance -= Amount;
                return true;
            }
        }
    }

    public class Deposit : IUpdate<AccountState, UnitType>, IAccountAffinity
    {
        public Guid AccountId { get; set; }

        public int Amount;

        [CreateIfNotExists]
        public UnitType Execute(IUpdateContext<AccountState> context)
        {
            context.State.Balance += Amount;
            return UnitType.Value;
        }
    }

    public interface IGlobalCounterAffinity : ISingletonAffinity<IGlobalCounterAffinity> { }

    public class GlobalCounterState :
        ISingletonState<IGlobalCounterAffinity>,
        ISubscribe<SomeEvent, IGlobalCounterAffinity>,
        IInitialize
    {
        public int Count;

        public void On(ISubscriptionContext context, SomeEvent evt)
        {
            Count++;
        }

        public Task OnInitialize(IInitializationContext context)
        {
            context.Logger.LogInformation($"initializing singleton");
            return Task.CompletedTask;
        }
    }

    public class SomeEvent : IEvent, IGlobalCounterAffinity
    {
        // event may have some content
    }

    public class ReadGlobalCounter : IRead<GlobalCounterState, int>, IGlobalCounterAffinity
    {
        public int Execute(IReadContext<GlobalCounterState> context)
        {
            return context.State.Count;
        }
    }

    

    [DataContract]
    public class TestStates : TestOrchestration
    {
        protected override async Task Run(IOrchestrationContext context)
        {
            // ------ global counter

            context.ForkEvent(new SomeEvent());
            context.ForkEvent(new SomeEvent());
            context.ForkEvent(new SomeEvent());
            context.ForkEvent(new SomeEvent());
            context.ForkEvent(new SomeEvent());
            await context.Finish();

            var count = await context.PerformRead(new ReadGlobalCounter());
            Assert.Equal(5, count);

            // ------- account

            // get a fresh guid
            var guid = await context.NewGuid();

            // reads and withdraws must fail
            try
            {
                await context.PerformRead(new ReadBalance() { AccountId = guid });
                Assert.Fail("KeyNotFoundException expected");
            }
            catch (KeyNotFoundException) { }
            try
            {
                await context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 });
                Assert.Fail("KeyNotFoundException expected");
            }
            catch (KeyNotFoundException) { }

            // existence checks must fail because we don't hold the lock
            try
            {
                await context.StateExists<AccountState, IAccountAffinity, Guid>(guid);
                Assert.Fail("SynchronizationDisciplineException expected");
            }
            catch (SynchronizationDisciplineException) { }

            // run a locked creation 
            await context.PerformOrchestration(new CreateAccount() { AccountId = guid });

            // existence checks must fail because we don't hold the lock
            try
            {
                await context.StateExists<AccountState, IAccountAffinity, Guid>(guid);
                Assert.Fail("SynchronizationDisciplineException expected");
            }
            catch (SynchronizationDisciplineException) { }


            // reads should succeed and return the correct balance
            var amount = await context.PerformRead(new ReadBalance() { AccountId = guid });
            Assert.Equal(11, amount);

            // deposit should succeed and create the account
            var tasks = new List<Task<bool>>
            {
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 }),
                context.PerformUpdate(new TryWithdraw() { AccountId = guid, Amount = 2 })
            };

            await Task.WhenAll(tasks);

            Assert.Equal(5, tasks.Count(t => t.Result));

            var bal = await context.PerformRead(new ReadBalance() { AccountId = guid });

            Assert.Equal(1, bal);
        }
    }

    [DataContract]
    public class CreateAccount : IOrchestration<UnitType>, IAccountAffinity
    {
        [DataMember]
        public Guid AccountId { get; set; }

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            // account does not exist yet
            var exists = await context.StateExists<AccountState, IAccountAffinity, Guid>(AccountId);
            Assert.Equal(false, exists);

            // thus reads and withdraws must fail
            try
            {
                await context.PerformRead(new ReadBalance() { AccountId = AccountId });
                Assert.Fail("KeyNotFoundException expected");
            }
            catch (KeyNotFoundException) { }
            try
            {
                await context.PerformUpdate(new TryWithdraw() { AccountId = AccountId, Amount = 2 });
                Assert.Fail("KeyNotFoundException expected");
            }
            catch (KeyNotFoundException) { }

            // deposit should succeed and create the account
            await context.PerformUpdate(new Deposit() { AccountId = AccountId, Amount = 1 });

            // account does now exist
            exists = await context.StateExists<AccountState, IAccountAffinity, Guid>(AccountId);
            Assert.Equal(true, exists);

            // account contains correct balance
            var amount = await context.PerformRead(new ReadBalance() { AccountId = AccountId });
            Assert.Equal(11, amount);

            return UnitType.Value;
        }
    }
}
