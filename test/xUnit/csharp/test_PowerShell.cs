// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PSTests.Parallel
{
    public static class PowerShellTests
    {
        [Fact]
        public static async Task TestPowerShellInvokeAsync()
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
        public static async Task TestPowerShellInvokeAsyncWithInput()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-Command");

                var results = await ps.InvokeAsync(new PSDataCollection<string>(new[] { "Get-Process" }));

                Assert.Single(results);
                Assert.IsType<CmdletInfo>(results[0]?.BaseObject);
                Assert.Equal("Get-Process", ((CmdletInfo)results[0].BaseObject).Name);
            }
        }

        [Fact]
        public static async Task TestPowerShellInvokeAsyncWithInputAndOutput()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-Command");

                var results = new PSDataCollection<CmdletInfo>();
                await ps.InvokeAsync(new PSDataCollection<string>(new[] { "Get-Process" }), results);

                Assert.Single(results);
                Assert.IsType<CmdletInfo>(results[0]);
                Assert.Equal("Get-Process", results[0].Name);
            }
        }

        [Fact]
        public static async Task TestPowerShellInvokeAsyncWithErrorInScript()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddScript(@"
try {
    Get-Process -Invalid 42 -ErrorAction Stop
} catch {
    throw
}
");
                var results = await ps.InvokeAsync();

                Assert.True(results.IsFaulted);
                Assert.IsType<AggregateException>(results.Exception);
                Assert.IsType<ParameterBindingException>(results.Exception.InnerException);
                Assert.Equal(results.Exception.InnerException.CommandInvocation.InvocationName, "Get-Process");
                Assert.Equal(results.Exception.InnerException.ParameterName, "Invalid");
                Assert.Equal(results.Exception.InnerException.ErrorId, "NamedParameterNotFound");
            }
        }

        [Fact]
        public static async Task TestPowerShellInvokeAsyncInUnopenedRunspace()
        {
            using (var rs = RunspaceFactory.CreateRunspace())
            using (var ps = PowerShell.Create(rs))
            {
                var results = await ps.AddScript(@"'This will not run'")
                                      .InvokeAsync();

                Assert.True(results.IsFaulted);
                Assert.IsType<AggregateException>(results.Exception);
                Assert.IsType<InvalidRunspaceStateException>(results.Exception.InnerException);
                Assert.Equal(results.Exception.InnerException.CurrentState, RunspaceState.BeforeOpen);
                Assert.Equal(results.Exception.InnerException.ExpectedState, RunspaceState.Opened);
            }
        }

        [Fact]
        public static async Task TestPowerShellInvokeAsyncInBusyRunspace()
        {
            using (var rs = RunspaceFactory.CreateRunspace())
            {
                rs.Open();
                using (var ps = PowerShell.Create(rs))
                {
                    await ps.AddScript(@"@(1..120).foreach{Start-Sleep -Milliseconds 500}")
                            .InvokeAsync();

                    int time = 0;
                    while (rs.RunspaceAvailability != RunspaceAvailability.Busy)
                    {
                        if ((time += 100) >= 120000)
                        {
                            break;
                        }
                        Thread.Sleep(100);
                    }

                    Assert.Equal(rs.RunspaceAvailability, RunspaceAvailability.Busy);

                    using (var ps2 = PowerShell.Create(rs))
                    {
                        var results = await ps2.AddScript(@"'This will not run'")
                                               .InvokeAsync();

                        Assert.True(results.IsFaulted);
                        Assert.IsType<AggregateException>(results.Exception);
                        Assert.IsType<PSInvalidOperationException>(results.Exception.InnerException);
                    }

                    ps.Stop(() => { }, null);
                }
            }
        }

        [Fact]
        public static async Task TestPowerShellInvokeAsyncMultiple()
        {
            var tasks = new List<Task>();

            using (var ps1 = PowerShell.Create())
            using (var ps2 = PowerShell.Create())
            {
                tasks.Add(await ps1.AddScript(@"@(1..5).foreach{Start-Sleep -Milliseconds 500; $_}")
                                   .InvokeAsync());
                tasks.Add(await ps2.AddScript(@"@(6..10).foreach{Start-Sleep -Milliseconds 500; $_}")
                                   .InvokeAsync());
            }

            foreach (var task in tasks)
            {
                Assert.Equal(task.Status, TaskStatus.RanToCompletion);
                Assert.True(task.IsCompletedSuccessfully);
            }

            var results = tasks.Select(x => x.Result).ToList<PSObject>();
            Assert.Equal(results.Count, 10);
        }

        // More testing with these tests, plus new tests in progress for next commit
    }
}
