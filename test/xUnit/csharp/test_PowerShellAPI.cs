// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Sequential
{
    // Not static because a test requires non-const variables
    public static class PowerShellHostingScenario
    {
        // Test that it does not throw an exception
        [Fact]
        public static void TestStartJobThrowTerminatingException()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Start-Job").AddParameter("ScriptBlock", ScriptBlock.Create("1+1"));
                var ex = Assert.Throws<CmdletInvocationException>(() => ps.Invoke());
                Assert.IsType<PSNotSupportedException>(ex.InnerException);
                Assert.Equal("IPCPwshExecutableNotFound,Microsoft.PowerShell.Commands.StartJobCommand", ex.ErrorRecord.FullyQualifiedErrorId);
            }
        }
    }
}
