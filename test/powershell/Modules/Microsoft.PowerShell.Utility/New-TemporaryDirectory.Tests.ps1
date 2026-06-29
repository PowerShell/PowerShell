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

    Context "Basic creation" {
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
    }

    Context "Prefix parameter" {
        It "creates a directory with the specified prefix" {
            $tempDirectory = New-TemporaryDirectory -Prefix "TestPrefix_"

            $tempDirectory | Should -Exist
            $tempDirectory | Should -BeOfType System.IO.DirectoryInfo
            $tempDirectory.Name | Should -BeLike "TestPrefix_*"
            $tempDirectory.FullName | Should -BeLikeExactly "$([System.IO.Path]::GetTempPath())*"
        }

        It "creates unique directories with the same prefix" {
            $first = New-TemporaryDirectory -Prefix "MyApp_"
            $second = New-TemporaryDirectory -Prefix "MyApp_"

            try {
                $first.FullName | Should -Not -BeExactly $second.FullName
                $first.Name | Should -BeLike "MyApp_*"
                $second.Name | Should -BeLike "MyApp_*"
            }
            finally {
                if ($first -and (Test-Path -LiteralPath $first.FullName)) {
                    Remove-Item -LiteralPath $first.FullName -Force -Recurse -ErrorAction SilentlyContinue
                }
                if ($second -and (Test-Path -LiteralPath $second.FullName)) {
                    Remove-Item -LiteralPath $second.FullName -Force -Recurse -ErrorAction SilentlyContinue
                }
            }
        }
    }

    Context "ShouldProcess support" {
        It "with WhatIf does not create a directory" {
            New-TemporaryDirectory -WhatIf | Should -BeNullOrEmpty
        }

        It "with WhatIf and Prefix does not create a directory" {
            New-TemporaryDirectory -Prefix "Test_" -WhatIf | Should -BeNullOrEmpty
        }
    }

    Context "OutputType" {
        It "has an OutputType of System.IO.DirectoryInfo" {
            (Get-Command New-TemporaryDirectory).OutputType.Name | Should -Contain "System.IO.DirectoryInfo"
        }
    }
}
