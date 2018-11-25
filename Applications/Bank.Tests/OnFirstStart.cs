// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using Bank.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bank.Tests
{

    public class OnFirstStart : IStartupOrchestration
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            await context.PerformOrchestration(new SignUp()
            {
                UserId = "al302",
                FullName = "Alice",
                InitialBalance = 100,
                InitialCredentials = "password123!",
                CheckingAccountId = await context.NewGuid(),
                SavingsAccountId = await context.NewGuid(),
            });
            await context.PerformOrchestration(new SignUp()
            {
                UserId = "bobba1965",
                FullName = "Bob",
                InitialBalance = 100,
                InitialCredentials = "maythe4thbewithy@u",
                CheckingAccountId = await context.NewGuid(),
                SavingsAccountId = await context.NewGuid(),
            });

            return UnitType.Value;
        }
    }

}