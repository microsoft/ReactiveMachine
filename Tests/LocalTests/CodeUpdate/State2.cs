using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests.CodeUpdate
{
    [AddedInVersionAttribute(1)]
    public class State2 :
        IAffinity1, 
        ISingletonState<IAffinity1>,
        ISubscribe<Event2, IAffinity1>
    {
        public long Balance;

        public void On(ISubscriptionContext context, Event2 evt)
        {
            context.Logger.LogInformation($"Received {evt}");

            Balance++;
        }
    }

    [AddedInVersionAttribute(1)]
    public class Read2long :
        IRead<State2, long>,
        IAffinity1

    {
        public long Execute(IReadContext<State2> context)
        {
            return context.State.Balance;
        }
 
    }

    [AddedInVersionAttribute(1)]
    public class Read2int :
        IRead<State2, int>,
        IAffinity1

    {
        public int Execute(IReadContext<State2> context)
        {
            return (int) context.State.Balance;
        }

    }

    [AddedInVersionAttribute(1)]
    public class Update2 :
        IUpdate<State2, UnitType>,
        IAffinity1

    {
        public long NewBalance;

        public UnitType Execute(IUpdateContext<State2> context)
        {
            context.State.Balance = NewBalance;
            return UnitType.Value;
        }
    }
}
