// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        [Fact]
        public void TestRunspaceOpenedEvent()
        {
            using Runspace runspace = RunspaceFactory.CreateRunspace();
            var order = new List<string>();
            int openThread = Environment.CurrentManagedThreadId;
            int handlerThread = -1;
            RunspaceState stateInHandler = RunspaceState.BeforeOpen;
            Debugger debuggerInHandler = null;

            runspace.StateChanged += (s, args) => order.Add(args.RunspaceStateInfo.State.ToString());
            EventHandler<RunspaceOpenedEventArgs> handler = (sender, e) =>
            {
                if (e.Runspace == runspace)
                {
                    handlerThread = Environment.CurrentManagedThreadId;
                    stateInHandler = e.Runspace.RunspaceStateInfo.State;
                    debuggerInHandler = e.Runspace.Debugger;
                    order.Add("RunspaceOpened");
                }
            };

            Runspace.RunspaceOpened += handler;
            try
            {
                runspace.Open();
            }
            finally
            {
                Runspace.RunspaceOpened -= handler;
            }

            Assert.Equal(openThread, handlerThread);
            Assert.Equal(RunspaceState.Opened, stateInHandler);
            Assert.NotNull(debuggerInHandler);
            Assert.Equal(new[] { "Opening", "RunspaceOpened", "Opened" }, order);
        }

        [Fact]
        public void TestRunspaceOpenedEventRaisedBeforeStartupScripts()
        {
            string startupScript = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
            File.WriteAllText(startupScript, "$global:FromStartupScript = 'set'");
            try
            {
                InitialSessionState iss = InitialSessionState.CreateDefault2();
                iss.StartupScripts.Add(startupScript);
                using Runspace runspace = RunspaceFactory.CreateRunspace(iss);

                bool handlerRan = false;
                object valueInHandler = "not checked";
                EventHandler<RunspaceOpenedEventArgs> handler = (sender, e) =>
                {
                    if (e.Runspace == runspace)
                    {
                        handlerRan = true;
                        valueInHandler = e.Runspace.SessionStateProxy.GetVariable("FromStartupScript");
                    }
                };

                Runspace.RunspaceOpened += handler;
                try
                {
                    runspace.Open();
                }
                finally
                {
                    Runspace.RunspaceOpened -= handler;
                }

                Assert.True(handlerRan);
                Assert.Null(valueInHandler);
                Assert.Equal("set", runspace.SessionStateProxy.GetVariable("FromStartupScript"));
            }
            finally
            {
                File.Delete(startupScript);
            }
        }

        [Fact]
        public void TestRunspaceOpenedEventForRunspacePool()
        {
            var opened = new ConcurrentDictionary<Guid, bool>();
            EventHandler<RunspaceOpenedEventArgs> handler = (sender, e) => opened[e.Runspace.InstanceId] = true;

            Runspace.RunspaceOpened += handler;
            try
            {
                using RunspacePool pool = RunspaceFactory.CreateRunspacePool(1, 1);
                pool.Open();

                using PowerShell ps = PowerShell.Create();
                ps.RunspacePool = pool;
                Guid poolRunspaceId = ps.AddScript("[runspace]::DefaultRunspace.InstanceId").Invoke<Guid>()[0];

                Assert.True(opened.ContainsKey(poolRunspaceId));
            }
            finally
            {
                Runspace.RunspaceOpened -= handler;
            }
        }

        [Fact]
        public void TestRunspaceOpenedEventHandlerFailureBreaksRunspace()
        {
            using Runspace runspace = RunspaceFactory.CreateRunspace();
            EventHandler<RunspaceOpenedEventArgs> handler = (sender, e) =>
            {
                if (e.Runspace == runspace)
                {
                    throw new InvalidOperationException("handler failure");
                }
            };

            Runspace.RunspaceOpened += handler;
            try
            {
                var exc = Assert.Throws<InvalidOperationException>(() => runspace.Open());
                Assert.Equal("handler failure", exc.Message);
                Assert.Equal(RunspaceState.Broken, runspace.RunspaceStateInfo.State);
            }
            finally
            {
                Runspace.RunspaceOpened -= handler;
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
