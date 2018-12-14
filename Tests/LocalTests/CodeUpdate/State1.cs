using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests.CodeUpdate
{
    [ReplacedInVersion(1)]
    public class State1 :
       ISingletonState<IAffinity1>,
       ISubscribe<Event1, IAffinity1>
    {
        public int Balance;

        public void On(ISubscriptionContext context, Event1 evt)
        {
            context.Logger.LogInformation($"Received {evt}");

            Balance++;
        }

        public State2 Upgrade()
        {
            return new State2() { Balance = Balance };
        }
    }

    [ReplacedInVersion(1)]
    public class Read1 :
        IRead<State1, int>,
        IAffinity1
    {
        public int Execute(IReadContext<State1> context)
        {
            return context.State.Balance;
        }

        public Read2int Upgrade()
        {
            return new Read2int();
        }
    }

    [ReplacedInVersion(1)]
    public class Update1 :
        IUpdate<State1, UnitType>,
        IAffinity1
    {
        public int NewBalance;

        public UnitType Execute(IUpdateContext<State1> context)
        {
            context.State.Balance = NewBalance;
            return UnitType.Value;
        }

        public Update1 Upgrade()
        {
            return new Update1() { NewBalance = NewBalance };
        }

    }
}
