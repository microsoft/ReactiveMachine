---
title: "Serialization"
description: is required pervasively
weight: 34
menu:
  main: 
    parent: "Hosts"
    weight: 34
---

<p style="color:red; font-size:20pt">(section needs work)</p>

In the code, all of the entities (Orchestrations, States, Affinities, Activities, Reads, Updates, Events) and all return value types must be serializable classes. This allows the runtim to perform **Serialization** as needed to persist all states and orchestration progress durably and recover them automatically after machine or connection failures.
