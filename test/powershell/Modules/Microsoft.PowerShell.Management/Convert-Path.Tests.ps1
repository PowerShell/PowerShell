# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Convert-Path tests" -Tag CI {
    It "Convert-Path should handle provider qualified paths" {
        Convert-Path "FileSystem::${TESTDRIVE}" | Should -Be "${TESTDRIVE}"
    }
    It "Convert-Path should return the proper path" {
        Convert-Path "$TESTDRIVE" | Should -Be "$TESTDRIVE"
    }
    It "Convert-Path supports pipelined input" {
        "$TESTDRIVE" | Convert-Path | Should -Be "$TESTDRIVE"
    }
    It "Convert-Path supports pipelined input by property name" {
        get-item $TESTDRIVE | Convert-Path | Should -Be "$TESTDRIVE"
    }
    It "Convert-Path without arguments is an error" {
        try {
            $ps = [powershell]::Create()
            $ps.AddCommand("Convert-Path").Invoke()
            throw "Execution should not have reached here"
        }
        catch {
            $_.fullyqualifiederrorid | Should -Be ParameterBindingException
        }
    }
    It "Convert-Path with null path is an error" {
        try {
            Convert-Path -path ""
            throw "Execution should not have reached here"
        }
        catch {
            $_.fullyqualifiederrorid | Should -Be "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ConvertPathCommand"
        }
    }
    It "Convert-Path with non-existing non-filesystem path is an error" {
        try {
            Convert-Path -path "env:thisvariableshouldnotexist" -ea stop
            throw "Execution should not have reached here"
        }
        catch {
            $_.fullyqualifiederrorid | Should -Be "PathNotFound,Microsoft.PowerShell.Commands.ConvertPathCommand"
        }
    }
    It "Convert-Path can handle multiple directories" {
        $d1 = Setup -D dir1 -pass
        $d2 = Setup -D dir2 -pass
        $result = convert-path "${TESTDRIVE}/dir?"
        $result.count | Should -Be 2
        $result -join "," | Should -Be (@("$d1","$d2") -join ",")
    }
    It "Convert-Path should return something which exists" {
        Convert-Path $TESTDRIVE | Test-Path | Should -Be $true
    }
}
