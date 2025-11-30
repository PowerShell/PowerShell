# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Microsoft.WinGet.CommandNotFound" -tags "CI" {
    BeforeAll {
        if (Get-Module Microsoft.WinGet.CommandNotFound) {
            Remove-Module Microsoft.WinGet.CommandNotFound
        }
    }

    Context "Windows only" {
        # Microsoft.WinGet.CommandNotFound relies on winget "being available" by checking if Get-Command returns a result
        Mock Get-Command -ParameterFilter { $cmd -eq "winget" } -MockWith { return Get-Command * }

        It "Should import the module correctly" -Skip:(-not $IsWindows) {
            try {
                # define an alias to simulate winget being available on the system
                New-Alias -Name winget -Value Get-ChildItem
                $module = Import-Module -Name Microsoft.WinGet.CommandNotFound -PassThru
                $module.Name | Should -BeExactly 'Microsoft.WinGet.CommandNotFound'
                $module.Version | Should -BeGreaterThan "1.0.4"
            }
            finally {
                Get-Module -Name Microsoft.WinGet.CommandNotFound | Remove-Module -Force
            }
        }

        It "Should be installed to `$PSHOME" -Skip:(-not $IsWindows) {
            $module = Get-Module (Join-Path -Path $PSHOME -ChildPath "Modules" -AdditionalChildPath "Microsoft.WinGet.CommandNotFound") -ListAvailable
            $module.Name | Should -BeExactly 'Microsoft.WinGet.CommandNotFound'
            $module.Version | Should -BeGreaterThan "1.0.4"
            $module.Path | Should -Be (Join-Path -Path $PSHOME -ChildPath "Modules/Microsoft.WinGet.CommandNotFound/Microsoft.WinGet.CommandNotFound.psd1")
        }
    }

    Context "Linux and macOS only" {
        It "Should not be installed" -Skip:($IsWindows) {
            Get-Module -Name Microsoft.WinGet.CommandNotFound -ListAvailable | Should -BeNullOrEmpty
        }
    }
}
