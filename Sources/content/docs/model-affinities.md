---
title: "Affinities"
description: define partitions
weight: 23
menu:
  main: 
    parent: "Programming Model"
    weight: 23
---

Affinities are used to declare a partitioning scheme. Affinity partitions provide an abstract notion of locality. They have several important applications:

1. Partitions allow state to be stored and accessed scalably, because they can be load-balanced automatically across multiple host machines. Partitions are identified by **partition keys**.

1. Partitions can be **locked**. This allows orchestrations to perform multiple read and update operations consistently, which is often important (yet nontrivial to implement without locks). For example:

 * An orchestration can lock a partition to test if a state exists before creating it, without risking buggy races.
  
 * An orchestration can implement an atomic balance transfer between two accounts by locking both accounts, checking the available balance, and then updating the balance in both accounts.

1. Partitions can be used to **place** orchestrations and activities. Judicious placement can improve performance. For example, if an orchestration accesses multiple states that reside in the same partition, then it is a good idea to place that orchestration on the same partition, because it reduces delay incurred when sending messages across the network.

## Declaring Affinities

There are two types of affinities. Both are declared as a C# interfaces.

**Singleton Affinities** declare a single partition. These are appropriate for states that do not have a natural internal partitioning, and do not need to scale (in terms of memory or load) beyond what is easily provided by a single core on a single machine.

```c#
public interface ICounterAffinity : ISingletonAffinity<ICounterAffinity>
{
}
```

The interface must implement `ISingletonAffinity` with one generic type parameter which is the same as the interface being declared.

**Partitioned Affinities** define a key name and type to be used for identifying partitions. It is best to pick keys that are naturally present in the application logic: for example, for state that represents user information, we can choose the unique user id to be the partition key. Or, for bank account information, we can use a unique Guid identifying the account.

```c#
public interface IUserAffinity : IPartitionedAffinity<IUserAffinity, string>
{
    string UserId { get; }
}
```

```csharp
public interface IAccountAffinity : IPartitionedAffinity<IAccountAffinity,Guid>
{
    Guid AccountId { get; }
}
```

The interface must contain a single property with a getter. This property provides a name  for the partition key, and a type. The interface must implement `IPartitionedAffinity` with 2 generic type parameters, the first one being the same as the interface being declared, and the second one being the type of the partition key.

## Implementing Affinities

The classes that define orchestrations, activities, read operations, update operations, or events can implement an affinity (or even multiple affinities). This means any object of that class becomes associated with one or more partitions. There are several reasons to associate objects with specific partitions:

* A read or update operation must be associated with the partition it is reading or updating.

* An event must be associated with all the partitions that it is influencing, i.e. states that are subscribing to it.

* An orchestration or activity that is associated with one or more partitions can lock them all prior to execution (the "lock set").

* An orchestration or activity associated with a single partition is executed on the same host as that partition, which can improve performance.

### Example 1: Update Operation

Consider an update operation called `Deposit` that targets an account state that is partitioned by `IAccountAffinity`. To indicate which account is targeted, we implement the `IAccountAffinity` interface, by supplying a getter for the property `AccountId`. Therefore, any instance of this operation can be targeted at the correct account partition.

```c#
public class Deposit : 
    IUpdate<AccountState, UnitType>, 
    IAccountAffinity
{
    public Guid AccountId { get; set; }
    public int Amount;

    public UnitType Execute(IUpdateContext<AccountState> context) {  ...  }
}
```

### Example 2: Event Targeting Multiple Partitions

Consider an event representing a money transfer between two accounts, issued by some user. Conceivably, this event influences both account states (corresponding to source and destination) as well as a user state (corresponding to the initiator). As before, we can associate the object with a user by implementing IUserAffinity. However, for the accounts we need more than one: thus we implement `IMultiple<AccountAffinity, Guid>`, which allows us to enumerate the associated partition keys.

```c#
public class TransferEvent : 
    IEvent,
    IUserAffinity,
    IMultiple<IAccountAffinity, Guid>
{
    public string InitiatedBy;
    public Guid FromAccount;
    public Guid ToAccount;

    public UserId => InitiatedBy;                   // implements IUserAffinity
    public IEnumerable<Guid> DeclareAffinities()    // implements IMultiple<IAccountAffinity,Guid>
    {
        yield return FromAccount;
        yield return ToAccount;
    }
}
```

# Locked Execution

An orchestration or activity can use the `Locked` attribute on its Execute method to indicate that all the associated partitions should be locked prior to execution (and released afterwards).  

### Example 3: Locked Orchestration

If we need to check a condition before performing an orchestration, we can do so under a lock to prevent races. For example, we can check if an account exists before calling an orchestration that initializes it:

```csharp
public class CreateAccount : IOrchestration<bool>, IAccountAffinity
{
    public Guid AccountId { get; set; }

    [Lock]
    public async Task<bool> Execute(IOrchestrationContext context)
    {
        var exists = await context.StateExists<AccountState, IAccountAffinity, Guid>(AccountId);

        if (exists)
        {
            await context.PerformOrchestration(new CreateAccount() { AccountId = AccountId });
            return true;
        }
        else
        {
            return false;
        }
    }
}
```

Note that for this type of situation, it is often more convenient to use an initializer orchestration, which can execute automatically when accessing a non-existent state. However, here we also want to return a boolean indicating whether the creation happened or not.

### Example 4: Locked Activities

Consider activities that can read and write external cloud storage (e.g. blobs). By declaring them locked, we ensure that no concurrent read or writes can target the same path.

```csharp
public interface IPathAffinity : IPartitionedAffinity<IPathAffinity, string>
{
    string Path { get; set; }
}

public class ReadBlob : IActivity<string>, IPathAffinity
{
    public string Path { get; set; }

    [Lock]
    public Task<string> Execute(IContext context)
    {
        context.Logger.LogInformation($"Reading From {Path}");
        ...
    }
}

public class WriteBlob : IActivity<UnitType>, IPathAffinity
{
    public string Path { get; set; }
    public string Content;

    [Lock]
    public Task<UnitType> Execute(IContext context)
    {
        context.Logger.LogInformation($"Writing {Content.Length} Characters to {Path}");
        ...
    }
}
```

Note that this is a different method than using e-tags; here, storage conflicts are not just detected after the fact, but prevented before they happen. This is also called "pessimistic" concurrency control, as opposed to "optimistic" concurrency control.  

# Synchronization Rules

In most cases, locks are automatically acquired when performing an operation that requires locks. However, there are two exceptions:

* An orchestration cannot call a locked activity unless it already holds all associated locks.
* An orchestration cannot call `IOrchestrationContext.StateExists` unless it already holds the associated lock.

Also, there are additional rules that limit what you can do from within a locked orchestration. Essentially, they force orchestrations to acquire all the needed locks upfront. This ensures that no deadlocks can result from cyclic wait dependencies.

* A locked orchestration can perform a read or write operation only if it already holds the associated lock.
* A locked orchestration can perform an event only if it already holds all associated locks.
* A locked orchestration can perform a locked activity only if it already holds all associated locks.
* A locked orchestration can perform a locked orchestration only if it already holds all associated locks.
* A locked orchestration cannot perform an unlocked orchestration.
* A locked orchestration can however always fork anything (as opposed to perform)
