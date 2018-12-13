---
title: "Orchestrations"
description: Composing operations together
weight: 5
menu:
  main: 
    parent: "Interfaces"
    weight: 15
---

# Orchestrations

Orchestrations describe operations that are composed of one or more other operations. They are written in async/await style, and execute deterministically and reliably. When executing an operation, orchestrations can either perform it (meaning they wait for its completion) or fork it (meaning it executes completely independently of its parent).

Orchestrations can be composed of:

* **Operations**: application-specific operations that alter state within the system;
* **Activities**: application-specific operations that contain side-effects;
* **Orchestrations**: other orchestrations.

## Example Activities: Read / Write Blob

To demonstrate orchestrations, we begin by defining two activities for reading and writing from external storage.

First, we define the ```ReadBlob``` activity.  This activity defines a return value of ```string``` and the activity uses the at-least-once retry strategy under failure.  The class defines instance variables for the input for the activity an execute method that is used to perform the action.  It returns a ```Task<string>``` as its output value.

The ```WriteBlob``` activity is mostly the same, but demonstrates that activities can return void by returning a ```UnitType``` value.

```c#
public class ReadBlob : IAtLeastOnceActivity<string>
{
    public string Path;

    public Task<string> Execute(IActivityContext context)
    {
        context.Logger.LogInformation("Reading From Storage");
        var content = await Utils.GetBlob(Path).DownloadTextAsync();
        return content;
    }
}

public class WriteBlob : IAtLeastOnceActivity<UnitType>
{
    public string Path;
    public string Content;

    ...
```

## Example Orchestration: Read / Write Activity

To use these activities, we create an orchestration.  Our ```CopyBlob``` orchestration returns ```UnitType```, and performs a read and subsequent write.

```c#
public class CopyBlob : IOrchestration<UnitType>
{
    public string From;
    public string To;

    public async Task<UnitType> Execute(IOrchestrationContext context)
    {
        var content = await context.PerformActivity(new ReadBlob() { Path = From });
            
        await context.PerformActivity(new WriteBlob() { Path = To, Content = content});
    }
}
```

This orchestration is **guaranteed** to execute: programmers do not need to worry about the resilience of the orchestration, only the interaction with the external services, defined by activities.