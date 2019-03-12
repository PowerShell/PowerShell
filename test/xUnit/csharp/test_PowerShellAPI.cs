// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Sequential
{
    // Not static because a test requires non-const variables
    public class PowerShellHostingScenario
    {
        // Test that it does not throw an exception
        [Fact]
        public void TestStartJobThrowTerminatingException()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Start-Job").AddParameter("ScriptBlock", ScriptBlock.Create("1+1"));
                Exception ex = Assert.Throws<CmdletInvocationException>(() => ps.Invoke());
                Assert.IsType<PSNotSupportedException>(ex.InnerException);
            }
        }
    }
}
