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
    public class GetAccountInfo :
        IRead<AccountState, GetAccountInfo.Response>,
        IAccountAffinity
    {
        public Guid AccountId { get; set; }

        [DataContract]
        public class Response
        {
            [DataMember]
            public int Balance;

            [DataMember]
            public string Owner;
        }

        public Response Execute(IReadContext<AccountState> context)
        {
            return new Response()
            {
                Balance = context.State.Balance,
                Owner = context.State.Owner
            };
        }
    }
}
