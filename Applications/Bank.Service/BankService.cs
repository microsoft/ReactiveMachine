// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Bank.Service;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bank
{
    public class BankService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            builder.ScanThisDLL();
        }
    }
}