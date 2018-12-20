---
title: "Events"
description: update all subscribed states atomically
weight: 3
menu:
  main: 
    parent: "Programming Model"
    weight: 33
---

<p style="color:red; font-size:20pt">(section needs work)</p>

Events provide reliable, consistent pub-sub. When an event is raised by an orchestration, all the states that subscribe to it are modified. Events appear to be globally ordered and virtually synchronous.