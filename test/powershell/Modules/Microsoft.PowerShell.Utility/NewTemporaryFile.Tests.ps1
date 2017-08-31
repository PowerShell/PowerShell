# This is a Pester test suite to validate the New-TemporaryFile cmdlet in the Microsoft.PowerShell.Utility module.
#
# Copyright (c) Microsoft Corporation, 2015
#

<#
    Purpose:
        Verify that New-TemporaryFile creates a temporary file.
        It has an 'Extension' parameter to change the default extension '.tmp'.

    Action:
        Run New-TemporaryFile.

    Expected Result:
        A FileInfo object for the temporary file is returned and the temporary file gets created correctly.
#>

Describe "NewTemporaryFile" -Tags "CI" {

    $defaultExtension = '.tmp'

    AfterEach {
        if ($null -ne $script:tempFile)
        {
            Remove-Item $script:tempFile -ErrorAction SilentlyContinue -Force # variable needs script scope because it gets defined in It block
        }
    }

    It "creates a new temporary file" {
        $script:tempFile = New-TemporaryFile
        $tempFile | Should Exist
        $tempFile | Should BeOfType System.IO.FileInfo
        $tempFile.Extension | Should be $defaultExtension
    }

    $extensionParameterTestCases = @(
        @{ extension = 'csv' }
        @{ extension = '.csv' }
        @{ extension = '..csv' }
        @{ extension = 'txt.csv' }
        @{ extension = '.txt.csv' }
    )
    It "creates a new temporary file with a specific extension" -TestCases $extensionParameterTestCases -Test {
        Param ([string]$extension)
        
        $script:tempFile = New-TemporaryFile -Extension $extension
            $tempFile | Should Exist
            # Because the internal algorithm does renaming it is worthwhile checking that the original file does not get left behin
            [System.IO.Path]::ChangeExtension($tempFile, $defaultExtension) | Should Not Exist
            $tempFile | Should BeOfType System.IO.FileInfo
            $tempFile.Extension | Should be ".csv"
            $tempFile.FullName.EndsWith($extension) | Should be $true
    }

    It "New-TemporaryItem with an an invalid character in the -Extension parameter should throw NewTemporaryInvalidArgument error" {
        $invalidFileNameChars = [System.IO.Path]::GetInvalidFileNameChars()
        foreach($invalidFileNameChar in $invalidFileNameChars)
        {
            { New-TemporaryFile -Extension $invalidFileNameChar -ErrorAction Stop } | ShouldBeErrorId "NewTemporaryInvalidArgument,Microsoft.PowerShell.Commands.NewTemporaryFileCommand"
        }
    }

    It "with WhatIf does not create a file" {
        New-TemporaryFile -WhatIf | Should Be $null
    }

    It "has an OutputType of System.IO.FileInfo" {
        (Get-Command New-TemporaryFile).OutputType | Should Be "System.IO.FileInfo"
    }
}
