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
