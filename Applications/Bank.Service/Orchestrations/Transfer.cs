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
    public class Transfer : 
        IOrchestration<bool>,
        IMultiple<IAccountAffinity, Guid>
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string Credentials;

        [DataMember]
        public Guid FromAccount;

        [DataMember]
        public Guid ToAccount;

        [DataMember]
        public int Amount;

        public IEnumerable<Guid> DeclareAffinities()
        {
            yield return FromAccount;
            yield return ToAccount;
        }

        [Lock]
        public async Task<bool> Execute(IOrchestrationContext context)
        {
            // perform authentication
            await context.PerformRead(new Authentication()
            {
                UserId = UserId,
                Credentials = Credentials
            });

            // check all involved state so we can validate preconditions
            var t1 = context.PerformRead(new CheckAccount() { AccountId = FromAccount });
            var t2 = context.PerformRead(new CheckAccount() { AccountId = ToAccount });

            // get a timestamp
            var timestamp = await context.ReadDateTimeUtcNow();

            // wait for the checks to complete. This ensures both accounts exist.
            // (otherwise an exception is thrown)
            var fromAccount = await t1;
            var toAccount = await t2;

            if (fromAccount == null)
                throw new KeyNotFoundException($"no such account: {fromAccount}");
            if (toAccount == null)
                throw new KeyNotFoundException($"no such account: {toAccount}");

            if (fromAccount.Owner != UserId)
            {
                throw new InvalidOperationException("only owner of account can issue transfer");
            }
            else if (fromAccount.Balance < Amount)
            {
                return false;
            }
            else
            {
                await context.PerformEvent(new AmountTransferred()
                {
                    TransferRequest = this,
                    Timestamp = timestamp
                });

                return true;
            }
        }

    }
}
