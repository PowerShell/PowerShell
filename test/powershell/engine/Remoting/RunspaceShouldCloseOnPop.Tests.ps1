# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Runspace.SetShouldCloseOnPop' -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {
        Import-Module HelpersCommon

        if (-not ('CloseOnPopTestHost' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

public sealed class CloseOnPopTestHost : PSHost, IHostSupportsInteractiveSession
{
    private readonly Guid _instanceId = Guid.NewGuid();
    private Runspace _pushedRunspace;

    // The UI must be non-null for the client to dispatch remote host calls
    // like PopRunspace but it is not used by these tests.
    public CloseOnPopTestHost(PSHostUserInterface ui) => UI = ui;

    public override string Name => "CloseOnPopTestHost";
    public override Version Version => new Version(1, 0);
    public override Guid InstanceId => _instanceId;
    public override PSHostUserInterface UI { get; }
    public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
    public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;
    public override void SetShouldExit(int exitCode) { }
    public override void EnterNestedPrompt() { }
    public override void ExitNestedPrompt() { }
    public override void NotifyBeginApplication() { }
    public override void NotifyEndApplication() { }

    public bool IsRunspacePushed => _pushedRunspace != null;
    public Runspace Runspace => _pushedRunspace;
    public void PushRunspace(Runspace runspace) => _pushedRunspace = runspace;
    public void PopRunspace() => _pushedRunspace = null;
}
'@
        }

        $targetProcess = Start-Process -FilePath "$PSHOME/pwsh" -ArgumentList '-NoProfile', '-NonInteractive', '-Command', 'Start-Sleep -Seconds 300' -PassThru

        function New-TestRemoteRunspace {
            param ($TestHost)

            $connectionInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($targetProcess.Id)
            $connectionInfo.OpenTimeout = 10000
            $typeTable = [System.Management.Automation.Runspaces.TypeTable]::LoadDefaultTypeFiles()

            $runspace = [runspacefactory]::CreateRunspace($connectionInfo, $TestHost, $typeTable)
            $runspace.Open()
            $runspace
        }

        function Invoke-ExitPSSession {
            param ($Runspace)

            $ps = [powershell]::Create()
            try {
                $ps.Runspace = $Runspace
                $null = $ps.AddCommand('Exit-PSSession').Invoke()
            }
            finally {
                $ps.Dispose()
            }
        }
    }

    AfterAll {
        $targetProcess | Stop-Process -Force -ErrorAction SilentlyContinue
    }

    It 'Throws for a local runspace' {
        $runspace = [runspacefactory]::CreateRunspace()
        try {
            { $runspace.SetShouldCloseOnPop() } | Should -Throw -ErrorId 'InvalidOperationException'
        }
        finally {
            $runspace.Dispose()
        }
    }

    It 'Closes the remote runspace when popped by the remote session' {
        $testHost = [CloseOnPopTestHost]::new($Host.UI)
        $runspace = New-TestRemoteRunspace -TestHost $testHost
        try {
            $runspace.SetShouldCloseOnPop()
            $testHost.PushRunspace($runspace)

            Invoke-ExitPSSession -Runspace $runspace

            $testHost.IsRunspacePushed | Should -BeFalse
            Wait-UntilTrue -sb { $runspace.RunspaceStateInfo.State -eq 'Closed' } -TimeoutInMilliseconds 10000 | Should -BeTrue
        }
        finally {
            $runspace.Dispose()
        }
    }

    It 'Leaves the remote runspace open when popped if not marked to close' {
        $testHost = [CloseOnPopTestHost]::new($Host.UI)
        $runspace = New-TestRemoteRunspace -TestHost $testHost
        try {
            $testHost.PushRunspace($runspace)

            Invoke-ExitPSSession -Runspace $runspace

            $testHost.IsRunspacePushed | Should -BeFalse
            $runspace.RunspaceStateInfo.State | Should -Be 'Opened'
        }
        finally {
            $runspace.Dispose()
        }
    }
}
