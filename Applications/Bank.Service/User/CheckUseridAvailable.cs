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
    public class CheckUseridAvailable :
        IRead<UserState, bool>,
        IUserAffinity
    {
        public string UserId { get; set; }

        public bool Execute(IReadContext<UserState> context)
        {
            return ! context.State.Created.HasValue;
        }
    }
}
