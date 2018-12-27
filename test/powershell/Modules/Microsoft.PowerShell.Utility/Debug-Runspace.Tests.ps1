# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Debug-Runspace" -tag "CI" {
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
        { Debug-Runspace -runspace $rs2 -ErrorAction stop } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the runspace has no debugger" {
        $rs1.Debugger.SetDebugMode("None")
        { Debug-Runspace -runspace $rs1 -ErrorAction stop } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

}

