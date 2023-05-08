# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Environment-Variables" -Tags "CI" {

    It "Should have environment variables" {
        Get-Item ENV: | Should -Not -BeNullOrEmpty
    }

    It "Should have a nonempty PATH" {
        $ENV:PATH | Should -Not -BeNullOrEmpty
    }

    It "Should contain /bin in the PATH" {
        if ($IsWindows) {
            $ENV:PATH | Should -Match "C:"
        } else {
            $ENV:PATH | Should -Match "/bin"
        }
    }

    It "Should have the correct HOME" {
        if ($IsWindows) {
            if (!$ENV:HOMEPATH) {
                Set-ItResult -Skipped -Because "Homepath is not set"
            }

            # \Windows\System32 is found as $env:HOMEPATH for temporary profiles
            $expected = "\Users", "\Windows"
            Split-Path $ENV:HOMEPATH -Parent | Should -BeIn $expected
        } else {
            $expected = /bin/bash -c "cd ~ && pwd"
            $ENV:HOME | Should -Be $expected
        }
    }

    It "Should be able to set the environment variables" {
        $expected = "this is a test environment variable"
        { $ENV:TESTENVIRONMENTVARIABLE = $expected } | Should -Not -Throw

        $ENV:TESTENVIRONMENTVARIABLE | Should -Not -BeNullOrEmpty
        $ENV:TESTENVIRONMENTVARIABLE | Should -Be $expected

    }

    Context "~ in PATH" {
        AfterEach {
            $env:PATH = $oldPath
        }

        BeforeAll {
            $oldPath = $env:PATH
            $pwsh = (Get-Command pwsh | Select-Object -First 1).Source
            if ($IsWindows) {
                $pwsh2 = "pwsh2.exe"
            } else {
                $pwsh2 = "pwsh2"
            }

            Copy-Item -Path $pwsh -Destination "~/$pwsh2"
            $testPath = Join-Path -Path "~" -ChildPath (New-Guid)
            New-Item -Path $testPath -ItemType Directory > $null
            Copy-Item -Path $pwsh -Destination "$testPath/$pwsh2"
        }

        AfterAll {
            Remove-Item -Path "~/pwsh2" -Force
            Remove-Item -Path $testPath -Recurse -Force
        }

        It "Should be able to resolve ~ in PATH" {
            $env:PATH = "~" + [System.IO.Path]::PathSeparator + $env:PATH
            $out = Get-Command pwsh2
            $out.Source | Should -BeExactly (Join-Path -Path ([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)) -ChildPath $pwsh2)
        }

        It "Should be able to resolve ~/folder in PATH" {
            $env:PATH = $testPath + [System.IO.Path]::PathSeparator + $env:PATH
            $out = Get-Command pwsh2
            $out.Source | Should -BeExactly (Join-Path -Path (Resolve-Path $testPath) -ChildPath $pwsh2)
        }
    }
}
