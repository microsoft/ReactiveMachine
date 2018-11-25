// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

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
