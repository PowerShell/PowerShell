// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.All
{
    public static class AllTests
    {
        [Fact]
        public static void All()
        {
            // It just needs an arbitrary type
            Assert.False(false);
        }
    }
}
