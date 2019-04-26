// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Xunit;

namespace PSTests.Sequential
{
    // NOTE: do not call AddCommand("out-host") after invoking or MergeMyResults,
    // otherwise Invoke will not return any objects
    public class RunspaceTests
    {
        private static int count = 1;
        private static string script = string.Format($"get-command get-command");

        [Fact]
        public void TestRunspaceWithPipeline()
        {
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                using (var pipeline = runspace.CreatePipeline(script))
                {
                    int objCount = 0;
                    foreach (var result in pipeline.Invoke())
                    {
                        ++objCount;
                        Assert.NotNull(result);
                    }

                    Assert.Equal(count, objCount);
                }

                runspace.Close();
            }
        }

        [Fact]
        public void TestRunspaceWithPowerShell()
        {
            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.Runspace = runspace;

                    powerShell.AddScript(script);

                    int objCount = 0;
                    foreach (var result in powerShell.Invoke())
                    {
                        ++objCount;
                        Assert.NotNull(result);
                    }

                    Assert.Equal(count, objCount);
                }

                runspace.Close();
            }
        }

        [Fact]
        public void TestRunspaceWithPowerShellAndInitialSessionState()
        {
            // CreateDefault2 is intentional.
            InitialSessionState iss = InitialSessionState.CreateDefault();

            // NOTE: instantiate custom host myHost for the next line to capture stdout and stderr output
            //       in addition to just the PSObjects
            using (Runspace runspace = RunspaceFactory.CreateRunspace(/*myHost,*/iss))
            {
                runspace.Open();
                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.Runspace = runspace;
                    powerShell.AddScript("Import-Module Microsoft.PowerShell.Utility -Force");
                    powerShell.AddScript(script);

                    int objCount = 0;

                    var results = powerShell.Invoke();

                    foreach (var result in results)
                    {
                        // this is how an object would be captured here and looked at,
                        // each result is a PSObject with the data from the pipeline
                        ++objCount;
                        Assert.NotNull(result);
                    }

                    Assert.Equal(count, objCount);
                }

                runspace.Close();
            }
        }

        [Fact]
        public void TestRunspaceSetBreakpoints()
        {
            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                var expectedBreakpoints = new Breakpoint[] {
                    new LineBreakpoint(@"./path/to/some/file.ps1", 1),
                    new CommandBreakpoint(@"./path/to/some/file.ps1", new WildcardPattern("Write-Host"), "Write-Host"),
                };

                runspace.Open();

                try
                {
                    runspace.Debugger.SetBreakpoints(expectedBreakpoints);
                    List<Breakpoint> actualBreakpoints = runspace.Debugger.GetBreakpoints();
                    Assert.Equal(expectedBreakpoints.Length, actualBreakpoints.Count);
                    Assert.Equal(expectedBreakpoints, actualBreakpoints);
                }
                finally
                {
                    runspace.Close();
                }
            }
        }
    }
}
