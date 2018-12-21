---
title: "Events"
description: update all subscribed states atomically
weight: 3
menu:
  main: 
    parent: "Programming Model"
    weight: 33
---

<p style="color:red; font-size:20pt">(section needs work)</p>

Events provide reliable, consistent pub-sub. When an event is raised by an orchestration, all the states that subscribe to it are modified. Events appear to be globally ordered and virtually synchronous.



Typically, events are forked rather than performed. However, sometimes orchestrations want to wait for all effects of an event to have been applied; in that case, they can perform the event and await the task. 

```csharp
await context.PerformEvent(new SomeEvent());   
```
