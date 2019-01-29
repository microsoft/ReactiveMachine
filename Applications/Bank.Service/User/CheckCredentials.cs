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
    internal class CheckCredentials : 
        IRead<UserState, bool>, 
        IUserAffinity
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string Credentials;

        public bool Execute(IReadContext<UserState> context)
        {
            // in  reality this would be something more interesting,
            // e.g. validate a crypto-signed token
            return Credentials == context.State.InitialCredentials; 
        }    
    }
}
