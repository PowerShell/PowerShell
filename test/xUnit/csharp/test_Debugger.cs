// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Threading;
using Xunit;

namespace PSTests.Sequential
{
    public class RemotingTestFixture : IDisposable
    {
        public void Dispose()
        {
            // Restart the WinRM service before exiting test because when VSTS
            // unloads the app domain WinRM can throw an exception if it is not
            // finished cleaning up server side state.
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddCommand("Restart-Service")
                  .AddParameter("Name", "WinRM")
                  .Invoke();
            }
        }
    }

    [CollectionDefinition("Remoting test collection")]
    public class RemotingTestCollection: ICollectionFixture<RemotingTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class DebuggerTestsBase
    {
        protected static bool s_testSucceeded;
        protected static string s_errorMessage = string.Empty;
        protected static string s_errorId = string.Empty;
        protected static Collection<ErrorRecord> s_errorRecords = new Collection<ErrorRecord>();

        protected virtual Runspace InitializeRunspace()
        {
            var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            runspace.Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
            return runspace;
        }

        protected virtual Runspace InitializeRunspace(EventHandler<DebuggerStopEventArgs> debuggerStopHandler)
        {
            var runspace = InitializeRunspace();
            runspace.Debugger.DebuggerStop += debuggerStopHandler;
            return runspace;
        }

        protected static void CleanupRunspace(Runspace runspace)
        {
            if (runspace != null)
            {
                runspace.Close();
                runspace.Dispose();
            }
        }

        protected static void CleanupRunspace(Runspace runspace, EventHandler<DebuggerStopEventArgs> debuggerStopHandler)
        {
            if (runspace?.Debugger != null)
            {
                runspace.Debugger.DebuggerStop -= debuggerStopHandler;
            }
            CleanupRunspace(runspace);
        }

        protected static void RunScript(Runspace runspace, string script, bool exposeFlowControlExceptions = false)
        {
            using var ps = PowerShell.Create(runspace);

            ps.AddScript(script);

            if (exposeFlowControlExceptions)
            {
                var settings = new PSInvocationSettings();
                if (exposeFlowControlExceptions)
                {
                    settings.ExposeFlowControlExceptions = true;
                }
                ps.Invoke(null, settings);
            }
            else
            {
                ps.Invoke();
            }

            if (ps.Streams.Error.Count > 0)
            {
                s_testSucceeded = false;
                foreach (var item in ps.Streams.Error)
                {
                    s_errorRecords.Add(item);
                }
            }
        }

        protected static void ProcessScriptErrors(string errorId)
        {
            if (s_errorRecords.Count > 0)
            {
                s_testSucceeded = false;
                s_errorMessage = string.Format(
                    "FAIL: Test failed with script errors. First error message: {0}",
                    s_errorRecords[0].ErrorDetails.Message);
                s_errorId = errorId;
            }
        }

    }

    public class LocalDebuggerTests : DebuggerTestsBase
    {
        private static void DebuggerStopHandler_Quit(object sender, DebuggerStopEventArgs e)
        {
            // Signal debugger quit.
            e.ResumeAction = DebuggerResumeAction.Stop;
        }

        private static void DebuggerStopHandler_CaptureSuccessAndQuit(object sender, DebuggerStopEventArgs e)
        {
            // Debug job stop occurred. Success.
            s_errorMessage = string.Empty;
            s_errorId = string.Empty;
            s_testSucceeded = true;

            // Stop script running in debugger.
            e.ResumeAction = DebuggerResumeAction.Stop;
        }

        [Fact]
        public void TestDebuggerQuitException()
        {
            // Define the fail values
            s_testSucceeded = false;
            s_errorMessage = "Expected debugger terminate exception from debugger stop command.";
            s_errorId = "TestDebuggerBreak:NoDebuggerTerminateException";

            // Create local runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler_Quit);
            try
            {
                var script = @"1..10 | % { if ($_ -eq 5) { Wait-Debugger } }";
                RunScript(runspace, script, exposeFlowControlExceptions:true);
            }
            catch (TerminateException)
            {
                // Caught expected exception.
                s_testSucceeded = true;
                s_errorMessage = string.Empty;
                s_errorId = string.Empty;
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(e.Message))
                {
                    s_testSucceeded = false;
                    s_errorMessage = e.Message;
                    s_errorId = "TestInvokeError";
                }
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler_Quit);
                Assert.True(
                    s_testSucceeded,
                    string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
            }
        }

#if !UNIX
        [Fact]
        public void TestDebugJobIPC()
        {
            // Define the fail values
            s_testSucceeded = false;
            s_errorMessage = "Expected debugger stop to occur.";
            s_errorId = "TestDebugJobIPC:NoDebuggerStop";

            // Create local runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler_CaptureSuccessAndQuit);
            try
            {
                try
                {
                    // Create test script with some delay to wait for break action.
                    string script = @"
                        $ErrorActionPreference = 'Stop'

                        $job = Start-ThreadJob { 1..100 | ForEach-Object { Start-Sleep -Milliseconds 100; $_ } }
                        while (-not $job.HasMoreData -and $job.JobStateInfo.State -in @('NotStarted','Running'))
                        {
                            Start-Sleep -Milliseconds 100
                        }

                        Debug-Job -Id $job.Id -BreakAll 1>$null

                        Remove-Job -Id $job.Id -Force
                    ";
                    RunScript(runspace, script);
                }
                catch (Exception e)
                {
                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        s_testSucceeded = false;
                        s_errorMessage = e.Message;
                        s_errorId = "TestInvokeError";
                    }
                }

                ProcessScriptErrors("TestDebugJobIPC");
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler_CaptureSuccessAndQuit);
                Assert.True(
                    s_testSucceeded,
                    string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
            }
        }

        [Fact]
        public void TestDebugJobWinRM()
        {
            // Define the fail values
            s_testSucceeded = false;
            s_errorMessage = "Expected debugger stop to occur.";
            s_errorId = "TestDebugJobWinRM:NoDebuggerStop";

            // Create local runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler_CaptureSuccessAndQuit);
            try
            {
                try
                {
                    // Create test script with some delay to wait for break action.
                    string script = @"
                        $ErrorActionPreference = 'Stop'

                        $job = Invoke-Command -ComputerName localhost -ScriptBlock { 1..100 | ForEach-Object { Start-Sleep -Milliseconds 100; $_ } } -AsJob

                        # Wait for output to indicate job is running.
                        while (-not $job.HasMoreData -and $job.JobStateInfo.State -in @('NotStarted','Running'))
                        {
                            Start-Sleep -Milliseconds 100
                        }

                        Debug-Job -Id $job.Id -BreakAll
                    ";
                    RunScript(runspace, script);
                }
                catch (Exception e)
                {
                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        s_testSucceeded = false;
                        s_errorMessage = e.Message;
                        s_errorId = "TestInvokeError";
                    }
                }

                ProcessScriptErrors("TestDebugJobWinRM");
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler_CaptureSuccessAndQuit);
                Assert.True(
                    s_testSucceeded,
                    string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
            }
        }
#endif
    }

#if !UNIX
    [Collection("Remoting test collection")]
    public class RemoteDebuggerTests : DebuggerTestsBase, IDisposable
    {
        private static bool s_stopTest;
        private static bool s_eventReceived;
        private static int s_eventHandlerPass;
        private static DebuggerResumeAction s_resumeAction;
        // These next two variables are only used by the remote debugger disconnect test that is currently commented out
        //private static Runspace s_runspace;
        //private static RunspaceState s_previousRunspaceState;
        private static string s_scriptFilePath = Path.Combine(Path.GetTempPath(), "RemoteDebuggerTestScript.ps1");

        public RemoteDebuggerTests()
        {
            var scriptFile = @"
                $psName = 'PowerShell'
                $winRMName = 'WinRM'
                $svrHost = 'svcHost'
                $count = 10

                1..$count | foreach { ""TestOutput ${_}"" }

                Write-Output ""Script Complete for count: ${count}""
            ";

            // Create the script file.
            File.WriteAllText(s_scriptFilePath, scriptFile);
        }

        public void Dispose()
        {
            if (File.Exists(s_scriptFilePath))
            {
                File.Delete(s_scriptFilePath);
            }
        }

        protected override Runspace InitializeRunspace()
        {
            using PowerShell ps = PowerShell.Create();
            ps.AddScript("[System.Management.Automation.Runspaces.TypeTable]::LoadDefaultTypeFiles()");
            var results = ps.Invoke<TypeTable>();

            Assert.True(results?.Count == 1, "Remote Debugger test failed because PowerShell default types could not be loaded.");

            // Clear our s_eventReceived flag.
            s_eventReceived = false;

            // Get Default type table.
            TypeTable defaultTypes = results[0];

            // Create remote runspace
            var wsManConnectionInfo = new WSManConnectionInfo();
            var runspace = RunspaceFactory.CreateRunspace(
                wsManConnectionInfo,
                null,
                defaultTypes);
            runspace.Open();
            runspace.Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
            return runspace;
        }

        protected virtual Runspace InitializeRunspace(EventHandler<BreakpointUpdatedEventArgs> breakpointUpdatedHandler)
        {
            var runspace = InitializeRunspace();
            runspace.Debugger.BreakpointUpdated += breakpointUpdatedHandler;
            return runspace;
        }

        protected static void CleanupRunspace(Runspace runspace, EventHandler<BreakpointUpdatedEventArgs> breakpointUpdatedHandler)
        {
            if (runspace?.Debugger != null)
            {
                runspace.Debugger.BreakpointUpdated -= breakpointUpdatedHandler;
            }
            CleanupRunspace(runspace);
        }

        private bool TestCondition(bool condition, string msg, string errorId)
        {
            if (!condition)
            {
                s_testSucceeded = false;
                s_stopTest = true;
                s_errorMessage = msg;
                s_errorId = errorId;
            }

            return condition;
        }

        private void WaitForEvent()
        {
            int count = 0;
            while (!s_eventReceived && ++count <= 5)
            {
                Thread.Sleep(100);
            }
        }

        [Fact]
        public void TestRemoteDebuggerLineBreakpointUpdated()
        {
            void BreakpointUpdatedHandler(object sender, BreakpointUpdatedEventArgs e)
            {
                s_testSucceeded = true;
                s_errorMessage = string.Empty;
                s_errorId = string.Empty;

                var bp = e.Breakpoint as LineBreakpoint;
                if (!TestCondition(bp != null,
                    "Expected line breakpoint.", "TestLineBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Script.Equals(s_scriptFilePath, StringComparison.OrdinalIgnoreCase),
                    "LineBreakpoint contains incorrect script file.", "TestLineBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Line == 4,
                    "Expected line breakpoint line to be 4.", "TestLineBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Action.ToString().Equals("'Hello'"),
                    "Line breakpoint has incorrect action script.", "TestLineBreakpointUpdated"))
                {
                    return;
                }

                s_eventReceived = true;
            }

            s_testSucceeded = false;
            s_eventReceived = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(BreakpointUpdatedHandler);
            try
            {
                s_errorMessage = "Expected update breakpoint event to occur.";
                s_errorId = "TestLineBreakpointUpdated:NoDebuggerStop";

                RunScript(runspace, $@"Set-PSBreakpoint -Script '{s_scriptFilePath}' -Line 4 -Action {{'Hello'}}");

                // Wait for the breakpoint updated event to be handled
                WaitForEvent();
            }
            finally
            {
                CleanupRunspace(runspace, BreakpointUpdatedHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerVariableBreakpointUpdated()
        {
            void BreakpointUpdatedHandler(object sender, BreakpointUpdatedEventArgs e)
            {
                s_testSucceeded = true;
                s_errorMessage = string.Empty;
                s_errorId = string.Empty;

                var bp = e.Breakpoint as VariableBreakpoint;
                if (!TestCondition(bp != null,
                    "Expected variable breakpoint.", "TestVariableBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Script.Equals(s_scriptFilePath, StringComparison.OrdinalIgnoreCase),
                    "LineBreakpoint contains incorrect script file.", "TestVariableBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Variable.Equals("myVar", StringComparison.OrdinalIgnoreCase),
                    "Variable breakpoint has incorrect variable name.", "TestVariableBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Action.ToString().Equals("'Hello'"),
                    "Line breakpoint has incorrect action script.", "TestVariableBreakpointUpdated"))
                {
                    return;
                }

                s_eventReceived = true;
            }

            s_testSucceeded = false;
            s_eventReceived = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(BreakpointUpdatedHandler);
            try
            {
                s_errorMessage = "Expected update breakpoint event to occur.";
                s_errorId = "TestVariableBreakpointUpdated:NoDebuggerStop";

                RunScript(
                    runspace,
                    $@"Set-Variable -Name myVar -Value 102; Set-PSBreakpoint -Script '{s_scriptFilePath}' -Variable myVar -Action {{'Hello'}}");

                // Wait for the breakpoint updated event to be handled
                WaitForEvent();
            }
            finally
            {
                CleanupRunspace(runspace, BreakpointUpdatedHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerCommandBreakpointUpdated()
        {
            void BreakpointUpdatedHandler(object sender, BreakpointUpdatedEventArgs e)
            {
                s_testSucceeded = true;
                s_errorMessage = string.Empty;
                s_errorId = string.Empty;

                var bp = e.Breakpoint as CommandBreakpoint;
                if (!TestCondition(bp != null,
                    "Expected command breakpoint.", "TestCommandBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Script.Equals(s_scriptFilePath, StringComparison.OrdinalIgnoreCase),
                    "LineBreakpoint contains incorrect script file.", "TestCommandBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Command.Equals("Disconnect-PSSession", StringComparison.OrdinalIgnoreCase),
                    "Command breakpoint has incorrect command name.", "TestCommandBreakpointUpdated"))
                {
                    return;
                }

                if (!TestCondition(bp.Action.ToString().Equals("'Hello'"),
                    "Line breakpoint has incorrect action script.", "TestCommandBreakpointUpdated"))
                {
                    return;
                }

                s_eventReceived = true;
            }

            s_testSucceeded = false;
            s_eventReceived = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(BreakpointUpdatedHandler);
            try
            {
                s_errorMessage = "Expected update breakpoint event to occur.";
                s_errorId = "TestCommandBreakpointUpdated:NoDebuggerStop";

                RunScript(
                    runspace,
                    $@"Set-PSBreakpoint -Script '{s_scriptFilePath}' -Command Disconnect-PSSession -Action {{'Hello'}}");

                // Wait for the breakpoint updated event to be handled
                WaitForEvent();
            }
            finally
            {
                CleanupRunspace(runspace, BreakpointUpdatedHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteSetLineBreakpointOnStop()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;

                if (s_eventHandlerPass == 1)
                {
                    if (!TestCondition(e.Breakpoints.Count == 1,
                        "Expected line breakpoint in debugger stop event.", "TestSetLineBreakpointOnStop"))
                    {
                        return;
                    }

                    LineBreakpoint bp = e.Breakpoints[0] as LineBreakpoint;
                    if (!TestCondition(bp != null,
                        "Expected line breakpoint in debugger stop event.", "TestSetLineBreakpointOnStop"))
                    {
                        return;
                    }

                    TestCondition(bp.Line == 2,
                        "Expected line breakpoint == 2 in debugger stop event.", "TestSetLineBreakpointOnStop");

                    // Set breakpoint at line 4 and allow script to continue.
                    Debugger debugger = sender as Debugger;
                    if (!TestCondition(debugger != null,
                        "Sender is not of Debugger type.", "HandleSetLineBreakpointOnStop"))
                    {
                        return;
                    }

                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                    PSCommand cmd = new PSCommand();
                    cmd.AddCommand("Set-PSBreakpoint")
                       .AddParameter("Script", s_scriptFilePath)
                       .AddParameter("Line", 4);
                    DebuggerCommandResults results = debugger.ProcessCommand(
                        cmd,
                        output);

                    if (!TestCondition(results.ResumeAction == null,
                        "Debugger Help command should not return a Resume action.", "HandleSetLineBreakpointOnStop"))
                    {
                        return;
                    }
                    if (!TestCondition(results.EvaluatedByDebugger == false,
                        "Debugger Help command should not report evaluated by debugger", "HandleSetLineBreakpointOnStop"))
                    {
                        return;
                    }

                    s_eventHandlerPass = 2;
                }
                else if (s_eventHandlerPass == 2)
                {
                    s_testSucceeded = true;

                    if (!TestCondition(e.Breakpoints.Count == 1,
                        "Expected line breakpoint in debugger stop event.", "HandleSetLineBreakpointOnStop"))
                    {
                        return;
                    }

                    LineBreakpoint bp = e.Breakpoints[0] as LineBreakpoint;
                    if (!TestCondition(bp != null,
                        "Expected line breakpoint in debugger stop event.", "HandleSetLineBreakpointOnStop"))
                    {
                        return;
                    }

                    TestCondition(bp.Line == 4,
                        "Expected pass 2 line breakpoint == 4 in debugger stop event.", "HandleSetLineBreakpointOnStop");
                }
            }

            s_testSucceeded = false;
            s_eventHandlerPass = 1;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestSetLineBreakpointOnStop:NoDebuggerStop";

                string script = $@"
                    gbp | rbp;
                    Set-PSBreakpoint -Script '{s_scriptFilePath}' -Line 2;
                    & '{s_scriptFilePath}';
                    gbp | rbp
                ";
                RunScript(runspace, script);
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerLineBreakpointStop()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;
                s_testSucceeded = true;

                if (!TestCondition(e.Breakpoints.Count == 1,
                    "Expected line breakpoint in debugger stop event.", "TestLineBreakpointStop"))
                {
                    return;
                }

                LineBreakpoint bp = e.Breakpoints[0] as LineBreakpoint;
                if (!TestCondition(bp != null,
                    "Expected line breakpoint in debugger stop event.", "TestLineBreakpointStop"))
                {
                    return;
                }

                TestCondition(bp.Line == 4,
                    "Expected line breakpoint == 4 in debugger stop event.", "TestLineBreakpointStop");
            }


            s_testSucceeded = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestLineBreakpointStop:NoDebuggerStop";

                var script = $@"
                    gbp | rbp;
                    Set-PSBreakpoint -Script '{s_scriptFilePath}' -Line 4;
                    & '{s_scriptFilePath}';
                    gbp | rbp
                ";
                RunScript(runspace, script);
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerVariableBreakpointStop()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;
                s_testSucceeded = true;

                if (!TestCondition(e.Breakpoints.Count == 1,
                    "Expected variable breakpoint in debugger stop event.", "TestVariableBreakpointStop"))
                {
                    return;
                }

                VariableBreakpoint bp = e.Breakpoints[0] as VariableBreakpoint;
                if (!TestCondition(bp != null,
                    "Expected variable breakpoint in debugger stop event.", "TestVariableBreakpointStop"))
                {
                    return;
                }

                if (!TestCondition(bp.Variable.Equals("winRMName", StringComparison.OrdinalIgnoreCase),
                    "Expected variable breakpoint == winRMName in debugger stop event.", "TestVariableBreakpointStop"))
                {
                    return;
                }

                TestCondition(e.InvocationInfo.ScriptLineNumber == 3,
                    "Expected variable breakpoint line == 3 in debugger stop event.", "TestVariableBreakpointStop");
            }

            s_testSucceeded = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestVariableBreakpointStop:NoDebuggerStop";

                var script = $@"
                    gbp | rbp;
                    Set-PSBreakpoint -Script '{s_scriptFilePath}' -Variable winRMName;
                    & '{s_scriptFilePath}';
                    gbp | rbp
                ";
                RunScript(runspace, script);
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerCommandBreakpointStop()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;
                s_testSucceeded = true;

                if (!TestCondition(e.Breakpoints.Count == 1,
                    "Expected command breakpoint in debugger stop event.", "TestCommandBreakpointStop"))
                {
                    return;
                }

                CommandBreakpoint bp = e.Breakpoints[0] as CommandBreakpoint;
                if (!TestCondition(bp != null,
                    "Expected command breakpoint in debugger stop event.", "TestCommandBreakpointStop"))
                {
                    return;
                }

                if (!TestCondition(bp.Command.Equals("Write-Output", StringComparison.OrdinalIgnoreCase),
                    "Expected variable breakpoint == winRMName in debugger stop event.", "TestCommandBreakpointStop"))
                {
                    return;
                }

                TestCondition(e.InvocationInfo.ScriptLineNumber == 9,
                    "Expected command breakpoint line == 9 in debugger stop event.", "TestCommandBreakpointStop");
            }

            s_testSucceeded = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestCommandBreakpointStop:NoDebuggerStop";

                var script = $@"
                    gbp | rbp;
                    Set-PSBreakpoint -Script '{s_scriptFilePath}' -Command Write-Output
                    & '{s_scriptFilePath}';
                    gbp | rbp
                ";
                RunScript(runspace, script);
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerBreak()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;

                // Should have received a debugger breakpoint stop.
                s_testSucceeded = true;

                // Stop script running in debugger.
                e.ResumeAction = DebuggerResumeAction.Stop;
            }

            // Create test script with some delay to wait for break action.
            var script = @"
                $maxValue = 5
                $count = 0
                while ($count -lt $maxValue)
                {
                    ++$count
                    'Output'
                    sleep 1
                }";

            s_testSucceeded = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestDebuggerBreak:NoDebuggerStop";

                using (PowerShell ps = PowerShell.Create(runspace))
                {
                    // Output data collection.
                    bool dataAdded = false;
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    output.DataAdded += (sender, dataAddedArgs) =>
                    {
                        dataAdded = true;
                    };

                    // Invoke script
                    ps.AddScript(script);
                    var async = ps.BeginInvoke<object, PSObject>(null, output);

                    // Wait for output to begin.
                    int count = 0;
                    while ((count++ < 20) && !dataAdded)
                    {
                        Thread.Sleep(500);
                    }

                    if (dataAdded)
                    {
                        // Break into debugger.
                        runspace.Debugger.SetDebuggerStepMode(true);

                        // Wait for command to complete.
                        ps.EndInvoke(async);
                    }
                    else
                    {
                        ps.BeginStop(null, null);
                    }
                }
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebugJobIPC()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                s_errorMessage = string.Empty;
                s_errorId = string.Empty;

                // Debug job stop occurred.  Success.
                s_testSucceeded = true;

                // Stop script running in debugger.
                e.ResumeAction = DebuggerResumeAction.Stop;
            }

            s_testSucceeded = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestDebugJobIPC:NoDebuggerStop";

                // Invoke a test script with some delay to wait for break action.
                var script = @"
                    $ErrorActionPreference = 'Stop'

                    $job = Start-ThreadJob { 1..100 | ForEach-Object { Start-Sleep -Milliseconds 100; $_ } }
                    while (-not $job.HasMoreData -and $job.JobStateInfo.State -in @('NotStarted','Running'))
                    {
                        Start-Sleep -Milliseconds 100
                    }

                    Debug-Job -Id $job.Id -BreakAll 1>$null

                    Remove-Job -Id $job.Id -Force
                ";
                RunScript(runspace, script);

                // Check for errors.
                if (s_errorRecords.Count > 0)
                {
                    string msg = string.Format("FAIL: Test failed with script errors. First error message: {0}",
                        s_errorRecords[0].ErrorDetails.Message);
                    TestCondition(false, msg, "TestDebugJobIPC");
                }
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        [Fact]
        public void TestRemoteDebuggerProcessCommand()
        {
            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;
                s_testSucceeded = true;

                Debugger debugger = sender as Debugger;
                if (!TestCondition(debugger != null,
                    "Sender is not of Debugger type.", "TestDebuggerProcessCommand"))
                {
                    return;
                }

                // Test ProcessCommands
                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                // Help
                PSCommand cmd = new PSCommand();
                cmd.AddCommand("?");
                DebuggerCommandResults results = debugger.ProcessCommand(
                    cmd,
                    output);

                if (!TestCondition(results.ResumeAction == null,
                    "Debugger Help command should not return a Resume action.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(results.EvaluatedByDebugger == true,
                    "Debugger Help command should report evaluated by debugger", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(output.Count == 22,
                    "Debugger Help command should return 22 string items", "TestDebuggerProcessCommand"))
                {
                    return;
                }

                // Exit - Should result in a debugger "Continue" result.
                output.Clear();
                cmd.Clear();
                cmd.AddCommand("exit");
                results = debugger.ProcessCommand(
                    cmd,
                    output);
                if (!TestCondition(results.ResumeAction == DebuggerResumeAction.Continue,
                    "Debugger Exit command should return a Resume action of 'Continue'.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(results.EvaluatedByDebugger == true,
                    "Debugger exit command should report evaluated by debugger.", "TestDebuggerProcessCommand"))
                {
                    return;
                }

                // GetDebuggerStopArgs
                DebuggerStopEventArgs stopArgs = debugger.GetDebuggerStopArgs();
                if (!TestCondition(stopArgs != null,
                    "GetDebuggerStopArgs should not return null.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(stopArgs.ResumeAction == e.ResumeAction,
                    "GetDebuggerStopArgs should return same resume action as stop event.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(stopArgs.Breakpoints.Count == e.Breakpoints.Count,
                    "GetDebuggerStopArgs should return same breakpoints as stop event.", "TestDebuggerProcessCommand"))
                {
                    return;
                }

                // List
                output.Clear();
                cmd.Clear();
                cmd.AddCommand("list");
                results = debugger.ProcessCommand(
                    cmd,
                    output);
                if (!TestCondition(results.ResumeAction == null,
                    "Debugger List command should not return a Resume action.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(results.EvaluatedByDebugger == true,
                    "Debugger List command should report evaluated by debugger.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(output.Count >= 9,
                    "Debugger List command should return at least 9 items.", "TestDebuggerProcessCommand"))
                {
                    return;
                }

                // Evaluate PS script.
                output.Clear();
                cmd.Clear();
                cmd.AddScript("Set-Variable -Name myVar -Value 102; Get-Variable -Name myVar");
                results = debugger.ProcessCommand(
                        cmd,
                        output);
                if (!TestCondition(output.Count == 1,
                    "Debugger PS script should return 1 item.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(results.ResumeAction == null,
                    "Debugger PS script should return null resume action.", "TestDebuggerProcessCommand"))
                {
                    return;
                }
                if (!TestCondition(results.EvaluatedByDebugger == false,
                    "Debugger PS script should EvaluatedByDebugger == false.", "TestDebuggerProcessCommand"))
                {
                    return;
                }

                if (s_resumeAction == DebuggerResumeAction.Continue)
                {
                    // Single step
                    output.Clear();
                    cmd.Clear();
                    cmd.AddCommand("s");
                    results = debugger.ProcessCommand(
                        cmd,
                        output);
                    if (!TestCondition(results.ResumeAction != null,
                        "Debugger Single Step command should return a Resume action.", "TestDebuggerProcessCommand"))
                    {
                        return;
                    }
                    TestCondition(results.ResumeAction.Value == DebuggerResumeAction.StepInto,
                        "Debugger Single Step command should return a StepInto Resume action.", "TestDebuggerProcessCommand");
                    s_resumeAction = results.ResumeAction.Value;
                    e.ResumeAction = results.ResumeAction.Value;
                }
                else
                {
                    // Quit
                    output.Clear();
                    cmd.Clear();
                    cmd.AddCommand("quit");
                    results = debugger.ProcessCommand(
                        cmd,
                        output);
                    if (!TestCondition(results.ResumeAction != null,
                        "Debugger Quit command should return a Resume action.", "TestDebuggerProcessCommand"))
                    {
                        return;
                    }
                    TestCondition(results.ResumeAction.Value == DebuggerResumeAction.Stop,
                        "Debugger Quit command should return a Stop Resume action.", "TestDebuggerProcessCommand");
                    s_resumeAction = results.ResumeAction.Value;
                    e.ResumeAction = results.ResumeAction.Value;
                }
            }

            s_testSucceeded = false;
            s_resumeAction = DebuggerResumeAction.Continue;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestDebuggerProcessCommand:NoDebuggerStop";

                // Run script to break at first sequence point.
                var script = $@"
                    gbp | rbp;
                    Set-PSBreakpoint -Script '{s_scriptFilePath}' -Line 1;
                    & '{s_scriptFilePath}'
                ";
                RunScript(runspace, script);
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

        //
        // Disconnect/reconnect test.
        // Currently disabled because of intermittent WinRM error during connect on slow VMs.
        //
        /*
        [Fact]
        public void TestRemoteDebuggerDisconnect()
        {
            void StateChangedHandler(object sender, RunspaceStateEventArgs e)
            {
                if (s_previousRunspaceState == RunspaceState.Connecting &&
                    e.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    // Release PowerShell command from debug stop mode.
                    if (s_runspace.Debugger.GetDebuggerStopArgs() != null)
                    {
                        s_runspace.Debugger.SetDebuggerAction(DebuggerResumeAction.Continue);
                    }
                }

                s_previousRunspaceState = e.RunspaceStateInfo.State;
            }

            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_resumeAction == DebuggerResumeAction.Stop)
                {
                    // Disconnect runspace.
                    s_runspace.Disconnect();
                    s_resumeAction = DebuggerResumeAction.Continue;
                    return;
                }
            }

            s_testSucceeded = false;
            s_resumeAction = DebuggerResumeAction.Stop;

            // Set up the runspace
            s_runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                // Set up callback
                s_runspace.StateChanged += StateChangedHandler;

                // Run script.
                using (PowerShell ps = PowerShell.Create(s_runspace))
                {
                    // Run script to break at first sequence point.
                    var script = $@"
                        gbp | rbp;
                        Set-PSBreakpoint -Script '{s_scriptFilePath}' -Line 1;
                        & '{s_scriptFilePath}'
                    ";
                    ps.AddScript(script);
                    try
                    {
                        ps.Invoke();
                    }
                    catch (RuntimeException)
                    { }
                    if (ps.InvocationStateInfo.State == PSInvocationState.Disconnected)
                    {
                        // Reconnect PowerShell command in debug stop mode.
                        IAsyncResult async = ps.ConnectAsync();

                        ps.EndInvoke(async);
                        s_testSucceeded = (ps.InvocationStateInfo.State == PSInvocationState.Completed);
                    }
                }
            }
            finally
            {
                s_runspace.StateChanged -= StateChangedHandler;
                CleanupRunspace(s_runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }
        */

        [Fact]
        public void TestRemoteDebuggerProcessCommandStop()
        {
            void StopDebuggerCommandProc(object state)
            {
                Debugger debugger = state as Debugger;
                Thread.Sleep(3000);
                debugger.StopProcessCommand();
            }

            void DebuggerStopHandler(object sender, DebuggerStopEventArgs e)
            {
                if (s_stopTest)
                {
                    return;
                }

                s_errorMessage = string.Empty;
                s_errorId = string.Empty;
                s_testSucceeded = true;

                Debugger debugger = sender as Debugger;
                if (!TestCondition(debugger != null,
                    "Sender is not of Debugger type.", "TestProcessCommandStop"))
                {
                    return;
                }

                // Start long running command for debugger to run.
                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                PSCommand cmd = new PSCommand();
                cmd.AddScript(@"1..1000 | % {sleep 1; ""Output $_""}");

                // Start worker thread to stop running debugger command.
                ThreadPool.QueueUserWorkItem(
                    StopDebuggerCommandProc, debugger);

                // Start command
                DebuggerCommandResults results = debugger.ProcessCommand(cmd, output);
                if (!TestCondition(results.ResumeAction == null,
                        "Debugger script evaluation should return null resume action", "TestProcessCommandStop"))
                {
                    return;
                }
                if (!TestCondition(results.EvaluatedByDebugger == false,
                    "Debugger script evaluation should return debugger evaluated == false.", "TestProcessCommandStop"))
                {
                    return;
                }

                // Wait for debugger stop command to take effect before continuing.
                Thread.Sleep(5000);
            }

            s_testSucceeded = false;

            // Set up the runspace
            using var runspace = InitializeRunspace(DebuggerStopHandler);
            try
            {
                s_errorMessage = "Expected debugger stop to occur.";
                s_errorId = "TestProcessCommandStop:NoDebuggerStop";

                // Run script to break at first sequence point.
                var script = $@"
                    gbp | rbp;
                    Set-PSBreakpoint -Script '{s_scriptFilePath}' -Line 1;
                    & '{s_scriptFilePath}'
                ";
                RunScript(runspace, script);
            }
            finally
            {
                CleanupRunspace(runspace, DebuggerStopHandler);
            }

            Assert.True(
                s_testSucceeded,
                string.Format("FAIL {0}:{1}", s_errorMessage, s_errorId));
        }

    }
#endif

    public class RunspaceDebuggingTestsBase
    {
        protected static bool s_debuggerStop;

        /// <summary>
        /// Runs the Debug-Runspace cmdlet for the given Runspace name.
        /// </summary>
        protected void DebugRunspace(string debugRunspaceName)
        {
            string scriptDebugRunspace = @"
                param ([string] $runspaceName)
                Debug-Runspace -Name $runspaceName -BreakAll
            ";

            using Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            using PowerShell ps = PowerShell.Create(rs);

            ps.AddScript(scriptDebugRunspace)
              .AddParameter("runspaceName", debugRunspaceName);

            // Run Debug-Runspace command.
            var async = ps.BeginInvoke();

            // Wait for debug stop to occur.
            WaitFor(
                () => { return s_debuggerStop; },
                30000,
                1000);

            // Signal Debug-Runspace to end.
            rs.Debugger.RaiseNestedDebuggingCancelEvent();

            // Let command complete
            ps.EndInvoke(async);
        }

        protected void DebugRunspace(string debugRunspaceName, out Runspace runspace, out PowerShell powershell, out IAsyncResult async)
        {
            string scriptDebugRunspace = @"
                param ([string] $runspaceName)
                Debug-Runspace -Name $runspaceName
            ";

            runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            powershell = PowerShell.Create(runspace);

            async = powershell.AddScript(scriptDebugRunspace)
                              .AddParameter("runspaceName", debugRunspaceName)
                              .BeginInvoke();

            if (!WaitFor(
                    () => { return s_debuggerStop; },
                    30000,
                    1000))
            {
                runspace.Dispose();
                runspace = null;
                powershell.Dispose();
                powershell = null;
                async = null;
            }
        }

        /// <summary>
        /// Waits for condition as defined by the predicate function.
        /// </summary>
        protected static bool WaitFor(
            Func<bool> predicate,
            int timeoutMs,
            int timeoutIncMs)
        {
            int totalTimeMs = 0;

            while (!predicate() && (totalTimeMs < timeoutMs))
            {
                System.Threading.Thread.Sleep(timeoutIncMs);
                totalTimeMs += timeoutIncMs;
            }

            return predicate();
        }

        protected void TestDebugOptionOnRunspace(Runspace runspace, string testName)
        {
            string script = @"
            ""Hello""
        ";
            string scriptBreakAll = @"
            param ([string] $RunspaceName)
            Enable-RunspaceDebug -RunspaceName $RunspaceName -BreakAll
        ";
            string scriptNoBreakAll = @"
            param ([string] $RunspaceName)
            Disable-RunspaceDebug -RunspaceName $RunspaceName
        ";
            bool debuggerStop = false;

            runspace.Debugger.DebuggerStop += (sender, args) =>
            {
                debuggerStop = true;
            };

            using (Runspace localRunspace = RunspaceFactory.CreateRunspace())
            {
                localRunspace.Open();

                // Set BreakAll option on Runspace
                using (PowerShell ps = PowerShell.Create(localRunspace))
                {
                    ps.AddScript(scriptBreakAll)
                      .AddParameter("RunspaceName", runspace.Name)
                      .Invoke();
                }

                // Run script with BreakAll option.
                debuggerStop = false;
                using (PowerShell ps = PowerShell.Create(runspace))
                {
                    ps.AddScript(script).Invoke();
                }
                Assert.True(debuggerStop,
                    string.Format("FAIL: {0}: Expected breakpoint hit in debugger.", testName));

                // Un-set BreakAll option on Runspace
                using (PowerShell ps = PowerShell.Create(localRunspace))
                {
                    ps.AddScript(scriptNoBreakAll)
                      .AddParameter("RunspaceName", runspace.Name)
                      .Invoke();
                }

                // Run script with all options off.
                debuggerStop = false;
                using (PowerShell ps = PowerShell.Create(runspace))
                {
                    ps.AddScript(script).Invoke();
                }
                Assert.False(debuggerStop,
                    string.Format("FAIL: {0}: Expected no breakpoint hit in debugger.", testName));
            }
        }
    }

    public class RunspaceDebuggingTests : RunspaceDebuggingTestsBase
    {
        [Fact]
        public void TestMonitorRunspaceInfo()
        {
            Exception ex = null;

            // Null argument.
            try
            {
                new PSStandaloneMonitorRunspaceInfo(null);
            }
            catch (PSArgumentNullException e)
            {
                ex = e;
            }

            Assert.True(ex != null,
                "FAIL: TestMonitorRunspaceInfo: Expected null argument exception for null runspace argument.");

            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                // Standalone runspace.
                PSStandaloneMonitorRunspaceInfo staaRunspaceInfo = new PSStandaloneMonitorRunspaceInfo(runspace);

                Assert.True(staaRunspaceInfo.Runspace.InstanceId.Equals(runspace.InstanceId),
                    "FAIL: TestMonitorRunspaceInfo: Unexpected PSStandaloneMonitorRunspaceInfo.Runspace property.");

                Assert.True(staaRunspaceInfo.RunspaceType == PSMonitorRunspaceType.Standalone,
                    "FAIL: TestMonitorRunspaceInfo: Unexpected PSStandaloneMonitorRunspaceInfo.RunspaceType property.");

                Assert.True(staaRunspaceInfo.NestedDebugger == null,
                    "FAIL: TestMonitorRunspaceInfo: Expected PSStandaloneMonitorRunspaceInfo.NestedDebugger property to be null.");

                // Embedded runspace.
                using (var ps = PowerShell.Create())
                {
                    var parentDebuggerId = Guid.NewGuid();
                    PSMonitorRunspaceType runspaceType = PSMonitorRunspaceType.InvokeCommand;

                    PSEmbeddedMonitorRunspaceInfo embRunspaceInfo = new PSEmbeddedMonitorRunspaceInfo(
                        runspace,
                        runspaceType,
                        ps,
                        parentDebuggerId);

                    Assert.True(embRunspaceInfo.Runspace.InstanceId.Equals(runspace.InstanceId),
                    "FAIL: TestMonitorRunspaceInfo: Unexpected PSEmbeddedMonitorRunspaceInfo.Runspace property.");

                    Assert.True(embRunspaceInfo.ParentDebuggerId.Equals(parentDebuggerId),
                        "FAIL: TestMonitorRunspaceInfo: Unexpected PSEmbeddedMonitorRunspaceInfo.AssoicationId property.");

                    Assert.True(embRunspaceInfo.RunspaceType == runspaceType,
                        "FAIL: TestMonitorRunspaceInfo: Unexpected PSEmbeddedMonitorRunspaceInfo.RunspaceType property.");

                    Assert.True(embRunspaceInfo.Command.InstanceId.Equals(ps.InstanceId),
                        "FAIL: TestMonitorRunspaceInfo: Unexpected PSEmbeddedMonitorRunspaceInfo.Command property.");

                    Assert.True(embRunspaceInfo.NestedDebugger == null,
                        "FAIL: TestMonitorRunspaceInfo: Expected PSEmbeddedMonitorRunspaceInfo.NestedDebugger property to be null.");
                }
            }
        }

        [Fact]
        public void TestMonitorRunspaceInfoAPIs()
        {
            // Create a local runspace
            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                Guid associationId = Guid.NewGuid();
                PSStandaloneMonitorRunspaceInfo runspaceInfo = new PSStandaloneMonitorRunspaceInfo(runspace);
                Exception ex = null;

                //
                // Null arguments.
                //

                try
                {
                    DebuggerUtils.StartMonitoringRunspace(
                        null, runspaceInfo);
                }
                catch (PSArgumentNullException e)
                {
                    ex = e;
                }
                Assert.True(ex != null,
                    "FAIL: TestMonitorRunspaceInfoAPIs: Expected StartMonitoringRunspace debugger null argument exception.");

                ex = null;
                try
                {
                    DebuggerUtils.StartMonitoringRunspace(
                        runspace.Debugger, null);
                }
                catch (PSArgumentNullException e)
                {
                    ex = e;
                }
                Assert.True(ex != null,
                    "FAIL: TestMonitorRunspaceInfoAPIs: Expected StartMonitoringRunspace runspaceInfo null argument exception.");

                ex = null;
                try
                {
                    DebuggerUtils.EndMonitoringRunspace(
                        null, runspaceInfo);
                }
                catch (PSArgumentNullException e)
                {
                    ex = e;
                }
                Assert.True(ex != null,
                    "FAIL: TestMonitorRunspaceInfoAPIs: Expected EndMonitoringRunspace debugger null argument exception.");

                ex = null;
                try
                {
                    DebuggerUtils.EndMonitoringRunspace(
                        runspace.Debugger, null);
                }
                catch (PSArgumentNullException e)
                {
                    ex = e;
                }
                Assert.True(ex != null,
                    "FAIL: TestMonitorRunspaceInfoAPIs: Expected EndMonitoringRunspace runspaceInfo null argument exception.");
            } // End using Runspace
        }

        [Fact]
        public void TestNestedDebuggerAPIs()
        {
            using (var runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                //
                // Null arguments.
                //

                Exception ex = null;
                try
                {
                    new StandaloneRunspaceDebugger(
                        null);
                }
                catch (PSArgumentNullException e)
                {
                    ex = e;
                }
                Assert.True(ex != null,
                    "FAIL: TestNestedDebuggerAPIs: Expected StandaloneRunspaceDebugger runspace null argument exception.");

                ex = null;
                try
                {
                    new EmbeddedRunspaceDebugger(
                        runspace,
                        null,
                        null,
                        PSMonitorRunspaceType.InvokeCommand,
                        Guid.NewGuid());
                }
                catch (PSArgumentNullException e)
                {
                    ex = e;
                }
                Assert.True(ex != null,
                    "FAIL: TestNestedDebuggerAPIs: Expected EmbeddedRunspaceDebugger rootDebugger null argument exception.");

                //
                // Valid construction.
                //

                var nestedRunspaceDebugger = new StandaloneRunspaceDebugger(
                    runspace);

                Assert.True((nestedRunspaceDebugger.DebugMode & runspace.Debugger.DebugMode) == runspace.Debugger.DebugMode,
                    "FAIL: TestNestedDebuggerAPIs: Unexpected StandaloneRunspaceDebugger debug mode.");

                Assert.True(nestedRunspaceDebugger.ParentDebuggerId.Equals(Guid.Empty),
                    "FAIL: TestNestedDebuggerAPIs: Unexpected StandaloneRunspaceDebugger ParentDebuggerId");

                Assert.True(nestedRunspaceDebugger.RunspaceType == PSMonitorRunspaceType.Standalone,
                    "FAIL: TestNestedDebuggerAPIs: Unexpected StandaloneRunspaceDebugger runspace type.");
            }
        }

        [Fact]
        public void TestNestedStandaloneDebugger()
        {
            // Nested script runs for 30 seconds.
            string nestedScript = @"
                1..30 | % { sleep 1; ""Output $_"" }
            ";

            // Create and open the root runspace.
            using (var rootRunspace = RunspaceFactory.CreateRunspace())
            {
                rootRunspace.Open();

                bool standaloneBreakAllReceived = false;

                rootRunspace.Debugger.DebuggerStop += (sender, args) =>
                {
                    // Detect nested debugger break all event.
                    standaloneBreakAllReceived = true;

                    // Stop script.
                    args.ResumeAction = DebuggerResumeAction.Stop;
                };

                // Create the nested runspace.
                using (var nestedRunspace = RunspaceFactory.CreateRunspace())
                {
                    nestedRunspace.Open();
                    var monitorRunspaceInfo = new PSStandaloneMonitorRunspaceInfo(
                        nestedRunspace);

                    try
                    {
                        // Start root debugger monitoring of nested runspace.
                        DebuggerUtils.StartMonitoringRunspace(rootRunspace.Debugger, monitorRunspaceInfo);

                        // Run script and do "break all".
                        using (var ps = PowerShell.Create(nestedRunspace))
                        {
                            var async = ps.AddScript(nestedScript).BeginInvoke();

                            Thread.Sleep(1000);

                            nestedRunspace.Debugger.SetDebuggerStepMode(true);

                            ps.EndInvoke(async);
                        }
                    }
                    catch (Exception e)
                    {
                        string msg = string.Format("FAIL: TestNestedStandaloneDebugger: Unexpected Exception thrown: {0}",
                            (!string.IsNullOrEmpty(e.Message)) ? e.Message : string.Empty);

                        Assert.True(false, msg);
                    }
                    finally
                    {
                        // End root debugger monitoring of nested runspace.
                        DebuggerUtils.EndMonitoringRunspace(rootRunspace.Debugger, monitorRunspaceInfo);
                    }
                }

                Assert.True(standaloneBreakAllReceived,
                    "FAIL: TestNestedStandaloneDebugger: Expected nested debugger break all event to be received.");

                Assert.False(rootRunspace.Debugger.IsPushed,
                    "FAIL: TestNestedEmbeddedDebugger: Nested debugger should no longer be pushed on the root debugger.");
            }
        }

        [Fact]
        public void TestNestedEmbeddedDebugger()
        {
            string script = @"
                param ($rootDebugger)
                $rs = [runspacefactory]::CreateRunspace()
                $rs.Open()
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $monitorInfo = new-object System.Management.Automation.Internal.PSEmbeddedMonitorRunspaceInfo(
                    $rs, [System.Management.Automation.Internal.PSMonitorRunspaceType]::InvokeCommand, 
                    $ps, [Guid]::Empty)
                $ps.AddScript('1..30 | % { sleep 1; ""Output $_"" }')

                Write-Output $rs.Debugger
                Write-Output $monitorInfo

                try
                {
                    [System.Management.Automation.Internal.DebuggerUtils]::StartMonitoringRunspace($rootDebugger, $monitorInfo)

                    $async = $ps.BeginInvoke()
                    sleep 1
                    $rs.Debugger.SetDebuggerStepMode($true)
                    $ps.EndInvoke($async)
                }
                finally
                {
                    [System.Management.Automation.Internal.DebuggerUtils]::EndMonitoringRunspace($rootDebugger, $monitorInfo)
                }
            ";

            // Create and open the root runspace.
            using (var rootRunspace = RunspaceFactory.CreateRunspace())
            {
                rootRunspace.Open();

                bool embeddedBreakAllReceived = false;

                rootRunspace.Debugger.DebuggerStop += (sender, args) =>
                {
                    // Detect nested debugger break all event.
                    Debugger debugger = sender as Debugger;
                    embeddedBreakAllReceived = debugger.IsPushed &&
                                                (args.InvocationInfo.ScriptLineNumber == 1);

                    // Stop script.
                    args.ResumeAction = DebuggerResumeAction.Stop;
                };

                // Run script.
                using (var ps = PowerShell.Create(rootRunspace))
                {
                    ps.AddScript(script)
                      .AddParameter("rootDebugger", rootRunspace.Debugger)
                      .Invoke();
                }

                Assert.True(embeddedBreakAllReceived,
                    "FAIL: TestNestedEmbeddedDebugger: Expected nested debugger break all event to be received.");

                Assert.False(rootRunspace.Debugger.IsPushed,
                    "FAIL: TestNestedEmbeddedDebugger: Nested debugger should no longer be pushed on the root debugger.");
            }
        }

        /// <summary>
        /// Tests that a debug break stop is preserved in a runspace, blocking
        /// script execution if UnhandledBreakpointMode is set to Wait in the
        /// debugger, and the break event can be handled once a handler is added
        /// and the debug stop wait is released with ReleaseSavedDebugStop().
        /// </summary>
        [Fact]
        public void TestPreserveDebugStopOnRunspace()
        {
            // One statement script.
            string script = @"
                Write-Output ""Hello from script.""
            ";

            bool debugStopReceived = false;

            // Create local runspace
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                // Set debugger to step mode.
                runspace.Debugger.SetDebuggerStepMode(true);

                // Set debugger to preserve debug stops.
                runspace.Debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Wait;

                // Run script.
                using (PowerShell ps = PowerShell.Create(runspace))
                {
                    // Start script.  Script will halt in debugger.
                    var async = ps.AddScript(script).BeginInvoke();

                    // Wait for debugger halt, for up to 30 seconds.
                    if (!WaitFor(
                        () => { return runspace.Debugger.IsPendingDebugStopEvent; },
                        30000,
                        1000))
                    {
                        Assert.True(false,
                            "FAIL: TestPreserveDebugStopOnRunspace: Expected runspace debugger stop.");
                    }

                    // Subscribe to debug stop event.
                    runspace.Debugger.DebuggerStop += (sender, args) =>
                    {
                        debugStopReceived = true;
                    };

                    // Release the debug stop wait.
                    runspace.Debugger.ReleaseSavedDebugStop();

                    ps.EndInvoke(async);
                }

                // Verify that the saved debug stop event was handled.
                Assert.True(debugStopReceived,
                    "FAIL: TestPreserveDebugStopOnRunspace: Expected debugger break event to be received.");
            }
        }

        /// <summary>
        /// Tests that a debug stop event is preserved in a local runspace when 
        /// UnhandledBreakpointMode property is set to wait, and that a root debugger
        /// handles the saved debug stop event once the local runspace is passed to the 
        /// root debugger for monitoring.
        /// </summary>
        [Fact]
        public void TestPreserveDebugStopOnNestedRunspace()
        {
            // One statement script.
            string script = @"
            Write-Output ""Hello from script.""
        ";

            bool debugStopReceivedInNestedRunspace = false;

            // Create root local runspace
            using (Runspace rootRunspace = RunspaceFactory.CreateRunspace())
            {
                rootRunspace.Open();

                rootRunspace.Debugger.DebuggerStop += (sender, args) =>
                {
                    debugStopReceivedInNestedRunspace = true;
                };

                // Create independent local runspace.
                using (Runspace runspace = RunspaceFactory.CreateRunspace())
                {
                    runspace.Open();

                    // Set debugger to step mode.
                    runspace.Debugger.SetDebuggerStepMode(true);

                    // Set debugger to preserve debug stops.
                    runspace.Debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Wait;

                    var monitorRunspaceInfo = new PSStandaloneMonitorRunspaceInfo(runspace);

                    using (PowerShell ps = PowerShell.Create(runspace))
                    {
                        // Run script.
                        var async = ps.AddScript(script).BeginInvoke();

                        // Wait for local runspace debugger to hit breakpoint.
                        if (!WaitFor(
                                () => { return runspace.Debugger.IsPendingDebugStopEvent; },
                                30000,
                                1000))
                        {
                            Assert.True(false,
                                "FAIL: TestPreserveDebugStopOnNestedRunspace: Expected runspace debugger stop.");
                        }

                        // Add local runspace to root debugger monitor, making
                        // it a nested runspace.
                        DebuggerUtils.StartMonitoringRunspace(rootRunspace.Debugger, monitorRunspaceInfo);

                        // Wait for script to complete, after debug stop event is handled.
                        ps.EndInvoke(async);

                        DebuggerUtils.EndMonitoringRunspace(rootRunspace.Debugger, monitorRunspaceInfo);
                    }
                }
            }

            // Verify that the saved debug stop event was handled.
            Assert.True(debugStopReceivedInNestedRunspace,
                "FAIL: TestPreserveDebugStopOnNestedRunspace: Expected debugger break event from nested runspace to be received.");
        }

        /// <summary>
        /// Tests the Debug-Runspace cmdlet for local runspaces.
        /// </summary>
        [Fact]
        public void TestDebugRunspaceOnLocalRunspace()
        {
            s_debuggerStop = false;
            bool dataAdded = false;
            string debugRunspaceName = "TestDebugRunspaceOnLocalRunspaceName";
            string script = @"
            1..60 | % { sleep 1; ""Output $_"" }
        ";

            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Name = debugRunspaceName;
                rs.Open();
                rs.Debugger.DebuggerStop += (sender, args) =>
                {
                    s_debuggerStop = true;
                    args.ResumeAction = DebuggerResumeAction.Stop;
                };

                using (PowerShell ps = PowerShell.Create(rs))
                {
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    output.DataAdded += (sender, args) =>
                    {
                        dataAdded = true;
                    };

                    var psAsync = ps.AddScript(script).BeginInvoke<object, PSObject>(null, output);

                    // Wait for command to run.
                    if (!WaitFor(() => { return dataAdded; },
                            30000, 1000))
                    {
                        Assert.True(false,
                            "FAIL: TestDebugRunspaceOnLocalRunspace: Expected script to run and output data.");
                    }

                    // Run the Debug-Runspace command in a new runspace.
                    DebugRunspace(debugRunspaceName);

                    ps.EndInvoke(psAsync);
                }
            }

            Assert.True(s_debuggerStop,
                "FAIL: TestDebugRunspaceOnLocalRunspace: Expected Debug-Runspace to break into debugger.");
        }

        /// <summary>
        /// Tests the Debug-Runspace cmdlet on a local Runspace for multiple scripts.
        /// </summary>
        [Fact]
        public void TestDebugRunspaceWithMultipleScriptsOnLocalRunspace()
        {
            bool dataAdded = false;
            string debugRunspaceName = "TestDebugRunspaceWithMultipleScriptsOnLocalRunspaceName";
            string script = @"
            1..60 | % { sleep 1; ""Output $_"" }
        ";

            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Name = debugRunspaceName;
                rs.Open();
                rs.Debugger.DebuggerStop += (sender, args) =>
                {
                    s_debuggerStop = true;
                    args.ResumeAction = DebuggerResumeAction.Stop;
                };

                // Execute first script run.
                s_debuggerStop = false;
                Runspace dbgRunspace = null;
                PowerShell dbgPowerShell = null;
                IAsyncResult async = null;
                using (PowerShell ps = PowerShell.Create(rs))
                {
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    output.DataAdded += (sender, args) =>
                    {
                        dataAdded = true;
                    };

                    dataAdded = false;
                    var psAsync = ps.AddScript(script).BeginInvoke<object, PSObject>(null, output);

                    // Wait for command to run.
                    if (!WaitFor(() => { return dataAdded; },
                            30000, 1000))
                    {
                        Assert.True(false,
                            "FAIL: TestDebugRunspaceWithMultipleScriptsOnLocalRunspace: Expected script to run and output data.");
                    }

                    // Run the Debug-Runspace command in a new runspace.
                    DebugRunspace(debugRunspaceName, out dbgRunspace, out dbgPowerShell, out async);
                    if (dbgRunspace == null || dbgPowerShell == null || async == null)
                    {
                        Assert.True(false,
                            "FAIL: TestDebugRunspaceWithMultipleScriptsOnLocalRunspace: Debug-Runspace did not run correctly, no debugger stop occurred.");
                    }

                    ps.EndInvoke(psAsync);
                }

                // Verify that this first script run stopped in the debugger.
                Assert.True(s_debuggerStop,
                "FAIL: TestDebugRunspaceWithMultipleScriptsOnLocalRunspace: Expected Debug-Runspace to break into debugger for first run script.");

                // Execute second script run.
                s_debuggerStop = false;
                dbgRunspace.Debugger.SetDebuggerStepMode(true);
                using (PowerShell ps = PowerShell.Create(rs))
                {
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                    var psAsync = ps.AddScript(script).BeginInvoke<object, PSObject>(null, output);
                    ps.EndInvoke(psAsync);
                }

                // Verify that this second script run stopped in the debugger.
                Assert.True(s_debuggerStop,
                "FAIL: TestDebugRunspaceWithMultipleScriptsOnLocalRunspace: Expected Debug-Runspace to break into debugger for second run script.");

                // Allow Debug-Runspace command to complete.
                dbgRunspace.Debugger.RaiseNestedDebuggingCancelEvent();
                dbgPowerShell.EndInvoke(async);

                // Clean up.
                dbgPowerShell.Dispose();
                dbgRunspace.Dispose();
            }
        }

        /// <summary>
        /// This tests the Debugger 'Exit' command that cancels debugging
        /// by detaching the debugger and allowing the job to continue running.
        /// </summary>
        [Fact]
        public void TestDebugJobIPCCancelEvent()
        {
            bool debuggerStop = false;
            string script = @"
                $ErrorActionPreference = 'Stop'

                $job = Start-ThreadJob { 1..10 | ForEach-Object { Start-Sleep -Seconds 1; $_ } }
                while (-not $job.HasMoreData -and $job.JobStateInfo.State -in @('NotStarted','Running'))
                {
                    Start-Sleep -Milliseconds 100
                }

                Debug-Job -Id $job.Id -BreakAll 1>$null

                Receive-Job -Id $job.Id -Wait -AutoRemoveJob
            ";

            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Open();
                rs.Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
                rs.Debugger.DebuggerStop += (sender, args) =>
                {
                    debuggerStop = true;

                    Debugger debugger = sender as Debugger;
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    PSCommand exitCommand = new PSCommand();
                    exitCommand.AddCommand("Exit");
                    debugger.ProcessCommand(exitCommand, output);
                };

                using (PowerShell ps = PowerShell.Create(rs))
                {
                    ps.Runspace = rs;

                    var output = ps.AddScript(script)
                                   .Invoke();

                    // Verify that a debug stop occurred and that the job ran
                    // to completion.
                    Assert.True(debuggerStop,
                        "FAIL: TestDebugJobIPCCancelEvent: Expected a debugger stop to occur.");

                    Assert.True(output.Count == 10,
                        "FAIL: TestDebugJobIPCCancelEvent: Expected job run to fully complete with 10 output items.");
                }
            }
        }

        /// <summary>
        /// Tests the BreakAll runspace debug option on a local runspace.
        /// </summary>
        [Fact]
        public void TestDebugOptionOnLocalRunspace()
        {
            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Open();
                TestDebugOptionOnRunspace(rs, "TestDebugOptionOnLocalRunspace");
            }
        }

        /// <summary>
        /// Tests the Wait-Debugger cmdlet on a local runspace.
        /// </summary>
        [Fact]
        public void TestDebugBreakCommandOnLocalRunspace()
        {
            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Open();
                TestDebugBreakOnRunspace(rs, "TestDebugBreakCommandOnLocalRunspace");
            }
        }

        [Fact]
        public void TestWaitDebuggerOnNonEnabledRunspace()
        {
            string script = @"
                Wait-Debugger
                ""Line where debugger will stop.""
            ";

            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Name = "TestLocalRSWaitDebugger";
                rs.Open();

                using (PowerShell ps = PowerShell.Create(rs))
                {
                    // Start script in runspace *not* enabled for debugging.
                    ps.AddScript(script).BeginInvoke();

                    // Wait for runspace to break into debugger.
                    bool breakHit = WaitFor(() => { return (rs.Debugger.InBreakpoint == true); }, 60000, 200);
                    Assert.True(breakHit,
                        "FAIL: TestWaitDebuggerOnNonEnabledRunspace: Expected Wait-Debugger to enable Runspace debugging and to break into the debugger.");

                    // Let the PowerShell and Runspace objects be closed/disposed while debugger is in breakpoint.
                }
            }
        }

        private void TestDebugBreakOnRunspace(Runspace runspace, string testName)
        {
            bool debuggerStop = false;
            string script = @"
                Wait-Debugger
                ""Line where debugger will stop.""
            ";

            runspace.Debugger.DebuggerStop += (sender, args) =>
            {
                debuggerStop = true;
            };

            using (PowerShell ps = PowerShell.Create(runspace))
            {
                ps.AddScript(script).Invoke();
            }

            Assert.True(debuggerStop,
                string.Format("FAIL: {0}: Expected Wait-Debugger to cause a debugger stop.", testName));
        }
    }

#if !UNIX
    [Collection("Remoting test collection")]
    public class RemoteRunspaceDebuggingTests : RunspaceDebuggingTestsBase
    {
        /// <summary>
        /// This test verifies that both local and remote runspaces are reflected in
        /// the global runspace collection.
        /// </summary>
        [Fact]
        public void TestRunspaceCollection()
        {
            int localRunspaceId = -1;
            int remoteRunspaceId = -1;

            // Create runspaces.
            var wc = new WSManConnectionInfo();
            using (Runspace localRunspace = RunspaceFactory.CreateRunspace())
            {
                localRunspace.Open();
                localRunspaceId = localRunspace.Id;

                Assert.True(Runspace.RunspaceDictionary.ContainsKey(localRunspaceId),
                    "FAIL: TestRunspaceCollection: Expected local runspace to appear in runspace collection.");

                using (Runspace remoteRunspace = RunspaceFactory.CreateRunspace(wc))
                {
                    remoteRunspace.Open();
                    remoteRunspaceId = remoteRunspace.Id;

                    Assert.True(Runspace.RunspaceDictionary.ContainsKey(remoteRunspaceId),
                        "FAIL: TestRunspaceCollection: Expected remote runspace to appear in runspace collection.");

                    Assert.True(Runspace.RunspaceDictionary.Values.Count >= 2,
                        "FAIL: TestRunspaceCollection: Expected at least two runspaces in the runspace collection");
                }
            }

            Assert.False(Runspace.RunspaceDictionary.ContainsKey(localRunspaceId),
                "FAIL: TestRunspaceCollection: Expected local runspace to removed from runspace collection.");

            Assert.False(Runspace.RunspaceDictionary.ContainsKey(remoteRunspaceId),
                "FAIL: TestRunspaceCollection: Expected remote runspace to removed from runspace collection.");
        }

        /// <summary>
        /// This tests the new Runspace name property.
        /// </summary>
        [Fact]
        public void TestRunspaceName()
        {
            // Test name in local runspace.
            string localRunspaceName = "MyLocalRunspace";
            using (Runspace localRunspace = RunspaceFactory.CreateRunspace())
            {
                Assert.True(localRunspace.Name.Contains("Runspace"),
                    "FAIL: TestRunspaceName: Expected local runspace name to contain 'Runspace'");

                localRunspace.Name = localRunspaceName;

                Assert.True(localRunspace.Name == localRunspaceName,
                    "FAIL: TestRunspaceName: Expected local runspace name to change to 'MyLocalRunspace'");
            }

            // Test name in remote runspace.
            string remoteRunspaceName = "MyRemoteRunspace";
            var wc = new WSManConnectionInfo();
            using (Runspace remoteRunspace = RunspaceFactory.CreateRunspace(wc))
            {
                Assert.True(remoteRunspace.Name.Contains("Runspace"),
                    "FAIL: TestRunspaceName: Expected remote runspace name to contain 'Runspace'");

                remoteRunspace.Name = remoteRunspaceName;

                Assert.True(remoteRunspace.Name == remoteRunspaceName,
                    "FAIL: TestRunspaceName: Expected remote runspace name to change to 'MyRemoteRunspace'");
            }
        }

        /// <summary>
        /// Tests the Debug-Runspace cmdlet for remote runspaces.
        /// </summary>
        [Fact]
        public void TestDebugRunspaceOnRemoteRunspace()
        {
            s_debuggerStop = false;
            bool dataAdded = false;
            string debugRunspaceName = "TestDebugRunspaceOnRemoteRunspaceName";
            string script = @"
            1..60 | % { sleep 1; ""Output $_"" }
        ";

            var wsmanConnection = new WSManConnectionInfo();
            var typeTable = GetDefaultTypeTable();
            Assert.True(typeTable != null,
                "FAIL: TestDebugRunspaceOnRemoteRunspace: Unable to load type table for remote session.");
            using (Runspace rs = RunspaceFactory.CreateRunspace(wsmanConnection, null, typeTable))
            {
                rs.Name = debugRunspaceName;
                rs.Open();
                rs.Debugger.DebuggerStop += (sender, args) =>
                {
                    s_debuggerStop = true;
                    args.ResumeAction = DebuggerResumeAction.Stop;
                };

                using (PowerShell ps = PowerShell.Create(rs))
                {
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    output.DataAdded += (sender, args) =>
                    {
                        dataAdded = true;
                    };

                    var psAsync = ps.AddScript(script).BeginInvoke<object, PSObject>(null, output);

                    // Wait for command to run.
                    if (!WaitFor(() => { return dataAdded; },
                            30000, 1000))
                    {
                        Assert.True(false,
                            "FAIL: TestDebugRunspaceOnRemoteRunspace: Expected script to run and output data.");
                    }

                    // Run the Debug-Runspace command in a new runspace.
                    DebugRunspace(debugRunspaceName);

                    ps.EndInvoke(psAsync);
                }
            }

            Assert.True(s_debuggerStop,
                "FAIL: TestDebugRunspaceOnRemoteRunspace: Expected Debug-Runspace to break into debugger.");
        }

        /// <summary>
        /// Tests the Debug-Runspace cmdlet on a remote Runspace for multiple scripts.
        /// </summary>
        [Fact]
        public void TestDebugRunspaceWithMultipleScriptsOnRemoteRunspace()
        {
            bool dataAdded = false;
            string debugRunspaceName = "TestDebugRunspaceWithMultipleScriptsOnRemoteRunspaceName";
            string script = @"
            1..60 | % { sleep 1; ""Output $_"" }
        ";

            var wsmanConnection = new WSManConnectionInfo();
            var typeTable = GetDefaultTypeTable();
            Assert.True(typeTable != null,
                "FAIL: TestDebugRunspaceWithMultipleScriptsOnRemoteRunspace: Unable to load type table for remote session.");
            using (Runspace rs = RunspaceFactory.CreateRunspace())
            {
                rs.Name = debugRunspaceName;
                rs.Open();
                rs.Debugger.DebuggerStop += (sender, args) =>
                {
                    s_debuggerStop = true;
                    args.ResumeAction = DebuggerResumeAction.Stop;
                };

                // Execute first script run.
                s_debuggerStop = false;
                Runspace dbgRunspace = null;
                PowerShell dbgPowerShell = null;
                IAsyncResult async = null;
                using (PowerShell ps = PowerShell.Create(rs))
                {
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    output.DataAdded += (sender, args) =>
                    {
                        dataAdded = true;
                    };

                    dataAdded = false;
                    var psAsync = ps.AddScript(script).BeginInvoke<object, PSObject>(null, output);

                    // Wait for command to run.
                    if (!WaitFor(() => { return dataAdded; },
                            30000, 1000))
                    {
                        Assert.True(false,
                            "FAIL: TestDebugRunspaceWithMultipleScriptsOnRemoteRunspace: Expected script to run and output data.");
                    }

                    // Run the Debug-Runspace command in a new runspace.
                    DebugRunspace(debugRunspaceName, out dbgRunspace, out dbgPowerShell, out async);
                    if (dbgRunspace == null || dbgPowerShell == null || async == null)
                    {
                        Assert.True(false,
                            "FAIL: TestDebugRunspaceWithMultipleScriptsOnRemoteRunspace: Debug-Runspace did not run correctly, no debugger stop occurred.");
                    }

                    ps.EndInvoke(psAsync);
                }

                // Verify that this first script run stopped in the debugger.
                Assert.True(
                    s_debuggerStop,
                    "FAIL: TestDebugRunspaceWithMultipleScriptsOnRemoteRunspace: Expected Debug-Runspace to break into debugger for first run script.");

                // Execute second script run.
                s_debuggerStop = false;
                dbgRunspace.Debugger.SetDebuggerStepMode(true);
                using (PowerShell ps = PowerShell.Create(rs))
                {
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                    var psAsync = ps.AddScript(script).BeginInvoke<object, PSObject>(null, output);
                    ps.EndInvoke(psAsync);
                }

                // Verify that this second script run stopped in the debugger.
                Assert.True(
                    s_debuggerStop,
                    "FAIL: TestDebugRunspaceWithMultipleScriptsOnRemoteRunspace: Expected Debug-Runspace to break into debugger for second run script.");

                // Allow Debug-Runspace command to complete.
                dbgRunspace.Debugger.RaiseNestedDebuggingCancelEvent();
                dbgPowerShell.EndInvoke(async);

                // Clean up.
                dbgPowerShell.Dispose();
                dbgRunspace.Dispose();
            }
        }

        /// <summary>
        /// Tests the BreakAll runspace debug option on a remote runspace.
        /// </summary>
        [Fact]
        public void TestDebugOptionOnRemoteRunspace()
        {
            var wsmanConnection = new WSManConnectionInfo();
            var typeTable = GetDefaultTypeTable();
            Assert.True(typeTable != null,
                "FAIL: TestDebugOptionOnRemoteRunspace: Unable to load type table for remote session.");
            using (Runspace rs = RunspaceFactory.CreateRunspace(wsmanConnection, null, typeTable))
            {
                rs.Open();
                TestDebugOptionOnRunspace(rs, "TestDebugOptionOnRemoteRunspace");
            }
        }

        /// <summary>
        /// Test that the debugger can execute commands during a debug session
        /// on a nested runspace.
        /// </summary>
        [Fact]
        public void TestCommandOnDebugRemoteNestedRunspace()
        {
            bool debuggerStop = false;
            bool commandExecuted = false;
            string script = @"
            $rsNested = [RunspaceFactory]::CreateRunspace()
            $rsNested.Open()
            $rsNested.Name = ""MyNestedRunspace18""

            Enable-RunspaceDebug $rsNested -BreakAll

            $ps = [PowerShell]::Create()
            $ps.Runspace = $rsNested
            $ps.AddScript(""Write-Output 'Hello'"")
            $async = $ps.BeginInvoke()

            Debug-Runspace $rsNested -BreakAll
        ";

            var wsmanConnection = new WSManConnectionInfo();
            var typeTable = GetDefaultTypeTable();
            Assert.True(typeTable != null,
                "FAIL: TestCommandOnDebugRemoteNestedRunspace: Unable to load type table for remote session.");
            using (Runspace rootRunspace = RunspaceFactory.CreateRunspace(wsmanConnection, null, typeTable))
            {
                rootRunspace.Open();
                rootRunspace.Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);

                rootRunspace.Debugger.DebuggerStop += (sender, args) =>
                {
                    debuggerStop = true;

                    // Verify that a command works correctly when run inside
                    // the debugger on a nested runspace.
                    Debugger debugger = sender as Debugger;
                    PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                    PSCommand command = new PSCommand();
                    command.AddScript("Test-Path $pwd");
                    DebuggerCommandResults results = debugger.ProcessCommand(command, output);
                    if (output.Count == 1)
                    {
                        commandExecuted = (bool)output[0].BaseObject;
                    }

                    // Signal Debug-Runspace to end.
                    command.Clear();
                    command.AddCommand("Detach");
                    results = debugger.ProcessCommand(command, output);
                };

                using (PowerShell ps = PowerShell.Create(rootRunspace))
                {
                    ps.AddScript(script).Invoke();
                }

                Assert.True(debuggerStop,
                    "FAIL: TestCommandOnDebugRemoteNestedRunspace: Expected a debugger stop.");

                Assert.True(commandExecuted,
                    "FAIL: TestCommandOnDebugRemoteNestedRunspace: Expected debugger stop command to execute successfully.");
            }
        }

        /// <summary>
        /// Creates a PowerShell default type table needed for remote session debugging.
        /// </summary>
        private static TypeTable GetDefaultTypeTable()
        {
            TypeTable rtnTypeTable = null;

            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("[System.Management.Automation.Runspaces.TypeTable]::LoadDefaultTypeFiles()");
                var results = ps.Invoke<TypeTable>();
                rtnTypeTable = results.FirstOrDefault();
            }

            return rtnTypeTable;
        }
    }
#endif

}
