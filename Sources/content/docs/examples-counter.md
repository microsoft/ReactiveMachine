---
title: "Counter Service"
description: Building a partitioned, distributed incrementable counter
weight: 13
menu:
  main: 
    parent: "Examples"
    weight: 23
---

# Counter Service

The counter service example shows how you can leverage affinities at the application level for transparently routing messages and partitioning state for scalability without requiring the user to manually route messages or handle the partitioning of data themselves.

## Events

To begin designing our distributed counter, we start by modeling events that comprise the counter: increment events.  These events are automatically partitioned using the ```ICounterAffinity``` affinity which requires that the event supply a partitioning key,  ```CounterId```.

```c#
[DataContract]
public class IncrementEvent :
    IEvent,
    ICounterAffinity
{
    [DataMember]
    public uint CounterId { get; set; }
}
```

## Affinity

To specify how our events should be partitioned on the nodes running the application, we describe the partitioning with the ```ICounterAffinity```.  The ```RoundRobinPlacement``` annotation specifies how the counter should be partitioned on the nodes according to the ```CounterId```.

```c#
public interface ICounterAffinity :
    IPartitionedAffinity<ICounterAffinity, uint>
{
    [RoundRobinPlacement]
    uint CounterId { get; }
}
```

## State

We define application state as a view over the events in the system.  

Here, we define ```Counter1```.  ```Counter1``` is a counter whose state is derived from summing the increment operations.  There will automatically be a single ```Counter1``` object for each different ```CounterId``` in the system, and that state will be automatically partitioned based on the affinity.

```c#
[DataContract]
public class Counter1 :
    IPartitionedState<ICounterAffinity, uint>,
    ISubscribe<IncrementEvent, ICounterAffinity, uint>
{
    [DataMember]
    public int Count;

    public void On(ISubscriptionContext<uint> context, IncrementEvent evt)
    {
        Count++;
    }
}
```

The ```ISubscribe``` specifies how you subscribe to events.  In this case, ```Counter1``` will subscribe to all ```IncrementEvents``` and ensure that an instance of the ```On``` method is specified for handling how each event modifies the local state, ```Count```.

Instead of modeling the distributed counter as individual increment operations, we could model the counter using state and operations that transform that state.

Here, ```Counter2``` is partitioned state containing one member: ```Count```.  It is partitioned using the same partitioning key and affinity as ```Counter1```.

```c#
[DataContract]
public class Counter2 :
    IPartitionedState<ICounterAffinity,uint>
{
    [DataMember]
    public int Count;
}
```

## Operations

We can then define an increment operation that performs updates.  The ```IUpdate``` interface specifies an update operation that is required to have one method, ```Execute```, that takes an execution context, ```Counter2``` and performs an operation that transforms the state of the counter object.  These operations are automatically routed to the correct node based on the object's affinity.

```c#
[DataContract]
public class IncrementUpdate :
    IUpdate<Counter2, UnitType>,
    ICounterAffinity
{
    [DataMember]
    public uint CounterId { get; set; }

    public UnitType Execute(IUpdateContext<Counter2> context)
    {
        context.State.Count++;
        return UnitType.Value;
    }
}
```

Since operations can be composed, we can define an operation that performs an
increment operation and then reads the value after.

```c#
[DataContract]
public class IncrementThenRead : 
    IUpdate<Counter2, UnitType>, 
    ICounterAffinity
{
    [DataMember]
    public uint CounterId { get; set; }

    public UnitType Execute(IUpdateContext<Counter2> context)
    {
        context.PerformUpdate(new IncrementUpdate() { CounterId = CounterId });

        context.Logger.LogDebug($"IncrementThenRead({context.State.Count}) End");

        return UnitType.Value;
    }
}
```