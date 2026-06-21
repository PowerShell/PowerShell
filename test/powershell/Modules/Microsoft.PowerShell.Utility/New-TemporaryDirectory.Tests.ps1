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

    It "creates a unique directory each time" {
        $tempDir1 = New-TemporaryDirectory
        $tempDir2 = New-TemporaryDirectory

        $tempDir1.FullName | Should -Not -Be $tempDir2.FullName

        if (Test-Path $tempDir1) {
            Remove-Item $tempDir1 -ErrorAction SilentlyContinue -Force
        }
        if (Test-Path $tempDir2) {
            Remove-Item $tempDir2 -ErrorAction SilentlyContinue -Force
        }
    }

    It "with WhatIf does not create a directory" {
        New-TemporaryDirectory -WhatIf | Should -BeNullOrEmpty
    }

    It "has an OutputType of System.IO.DirectoryInfo" {
        (Get-Command New-TemporaryDirectory).OutputType | Should -BeExactly "System.IO.DirectoryInfo"
    }
}
