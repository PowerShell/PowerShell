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
    $defaultTemporaryFileExtension = '.tmp'

    It "creates a new temporary file" {
        $tempFile = New-TemporaryFile
        try
        {
            
            Test-Path $tempFile | Should be $true
            $tempFile | Should BeOfType System.IO.FileInfo
            $tempFile.Extension | Should be $defaultTemporaryFileExtension
        }
        finally
        {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
        }
    }

    It "creates a new temporary file with a specific extension using the -Extension parameter" {
        $expectedExtension = '.csv'
        
        $tempFile = New-TemporaryFile -Extension $expectedExtension
        try
        {
            
            Test-Path $tempFile | Should be $true
            $tempFile | Should BeOfType System.IO.FileInfo
            $tempFile.Extension | Should be $expectedExtension
        }
        finally
        {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
        }

        $tempFile = New-TemporaryFile 'csv' # check that one can also omit the period and parameter name
        try
        {
            Test-Path $tempFile | Should be $true
            $tempFile | Should BeOfType System.IO.FileInfo
            $tempFile.Extension | Should be $expectedExtension
        }
        finally
        {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
        }
    }

    It "creates a new temporary file with the name being a Guid using -GuidBasedName switch" {        
        $tempFile = New-TemporaryFile -GuidBasedName
        try
        {
            Test-Path $tempFile | Should be $true
            $tempFile | Should BeOfType System.IO.FileInfo
            $tempFile.BaseName -as [Guid] | Should BeOfType Guid
            $tempFile.Extension | Should be $defaultTemporaryFileExtension
        }
        finally
        {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
        }
    }

    It "creates a new temporary file with -Extension parameter and -GuidBasedName switch" {        
        $expectedExtension = '.csv'
        
        $tempFile = New-TemporaryFile -Extension $expectedExtension -GuidBasedName
        try
        {
            Test-Path $tempFile | Should be $true
            $tempFile | Should BeOfType System.IO.FileInfo
            $tempFile.BaseName -as [Guid] | Should BeOfType Guid
            $tempFile.Extension | Should be $expectedExtension
        }
        finally
        {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -Force
        }
    }

    It "New-TemporaryItem with an an invalid character in the -Extension parameter should throw NewTemporaryInvalidArgument error" {
        $invalidFileNameChars = [System.IO.Path]::GetInvalidFileNameChars()
        foreach($invalidFileNameChar in $invalidFileNameChars)
        {
            try
            {
                New-TemporaryFile -Extension $invalidFileNameChar -ErrorAction Stop
                throw "No Exception!"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "NewTemporaryInvalidArgument,Microsoft.PowerShell.Commands.NewTemporaryFileCommand"
            }
        }
    }

    It "with WhatIf does not create a file" {
        New-TemporaryFile -WhatIf | Should Be $null
    }

    It "has an OutputType of System.IO.FileInfo" {
        (Get-Command New-TemporaryFile).OutputType | Should Be "System.IO.FileInfo"
    }
}
