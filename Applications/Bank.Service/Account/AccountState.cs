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
    public class AccountState : 
        IPartitionedState<IAccountAffinity, Guid>,
        ISubscribe<UserSignedUp, IAccountAffinity, Guid>,
        ISubscribe<AmountTransferred, IAccountAffinity, Guid>
    {
        [DataMember]
        public string Owner;

        [DataMember]
        public DateTime? Created;

        [DataMember]
        public DateTime? LastModified;

        [DataMember]
        public int Balance;

        [DataMember]
        AccountType Type;

        public enum AccountType {  Checking, Savings }

        public void On(ISubscriptionContext<Guid> context, UserSignedUp evt)
        {
            Owner = evt.UserId;
            
            Created = evt.Timestamp;
            LastModified = evt.Timestamp;

            if (evt.CheckingAccountId == context.Key)
            {
                Type = AccountType.Checking;
                Balance = 10;
            }
            else if (evt.SavingsAccountId == context.Key)
            {
                Type = AccountType.Savings;
                Balance = 0;
            }
        }

        public void On(ISubscriptionContext<Guid> context, AmountTransferred evt)
        {
            if (evt.TransferRequest.FromAccount == context.Key)
            {
                Balance -= evt.TransferRequest.Amount;
            }
            else if (evt.TransferRequest.ToAccount == context.Key)
            {
                Balance += evt.TransferRequest.Amount;
            }

            LastModified = evt.Timestamp;
        }

        
    }
}

