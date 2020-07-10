// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public static class PSTypeExtensionsTests
    {
        [Fact]
        public static void TestIsNumeric()
        {
            Assert.True(PSTypeExtensions.IsNumeric(42.GetType()));
        }
    }
}
