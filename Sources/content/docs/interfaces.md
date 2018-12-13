---
title: "Interfaces"
description: Interfaces for the Reactive Machine
weight: 5
menu:
  main: 
    weight: 20
---

# Interfaces 

The Reactive Machine interfaces: understanding how to build your application.

* **Orchestrations** describe operations that are composed of one or more other operations. They are written in async/await style, and execute deterministically and reliably. When executing an operation, orchestrations can either perform it (meaning they wait for its completion) or fork it (meaning it executes completely independently of its parent).
* **Activities** are operations that can be unreliable or nondeterministic, such as calls to external services.
* *States** represent a small piece of information (cf. key-value pair, or a grain, or virtual actor) that can be atomically accessed via a specified set of read and update operations.
* **Affinities** define locality, by specifying keys that can be used to place state, orchestrations, and activities. These keys are also used for synchronization (locking). Events provide reliable, consistent pub-sub. When an event is raised by an orchestration, all the states that subscribe to it are modified. Events appear to be globally ordered and virtually synchronous.