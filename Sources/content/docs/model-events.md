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


## Event Subscriptions

State classes can also implement special subscription interfaces that allow them to be updated automatically in response to subscribed events.

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