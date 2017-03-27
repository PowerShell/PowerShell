Describe "Convert-Path tests" -Tag CI {
    It "Convert-Path should handle provider qualified paths" {
        Convert-Path "FileSystem::${TESTDRIVE}" | should be "${TESTDRIVE}"
    }
    It "Convert-Path should return the proper path" {
        Convert-Path "$TESTDRIVE" | Should be "$TESTDRIVE"
    }
    It "Convert-Path supports pipelined input" {
        "$TESTDRIVE" | Convert-Path | Should be "$TESTDRIVE"
    }
    It "Convert-Path supports pipelined input by property name" {
        get-item $TESTDRIVE | Convert-Path | Should be "$TESTDRIVE"
    }
    It "Convert-Path without arguments is an error" {
        try {
            $ps = [powershell]::Create()
            $ps.AddCommand("Convert-Path").Invoke()
            throw "Execution should not have reached here"
        }
        catch {
            $_.fullyqualifiederrorid | should be ParameterBindingException
        }
    }
    It "Convert-Path with null path is an error" {
        try {
            Convert-Path -path ""
            throw "Execution should not have reached here"
        }
        catch {
            $_.fullyqualifiederrorid | should be "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ConvertPathCommand"
        }
    }
    It "Convert-Path with non-existing non-filesystem path is an error" {
        try {
            Convert-Path -path "env:thisvariableshouldnotexist" -ea stop
            throw "Execution should not have reached here"
        }
        catch {
            $_.fullyqualifiederrorid | should be "PathNotFound,Microsoft.PowerShell.Commands.ConvertPathCommand"
        }
    }
    It "Convert-Path can handle multiple directories" {
        $d1 = Setup -D dir1 -pass
        $d2 = Setup -D dir2 -pass
        $result = convert-path "${TESTDRIVE}/dir?"
        $result.count | Should be 2
        $result -join "," | should be (@("$d1","$d2") -join ",")
    }
    It "Convert-Path should return something which exists" {
        Convert-Path $TESTDRIVE | Test-Path | should be $true
    }
}
