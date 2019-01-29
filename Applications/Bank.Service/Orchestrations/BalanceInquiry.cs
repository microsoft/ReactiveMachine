// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Bank.Service
{
    [DataContract]
    public class BalanceInquiry : 
        IOrchestration<int>, 
        IUserAffinity,
        IAccountAffinity
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string Credentials;

        [DataMember]
        public Guid AccountId { get; set; }

        [Lock]
        public async Task<int> Execute(IOrchestrationContext context)
        {
            // first, check the supplied credentials
            bool credentialsValidated = false;
            try
            {
                credentialsValidated = await context.PerformRead(new CheckCredentials()
                {
                    UserId = UserId,
                    Credentials = Credentials
                });
            }
            catch (KeyNotFoundException) { }

            if (!credentialsValidated)
                throw new InvalidOperationException("Unauthorized");

            // if the specified account exists and is owned by this user, return balance
            try
            {
                var accountInfo = await context.PerformRead(
                        new GetAccountInfo()
                        {
                            AccountId = AccountId
                        });

                if (accountInfo.Owner == UserId)
                    return accountInfo.Balance;
            }
            catch (KeyNotFoundException) { }

            throw new InvalidOperationException("no such account");
        }
    }
}
