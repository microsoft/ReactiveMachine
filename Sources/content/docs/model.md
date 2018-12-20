---
title: "Programming Model"
description: express your application logic
weight: 2
menu:
  main: 
    weight: 2
---

**Orchestrations** define how to execute of one or more operations in a sequence, or in parallel. Orchestrations can

- perform **Activities** which encapsulate calls to external services, or any other nondeterministic behavior.
- perform **Read** or **Update** operations that target a particular state.
- raise **Events** that atomically update all subscribed states.
- specify one or more partition **Locks** that should be held during execution

**States** represent a small piece of information (like a key-value pair, or virtual actor). For each state, one can

- choose an **Affinity** that defines the desired partitioning scheme
- define **Read** or **Update** operations that access a state partition, and can return a value or exception
- define subscriptions to **Events** that update a state partition

**Affinities** describe an elastic partitioning scheme, i.e. a key type and placement attributes. Affinities can be used to

- partition **States** using a partition key
- place **Orchestrations** so they execute on a particular partition
- enable fine-grained concurrency control via per-partition **Locks**

