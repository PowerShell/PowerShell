# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Convert-Path tests" -Tag CI {
    It "Convert-Path should handle provider qualified paths" {
        Convert-Path -Path "FileSystem::${TestDrive}" | Should -BeExactly "${TestDrive}"
    }

    It "Convert-Path should return the proper path" {
        Convert-Path -Path "$TestDrive" | Should -BeExactly "$TestDrive"
    }

    It "Convert-Path supports pipelined input" {
        "$TestDrive" | Convert-Path | Should -BeExactly "$TestDrive"
    }

    It "Convert-Path supports pipelined input by property name" {
        Get-Item -Path $TestDrive | Convert-Path | Should -BeExactly "$TestDrive"
    }

    It "Convert-Path without arguments is an error" {
        $ps = [powershell]::Create()
        { $ps.AddCommand("Convert-Path").Invoke() } | Should -Throw -ErrorId "ParameterBindingException"
    }

    It "Convert-Path with null path is an error" {
        { Convert-Path -Path "" } | Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ConvertPathCommand"
    }

    It "Convert-Path with non-existing non-filesystem path is an error" {
        { Convert-Path -Path "env:thisvariableshouldnotexist" -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ConvertPathCommand"
    }

    It "Convert-Path can handle multiple directories" {
        $d1 = Setup -Dir -Path dir1 -PassThru
        $d2 = Setup -Dir -Path dir2 -PassThru
        $result = Convert-Path -Path "${TestDrive}/dir?"
        $result.count | Should -Be 2
        $result -join "," | Should -BeExactly (@("$d1","$d2") -join ",")
    }

    It "Convert-Path should return something which exists" {
        Convert-Path -Path $TestDrive | Should -Exist
    }
}
