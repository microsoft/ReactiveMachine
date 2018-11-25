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
    internal class Authentication : 
        IRead<UserState, UnitType>, 
        IUserAffinity
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string Credentials;

        public UnitType Execute(IReadContext<UserState> context)
        {
            // retrieve the information for this user
            var userInfo = context.State;

            // if authentication fails, raise event and throw exception
            if (!Validate(Credentials, userInfo.InitialCredentials))
            {
                throw new InvalidOperationException("Unauthorized");
            }

            // TODO
            // escape transaction to report failed login

            return UnitType.Value;
        }

        private bool Validate(string credentials, string initialCredentials)
        {
            return credentials == initialCredentials; // TODO make more interesting 
        }
    }
}
