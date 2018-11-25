// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests
{
    public static class Assert
    {
        public static void Equal<T>(T expected, T actual) where T : IEquatable<T>
        {
            if (!expected.Equals(actual))
            {
                throw new TestFailureException($"expected: {expected} actual: {actual}");
            }
        }

    }
}