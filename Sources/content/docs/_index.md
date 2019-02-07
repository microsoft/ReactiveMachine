---
title: The Reactive Machine
short: Overview
description: Elastic stateful  microservices for serverless environments
weight: 1
---

## Programming Model

The application logic is expressed declaratively, by defining various classes that server a specialized purpose, *Orchestrations*, *Activities*, *States*, *Affinities*, *Reads*, *Updates*, and *Events*.

**Orchestrations** define how to execute of one or more operations in a sequence, or in parallel. Orchestrations are written in async/await style, and may return a result value. Typically, orchestrations are executed by a service application to handle external requests or events.

**States** represent a small piece of information, like a key-value pair, or virtual actor. For each state you can define a partition key, read operations, update operations, and event subscriptions. States are passive: they do not initiate activity on their own, but can be read and modified by orchestrations.

The application logic does not need to deal with machine or connection failures, as those are taken care of automatically by the hosting layer. Also, the application logic does not need to assume a fixed number of host machines, but remains flexible in that regard, allowing deployment on a cluster of varying size (including a single machine).

## Hosts

The reactive machine compiler first transforms the application logic into an intermediate representation,  a *deterministically replayable distributed state machine*. This state machine is then be hosted on a back-end that provides exactly-once messaging and processing, using some variation of snapshot and replay. 

The hosting layer guarantees that all the application and execution state is backed by some combination of durable storage and/or durable queues, and can recover automatically from failures.

Currently, the repository contains two hosts. We plan on adding more hosts soon, and welcome community-contributed hosts. 

- The local **EmulatorHost** is meant for debugging and local profiling. It emulates the reactive machine processes in a single process, using either a single thread or multiple threads.

- The **FunctionsHost** is implemented on top of Azure Functions, Azure EventHubs, and Azure Blobs. It uses EventHubs to launch the reactive machine processes inside Azure functions, and to implement reliable communication between the processes. State snapshots are stored in Azure Blobs.

Different back-end hosts always provide the same application semantics, and are therefore easily interchangeable. But they can exhibit large differences in cost, scalability, latency, throughput, and recovery speed.

## Status

We are currently at **1.0.0-alpha**, meaning that this is preview which should give you an idea of what this is all about once finished. What we have is:

- A C# implementation of the reactive machine programming model and compiler
- Two host implementations (emulator and functions)
- Application examples to demonstrate the features of the programming model
- A Hello World sample to demonstrate how to use the 2 hosts

Before we can release 1.0.0-beta, we still need to do some significant work:

- Add an Ambrosia Host
- Plug the many gaping holes in the documentation
- Implement support for code updates, placement updates, and changing the number of processes
- Fix known bugs in existing tests
- Build and test on Linux

Further in the future, we will consider moving from 1.0.0-beta to 1.0.0 as a matter of stability, i.e. we will remove the 'beta' tag once we feel comfortable with users placing trust on the stability of the code in a production environment.

## Languages

Everything is written in C# .NET Standard 2.0. Support for other languages is conceivable, but not on our immediate Radar.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
