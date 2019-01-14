// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public static class SecuritySupportTests
    {
        [Fact]
        public static void TestScanContent()
        {
            Assert.Equal(AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED, AmsiUtils.ScanContent(string.Empty, string.Empty));
        }

        [Fact]
        public static void TestCloseSession()
        {
            AmsiUtils.CloseSession();
        }

        [Fact]
        public static void TestUninitialize()
        {
            AmsiUtils.Uninitialize();
        }
    }
}
