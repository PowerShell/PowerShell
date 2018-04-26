// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;
using System;
using System.Management.Automation;

namespace PowerShell.Hosting.SDK.Tests
{
    public static class HostingTests
    {
        [Fact]
        public static void TestCommandFromUtility()
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript("Get-Verb -Verb get").Invoke();

                foreach (dynamic item in results)
                {
                    Assert.Equal("Get", item.Verb);
                }
            }
        }

        [Fact]
        public static void TestCommandFromManagement()
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var path = Environment.CurrentDirectory;
                var results = ps.AddCommand("Test-Path").AddParameter("Path", path).Invoke<bool>();

                foreach (dynamic item in results)
                {
                    Assert.True(item);
                }
            }
        }

        [Fact]
        public static void TestCommandFromCore()
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript(@"$i = 0 ; 1..3 | ForEach-Object { $i += $_} ; $i").Invoke<int>();

                foreach (dynamic item in results)
                {
                    Assert.Equal(6,item);
                }
            }
        }
    }
}
