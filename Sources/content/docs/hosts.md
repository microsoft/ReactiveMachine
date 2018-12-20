---
title: "Hosts"
description: can deploy Reactive Machine services
weight: 3
menu:
  main: 
    weight: 3
---




In the code, all of the entities (Orchestrations, States, Affinities, Activities, Reads, Updates, Events) and all return value types must be serializable classes. This allows the runtim to perform **Serialization** as needed to persist all states and orchestration progress durably and recover them automatically after machine or connection failures.
