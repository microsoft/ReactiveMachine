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
    public class CheckAccount :
        IRead<AccountState, CheckAccount.Response>,
        IAccountAffinity
    {
        public Guid AccountId { get; set; }

        [DataContract]
        public class Response
        {
            [DataMember]
            public bool Exists;

            [DataMember]
            public int Balance;

            [DataMember]
            public string Owner;
        }

        public Response Execute(IReadContext<AccountState> context)
        {
            var accountInfo = context.State;

            if (!accountInfo.Created.HasValue)
            {
                return null;
            }
            else
            {
                return new Response()
                {
                    Balance = accountInfo.Balance,
                    Owner = accountInfo.Owner
                };
            }
        }
    }
}
