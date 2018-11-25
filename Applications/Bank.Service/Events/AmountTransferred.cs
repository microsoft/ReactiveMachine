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
    public class AmountTransferred :
        IEvent,
        IMultiple<IAccountAffinity,Guid>
    {
        [DataMember]
        public Transfer TransferRequest; // all the details of the request

        [DataMember]
        public DateTime Timestamp { get; set; } // the timestamp of the transfer

        public IEnumerable<Guid> DeclareAffinities()
        {
            yield return TransferRequest.FromAccount;
            yield return TransferRequest.ToAccount;
        }
    }
}
