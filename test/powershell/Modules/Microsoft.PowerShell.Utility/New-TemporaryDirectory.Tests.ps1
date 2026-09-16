# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "New-TemporaryDirectory" -Tags "CI" {
    BeforeEach {
        $tempDirectory = $null
    }

    AfterEach {
        if ($tempDirectory -and (Test-Path -LiteralPath $tempDirectory.FullName)) {
            Remove-Item -LiteralPath $tempDirectory.FullName -Force -Recurse -ErrorAction SilentlyContinue
        }
    }

    It "creates a new temporary directory" {
        $tempDirectory = New-TemporaryDirectory

        $tempDirectory | Should -Exist
        $tempDirectory | Should -BeOfType System.IO.DirectoryInfo
        $tempDirectory.FullName | Should -BeLikeExactly "$([System.IO.Path]::GetTempPath())*"
        (Get-Item -LiteralPath $tempDirectory.FullName).PSIsContainer | Should -BeTrue
    }

    It "creates unique temporary directories" {
        $tempDirectory = New-TemporaryDirectory
        $secondTempDirectory = New-TemporaryDirectory

        try {
            $secondTempDirectory.FullName | Should -Not -BeExactly $tempDirectory.FullName
            $secondTempDirectory | Should -Exist
            $secondTempDirectory | Should -BeOfType System.IO.DirectoryInfo
        }
        finally {
            if ($secondTempDirectory -and (Test-Path -LiteralPath $secondTempDirectory.FullName)) {
                Remove-Item -LiteralPath $secondTempDirectory.FullName -Force -Recurse -ErrorAction SilentlyContinue
            }
        }
    }

    It "with WhatIf does not create a directory" {
        New-TemporaryDirectory -WhatIf | Should -BeNullOrEmpty
    }

    It "with WhatIf and Prefix does not create a directory" {
        New-TemporaryDirectory -Prefix "Test_" -WhatIf | Should -BeNullOrEmpty
    }

    It "with WhatIf and Prefix reports a descriptive target" {
        $prefix = "Test_"
        $rawTarget = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), $prefix)
        $whatIfOutput = & "$PSHOME/pwsh" -NoProfile -CommandWithArgs 'param($prefix) New-TemporaryDirectory -Prefix $prefix -WhatIf' $prefix 2>&1 | Out-String

        $whatIfOutput | Should -Not -Match ([regex]::Escape("`"$rawTarget`""))
        $whatIfOutput | Should -Match ([regex]::Escape("with prefix '$prefix'"))
    }

    It "rejects special relative path Prefix '<Prefix>'" -TestCases @(
        @{ Prefix = "." }
        @{ Prefix = ".." }
    ) {
        param($Prefix)

        { New-TemporaryDirectory -Prefix $Prefix -ErrorAction Stop } | Should -Throw -ErrorId "NewTemporaryDirectoryInvalidPrefix,Microsoft.PowerShell.Commands.NewTemporaryDirectoryCommand"
    }

    It "rejects separator-like Prefix '<Prefix>'" -TestCases @(
        @{ Prefix = "path/name" }
        @{ Prefix = "path\name" }
        @{ Prefix = "../name" }
        @{ Prefix = "..\name" }
    ) {
        param($Prefix)

        { New-TemporaryDirectory -Prefix $Prefix -ErrorAction Stop } | Should -Throw -ErrorId "NewTemporaryDirectoryInvalidPrefix,Microsoft.PowerShell.Commands.NewTemporaryDirectoryCommand"
    }

    It "rejects Prefix with control character '<CharacterCode>'" -TestCases @(
        @{ CharacterCode = 10 }
        @{ CharacterCode = 13 }
        @{ CharacterCode = 9 }
        @{ CharacterCode = 27 }
    ) {
        param($CharacterCode)

        $Prefix = "bad$([char]$CharacterCode)name"
        { New-TemporaryDirectory -Prefix $Prefix -ErrorAction Stop } | Should -Throw -ErrorId "NewTemporaryDirectoryInvalidPrefix,Microsoft.PowerShell.Commands.NewTemporaryDirectoryCommand"
    }

    It "has an OutputType of System.IO.DirectoryInfo" {
        (Get-Command New-TemporaryDirectory).OutputType.Name | Should -Contain "System.IO.DirectoryInfo"
    }

    It "creates a directory with custom prefix" {
        $prefix = "MyApp"
        $tempDirectory = New-TemporaryDirectory -Prefix $prefix

        $tempDirectory | Should -Exist
        $tempDirectory | Should -BeOfType System.IO.DirectoryInfo
        $tempDirectory.Name | Should -BeLike "$prefix*"
    }

    It "rejects Prefix with invalid file name characters" {
        $invalidChars = [System.IO.Path]::GetInvalidFileNameChars()

        if ($invalidChars.Count -gt 0) {
            $badPrefix = "bad$($invalidChars[0])prefix"
            { New-TemporaryDirectory -Prefix $badPrefix -ErrorAction Stop } | Should -Throw -ErrorId "NewTemporaryDirectoryInvalidPrefix,Microsoft.PowerShell.Commands.NewTemporaryDirectoryCommand"
        }
    }
}
