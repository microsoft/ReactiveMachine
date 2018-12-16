---
title: The Reactive Machine
short: Overview
description: Elastic stateful  microservices for serverless environments
weight: 1
---

## Programming Model

The application logic is expressed using *Orchestrations* and *States*. 

- **Orchestrations** define how to execute of one or more operations in a sequence, or in parallel. Orchestrations are written in async/await style, and may return a result value. 
- **States** represent a small piece of information (like a key-value pair, or virtual actor), and are partitioned by **Affinities** (which define the key type and placement).

An orchestration can perform the following operations:

- **Activities** which encapsulate calls to external services, or any other nondeterministic behavior.
- **Read** or **Update** operations that target a particular state.
- **Events** that atomically update all subscribed states.
 
Note that server or connection failures are invisible to the application logic, and the number of servers is unspecified. 

In the code, all of the entities (Orchestrations, States, Affinities, Activities, Reads, Updates, Events) are represented by serializable classes.

## Hosts

The reactive machine compiler first transforms the application logic into an intermediate representation,  a *deterministically replayable distributed state machine*. This state machine is then be hosted on a back-end that provides exactly-once messaging and processing, using some variation of snapshot and replay. 

Currently, the repository contains two hosts. We plan on adding more hosts soon, and welcome community-contributed hosts.

- A local **EmulatorHost**, meant for debugging and local profiling. It emulates the reactive machine processes in a single process, using either a single thread or multiple threads.
- A **FunctionsHost**, built on top of Azure Functions, Azure EventHubs, and Azure Blobs. It uses EventHubs to launch the reactive machine processes inside Azure functions, and to implement reliable communication between the processes. State snapshots are stored in Azure Blobs.

Different back-end hosts always provide the same application semantics, and are therefore easily interchangeable. But they can exhibit large differences in cost, scalability, latency, throughput, and recovery speed. 

## Languages

Everything is written in C# .NET Standard 2.0. Support for other languages is conceivable, but not on our immediate Radar.

## Status

We are currently at 1.0.0-alpha, meaning that this is preview of what we expect to release in the first release (1.0.0). It includes:

- The C# version of the reactive machine programming model and compiler, on .NET standard 2.0
- Two host implementations (emulator and functions)
- Application examples to demonstrate the features of the programming model
- A Hello World sample to demonstrate how to use the 2 hosts

What remains to be done for 1.0.0 is:

- Get documentation to be reasonably complete
- Add support for code updates
- Fix known bugs in existing tests, and add more tests
- Build and test on Linux
