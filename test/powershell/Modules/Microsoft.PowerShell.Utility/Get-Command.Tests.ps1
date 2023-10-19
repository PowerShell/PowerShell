# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Command Feature tests" -Tag Feature {
    Context "-UseFuzzyMatch tests" {
        It "Should match cmdlets" {
            $cmds = Get-Command get-hlp -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds[0].Name | Should -BeExactly 'Get-Help' -Because "This should be closest match so shows up first"
            $cmds[0].Score | Should -Be 1
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

        It "Should use minimum distance" {
            $cmds = Get-Command get-hlp -UseFuzzyMatch -FuzzyMinimumDistance 3
            $cmds.Count | Should -BeGreaterThan 0
            foreach ($cmd in $cmds) {
                $cmd.Score | Should -BeLessOrEqual 3
            }
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
        }

        It "Can return multiple results relying on auto module loading" {
            $results = & "$PSHOME/pwsh" -outputformat xml -command "`$env:PSModulePath += '$testPSModulePath'; Get-Command i-fzz -UseAbbreviationExpansion"
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

            $params = @{
                UseAbbreviationExpansion = $true;
                Name = $name;
            }

            if ($module) {
                $params += @{ Module = $module }
            }

            $results = Get-Command @params
            $results | Should -HaveCount 1
            $results.Name | Should -BeExactly $expected
        }

        It "Can return multiple results for cmdlets matching abbreviation" {
            # use mixed casing to validate case insensitivity
            $results = Get-Command i-C -UseAbbreviationExpansion
            $results.Name | Should -Contain "Invoke-Command"
            $results.Name | Should -Contain "Import-Clixml"
            $results.Name | Should -Contain "Import-Csv"
        }

        It "Will return multiple results for functions matching abbreviation" {
            $manifestPath = Join-Path $testdrive "test.psd1"
            $modulePath = Join-Path $testdrive "test.psm1"

            New-ModuleManifest -Path $manifestPath -FunctionsToExport "Get-FoodBar","Get-FB" -RootModule test.psm1
            @"
            function Get-FoodBar { "foodbar" }
            function Get-FB { "fb" }
"@ > $modulePath

            try {
                Import-Module $manifestPath
                $results = Get-Command g-fb -UseAbbreviationExpansion
                $results | Should -HaveCount 2
                $results[0].Name | Should -BeIn "Get-FB","Get-FoodBar"
                $results[1].Name | Should -BeIn "Get-FB","Get-FoodBar"
                $results[0].Name | Should -Not -Be $results[1].Name
            }
            finally {
                Remove-Module test
            }
        }

        It "Non-existing cmdlets returns non-terminating error" {
            { Get-Command g-adf -ErrorAction Stop } | Should -Throw -ErrorId "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand"
        }

        It "No results if wildcard is used" {
            Get-Command i-psd* -UseAbbreviationExpansion | Should -BeNullOrEmpty
        }
    }
}

Describe "Get-Command" -Tag CI {
    BeforeAll {
        Import-Module Microsoft.PowerShell.Management
    }

    Context "Prefix tests" {
        It "Should not find the non-prefixed commands" {
            $TestModulePath = Join-Path -Path $testdrive PrefixModuleTest
            $ModuleFilePath = Join-Path -Path $TestModulePath PrefixModuleTest.psm1
            $null = New-Item -Path $TestModulePath -Force -ItemType Directory
            "function Test-DemoCommand1 {}" | Set-Content -Path $ModuleFilePath -Force
            Import-Module $ModuleFilePath -Prefix PrefixTest
            Get-Command -Name Test-DemoCommand1* -All | Should -HaveCount 0
            {Get-Command -Name Test-DemoCommand1 -All -ErrorAction Stop} | Should -Throw -ErrorId 'CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand'
        }
    }

    Context "-Syntax tests" {
        It "Should return a string object when -Name is an alias and -Syntax is specified" {
            $Result = Get-Command -Name del -Syntax

            $Result | Should -BeOfType [String]
            $Result | Should -Match 'del \[-Path\]'
        }

        It "Should replace commands with aliases in matching commands when using a wildcard search" {
            $Result = Get-Command -Name sp* -Syntax

            $Result | Should -BeOfType [String]
            $Result -join '' | Should -Match 'sp \(alias\) -> Set-ItemProperty'
            $Result -join '' | Should -Match 'sp \[-Path\]'
        }

        It "Should not add the alias (alias) -> command decorator for non-alias commands" {
            $Result = Get-Command -Name sp* -Syntax

            $Result -join '' | Should -Not -Match 'Split-Path \(alias\) -> Split-Path'
            $Result -join '' | Should -Match 'Split-Path \[-Path\]'
        }

        It "Should only replace aliases when given multiple entries including a command and an alias" {
            $Result = Get-Command -Name get-help, del -Syntax

            $Result -join '' | Should -Match 'del \(alias\) -> Remove-Item'
            $Result -join '' | Should -Match 'del \[-Path\]'
            $Result -join '' | Should -Match 'Get-Help \[\[-Name\]'
            $Result -join '' | Should -Not -Match 'del \(alias\) -> Get-Help'
        }

        It "Should return the path to an aliased script when -Syntax is specified" {
            # First, create a script file
            $TestGcmSyntax = @'
            [CmdletBinding()]
            param(
                [Parameter(Position=0, Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
                [ValidateNotNullOrEmpty()]
                [string[]]
                $Name
            )
            process {
                "Processing ${Name}"
            }
'@
            Set-Content -Path TestDrive:\Test-GcmSyntax.ps1 -Value $TestGcmSyntax

            # Now set up an alias for that file
            New-Alias -Name tgs -Value TestDrive:\Test-GcmSyntax.ps1

            $Result = Get-Command -Name tgs -Syntax

            $Result | Should -Match "tgs \(alias\) -> $([Regex]::Escape((Get-Item TestDrive:\\Test-GcmSyntax.ps1).FullName))"
        }
    }

    Context "-Name tests" {
        It "Should return a AliasInfo object when -Name is an alias" {
            $Result = Get-Command -Name del

            $Result | Should -BeOfType [System.Management.Automation.AliasInfo]
            $Result.DisplayName | Should -Be 'del -> Remove-Item'
        }

        It "Should return a CommandInfo object when -Name is a command" {
            $Result = Get-Command -Name Remove-Item

            $Result | Should -BeOfType [System.Management.Automation.CommandInfo]
        }

        It "Throws '<expected>' exception if '<Name>' name is used" -TestCases @(
            @{ Name = 'space'; Value = ' '; expected = "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand" }
            @{ Name = 'empty'; Value = ''; expected = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetCommandCommand" }
            @{ Name = 'null'; Value = $null; expected = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetCommandCommand" }
        ) {
            param($value, $expected)
            { Get-Command $value -ErrorAction Stop } | Should -Throw -ErrorId $expected
        }
    }
}
