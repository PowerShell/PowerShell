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

    It "has an OutputType of System.IO.DirectoryInfo" {
        (Get-Command New-TemporaryDirectory).OutputType.Name | Should -Contain "System.IO.DirectoryInfo"
    }
}
