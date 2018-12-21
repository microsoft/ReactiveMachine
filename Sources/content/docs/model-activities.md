---
title: "Activities"
description: encapsulate nondeterminism and external interactions
weight: 22
menu:
  main: 
    parent: "Programming Model"
    weight: 22
---


Activities are classes that define how to execute some task in an `Execute` method, and may include nondeterminism and I/O.

### Example 1 : Calling an External Service

We can define an activity that reads the contents of a blob from Azure Storage:
```c#
public class ReadBlob : IAtLeastOnceActivity<string>
{
    // the input to the activity
    public string Path;

    // must specify a time limit
    public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);

    public async Task<string> Execute(IContext context)
    {
        context.Logger.LogInformation("Reading From Storage");
        CloudBlockBlob blobReference = Utils.GetAzureBlob(Path);
        var content = await blobReference.DownloadTextAsync();
        return content;
    }
}
```
**Timeouts**. Activities must specify a TimeLimit property. A timer is automatically started when the activity starts executing. If the activity does not finish before the time limit is reached, a `System.TimeoutException` is thrown. In general, using timeouts is helpful to guarantee that orchestrations can make progress.

**Context object**. Just like for orchestrations, the `Execute` method is passed a context. However, this context has a lot fewer methods. Essentially, it just supports logging (via `context.Logger`) and accessing configuration information (via `context.GetConfiguration<TConfiguration>()`).

### Example 2: Performing a CPU-intensive computation

In the `Applications/Miner.Service` project, we demonstrate a hash-space mining application.  Searching for a hash collision is CPU intensive, so we use an orchestration to break the search space into small portions, and then run an activity for searching each portion. The runtime runs each portion on the thread pool.

```c#
namespace Miner.Service
{
    public class SearchPortion : IAtLeastOnceActivity<List<long>>
    {
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);
 
        public int Target;
        public long Start;
        public long Count;

        public Task<List<long>> Execute(IContext context)
        {
            context.Logger.LogInformation($"Starting portion [{Start},{Start + Count})");
            var results = new List<long>();
            // search given range for a hash collision
            for (var i = Start; i < Start + Count; i++)
            {
                if (i.GetHashCode() == Target)
                    results.Add(i);
            }
            context.Logger.LogInformation($"Finished portion [{Start},{Start + Count})");
            return Task.FromResult(results);
        }
    }
}
```

## Guidelines

Activities "determinize" nondeterministic, external, fallible, or otherwise unpredictable tasks, by logging the outcome. Under replay, activities are not repeated if the log has already recorded a result (a returned value or exception) for this activity: instead, the recorded result is used. Thus, the activity has become *deterministically replayable*.  

 Activities also provide parallelism: they always run on the .NET thread pool, and are therefore appropriate for long-running CPU-intensive tasks.

### Activites vs. Orchestrations

Activities complement orchestrations in terms of what you are allowed to do inside `Execute`:

|                    | Activity  |  Orchestration |
|--------------------|-----------|----------------|
| Nondeterminism            | ✓  | ❌ |
| External Calls            | ✓  | ❌ |
| Any type of I/O           | ✓  | ❌ |
| Long-running computations | ✓  | ❌ |
| Perform an operation      | ❌ | ✓  |
| Fork an operation         | ❌ | ✓  |
| Schedule an operation     | ❌ | ✓  |

### At-least-once vs. At-most-once

If a host recovers after failing while executing an activity, the system detects in the log that the activity did not complete. However, it is not possible to know how far the activity progressed before the failure; it may have already achieved its objective, or it may have failed before the activity even got really started. The right thing to do in this case may depend on the specific purpose of the activity, i.e. it is application-dependent.

In most cases, the right thing to do is to simply restart the activity, at the risk of executing it a second time. By using the interface `IAtLeastOnceActivity<string>`, we indicate to the runtime that this is always the desired course of action.

Sometimes, it is desirable to take some special action rather than just restarting an activity. The interface `IAtMostOnceActivity<TReturn>` can be used for that:

```c#
public class MyActivity : IAtMostOnceActivity<string>
{
    public async Task<string> Execute(IContext context)
    {
       ... // regular execution
    }
    
    public Task<TReturn> AfterFault(IContext context)
    {
         ... // custom handling
    }
}
```

The `AfterFault` handler is called when the runtime, during recovery, detects that this activity may have been previously executed but did not complete. Inside the handler, we can take an appropriate action to deal with this situation. For example, we can perform some tests to figure out if the desired effect of the activity (e.g. creating a blob) has already taken place (i.e. the blob already exists), and re-execute it only if those tests indicate so.

Conceptually, the `AfterFault` handler provides us with a mechanism that can wrap external calls that are not idempotent or not exactly idempotent into a truly idempotent activity.


