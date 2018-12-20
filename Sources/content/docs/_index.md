---
title: The Reactive Machine
short: Overview
description: Elastic stateful  microservices for serverless environments
weight: 1
---

## Programming Model

The application logic is expressed by defining classes that represent certain entitities, including *Orchestrations*, *Activities*, *States*, *Affinities*, *Reads*, *Updates*, and *Events*. 

All application and execution state is backed by durable storage, and is recovered automatically on failures. Also, the application logic does not fix the number of host machines, but allows the application to be distributed on a cluster of varying size.

**Orchestrations** define how to execute of one or more operations in a sequence, or in parallel. Orchestrations are written in async/await style, and may return a result value.  

**States** represent a small piece of information, like a key-value pair, or virtual actor. For each state you can define a partition key, read operations, update operations, and event subscriptions.  

## Hosts

The reactive machine compiler first transforms the application logic into an intermediate representation,  a *deterministically replayable distributed state machine*. This state machine is then be hosted on a back-end that provides exactly-once messaging and processing, using some variation of snapshot and replay. 

Currently, the repository contains two hosts. We plan on adding more hosts soon, and welcome community-contributed hosts. Different back-end hosts always provide the same application semantics, and are therefore easily interchangeable. But they can exhibit large differences in cost, scalability, latency, throughput, and recovery speed.

The local **EmulatorHost** is meant for debugging and local profiling. It emulates the reactive machine processes in a single process, using either a single thread or multiple threads.

The **FunctionsHost** is implemented on top of Azure Functions, Azure EventHubs, and Azure Blobs. It uses EventHubs to launch the reactive machine processes inside Azure functions, and to implement reliable communication between the processes. State snapshots are stored in Azure Blobs.

## Languages

Everything is written in C# .NET Standard 2.0. Support for other languages is conceivable, but not on our immediate Radar.

## Status and Plan

We are currently at 1.0.0-alpha, meaning that this is preview which should give you a pretty good idea of what this is all about once finished. What we have is:

- A C# implementation of the reactive machine programming model and compiler
- Two host implementations (emulator and functions)
- Application examples to demonstrate the features of the programming model
- A Hello World sample to demonstrate how to use the 2 hosts

Before we can release 1.0.0-beta, we need to:

- Plug holes in the documentation
- Implement support for code updates, placement updates, and changing the number of processes
- Fix known bugs in existing tests
- Build and test on Linux

Moving from 1.0.0-beta to 1.0.0 is then a matter of stability, i.e. we will remove the 'beta' tag once we feel comfortable with users placing trust on the stability of the code in a production environment.
