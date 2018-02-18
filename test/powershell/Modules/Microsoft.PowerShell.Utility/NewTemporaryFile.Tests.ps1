# Copyright (c) Microsoft Corporation. All rights reserved.
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
        try {
            $tempFile = New-TemporaryFile

            $tempFile | Should Exist
            $tempFile | Should BeOfType System.IO.FileInfo
        } finally {
            [System.IO.File]::Delete($tempFile)
        }
    }

    It "throws terminating error when it fails to create new temporary file to Windows limit of 65535 files" {

        try {
            $tempFiles = foreach ($i in (1..65536)) { New-TemporaryFile -ErrorAction Ignore }
            { New-TemporaryFile } | Should Throw "The file exists"
        } finally {
            $tempFiles | ForEach-Object { [System.IO.File]::Delete($PSItem) }
        }
    }


    It "with WhatIf does not create a file" {
        New-TemporaryFile -WhatIf | Should Be $null
    }

    It "has an OutputType of System.IO.FileInfo" {
        (Get-Command New-TemporaryFile).OutputType | Should Be "System.IO.FileInfo"
    }
}
