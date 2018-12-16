---
title: "Orchestrations"
description: compose operations deterministically and reliably
weight: 2
menu:
  main: 
    weight: 2
---

Orchestrations are classes that define how to execute of one or more operations in their `Execute` method.

Orchestrations can easily express sequential and parallel composition as straight-line code. For example:

```c#
public class SequentialOrchestration : IOrchestration<int>
{
    public async Task<int> Execute(IOrchestrationContext context)
    {
        int result1 = await context.Perform(...first operation...);
        int result2 = await context.Perform(...second operation...);
        return result1 + result2;
    }
}
```
```c#
public class ParallelOrchestration : IOrchestration<int>
{
    public async Task<int> Execute(IOrchestrationContext context)
    {
        Task<int> task1 = context.Perform(...first operation...);
        Task<int> task2 = context.Perform(...second operation...);
        return (await task1) + (await task2);
    }
}
```

Of course, it does not end there. Orchestrations are plain C# code with async/await, so we enjoy a rich language support that lets us express anything from simple loops to subtle nested exception handlers with pattern matching.   

## Requirements

An orchestration class must

- implement the interface `IOrchestration<TReturn>` where `TReturn` is the return type
- be serializable, and have a serializable return type
- specify all inputs to the orchestration as fields or properties
- provide an `Execute` function that specifies how the orchestration executes
- call no asynchronous APIs other than the ones provided by the `IOrchestrationContext`
- execute no nondeterministic code other than what is provided by the `IOrchestrationContext`
- execute a bounded number of operations

### Example

Consider a simple orchestration that reads the content of one blob, appends a timestamp, and then writes it to another blob. The code is shown below. 

```c#
public class CopyBlob : IOrchestration<UnitType>
{
    public string From;
    public string To;

    public async Task<UnitType> Execute(IOrchestrationContext context)
    {    
        // read content from a blob
        var content = await context.PerformActivity(new ReadBlob() { Path = From });

        // append the current time
        content = content + " " + await context.ReadDateTimeUtcNow();
        
        // write the modified content to a blob
        await context.PerformActivity(new WriteBlob() { Path = To, Content = content});

        return UnitType.Value;
    }
}
```
Note the following:

- The input to the orchestrations are the names of the source and destination blob, represented by the fields `From` and `To`.
- This orchestration does not return a meaningful value. However, our framework requires that all orchestrations do, so we use the type `UnitType` (cf. `void` in C#, or `unit` in F#). It is a predefined type that has a single value `UnitType.Value`, which we return at the end of `Execute`.
- Reading a blob is asynchronous and nondeterministic. Thus we cannot call Azure storage directly inside the `Execute` method. Rather, we encapsulate those calls inside a `ReadBlob` activity (defined in a separate class) and execute it via `context.PerformActivity`. This makes it deterministically replayable.
- Similarly, we encapsulate the Azure storage calls for writing a the blob in a `WriteBlob` activity.
- The timestamp is nondeterministic. Thus we cannot simply call `DateTime.UtcNow`. Instead, we call the specially provided method `context.ReadDateTimeUtcNow`. This makes the timestamp deterministically replayable.
 

## Perform vs. Fork

The orchestration context provides the means for performing operations. Those operations can themselves be orchestrations, or they can be activities, reads, writes, or events. Each type of operation has its corresponding method, with type-checked arguments and return values: 

```csharp
Task<TReturn> PerformOrchestration<TReturn>(IOrchestration<TReturn> orchestration);
Task<TReturn> PerformActivity<TReturn>(IActivityBase<TReturn> activity);
Task<TReturn> PerformUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
Task<TReturn> PerformRead<TState, TReturn>(IRead<TState, TReturn> read)
Task PerformEvent(IEvent evt);
```

All operations return a task. These tasks finish when the operation is completed, and can return a value or throw an exception. The expectation is that the orchestrations awaits these tasks prior to finishing. However, sometimes it makes sense for an orchestration to *not* await the task:

- An orchestration may not wish to observe the return value or exception of an orchestration or activity it launches, but simply start it and then continue immediately and independently.
- An orchestration may not want to wait for an update or event to be applied to all states it modifies, which could take significant time.

For those cases, we provide a variant to *fork* an operation, rather than perform it:

```csharp
void ForkOrchestration<TReturn>(IOrchestration<TReturn> orchestration);
void ForkActivity<TReturn>(IActivityBase<TReturn> activity);
void ForkUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
void ForkEvent(IEvent evt);
```

A forked operation executes completely independently of its parent. If it throws an exception, that exception is passed to a global exception handler (which is defined by the host).

Two forked operations are always performed *in order* if they target the same affinity partition. In particular, one can implement streams using forked events or updates.

## Eternal Orchestrations

Orchestrations must be mindful of not performing too many operations, because internally, the implementation records and replays a log of all operations. For situations where the application wants to run very long (or infinite) iterations, the recommended practice is to use *forking*. This avoids accumulating a history that grows without bounds, and is our equivalent of tail recursion, or of "eternal orchestrations".

Concretely, rather than running a long or infinite loop inside `Execute`, an orchestration can run a single iteration of the loop inside `Execute`, and then **fork itself** at the end. If an iteration variable is needed, it can be encoded as a (serializable) field of the orchestration.


