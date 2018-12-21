---
title: "States"
description: store information durably
weight: 23
menu:
  main: 
    parent: "Programming Model"
    weight: 23
---

States are used to store information durably and scalably. To use state in a reactive machine application, you

- must define how the state should be distributed, by declaring an **affinity**.
- must define the **state** to be stored.
- may define **read** operations that can access the state and return a value
- may define **update** operations that can read and update the state and return a value
- may define event **subscriptions** that can update the state in response to an event

### Example 1: Account State

Suppose we are implementing a service whose customers have some sort of account, containing some form of currency. Conceptually, the type of our data is something like `Dictionary<Guid,AccountState>`, but we want this dictionary to be partitioned over the machines in a cluster, so we can handle very large numbers of accounts, and we want it durably persisted in cloud storage.  

First, we need to define an **affinity**, i.e. the "type of the key". We do this by defining a new interface that implements `IPartitionedAffinity`:

```csharp
public interface IAccountAffinity : IPartitionedAffinity<IAccountAffinity,Guid>
{
    Guid AccountId { get; }
}
```

Second, we define the **state** of the account, i.e. the "type of the value". We do this by defining a new serializable class that implements `IPartitionedState`:

```csharp
public class AccountState : IPartitionedState<IAccountAffinity,Guid>
{
    public int Balance;
}
```

Finally, we want to define operations that can read and update the account state. As for orchestrations and activites, read and update operations are defined as serializable classes with an `Execute` method.

For example, we can define a **read operation** that returns an integer (the balance) by defining a new serializable class that implements `IRead`:

```csharp
public class ReadBalance : IRead<AccountState, int>, IAccountAffinity
{
    public Guid AccountId { get; set; }
    public int Execute(IReadContext<AccountState> context)
    {
        return context.State.Balance;
    }
}
```

Note that the class must also implement the `IAccountAffinity` interface, so that the runtime can determine which account is being targeted by this operation by getting the `AccountId` property. Inside the `Execute` method, we can use the `context` object to access the state and read the balance. A read operation *must not* modify the state.

To execute the read operation from within an orchestration, we construct a read operation and call `PerformRead`:

```csharp
int balance = await context.PerformRead(new ReadBalance() { AccountId = accountId });
```

To define an **update operation**, we do something similar, but we add another input (the amount to be withdrawn), we check to see if there is sufficient balance before withdrawing the amount, and we return a boolean indicating whether we did indeed withdraw:

```csharp
public class TryWithdraw : IUpdate<AccountState, bool>, IAccountAffinity
{
    public Guid AccountId { get; set; }
    public int Amount;

    public bool Execute(IUpdateContext<AccountState> context)
    {
        if (context.State.Balance < Amount) return false;
        context.State.Balance -= Amount;
        return true;
    }
}
```

To execute the update operation from within an orchestration, we construct it and call `PerformUpdate`:

```csharp
bool withdrawSuccessful = await context.PerformUpdate(new TryWithdraw()
{
    AccountId = accountId,
    Amount = 20
});
```

Note that calling multiple withdrawal operations concurrently never produces unsafe interleavings, because all operations on a state are always serialized.

### Example 2: Global Counter

Consider that we would like to maintain a single global counter to count some sort of rare event. Because the state is just one piece in this case, we define a singleton affinity:

```c#
public interface IGlobalCounterAffinity : ISingletonAffinity<IGlobalCounterAffinity>  {  }
```

Next, we define an event class. The event has to implement `IGlobalCounterAffinity` to let the runtime know that this event has an effect on that affinity.

```csharp
public class SomeEvent : IEvent, IGlobalCounterAffinity
{
    ...
}
```

Finally, we define the state, including a subscription to the event:

```csharp
public class GlobalCounterState :
    ISingletonState<IGlobalCounterAffinity>,
    ISubscribe<SomeEvent, IGlobalCounterAffinity>
{
    public int Count;

    public void On(ISubscriptionContext context, SomeEvent evt)
    {
        Count++;
    }
}
```

To fire off the event in an orchestration, we construct an object and call ForkEvent.

```csharp
context.ForkEvent(new SomeEvent());
```

## Initial State

There is no operation for creating a state. Rather, states are created lazily *when needed*, i.e. when they are accessed for the first time by a read, update, or event. 

All states must be serializable, and have a parameterless constructor. This constructor is used to create the initial state and must be deterministic.

Sometimes, it may be desirable to do additional work when initializing the state, such as logging, or forking operations. Also, for partitioned states, it is often desirable to make the initial state depend on the partition key. Thus, we support interfaces `IInitialize` (for singleton states) and `IInitialize<TKey>` (for partitioned states). If a state declares that interface, then the `OnInitialize` method is guaranteed to be called right before any other operations are applied to this state.

```csharp
public class AccountState :
    IPartitionedState<IAccountAffinity, Guid>,
    IInitialize<Guid>
{
    public int Balance;

    public void OnInitialize(IInitializationContext context, Guid key)
    {
        context.Logger.LogInformation($"initializing account {key}");
        Balance = 10;
    }
}
```

```csharp
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

    public void OnInitialize(IInitializationContext context)
    {
        context.Logger.LogInformation($"initializing GlobalCounterState");
    }
}
```