// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public interface IDeepCopier
    {
        T DeepCopy<T>(T other);
    }
}
