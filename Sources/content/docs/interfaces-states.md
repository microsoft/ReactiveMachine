---
title: "States"
description: Storing and evolving application state using events
weight: 7
menu:
  main: 
    parent: "Interfaces"
    weight: 20
---

# States

States represent a small piece of information (cf. key-value pair, or a grain, or virtual actor) that can be atomically accessed via a specified set of read and update operations.

## Bank Example

We start by defining an affinity for how our data should be partitioned.  To do this, we implement an affinity based on the users identity.

```c#
namespace Bank.Service
{
    public interface IUserAffinity : 
        IPartitionedAffinity<IUserAffinity,string>
    {
        string UserId { get; }
    }
}
```

We create an event that our state will subscribe to: the ```UserSignedUp``` event.

```c#
namespace Bank.Service
{
    [DataContract]
    public class UserSignedUp : 
        IEvent,
        IUserAffinity,
        IMultiple<IAccountAffinity,Guid>
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string FullName;

        [DataMember]
        public string InitialCredentials;

        [DataMember]
        public DateTime Timestamp;

        [DataMember]
        public Guid SavingsAccountId;

        [DataMember]
        public Guid CheckingAccountId;

        public IEnumerable<Guid> DeclareAffinities()
        {
            yield return SavingsAccountId;
            yield return CheckingAccountId;
        }
    }
}
```

Now, we can create a partitioned state that's derived from the ```UserSignedUp``` events.  These events are subscribed to by implementing the ```ISubscribe``` interface and specifying which events you wish to subscribe to.  The ```IPartitionedState``` interface designates how the partitioning key should be used to partition the state across the nodes.

```c#
namespace Bank.Service
{
    [DataContract]
    public class UserState :
         IPartitionedState<IUserAffinity, string>,
         ISubscribe<UserSignedUp, IUserAffinity, string>
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public DateTime? Created;

        [DataMember]
        public string FullName;

        [DataMember]
        public string InitialCredentials;

        [DataMember]
        public HashSet<Guid> Accounts = new HashSet<Guid>();

        public void On(ISubscriptionContext<string> context, UserSignedUp evt)
        {
            Created = evt.Timestamp;
            FullName = evt.FullName;
            InitialCredentials = evt.InitialCredentials;
            Accounts.Add(evt.SavingsAccountId);
            Accounts.Add(evt.CheckingAccountId);
        }
    }
}
```

We can now create an operation for reading the state.  To do this, we implement the ```IRead``` interface.  We specify that we are reading ```UserState``` and returning a ```bool```.  Our operation implements the ```IUserAffinity``` partitioning key, and therefore needs to carry a ```UserId``` for routing to the correct partition.

```c#
namespace Bank.Service
{
    [DataContract]
    public class CheckUseridAvailable :
        IRead<UserState, bool>,
        IUserAffinity
    {
        public string UserId { get; set; }

        public bool Execute(IReadContext<UserState> context)
        {
            return ! context.State.Created.HasValue;
        }
    }
}
```