# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-Command CI tests" -Tag Feature {
    Context "-UseFuzzyMatch tests" {
        It "Should match cmdlets" {
            $cmds = Get-Command get-hlp -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds[0].Name | Should -BeExactly 'Get-Help' -Because "This should be closest match so shows up first"
        }

        It "Should match native commands" {
            $ping = "ping"
            if ($IsWindows) {
                $ping = "PING.EXE"
            }

            $cmds = Get-Command pin -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds.Name | Should -Contain $ping
        }
    }

    Context "-UseAbbreviationExpansion tests" {
        BeforeAll {
            $configFilePath = Join-Path $testdrive "useabbreviationexpansion.json"

            @"
            {
                "ExperimentalFeatures": [
                  "PSUseAbbreviationExpansion"
                ]
            }
"@ > $configFilePath

        }

        It "Valid cmdlets works with name <name> and module <module>" -TestCases @(
            @{ Name = "i-psdf"; expected = "Import-PowerShellDataFile"; module = $null },
            @{ Name = "i-psdf"; expected = "Import-PowerShellDataFile"; module = "Microsoft.PowerShell.Utility" },
            @{ Name = "r-psb" ; expected = "Remove-PSBreakpoint"      ; module = $null },
            @{ Name = "r-psb" ; expected = "Remove-PSBreakpoint"      ; module = "Microsoft.PowerShell.Utility" }
        ) {
            param($name, $expected, $module)

            $command = "Get-Command $name -UseAbbreviationExpansion"

            if ($module) {
                $command += " -Module $module"
            }

            $results = pwsh -settingsfile $configFilePath -c "$command | ConvertTo-Json" | ConvertFrom-Json
            $results | Should -HaveCount 1
            $results.Name | Should -BeExactly $expected
        }

        It "Can return multiple results for cmdlets matching abbreviation" {
            $results = pwsh -settingsfile $configFilePath -c "Get-Command i-C -UseAbbreviationExpansion | ConvertTo-Json" | ConvertFrom-Json
            $results | Should -HaveCount 3
            $results[0].Name | Should -BeExactly "Invoke-Command"
            $results[1].Name | Should -BeExactly "Import-Clixml"
            $results[2].Name | Should -BeExactly "Import-Csv"
        }

        It "Will return multiple results for functions matching abbreviation" {
            $manifestPath = Join-Path $testdrive "test.psd1"
            $modulePath = Join-Path $testdrive "test.psm1"

            New-ModuleManifest -Path $manifestPath -FunctionsToExport "Get-FooBar","Get-FB" -RootModule test.psm1
            @"
            function Get-FooBar { "foobar" }
            function Get-FB { "fb" }
"@ > $modulePath

            $results = pwsh -settingsfile $configFilePath -c "Import-Module $manifestPath; Get-Command g-fb -UseAbbreviationExpansion | ConvertTo-Json" | ConvertFrom-Json
            $results | Should -HaveCount 2
            $results[0].Name | Should -BeExactly "Get-FB"
            $results[1].Name | Should -BeExactly "Get-FooBar"
        }

        It "Non-existing cmdlets returns non-terminating error" {
            pwsh -settingsfile $configFilePath -c 'try { get-command g-adf -ea stop } catch { $_.fullyqualifiederrorid }' | Should -BeExactly "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand"
        }

        It "No results if wildcard is used" {
            pwsh -settingsfile $configFilePath -c Get-Command i-psd* -UseAbbreviationExpansion | Should -BeNullOrEmpty
        }
    }
}
