# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This is a Pester test suite to validate the New-TemporaryDirectory cmdlet in the Microsoft.PowerShell.Utility module.

<#
    Purpose:
        Verify that New-TemporaryDirectory creates a temporary directory.

    Action:
        Run New-TemporaryDirectory.

    Expected Result:
        A DirectoryInfo object for the temporary directory is returned.
#>

Describe "New-TemporaryDirectory" -Tags "CI" {

    It "creates a new temporary directory" {
        $tempDir = New-TemporaryDirectory

        $tempDir | Should -Exist
        $tempDir | Should -BeOfType System.IO.DirectoryInfo
        $tempDir | Should -BeLikeExactly "$([System.IO.Path]::GetTempPath())*"

        if (Test-Path $tempDir) {
            Remove-Item $tempDir -ErrorAction SilentlyContinue -Force
        }
    }

    It "with WhatIf does not create a directory" {
        New-TemporaryDirectory -WhatIf | Should -BeNullOrEmpty
    }

    It "has an OutputType of System.IO.DirectoryInfo" {
        (Get-Command New-TemporaryDirectory).OutputType | Should -BeExactly "System.IO.DirectoryInfo"
    }
}
