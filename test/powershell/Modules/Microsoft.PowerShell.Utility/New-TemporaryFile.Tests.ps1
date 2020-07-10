# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This is a Pester test suite to validate the New-TemporaryFile cmdlet in the Microsoft.PowerShell.Utility module.

<#
    Purpose:
        Verify that New-TemporaryFile creates a temporary file.

    Action:
        Run New-TemporaryFile.

    Expected Result:
        A FileInfo object for the temporary file is returned.
#>

Describe "New-TemporaryFile" -Tags "CI" {

    It "creates a new temporary file" {
        $tempFile = New-TemporaryFile

        $tempFile | Should -Exist
        $tempFile | Should -BeOfType System.IO.FileInfo
        $tempFile | Should -BeLikeExactly "$([System.IO.Path]::GetTempPath())*"

        if (Test-Path $tempFile) {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
        }
    }

    It "with WhatIf does not create a file" {
        New-TemporaryFile -WhatIf | Should -BeNullOrEmpty
    }

    It "has an OutputType of System.IO.FileInfo" {
        (Get-Command New-TemporaryFile).OutputType | Should -BeExactly "System.IO.FileInfo"
    }
}
