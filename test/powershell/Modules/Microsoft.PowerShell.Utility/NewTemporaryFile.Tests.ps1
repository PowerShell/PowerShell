# This is a Pester test suite to validate the New-TemporaryFile cmdlet in the Microsoft.PowerShell.Utility module.
#
# Copyright (c) Microsoft Corporation, 2015
#

<#
    Purpose:
        Verify that New-TemporaryFile creates a temporary file.

    Action:
        Run New-TemporaryFile.

    Expected Result:
        A FileInfo object for the temporary file is returned.
#>

Describe "NewTemporaryFile" -Tags "CI" {

    It "creates a new temporary file" {
        $tempFile = New-TemporaryFile

        Test-Path $tempFile | Should be $true
        $tempFile | Should BeOfType System.IO.FileInfo
        $tempFile.Extension | Should be '.tmp'

        Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
    }

    It "creates a new temporary file with a specific extension using the -Extension paramter" {
        $expectedExtension = '.csv'
        $tempFile = New-TemporaryFile -Extension $expectedExtension

        Test-Path $tempFile | Should be $true
        $tempFile | Should BeOfType System.IO.FileInfo
        $tempFile.Extension | Should be $expectedExtension
        Remove-Item $tempFile -ErrorAction SilentlyContinue -Force

        $tempFile = New-TemporaryFile 'csv' # check that one can also omit the period and parameter name
        Test-Path $tempFile | Should be $true
        $tempFile | Should BeOfType System.IO.FileInfo
        $tempFile.Extension | Should be $expectedExtension
        Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
    }

    It "with WhatIf does not create a file" {
        New-TemporaryFile -WhatIf | Should Be $null
    }

    It "has an OutputType of System.IO.FileInfo" {
        (Get-Command New-TemporaryFile).OutputType | Should Be "System.IO.FileInfo"
    }
}
