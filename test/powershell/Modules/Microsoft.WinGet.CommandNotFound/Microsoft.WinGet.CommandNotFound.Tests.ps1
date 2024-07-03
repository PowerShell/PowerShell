# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Microsoft.WinGet.CommandNotFound" -tags "CI" {

    Context "Windows only" -Skip:(-not $IsWindows) {
        It "Should import the module correctly" {
            Import-Module -Name Microsoft.WinGet.CommandNotFound
            $module = Get-Module Microsoft.WinGet.CommandNotFound
            $module.Name | Should -BeExactly 'Microsoft.WinGet.CommandNotFound'
            $module.Version | Should -Match '^1.0.4$'
        }

        It "Should be installed to `$PSHOME" {
            $module = Get-Module (Join-Path -Path $PSHOME -ChildPath "Modules" -AdditionalChildPath "Microsoft.WinGet.CommandNotFound") -ListAvailable
            $module.Name | Should -BeExactly 'Microsoft.WinGet.CommandNotFound'
            $module.Version | Should -Match '^1.0.4$'
            $module.Path | Should -Be (Join-Path -Path $PSHOME -ChildPath "Modules/Microsoft.WinGet.CommandNotFound/Microsoft.WinGet.CommandNotFound.psd1")
        }
    }

    Context "Linux and macOS only" -Skip:($IsWindows) {
        It "Should not be installed to `$PSHOME" {
            Test-Path (Join-Path -Path $PSHOME -ChildPath "Modules" -AdditionalChildPath "Microsoft.WinGet.CommandNotFound") | Should -Be $False
        }
    }
}
