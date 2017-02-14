Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

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
        if ( $rs2 ) { $rs1.Dispose() }
    }

    It "Debugging a runspace should fail if the name is ambiguous" {
        { Debug-Runspace -Name "My*" -ErrorAction Stop } | ShouldBeErrorId "DebugRunspaceTooManyRunspaceFound,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the name is not found" {
        { Debug-Runspace -Name "dflkjsdkfjldkjssldfj" -ErrorAction Stop } | ShouldBeErrorId "DebugRunspaceNoRunspaceFound,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the runspace is not open" {
        {
            $rs2.Close()
            Debug-Runspace -runspace $rs2 -ErrorAction Stop
        } | ShouldBeErrorId "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

    It "Debugging a runspace should fail if the runspace has no debugger" {
        {
            $rs1.Debugger.SetDebugMode("None")
            Debug-Runspace -runspace $rs1 -ErrorAction Stop
        } | ShouldBeErrorId "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
    }

}


