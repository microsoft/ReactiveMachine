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
    public class SignUp : 
        IOrchestration<UnitType>, 
        IUserAffinity,
        IMultiple<IAccountAffinity, Guid>
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string FullName;

        [DataMember]
        public string InitialCredentials;

        [DataMember]
        public int InitialBalance;

        [DataMember]
        public Guid SavingsAccountId;

        [DataMember]
        public Guid CheckingAccountId;

        public IEnumerable<Guid> DeclareAffinities()
        {
            yield return SavingsAccountId;
            yield return CheckingAccountId;
        }

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var t1 = context.PerformRead(new CheckUseridAvailable() { UserId = UserId });
            var t2 = context.PerformRead(new CheckAccount() { AccountId = CheckingAccountId });
            var t3 = context.PerformRead(new CheckAccount() { AccountId = SavingsAccountId });
            var t4 = context.ReadDateTimeUtcNow();

            var available = await t1;
            var clash1 = (await t2) != null;
            var clash2 = (await t3) != null;
            DateTime timestamp = await t4;

            if (! available)
            {
                throw new Exception("user already exists");
            }
            if (clash1 || clash2)
            {
                throw new Exception("account id already exists");
            }

            await context.PerformEvent(new UserSignedUp()
            {
                UserId = UserId,
                FullName = FullName,
                InitialCredentials = InitialCredentials,
                Timestamp = timestamp,
                CheckingAccountId = CheckingAccountId,
                SavingsAccountId = SavingsAccountId,
            });
            
            return UnitType.Value;
        }
    }

}
