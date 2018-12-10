---
title: "Activities"
description: Interacting with the outside world
weight: 6
---

# Activities

Activities are operations that can be unreliable or nondeterministic, such as calls to external services.  In the Reactive Machine, the replay and recoverability of the system relies on the system remaining deterministic.  That means that any nondeterminism required in your application needs to be determinized through a record/replay mechanism.

## Sources of nondeterminism 

Nondeterminism can creep into your application in many ways.  Here's some of them:

* Accessing the clock
* Setting a timer that will fire after a particular amount of time
* Random number generation
* Accessing external services
* Incoming web requests
* CPU intensive computations that make take an arbitrary amount of time to return

## "Determinizing" nondeterminism

Activites allow nondeterminism to be effectively "determinized."  Activities are logged by the system prior to execution and the return values of activities are logged when the operation completes.  Under replay, requests are not reissued if they have already been issued and a value returned: instead, the return value in the log is used as the return value for the operation, ensuring that under replay completed activities are deterministic.  Activities provide at-least or at-most once semantics: under replay, the application developer can choose to _always_ reissue the request, or the developer can choose to _never_ reissue the request if the system knows that the request has already been previously issued: if so, an execption can be thrown and the application developer can handle this in a application-specific manner.

## Miner Service

To demonstrate, we provide an example of an activity from a hash-space mining service.  With this example, searching for a hash collision inside of a space can take an arbitrary amount of time to return, with an arbitrary result, otherwise, the time limit expires.  Therefore, we wrap this in an activity to ensure that it remains deterministic.  To ensure at-least once execution under failure, we use the ```IAtLeastOnceActivity``` interface.

```c#
namespace Miner.Service
{
    [DataContract]
    public class SearchPortion : IAtLeastOnceActivity<List<long>>
    {
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);

        [DataMember]
        public int Target;

        [DataMember]
        public long Start;

        [DataMember]
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
