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
    public class GetUserInfo :
        IRead<UserState, string>,
        IUserAffinity
    {
        public string UserId { get; set; }

        public string Execute(IReadContext<UserState> context)
        {
            return context.State.FullName;
        }
    }
}
