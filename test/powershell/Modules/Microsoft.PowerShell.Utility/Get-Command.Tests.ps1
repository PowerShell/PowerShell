# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-Command Feature tests" -Tag Feature {
    Context "-UseFuzzyMatch tests" {
        It "Should match cmdlets" {
            $cmds = Get-Command get-hlp -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds[0].Name | Should -BeExactly 'Get-Help' -Because "This should be closest match so shows up first"
        }

        It "Should match native commands" {
            $input = "pwsg"
            $expectedcmd = "pwsh"
            if ($IsWindows) {
                $expectedcmd = "pwsh.EXE"
            }

            $cmds = Get-Command $input -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds.Name | Should -Contain $expectedcmd
        }
    }

    Context "-UseAbbreviationExpansion tests" {
        BeforeAll {
            $testModulesPath = Join-Path $testdrive "Modules"
            $testPSModulePath = [System.IO.Path]::PathSeparator + $testModulesPath
            $null = New-Item -ItemType Directory -Path $testModulesPath
            $null = New-Item -ItemType Directory -Path (Join-Path $testModulesPath "test1")
            $null = New-Item -ItemType Directory -Path (Join-Path $testModulesPath "test2")

            Set-Content -Path (Join-Path $testModulesPath "test1/test1.psm1") -Value "function Import-FooZedZed {}"
            Set-Content -Path (Join-Path $testModulesPath "test2/test2.psm1") -Value "function Invoke-FooZedZed {}"

            $configFilePath = Join-Path $testdrive "useabbreviationexpansion.json"

            @"
            {
                "ExperimentalFeatures": [
                  "PSUseAbbreviationExpansion"
                ]
            }
"@ > $configFilePath

        }

        It "Can return multiple results relying on auto module loading" {
            $results = pwsh -outputformat xml -settingsfile $configFilePath -command "`$env:PSModulePath += '$testPSModulePath'; Get-Command i-fzz -UseAbbreviationExpansion"
            $results | Should -HaveCount 2
            $results.Name | Should -Contain "Invoke-FooZedZed"
            $results.Name | Should -Contain "Import-FooZedZed"
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

            $results = pwsh -outputformat xml -settingsfile $configFilePath -command "$command"
            $results | Should -HaveCount 1
            $results.Name | Should -BeExactly $expected
        }

        It "Can return multiple results for cmdlets matching abbreviation" {
            # use mixed casing to validate case insensitivity
            $results = pwsh -outputformat xml -settingsfile $configFilePath -command "Get-Command i-C -UseAbbreviationExpansion"
            $results.Count | Should -BeGreaterOrEqual 3
            $results.Name | Should -Contain "Invoke-Command"
            $results.Name | Should -Contain "Import-Clixml"
            $results.Name | Should -Contain "Import-Csv"
        }

        It "Will return multiple results for functions matching abbreviation" {
            $manifestPath = Join-Path $testdrive "test.psd1"
            $modulePath = Join-Path $testdrive "test.psm1"

            New-ModuleManifest -Path $manifestPath -FunctionsToExport "Get-FooBar","Get-FB" -RootModule test.psm1
            @"
            function Get-FooBar { "foobar" }
            function Get-FB { "fb" }
"@ > $modulePath

            $results = pwsh -outputformat xml -settingsfile $configFilePath -command "Import-Module $manifestPath; Get-Command g-fb -UseAbbreviationExpansion"
            $results | Should -HaveCount 2
            $results[0].Name | Should -BeExactly "Get-FB"
            $results[1].Name | Should -BeExactly "Get-FooBar"
        }

        It "Non-existing cmdlets returns non-terminating error" {
            pwsh -settingsfile $configFilePath -command 'try { get-command g-adf -ea stop } catch { $_.fullyqualifiederrorid }' | Should -BeExactly "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand"
        }

        It "No results if wildcard is used" {
            pwsh -settingsfile $configFilePath -command Get-Command i-psd* -UseAbbreviationExpansion | Should -BeNullOrEmpty
        }
    }
}
