# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Convert-Path tests" -Tag CI {
    BeforeDiscovery {
        $hiddenFilePrefixDisc = ($IsLinux -or $IsMacOS) ? '.' : ''
        $sep = [System.IO.Path]::DirectorySeparatorChar

        $relativeHiddenFilePath1Disc = ".$($sep)$($hiddenFilePrefixDisc)test1.txt"
        $relativeHiddenFilePath2Disc = ".$($sep)$($hiddenFilePrefixDisc)test2.txt"
        $relativeHiddenFileWildcardPathDisc = ".$($sep)$($hiddenFilePrefixDisc)test*.txt"
    }

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
        $d1 = New-Item -ItemType Directory -Path (Join-Path $TestDrive 'dir1') -Force
        $d2 = New-Item -ItemType Directory -Path (Join-Path $TestDrive 'dir2') -Force
        $result = Convert-Path -Path "${TestDrive}/dir?"
        $result.count | Should -Be 2
        $result -join "," | Should -BeExactly (@("$d1","$d2") -join ",")
    }

    It "Convert-Path should return something which exists" {
        Convert-Path -Path $TestDrive | Should -Exist
    }

    It "Convert-Path -Path '<Path>' -Force:<Force> should return '<ExpectedResult>'" -TestCases @(
        @{
            Path           = $relativeHiddenFilePath1Disc
            Force          = $false
        }
        @{
            Path           = $relativeHiddenFilePath2Disc
            Force          = $false
        }
        @{
            Path           = $relativeHiddenFileWildcardPathDisc
            Force          = $false
            IsWildcardNoForce = $true
        }
        @{
            Path           = $relativeHiddenFilePath1Disc
            Force          = $true
        }
        @{
            Path           = $relativeHiddenFilePath2Disc
            Force          = $true
        }
        @{
            Path           = $relativeHiddenFileWildcardPathDisc
            Force          = $true
            IsWildcardForce = $true
        }
    ) {
        param($Path, $Force, $IsWildcardNoForce, $IsWildcardForce)
        try {
            Push-Location -Path $TestDrive
            if ($IsWildcardNoForce) {
                Convert-Path -Path $Path -Force:$Force | Should -BeNullOrEmpty
            } elseif ($IsWildcardForce) {
                $result = Convert-Path -Path $Path -Force:$Force
                $result | Should -HaveCount 2
            } else {
                $resolved = (Resolve-Path -Path $Path).Path
                Convert-Path -Path $Path -Force:$Force | Should -BeExactly $resolved
            }
        }
        finally {
            Pop-Location
        }
    }
}
