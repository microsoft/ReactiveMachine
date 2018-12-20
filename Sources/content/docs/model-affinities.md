---
title: "Affinities"
description: express locality of state and operations
weight: 24
menu:
  main: 
    parent: "Programming Model"
    weight: 24
---

Affinities define locality, by specifying keys that can be used to place state, orchestrations, and activities. These keys are also used for synchronization (locking). Events provide reliable, consistent pub-sub. When an event is raised by an orchestration, all the states that subscribe to it are modified. Events appear to be globally ordered and virtually synchronous.

<p style="color:red; font-size:20pt">(section needs work)</p>

## Singletons

If we wish to place an object in only one location, the ```ISingletonAffinity``` interface can be used to force placement only at a single partition.

```c#
public interface ICounterAffinity :
    ISingletonAffinity<ICounterAffinity>
{
}
```

## Partitioned by key

Events in the reactive machine can be partitioned based on a partitioning key.  Here, we use the ```IPartitionedAffinity``` interface, and specify that the key is an unsigned integer.  We use the round robin placement strategy for how counters should be assigned to partitions.

```c#
public interface ICounterAffinity :
    IPartitionedAffinity<ICounterAffinity, uint>
{
    [RoundRobinPlacement]
    uint CounterId { get; }
}
```