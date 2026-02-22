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
        
        $debugTarget = [PowerShell]::Create()
        $null = $debugTarget.AddCommand('Wait-Event').AddParameter('SourceIdentifier', $onAttachName)
        $waitTask = $debugTarget.BeginInvoke()

        $debugTarget.Runspace.IsRemoteDebuggerAttached | Should -BeFalse

        $debugger = [PowerShell]::Create()
        $null = $debugger.AddCommand('Debug-Runspace').AddParameter('Id', $debugTarget.Runspace.Id)
        $debugTask = $debugger.BeginInvoke()
        
        $waitTask.AsyncWaitHandle.WaitOne(5000) | Should -BeTrue
        $waitInfo = $debugTarget.EndInvoke($waitTask)
        $waitInfo.SourceIdentifier | Should -Be $onAttachName

        $debugTarget.Runspace.IsRemoteDebuggerAttached | Should -BeTrue

        $debugger.Stop()
        $exp = {
            $debugger.EndInvoke($debugTask)
        } | Should -Throw -PassThru
        $exp.FullyQualifiedErrorId | Should -Be "PipelineStoppedException"

        $debugTarget.Runspace.IsRemoteDebuggerAttached | Should -BeFalse
    }
}
