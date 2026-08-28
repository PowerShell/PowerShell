# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Debug-Runspace" -Tag "CI" {
    BeforeAll {
        $rs1 = [runspacefactory]::CreateRunspace()
        $rs1.Open()
        $rs1.Name = "MyRunspace1"
        $rs2 = [runspacefactory]::CreateRunspace()
        $rs2.Open()
        $rs2.Name = "MyRunspace2"
    }
    AfterAll {
        if ( $rs1 ) { $rs1.Dispose() }
        if ( $rs2 ) { $rs2.Dispose() }
    }

    It "Debugging a runspace should fail if the name is ambiguous" {
        { Debug-Runspace -Name "My*" -ErrorAction stop } | Should -Throw -ErrorId "DebugRunspaceTooManyRunspaceFound,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the name is not found" {
        { Debug-Runspace -Name "dflkjsdkfjldkjssldfj" -ErrorAction stop } | Should -Throw -ErrorId "DebugRunspaceNoRunspaceFound,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the runspace is not open" {
        $rs2.Close()
        { Debug-Runspace -Runspace $rs2 -ErrorAction stop } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the runspace has no debugger" {
        $rs1.Debugger.SetDebugMode("None")
        { Debug-Runspace -Runspace $rs1 -ErrorAction stop } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }
    
    It "Should write attach event and mark runspace as having a remote debugger attached" {
        $onAttachName = [System.Management.Automation.PSEngineEvent]::OnDebugAttach

        $targetRunspace = $null
        $debugTarget = $null
        $debugger = $null
        $debugTask = $null

        try {
            # Open the target runspace up front so that 'Debug-Runspace' can never observe it in a
            # non-Opened state, and so that its event manager is guaranteed to exist when the
            # OnDebugAttach event is generated.
            $targetRunspace = [runspacefactory]::CreateRunspace()
            $targetRunspace.Open()

            $debugTarget = [PowerShell]::Create()
            $debugTarget.Runspace = $targetRunspace
            $null = $debugTarget.AddCommand('Wait-Event').AddParameter('SourceIdentifier', $onAttachName)
            $waitTask = $debugTarget.BeginInvoke()

            # 'BeginInvoke' only queues the work. Wait until the 'Wait-Event' pipeline is actually
            # running in the target runspace before attaching the debugger, so the attach event is
            # never generated against a runspace that has not started executing the waiter.
            $ready = Wait-UntilTrue -IntervalInMilliseconds 20 -TimeoutInMilliseconds 5000 -sb {
                $debugTarget.InvocationStateInfo.State -eq [System.Management.Automation.PSInvocationState]::Running -and
                $targetRunspace.RunspaceAvailability -eq [System.Management.Automation.Runspaces.RunspaceAvailability]::Busy
            }
            $ready | Should -BeTrue -Because "the 'Wait-Event' pipeline should be running in the target runspace"

            $targetRunspace.IsRemoteDebuggerAttached | Should -BeFalse

            $debugger = [PowerShell]::Create()
            $null = $debugger.AddCommand('Debug-Runspace').AddParameter('Id', $targetRunspace.Id)
            $debugTask = $debugger.BeginInvoke()

            $waitTask.AsyncWaitHandle.WaitOne(5000) | Should -BeTrue
            $waitInfo = $debugTarget.EndInvoke($waitTask)
            $waitInfo.SourceIdentifier | Should -Be $onAttachName

            $targetRunspace.IsRemoteDebuggerAttached | Should -BeTrue

            $debugger.Stop()
            $exp = {
                $debugger.EndInvoke($debugTask)
            } | Should -Throw -PassThru
            $exp.FullyQualifiedErrorId | Should -Be "PipelineStoppedException"

            # 'IsRemoteDebuggerAttached' is reset by the cmdlet as it unwinds, which happens
            # asynchronously with respect to 'Stop' completing.
            $detached = Wait-UntilTrue -IntervalInMilliseconds 20 -TimeoutInMilliseconds 5000 -sb {
                -not $targetRunspace.IsRemoteDebuggerAttached
            }
            $detached | Should -BeTrue

            $targetRunspace.IsRemoteDebuggerAttached | Should -BeFalse
        }
        finally {
            if ($debugger) {
                try { $debugger.Stop() } catch { Write-Warning "Failed to stop the debugger during cleanup: $_" }
                $debugger.Dispose()
            }

            if ($debugTarget) {
                try { $debugTarget.Stop() } catch { Write-Warning "Failed to stop the debug target during cleanup: $_" }
                $debugTarget.Dispose()
            }

            if ($targetRunspace) { $targetRunspace.Dispose() }
        }
    }
}
