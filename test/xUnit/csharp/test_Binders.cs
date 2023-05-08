// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel
{
    public static class PSEnumerableBinderTests
    {
        [Fact]
        public static void TestIsStaticTypePossiblyEnumerable()
        {
            // It just needs an arbitrary type
            Assert.False(PSEnumerableBinder.IsStaticTypePossiblyEnumerable(42.GetType()));
        }
    }
}
