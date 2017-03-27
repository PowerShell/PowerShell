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
        try {
            Debug-Runspace -Name "My*" -ea stop
            throw "Command did not throw exception"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "DebugRunspaceTooManyRunspaceFound,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
        }
    }

    It "Debugging a runspace should fail if the name is not found" {
        try {
            Debug-Runspace -Name "dflkjsdkfjldkjssldfj" -ea stop
            throw "Command did not throw exception"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "DebugRunspaceNoRunspaceFound,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
        }
    }

    It "Debugging a runspace should fail if the runspace is not open" {
        try {
            $rs2.Close()
            Debug-Runspace -runspace $rs2 -ea stop
            throw "Command did not throw exception"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
        }
    }

    It "Debugging a runspace should fail if the runspace has no debugger" {
        try {
            $rs1.Debugger.SetDebugMode("None")
            Debug-Runspace -runspace $rs1 -ea stop
            throw "Command did not throw exception"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "InvalidOperation,Microsoft.PowerShell.Commands.DebugRunspaceCommand"
        }
    }

}


