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
            // first, perform authentication
            await context.PerformRead(new Authentication()
            {
                UserId = UserId,
                Credentials = Credentials
            });

            // check the account
            var accountInfo = await context.PerformRead(
                new GetAccountInfo()
                {
                    AccountId = AccountId
                });

            if (accountInfo.Owner != UserId)
            {
                throw new InvalidOperationException("only owner of account can check balance");
            }

            return accountInfo.Balance;
        }
    }
}
