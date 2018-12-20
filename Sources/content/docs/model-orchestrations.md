---
title: "Orchestrations"
description: compose operations deterministically and reliably
weight: 21
menu:
  main: 
    parent: "Programming Model"
    weight: 21
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
- provide an `Execute` function that specifies how the orchestration executes
- specify all inputs to the orchestration as fields or properties
- be serializable, and have a serializable return type

Moreover, the `Execute` method must 

- call no asynchronous APIs other than the ones provided by the `IOrchestrationContext`
- execute no nondeterministic code other than what is provided by the `IOrchestrationContext`
- execute a bounded number of operations

These rules are required so we can replay orchestrations deterministically.

### Example

Consider a simple orchestration that reads the content of one blob, prepends a timestamp, and then writes it to another blob. The code is shown below. 

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
        var time = await context.ReadDateTimeUtcNow();
        content = time.ToString("o") + "\n" + content;
        
        // write a message to the log
        context.Logger.LogInformation($"Writing content to {To} at time {time:o}");

        // write the modified content to a blob
        await context.PerformActivity(new WriteBlob() { Path = To, Content = content});

        return UnitType.Value;
    }
}
```
Note the following:

- The input to the orchestrations are the names of the source and destination blob, represented by the fields `From` and `To`.
- This orchestration is not intended to return a value. However, our framework requires that all orchestrations do return *some* value, so we use the type `UnitType` (cf. `void` in C#, or `unit` in F#). It is a type we have already defined for this purpose; it has a single value `UnitType.Value`, which we return at the end of `Execute`.
- Reading a blob is asynchronous and nondeterministic. Thus we cannot call Azure storage directly inside the `Execute` method. Rather, we encapsulate those calls inside a `ReadBlob` activity (defined in a separate class) and execute it via `context.PerformActivity`. This makes it deterministically replayable.
- Similarly, we encapsulate the Azure storage calls for writing a the blob in a `WriteBlob` activity.
- The timestamp is nondeterministic. Thus we cannot simply call `DateTime.UtcNow`. Instead, we call the specially provided method `context.ReadDateTimeUtcNow`. This makes the timestamp deterministically replayable.
 

## The Orchestration Context

The orchestration context that is passed as an argument to the `Execute` functions has the type `IOrchestrationContext`, which provides methods for logging, and for performing, forking, and scheduling other operations.

### Logging

The `context.GetLogger` returns an `ILogger` that can be used for logging, following the standard practices for logging on .NET. Simply include the following clause at the beginning:

```csharp
using Microsoft.Extensions.Logging;
```

The needed package `Microsoft.Extensions.Logging.Abstractions` is automatically installed by NuGet when installing the `Microsoft.ReactiveMachine.Abstractions` package. 

### Performing

The orchestration context provides several methods for performing asynchronous operations. 

Each type of operation has its corresponding method, with type-checked arguments and return values: 

```csharp
Task<TReturn> PerformOrchestration<TReturn>(IOrchestration<TReturn> orchestration);
Task<TReturn> PerformActivity<TReturn>(IActivityBase<TReturn> activity);
Task<TReturn> PerformUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
Task<TReturn> PerformRead<TState, TReturn>(IRead<TState, TReturn> read)
Task PerformEvent(IEvent evt);
```

All performed operations are asynchronous, i.e. return a task. These tasks finish when the operation is completed and can return a value or throw an exception. The expectation is that the orchestrations awaits these tasks prior to finishing.

### Forking

Sometimes it makes sense for an orchestration to *not* await the operation it performs. For example, we may want to simply kick off a new operation and then continue the execution immediately and independently, without waiting for that operation to complete, and without ever observing any return value or exception.
This is particularly common for events, which are intended to decouple the producers and consumers.  

If not awaiting the returned task, orchestrations should *fork* the operation rather than perform it:

```csharp
void ForkOrchestration<TReturn>(IOrchestration<TReturn> orchestration);
void ForkActivity<TReturn>(IActivityBase<TReturn> activity);
void ForkUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
void ForkEvent(IEvent evt);
```

A forked operation executes completely independently of its parent. If it throws an exception, that exception is passed to a global exception handler (which is defined by the host).

Two forked operations are always performed *in order* if they target the same affinity partition. Thus, it is possible to implement ordered streams using forked events or updates.

### Scheduling

Sometimes, we want a forked operation to not start immediately. The following methods allow us to include a delay. 
```csharp
void ScheduleOrchestration<TReturn>(TimeSpan delay, IOrchestration<TReturn> orchestration);
void ScheduleActivity<TReturn>(TimeSpan delay, IActivityBase<TReturn> activity);
void ScheduleUpdate<TState, TReturn>(TimeSpan delay, IUpdate<TState, TReturn> update)
void ScheduleEvent(TimeSpan delay, IEvent evt);
```

The runtime will make a "best effort" to start the scheduled operation somewhere near the desired time. However, there are no specific guarantees about precision or relative ordering. The guarantees are thus a bit weaker than for fork.

### Configuration information

The context also provides a method for retrieving configuration information that was specified during the application build:
```csharp
TConfiguration GetConfiguration<TConfiguration>();
```

The configuration is identified by its type `TConfiguration`. If no such configuration exists, an exception is thrown.

## Guidelines

As mentioned earlier, the runtime records the execution of an orchestration's `Execute` function in a log that can be used for deterministic replay. For this mechanism to work correctly and efficiently, it is important to avoid nondeterminism, and to avoid long-running orchestrations.

### Sources of Nondeterminism 

Nondeterminism can creep into your application in many ways. Here is a list of common sources, together with the recommended solution. 

| Nondeterminism Source  | Solution  |
|---|---|
| Access the current clock |  use `context.GetDateTimeNow` or `context.GetDateTimeUtcNow` |
| Set a timer to fire after a specified time  |  use `context.ScheduleXXX` |
| Generate a random number | use `context.NewRandom` |
| Generate a fresh GUID | use `context.NewGuid` |
| Call external services | encapsulate in an Activity | 
| Run task on thread pool | run task as an Activity | 

### Long-Running Orchestrations

One must be mindful of not performing too many operations in an `Execute` method, because internally, the implementation records a log of all performed operations to allow deterministic replay. 

For situations where the application wants to run very long (or infinite) loop, the recommended practice is thus to use *forking* or *scheduling*. This avoids accumulating a history that grows without bounds.

Concretely, rather than running a long or infinite loop inside `Execute`, an orchestration can run a single iteration of the loop inside `Execute`, and then **fork or schedule itself** at the end. 

For example, we can implement an orchestration that re-executes every 10 minutes as follows:
```csharp
public class PeriodicOrchestration: IOrchestration<UnitType>

    public int IterationNumber;

    public Task<UnitType> Execute(IOrchestrationContext context)
    {
        // do something first
        ...
        // then schedule our next iteration
        IterationNumber++;
        context.ScheduleOrchestration(TimeSpan.FromMinutes(10), this);

        return UnitType.Value;
    }
}
```



