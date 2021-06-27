// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Xunit;

namespace PSTests.Sequential
{
    // NOTE: do not call AddCommand("out-host") after invoking or MergeMyResults,
    // otherwise Invoke will not return any objects
    public class RunspaceTests
    {
        private static readonly int count = 1;
        private static readonly string script = string.Format($"get-command get-command");

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

        [SkippableFact]
        public void TestAppDomainProcessExitEvenHandlerNotLeaking()
        {
            // Skip this flaky test for now.
            Skip.IfNot(false);

            Skip.IfNot(Platform.IsWindows);

            EventHandler eventHandler;
            Delegate[] delegates;
            FieldInfo field = typeof(AppContext).GetField("ProcessExit", BindingFlags.NonPublic | BindingFlags.Static);

            // Open runspace and invoke script.
            using (var ps = PowerShell.Create())
            {
                ps.AddScript("1").Invoke();
                eventHandler = (EventHandler)field.GetValue(null);
                delegates = eventHandler.GetInvocationList();
                Assert.Contains(delegates, d => d.Method.Name == "CurrentDomain_ProcessExit");
            }

            // Handler registered by PowerShell should be unregistered.
            eventHandler = (EventHandler)field.GetValue(null);
            delegates = eventHandler.GetInvocationList();
            Assert.DoesNotContain(delegates, d => d.Method.Name == "CurrentDomain_ProcessExit");
        }
    }
}
