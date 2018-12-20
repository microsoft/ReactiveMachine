---
title: "Motivation"
description: and the overall vision
weight: 5
---


# Motivation & Vision

With the promise of developer agility, independent operations, and elastic scalability, microservice based development has become the "new normal" in distributed application development.  Developers build application services such as dashboards, client services, and user-accesible APIs by "composing" existing cloud services, such as storage, databases, queues, and other microservices.

<img src="/img/motivation.PNG" alt="Motivation" style="width:8in; border:1px solid gray;"/>

## Stateless Microservices

However, many modern approaches to elastic scalability rely on an application tier that stateless and volatile.  Application state must be loaded at the start of each request, persisted to cloud storage at the end of each request, and cached locally, if the application developer is sure that it will not introduce consistency anomalies.  

<img src="/img/stateless.PNG" alt="Stateless and Volatile" style="width:8in; border:1px solid gray;"/>
 
This approch, while providing elastic scalability, unfortunately suffers many drawbacks.

* **Reliability**: Application requests that use several microservices may crash before all of their effects are completed -- a phenomena known as partial failure.   Application developers are left to encode logic for cleanup in their applications for dealing with these partial effects.
* **Consistent**: Application developers typically working with microservices do not have synchronization primitives available to them, requiring the user to implement complex logic for ensuring consistency and transactional isolation.
* **Performant**: As applications have to load state at the beginning of each request, and save state at the end of each request, application performance suffers.

## What Paradigm?

So, what paradigm is appropriate for building elastic, fault-tolerant microservices?  

<img src="/img/paradigms.PNG" alt="Paradigms" style="width:7in;"/>

It turns out that all of the different programming paradigms are *inter-expressible*.

However, tasks are more abstract: they are not tied to a particular computational model or network location.

## Comparison to Existing Frameworks

To position this along with the related work, we show where the Reactive Machine fits along with it's counterpart solutions.

<img src="/img/comparison.PNG" alt="Comparison to Existing Frameworks" style="width:7in;"/>

## Strategy

The Reactive Machine provides the developers of microservice applications with a task-based programming model composed of orchestrations, activities, events, and states.  The Reactive Machine then compiles these applications to an intermediate representation, a deterministic distributed state machine, that can be retargeted to popular cloud-based application environments and frameworks, such as Azure Functions, Kafka, Azure Event Hubs, Kubernetes, and Microsoft's Orleans.  

A unique feature of the Reactive Machine is that applications can be retargeted without altering application behavior, only the cost / performance trade-off differences between where the application is deployed.

<img src="/img/tactic.PNG" alt="Reactive Machine Strategy" style="width:9in; border:1px solid gray;"/>