---
title: "States"
description: store information durably
weight: 24
menu:
  main: 
    parent: "Programming Model"
    weight: 24
---

States are used to store information durably and scalably. To use state in a reactive machine application, you

- must define how the state should be partitioned, by declaring an **affinity**.
- must define the **state** to be stored.

Then, there are a variety of options about how the state should be managed and accessed. You

- may define **read** operations that can access the state and return a value
- may define **update** operations that can read and update the state and return a value
- may define an **initialize** orchestration that is called when the state is initialized
- may define event **subscriptions** that can update the state in response to an event

### Example 1: Account State

Suppose we are implementing a service whose customers have some sort of account, containing some form of currency. Conceptually, we need something like `Dictionary<Guid,AccountState>`; however, a simple dictionary is not good enough: the runtime should durably persist the data, and distribute it over all the machines in the cluster.

First, we define a **partitioned affinity**, i.e. the "type of the key", by defining an interface `IAccountAffinity`:

```csharp
public interface IAccountAffinity : IPartitionedAffinity<IAccountAffinity,Guid>
{
    Guid AccountId { get; }
}
```

Second, we define the **state** of the account, i.e. the "type of the value". We do this by defining a class

```csharp
public class AccountState : IPartitionedState<IAccountAffinity,Guid>
{
    public string Owner;
    public int Balance;
}
```

Note that it is not necessary to include the `AccountId` in the state.

Next, we define operations to access the account state. Read and update operations are defined as serializable classes with an `Execute` method. The execute method provides a `context` object that contains the current state as a property `context.State`.

For example, we can define a **read operation** that returns an integer (the balance) by defining a new serializable class that implements `IRead`:

```csharp
public class ReadBalance : IRead<AccountState, int>, IAccountAffinity
{
    public Guid AccountId { get; set; }

    public int Execute(IReadContext<AccountState> context)
    {
        context.Logger.LogInformation($"now reading account with id {AccountId}");
        return context.State.Balance; 
    }
}
```

This class implements `IAccountAffinity`, so that the runtime can determine which account is being targeted by getting the `AccountId` property. Inside the `Execute` method, we can use the `context` object to access the state and read the balance. A read operation *must not* modify the state -- doing so can corrupt the runtime.

To *execute this read* operation from within an orchestration, we construct it and pass it as an argument to `context.PerformRead`:

```csharp
int balance = await context.PerformRead(new ReadBalance() { AccountId = accountId });
```

To define an **update operation**, we do something similar, but to make this example more interesting, we add another parameter (the amount to be withdrawn), and then we check to see if there is sufficient balance before withdrawing the amount. We return a boolean indicating whether we did indeed withdraw:

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

Again, *to execute this update operation* from within an orchestration, we construct it and pass it as an argument to `context.PerformUpdate`:

```csharp
bool withdrawSuccessful = await context.PerformUpdate(new TryWithdraw()
{
    AccountId = accountId,
    Amount = 20
});
```

Note that calling multiple withdrawal operations concurrently never produces unsafe interleavings, because all operations on a state are always serialized.

## Initialization

A class defining a state must be serializable, and have a parameterless default constructor. This constructor is used to create the initial state.

Read operations that access a non-existent state always throw a `KeyNotFound` exception. The same is true for update operations, by default. However, this behavior can be changed by specifying the `[CreateIfNotExist]` attribute on the execute method.

For example, performing the following update operation ensures a state exists:

```csharp
public class EnsureAccountExists : IUpdate<AccountState, UnitType>, IAccountAffinity
{
    public Guid AccountId { get; set; }
 
    [CreateIfNotExist]
    public UnitType Execute(IUpdateContext<AccountState> context)
    {
        return UnitType.Value;
    }
}
```

The state object is created using the default constructor. This constructor must be deterministic! For example, it must not use timestamps, random Guids, sequence numbers obtained from static counters, etc.
However, it is often desirable to do more initialization than what can be done in a default constructor. Thus, we support a special declaration for intializer orchestrations.

The interfaces `IInitialize` (for singleton states) and `IInitialize<TKey>` (for partitioned states) can be used for this purpose. If a state declares that interface, then the `OnInitialize` method is guaranteed to be called exactly once, before any other operations are applied to this state.

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

