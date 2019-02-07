---
title: "Programming Model"
description: express your application logic
weight: 2
menu:
  main: 
    weight: 2
---

The application logic is expressed declaratively, by defining various classes that server a specialized purpose, *Orchestrations*, *Activities*, *States*, *Affinities*, *Reads*, *Updates*, and *Events*.

**Orchestrations** define how to execute of one or more operations in a sequence, or in parallel. Orchestrations can

- perform **Activities** which encapsulate calls to external services, or any other nondeterministic behavior.
- perform **Read** or **Update** operations that target a particular state.
- raise **Events** that atomically update all subscribed states.
- specify one or more affinity locks that should be held during execution

**Affinities** describe an elastic partitioning scheme, i.e. a key type and placement attributes. Affinities can be used to

- partition **States** using a partition key
- place **Orchestrations** so they execute on a particular affinity
- enable fine-grained concurrency control via affinity locking

**States** represent a small piece of information (like a key-value pair, or virtual actor). For each state, one can

- choose an **Affinity** that defines the desired partitioning scheme
- define **Read** or **Update** operations that access a state, and can return a value or exception
- define an **Initialization** orchestration
- define subscriptions to **Events** that update a state
