# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Convert-Path tests" -Tag CI {
    BeforeAll {
        $hiddenFilePrefix = ($IsLinux -or $IsMacOS) ? '.' : ''

        $hiddenFilePath1 = Join-Path -Path $TestDrive -ChildPath "$($hiddenFilePrefix)test1.txt"
        $hiddenFilePath2 = Join-Path -Path $TestDrive -ChildPath "$($hiddenFilePrefix)test2.txt"

        $hiddenFile1 = New-Item -Path $hiddenFilePath1 -ItemType File
        $hiddenFile2 = New-Item -Path $hiddenFilePath2 -ItemType File

        $relativeHiddenFilePath1 = ".$([System.IO.Path]::DirectorySeparatorChar)$($hiddenFilePrefix)test1.txt"
        $relativeHiddenFilePath2 = ".$([System.IO.Path]::DirectorySeparatorChar)$($hiddenFilePrefix)test2.txt"

        if ($IsWindows) {
            $hiddenFile1.Attributes = "Hidden"
            $hiddenFile2.Attributes = "Hidden"
        }

        $hiddenFileWildcardPath = Join-Path -Path $TestDrive -ChildPath "$($hiddenFilePrefix)test*.txt"
        $relativeHiddenFileWildcardPath = ".$([System.IO.Path]::DirectorySeparatorChar)$($hiddenFilePrefix)test*.txt"
    }

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

    It "Convert-Path -Path '<Path>' -Force:<Force> should return '<ExpectedResult>'" -TestCases @(
        @{
            Path           = $relativeHiddenFilePath1
            BasePath       = $TestDrive
            Force          = $false
            ExpectedResult = $hiddenFilePath1
        }
        @{
            Path           = $relativeHiddenFilePath2
            BasePath       = $TestDrive
            Force          = $false
            ExpectedResult = $hiddenFilePath2
        }
        @{
            Path           = $relativeHiddenFileWildcardPath
            BasePath       = $TestDrive
            Force          = $false
            ExpectedResult = $null
        }
        @{
            Path           = $relativeHiddenFilePath1
            BasePath       = $TestDrive
            Force          = $true
            ExpectedResult = $hiddenFilePath1
        }
        @{
            Path           = $relativeHiddenFilePath2
            BasePath       = $TestDrive
            Force          = $true
            ExpectedResult = $hiddenFilePath2
        }
        @{
            Path           = $relativeHiddenFileWildcardPath
            BasePath       = $TestDrive
            Force          = $true
            ExpectedResult = @($hiddenFilePath1, $hiddenFilePath2)
        }
    ) {
        param($Path, $BasePath, $Force, $ExpectedResult)
        try {
            Push-Location -Path $BasePath
            Convert-Path -Path $Path -Force:$Force | Should -BeExactly $ExpectedResult
        }
        finally {
            Pop-Location
        }
    }
}
