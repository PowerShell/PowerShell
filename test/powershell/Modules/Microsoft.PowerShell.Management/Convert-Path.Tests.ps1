# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Convert-Path tests" -Tag CI {
    It "Convert-Path should handle provider qualified paths" {
        Convert-Path "FileSystem::${TESTDRIVE}" | Should -BeExactly "${TESTDRIVE}"
    }
    It "Convert-Path should return the proper path" {
        Convert-Path "$TESTDRIVE" | Should -BeExactly "$TESTDRIVE"
    }
    It "Convert-Path supports pipelined input" {
        "$TESTDRIVE" | Convert-Path | Should -BeExactly "$TESTDRIVE"
    }
    It "Convert-Path supports pipelined input by property name" {
        get-item $TESTDRIVE | Convert-Path | Should -BeExactly "$TESTDRIVE"
    }
    It "Convert-Path without arguments is an error" {
        $ps = [powershell]::Create()
        { $ps.AddCommand("Convert-Path").Invoke() } | Should -Throw -ErrorId "ParameterBindingException"
    }
    It "Convert-Path with null path is an error" {
        { Convert-Path -path "" } | Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ConvertPathCommand"
    }
    It "Convert-Path with non-existing non-filesystem path is an error" {
        { Convert-Path -path "env:thisvariableshouldnotexist" -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ConvertPathCommand"
    }
    It "Convert-Path can handle multiple directories" {
        $d1 = Setup -D dir1 -pass
        $d2 = Setup -D dir2 -pass
        $result = convert-path "${TESTDRIVE}/dir?"
        $result.count | Should -Be 2
        $result -join "," | Should -BeExactly (@("$d1","$d2") -join ",")
    }
    It "Convert-Path should return something which exists" {
        Convert-Path $TESTDRIVE | Test-Path | Should -BeTrue
    }
}
