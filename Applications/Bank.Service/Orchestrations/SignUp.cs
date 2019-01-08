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
            // we want to check that none of the ids clash with existing ones
            var userExists = context.StateExists<UserState,IUserAffinity,string>(UserId);
            var checkingExists = context.StateExists<AccountState,IAccountAffinity,Guid>(CheckingAccountId);
            var savingsExists = context.StateExists<AccountState, IAccountAffinity, Guid>(SavingsAccountId);

            // we want to record a timestamp for the creation
            var timestamp = await context.ReadDateTimeUtcNow();

            
            if (await userExists)
            {
                throw new Exception("user already exists");
            }
            if (await checkingExists || await savingsExists)
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
