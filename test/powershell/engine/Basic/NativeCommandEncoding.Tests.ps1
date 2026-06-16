# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Native command output encoding tests' -Tags 'CI' {
    BeforeAll {
        $WriteConsoleOutPath = Join-Path $PSScriptRoot assets WriteConsoleOut.ps1
        $defaultEncoding = [Console]::OutputEncoding.WebName
    }

    BeforeEach {
        Clear-Variable -Name PSApplicationOutputEncoding
    }

    AfterEach {
        Clear-Variable -Name PSApplicationOutputEncoding
    }

    It 'Defaults to [Console]::OutputEncoding if not set' {
        $actual = pwsh -File $WriteConsoleOutPath -Value café -Encoding $defaultEncoding
        $actual | Should -Be café
    }

    It 'Defaults to [Console]::OutputEncoding if set to $null' {
        $PSApplicationOutputEncoding = $null
        $actual = pwsh -File $WriteConsoleOutPath -Value café -Encoding $defaultEncoding
        $actual | Should -Be café
    }

    It 'Uses scoped $PSApplicationOutputEncoding value' {
        $PSApplicationOutputEncoding = [System.Text.Encoding]::Unicode
        $actual = & {
            $PSApplicationOutputEncoding = [System.Text.UTF8Encoding]::new()
            pwsh -File $WriteConsoleOutPath -Value café -Encoding utf-8
        }

        $actual | Should -Be café

        # Will use UTF-16-LE hence the different values
        $actual = pwsh -File $WriteConsoleOutPath -Value café -Encoding Unicode
        $actual | Should -Be café
    }

    It 'Uses variable in class method' {
        class NativeEncodingTestClass {
            static [string] RunTest([string]$Script) {
                $PSApplicationOutputEncoding = [System.Text.Encoding]::Unicode
                return pwsh -File $Script -Value café -Encoding unicode
            }
        }

        $actual = [NativeEncodingTestClass]::RunTest($WriteConsoleOutPath)
        $actual | Should -Be café
    }

    It 'Fails to set variable with invalid encoding object' {
        $ps = [PowerShell]::Create()
        $ps.AddScript('$PSApplicationOutputEncoding = "utf-8"').Invoke()

        $ps.Streams.Error.Count | Should -Be 1
        [string]$ps.Streams.Error[0] | Should -Be 'Cannot convert the "utf-8" value of type "System.String" to type "System.Text.Encoding".'
    }
}
