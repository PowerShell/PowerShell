// Copyright (c) Microsoft Corporation.
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
            try
            {
                Assert.Equal(AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED, AmsiUtils.ScanContent(string.Empty, string.Empty));
            }
            finally
            {
                AmsiUtils.Uninitialize();
            }
        }
    }
}
