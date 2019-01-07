# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##
## PowerShell Invoke-Command -RemoteDebug Tests
##

if ($IsWindows)
{
    $typeDef = @'
    using System;
    using System.Globalization;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Management.Automation.Host;

    namespace TestRunner
    {
        public class DummyHost : PSHost, IHostSupportsInteractiveSession
        {
            public Runspace _runspace;
            private Guid _instanceId = Guid.NewGuid();

            public override CultureInfo CurrentCulture
            {
                get { return CultureInfo.CurrentCulture; }
            }
            public override CultureInfo CurrentUICulture
            {
                get { return CultureInfo.CurrentUICulture; }
            }
            public override Guid InstanceId
            {
                get { return _instanceId; }
            }
            public override string Name
            {
                get { return "DummyTestHost"; }
            }
            public override PSHostUserInterface UI
            {
                get { return null; }
            }
            public override Version Version
            {
                get { return new Version(1, 0); }
            }
            public override void EnterNestedPrompt() { }
            public override void ExitNestedPrompt() { }
            public override void NotifyBeginApplication() { }
            public override void NotifyEndApplication() { }
            public override void SetShouldExit(int exitCode) { }
            public void PushRunspace(Runspace runspace) { }
            public void PopRunspace() { }
            public bool IsRunspacePushed { get { return false; } }
            public Runspace Runspace { get { return _runspace; } private set { _runspace = value; } }
        }

        public class TestDebugger : Debugger
        {
            private Runspace _runspace;
            public int DebugStopCount
            {
                private set;
                get;
            }
            public int RunspaceDebugProcessingCount
            {
                private set;
                get;
            }
            public bool RunspaceDebugProcessCancelled
            {
                private set;
                get;
            }

            private void HandleDebuggerStop(object sender, DebuggerStopEventArgs args)
            {
                DebugStopCount++;
                var debugger = sender as Debugger;
                var command = new PSCommand();
                command.AddScript("prompt");
                var output = new PSDataCollection<PSObject>();
                debugger.ProcessCommand(command, output);
            }
            private void HandleStartRunspaceDebugProcessing(object sender, StartRunspaceDebugProcessingEventArgs args)
            {
                args.UseDefaultProcessing = true;
                RunspaceDebugProcessingCount++;
            }
            private void HandleCancelRunspaceDebugProcessing(object sender, EventArgs args)
            {
                RunspaceDebugProcessCancelled = true;
            }
            public TestDebugger(Runspace runspace)
            {
                _runspace = runspace;
                _runspace.Debugger.DebuggerStop += HandleDebuggerStop;
                _runspace.Debugger.StartRunspaceDebugProcessing += HandleStartRunspaceDebugProcessing;
                _runspace.Debugger.CancelRunspaceDebugProcessing += HandleCancelRunspaceDebugProcessing;
            }
            public void Release()
            {
                if (_runspace == null) { return; }
                _runspace.Debugger.DebuggerStop -= HandleDebuggerStop;
                _runspace.Debugger.StartRunspaceDebugProcessing -= HandleStartRunspaceDebugProcessing;
                _runspace.Debugger.CancelRunspaceDebugProcessing -= HandleCancelRunspaceDebugProcessing;
            }

            public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output) { return null; }
            public override void SetDebuggerAction(DebuggerResumeAction resumeAction) { }
            public override DebuggerStopEventArgs GetDebuggerStopArgs() { return null; }
            public override void StopProcessCommand() { }
        }
    }
'@
}

Describe "Invoke-Command remote debugging tests" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {

        if (!$IsWindows)
        {
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        $sb = [scriptblock]::Create('"Hello!"')

        Add-Type -TypeDefinition $typeDef

        $dummyHost = [TestRunner.DummyHost]::new()
        [runspace] $rs = [runspacefactory]::CreateRunspace($dummyHost)
        $rs.Open()
        $dummyHost._runspace = $rs

        $testDebugger = [TestRunner.TestDebugger]::new($rs)

        [runspace] $rs2 = [runspacefactory]::CreateRunspace()
        $rs2.Open()

        [powershell] $ps = [powershell]::Create()
        $ps.Runspace = $rs

        [powershell] $ps2 = [powershell]::Create()
        $ps2.Runspace = $rs2
    }

    AfterAll {

        if (!$IsWindows)
        {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
            return
        }

        if ($null -ne $testDebugger) { $testDebugger.Release() }
        if ($null -ne $ps) { $ps.Dispose() }
        if ($null -ne $ps2) { $ps2.Dispose() }
        if ($null -ne $rs) { $rs.Dispose() }
        if ($null -ne $rs2) { $rs2.Dispose() }
        if ($null -ne $remoteSession) { Remove-PSSession $remoteSession -ErrorAction SilentlyContinue }
    }

    BeforeEach {

        $remoteSession = New-RemoteSession
    }

    AfterEach {

        $ps.Commands.Clear()
        $ps2.Commands.Clear()

        Remove-PSSession $remoteSession -ErrorAction SilentlyContinue
        $remoteSession = $null
    }

    It "Verifies that asynchronous 'Invoke-Command -RemoteDebug' is ignored" {

        $ps.AddCommand("Invoke-Command").
            AddParameter("Session", $remoteSession).
            AddParameter("ScriptBlock", $sb).
            AddParameter("RemoteDebug", $true).
            AddParameter("AsJob", $true)
        $result = $ps.Invoke()
        $testDebugger.DebugStopCount | Should -Be 0
    }

    It "Verifies that synchronous 'Invoke-Command -RemoteDebug' invokes debugger" {

        $ps.AddCommand("Invoke-Command").
            AddParameter("Session", $remoteSession).
            AddParameter("ScriptBlock", $sb).
            AddParameter("RemoteDebug", $true)
        $result = $ps.Invoke()
        $testDebugger.RunspaceDebugProcessingCount | Should -Be 1
        $testDebugger.DebugStopCount | Should -Be 1
    }

    It "Verifies the debugger 'CancelDebuggerProcessing' API method" {

        $rs.Debugger.CancelDebuggerProcessing()
        $testDebugger.RunspaceDebugProcessCancelled | Should -BeTrue
    }

    It "Verifies that 'Invoke-Command -RemoteDebug' running in a runspace without PSHost is ignored" {

        $ps2.AddCommand("Invoke-Command").
            AddParameter("Session", $remoteSession).
            AddParameter("ScriptBlock", $sb).
            AddParameter("RemoteDebug", $true)
        $result = $ps2.Invoke()
        $result | Should -Be "Hello!"
    }
}
