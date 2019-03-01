// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class PowerShellTests
    {
        [Fact]
        public static async System.Threading.Tasks.Task TestPowerShellInvokeAsync()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-Process")
                  .AddParameter("Id", Process.GetCurrentProcess().Id);

                var results = await ps.InvokeAsync();

                Assert.Single(results);
                Assert.IsType<Process>(results[0]?.BaseObject);
                Assert.Equal(Process.GetCurrentProcess().Id, ((Process)results[0].BaseObject).Id);
            }
        }

        [Fact]
        public static async System.Threading.Tasks.Task TestPowerShellInvokeAsyncWithInput()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-Command");

                var results = await ps.InvokeAsync(new PSDataCollection<string>(new[] { "Get-Command" }));

                Assert.Single(results);
                Assert.IsType<CmdletInfo>(results[0]?.BaseObject);
                Assert.Equal("Get-Command", ((CmdletInfo)results[0].BaseObject).Name);
            }
        }

        [Fact]
        public static async System.Threading.Tasks.Task TestPowerShellInvokeAsyncWithInputAndOutput()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-Command");

                var results = new PSDataCollection<CmdletInfo>();
                await ps.InvokeAsync(new PSDataCollection<string>(new[] { "Get-Command" }), results);

                Assert.Single(results);
                Assert.IsType<CmdletInfo>(results[0]);
                Assert.Equal("Get-Command", results[0].Name);
            }
        }

        [Fact]
        public static async System.Threading.Tasks.Task TestPowerShellInvokeAsyncWithErrorInScript()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-Process")
                  .AddParameter("InvalidParameterName", 42)
                  .AddParameter("ErrorAction", ActionPreference.Stop);

                var results = await ps.InvokeAsync();

            }
        }
    }
}
