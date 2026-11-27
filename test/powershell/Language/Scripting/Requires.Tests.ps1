# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Requires tests" -Tags "CI" {
    Context "Parser error" {

        $testcases = @(
                        @{command = "#requiresappID`r`n`$foo = 1; `$foo" ; testname = "appId with newline"}
                        @{command = "#requires -version A `r`n`$foo = 1; `$foo" ; testname = "version as character"}
                        @{command = "#requires -version 2b `r`n`$foo = 1; `$foo" ; testname = "alphanumeric version"}
                        @{command = "#requires -version 1. `r`n`$foo = 1; `$foo" ; testname = "version with dot"}
                        @{command = "#requires -version '' `r`n`$foo = 1; `$foo" ; testname = "empty version"}
                        @{command = "#requires -version 1.0. `r`n`$foo = 1; `$foo" ; testname = "version with two dots"}
                        @{command = "#requires -version 1.A `r`n`$foo = 1; `$foo" ; testname = "alphanumeric version with dots"}
                    )

        It "throws ParserException - <testname>" -TestCases $testcases {
            param($command)
            { [scriptblock]::Create($command) } | Should -Throw -ErrorId "ParseException"
        }
    }

    Context "Issue#20107 - Requires parameter validation" {
        It "Rejects an unknown #requires parameter before running the script" {
            $scriptPath = Join-Path $TestDrive 'UnknownRequires.ps1'
            $outputPath = Join-Path $TestDrive 'ShouldNotExist.txt'
            Set-Content -LiteralPath $scriptPath -Value "#requires -RunAsAdminstrator`nSet-Content -LiteralPath '$outputPath' -Value 'ran'"

            { & $scriptPath } | Should -Throw -ErrorId 'RequiresUnknownParameter'
            $outputPath | Should -Not -Exist
        }

        It "Reports an unknown parameter for '<ParameterName>' with <ArgumentKind>" -TestCases @(
            @{ ParameterName = 'RunAsAdminstrator'; Argument = ''; ArgumentKind = 'no argument' }
            @{ ParameterName = 'RunAsAdminstrator'; Argument = ' ignored'; ArgumentKind = 'a separate argument' }
            @{ ParameterName = 'RunAsAdminstrator'; Argument = ':ignored'; ArgumentKind = 'an inline argument' }
            @{ ParameterName = 'RunAsAdminstrator'; Argument = ' $value'; ArgumentKind = 'a variable argument' }
            @{ ParameterName = 'RunAsAdminstrator'; Argument = ':$value'; ArgumentKind = 'an inline variable argument' }
            @{ ParameterName = 'Unknown'; Argument = ''; ArgumentKind = 'no argument' }
            @{ ParameterName = 'Verson'; Argument = ' 7.0'; ArgumentKind = 'a separate argument' }
        ) {
            param([string]$ParameterName, [string]$Argument, [string]$ArgumentKind)

            $parseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput(
                "#requires -$ParameterName$Argument", [ref]$null, [ref]$parseErrors)

            $parseErrors | Should -HaveCount 1
            $parseErrors[0].ErrorId | Should -BeExactly 'RequiresUnknownParameter'
            $parseErrors[0].Extent.Text | Should -BeLike "-$ParameterName*"
            $parseErrors[0].Message | Should -Match ([regex]::Escape($ParameterName))
        }

        It "Preserves <ExpectedErrorId> for '<Script>'" -TestCases @(
            @{ Script = '#requires -ShellId'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -s'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -PSSnapin'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -pss'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -Version'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -v'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -PSEdition'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -pse'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -Assembly'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -a'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -Modules'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -m'; ExpectedErrorId = 'ParameterRequiresArgument' }
            @{ Script = '#requires -Version $value'; ExpectedErrorId = 'RequiresArgumentMustBeConstant' }
            @{ Script = '#requires -Modules $value'; ExpectedErrorId = 'RequiresArgumentMustBeConstant' }
            @{ Script = '#requires -RunAsAdministrator ignored'; ExpectedErrorId = 'ParameterCannotHaveArgument' }
            @{ Script = '#requires -r:ignored'; ExpectedErrorId = 'ParameterCannotHaveArgument' }
        ) {
            param([string]$Script, [string]$ExpectedErrorId)

            $parseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput($Script, [ref]$null, [ref]$parseErrors)

            $parseErrors | Should -HaveCount 1
            $parseErrors[0].ErrorId | Should -BeExactly $ExpectedErrorId
        }

        It "Accepts the supported abbreviation or casing in '<Script>'" -TestCases @(
            @{ Script = '#requires -s Microsoft.PowerShell' }
            @{ Script = '#requires -pss TestSnapin' }
            @{ Script = '#requires -v 7.0' }
            @{ Script = '#requires -pse Core' }
            @{ Script = '#requires -a System.Runtime' }
            @{ Script = '#requires -m TestModule' }
            @{ Script = '#requires -r' }
            @{ Script = '#requires -VERSION 7.0' }
            @{ Script = '#requires -RUNASADMINISTRATOR' }
        ) {
            param([string]$Script)

            $parseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput($Script, [ref]$null, [ref]$parseErrors)

            $parseErrors | Should -HaveCount 0
        }
    }

    Context "Interactive requires" {

        BeforeAll {
            $ps = [powershell]::Create()
        }

        AfterAll {
            $ps.Dispose()
        }

        It "Successfully does nothing when given '#requires' interactively" {
            $settings = [System.Management.Automation.PSInvocationSettings]::new()
            $settings.AddToHistory = $true

            { $ps.AddScript("#requires").Invoke(@(), $settings) } | Should -Not -Throw
        }
    }

    Context "Version checks" {
        BeforeAll {
            $currentVersion = $PSVersionTable.PSVersion

            $powerShellVersions = "1.0", "2.0", "3.0", "4.0", "5.0", "5.1", "6.0", "6.1", "6.2", "7.0", "7.1", "7.2", "7.3", "7.4", "7.5", "7.6", "7.7", "7.8"
            $latestVersion = [version]($powerShellVersions | Sort-Object -Descending -Top 1)
            $nonExistingMinor = "$($latestVersion.Major).$($latestVersion.Minor + 1)"
            $nonExistingMajor = "$($latestVersion.Major + 1).0"

            foreach ($version in ($powerShellVersions + $nonExistingMinor + $nonExistingMajor)) {
                $filePath = Join-Path -Path $TestDrive -ChildPath "$version.ps1"
                $null = New-Item -Path $filePath -Value "#requires -version $version"
            }

            $filesSuccessTestCase = foreach ($version in $powerShellVersions) {
                @{
                    Name = "Check for version $version"
                    File = Join-Path -Path $TestDrive -ChildPath "$version.ps1"
                    Version = $version
                }
            }

            $filesFailTestCase = foreach ($version in @($nonExistingMinor) + @($nonExistingMajor)) {
                @{
                    Name = "Check for version $version"
                    File = Join-Path -Path $TestDrive -ChildPath "$version.ps1"
                }
            }
        }

        It "<Name>" -TestCase $filesSuccessTestCase {
            param(
                $Name,
                $File,
                $Version
            )

            if ($currentVersion -notmatch '^7' -and $Version -match '^7') {
                Set-ItResult -Skipped -Because "Test not valid for current version - $currentVersion and test version = $Version"
            }

            { . $File } | Should -Not -Throw
        }

        It "<Name>" -TestCase $filesFailTestCase {
            param(
                $Name,
                $File
            )

            { . $File } | Should -Throw -ExceptionType ([System.Management.Automation.ScriptRequiresException])
        }
    }
}

Describe "#requires -Modules" -Tags "CI" {
    BeforeAll {
        $success = 'SUCCESS'

        $sep = [System.IO.Path]::DirectorySeparatorChar
        $altSep = [System.IO.Path]::AltDirectorySeparatorChar

        $scriptPath = Join-Path $TestDrive 'script.ps1'

        $moduleName = 'Banana'
        $moduleVersion = '0.12.1'
        $moduleDirPath = Join-Path $TestDrive 'modules'
        New-Item -Path $moduleDirPath -ItemType Directory
        $modulePath = "$moduleDirPath${sep}$moduleName"
        New-Item -Path $modulePath -ItemType Directory
        $manifestPath = "$modulePath${altSep}$moduleName.psd1"
        $psm1Path = Join-Path $modulePath "$moduleName.psm1"
        New-Item -Path $psm1Path -Value "function Test-RequiredModule { '$success' }"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $moduleVersion -RootModule "$moduleName.psm1"
    }

    Context "Requiring non-existent modules" {
        BeforeAll {
            $badName = 'ModuleThatDoesNotExist'
            $badPath = Join-Path $TestDrive 'ModuleThatDoesNotExist'
            $version = '1.0'
            $requiredVersion = '1.1'
            $maximumVersion = '1.2'
            $testCases = @(
                @{
                    ModuleRequirement = $badName
                    Scenario = 'fully qualified with module name'
                    ExpectedMessageStrings = @($badName)
                }
                @{
                    ModuleRequirement = $badPath
                    Scenario = 'fully qualified with module path'
                    ExpectedMessageStrings = @($badPath)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badName'; ModuleVersion = '$version' }"
                    Scenario = 'fully qualified with module name and version'
                    ExpectedMessageStrings = @($badName, $version)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badPath'; ModuleVersion = '$version' }"
                    Scenario = 'fully qualified with module name and version'
                    ExpectedMessageStrings = @($badPath, $version)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badName'; RequiredVersion = '$requiredVersion' }"
                    Scenario = 'fully qualified with module name and required version'
                    ExpectedMessageStrings = @($badName, $requiredVersion)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badPath'; RequiredVersion = '$requiredVersion' }"
                    Scenario = 'fully qualified with module path and required version'
                    ExpectedMessageStrings = @($badPath, $requiredVersion)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badName'; MaximumVersion = '$maximumVersion' }"
                    Scenario = 'fully qualified with module name and maximum version'
                    ExpectedMessageStrings = @($badName, $maximumVersion)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badPath'; MaximumVersion = '$maximumVersion' }"
                    Scenario = 'fully qualified with module path and maximum version'
                    ExpectedMessageStrings = @($badPath, $maximumVersion)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badName'; ModuleVersion = '$version'; MaximumVersion = '$maximumVersion' }"
                    Scenario = 'fully qualified with module name and version and maximum version'
                    ExpectedMessageStrings = @($badName, $version, $maximumVersion)
                }
                @{
                    ModuleRequirement = "@{ ModuleName = '$badPath'; ModuleVersion = '$version'; MaximumVersion = '$maximumVersion' }"
                    Scenario = 'fully qualified with module path and version and maximum version'
                    ExpectedMessageStrings = @($badPath, $version, $maximumVersion)
                }
            )
        }

        It "Fails parsing a script that requires module by <Scenario>" -TestCases $testCases {
            param([string]$ModuleRequirement, [string]$Scenario, [string[]]$ExpectedMessageStrings)

            $script = "#requires -Modules $ModuleRequirement`n`nWrite-Output 'failed'"
            $null = New-Item -Path $scriptPath -Value $script -Force

            $ex = { & $scriptPath } | Should -Throw -ErrorId 'ScriptRequiresMissingModules' -PassThru

            $expectedPattern = $ExpectedMessageStrings.ForEach{ [regex]::Escape($_) } -join '|'
            $stringMatches = [regex]::Matches($ex.Exception.Message, $expectedPattern)
            $stringMatches | Should -HaveCount $ExpectedMessageStrings.Count
        }
    }

    Context "Already loaded module" {
        BeforeAll {
            Import-Module $modulePath -ErrorAction Stop
            $testCases = @(
                @{ ModuleRequirement = "'$moduleName'"; Scenario = 'name' }
                @{ ModuleRequirement = "'$modulePath'"; Scenario = 'path' }
                @{ ModuleRequirement = "'$manifestPath'"; Scenario = 'manifest path' }
                @{ ModuleRequirement = "@{ ModuleName='$moduleName'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with name' }
                @{ ModuleRequirement = "@{ ModuleName='$modulePath'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with path' }
                @{ ModuleRequirement = "@{ ModuleName='$manifestPath'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with manifest path' }
            )
        }

        AfterAll {
            Remove-Module $moduleName -ErrorAction SilentlyContinue
        }

        It "Successfully runs a script requiring a loaded module by <Scenario>" -TestCases $testCases {
            param([string]$ModuleRequirement, [string]$Scenario)

            $script = "#requires -Modules $ModuleRequirement`n`nTest-RequiredModule"
            [scriptblock]::Create($script).Invoke() | Should -BeExactly $success
        }
    }

    Context "Loading by name" {
        BeforeAll {
            $oldModulePath = $env:PSModulePath
            $env:PSModulePath = $moduleDirPath + [System.IO.Path]::PathSeparator + $env:PSModulePath

            $testCases = @(
                @{ ModuleRequirement = "'$moduleName'"; Scenario = 'name' }
                @{ ModuleRequirement = "'$modulePath'"; Scenario = 'path' }
                @{ ModuleRequirement = "'$manifestPath'"; Scenario = 'manifest path' }
                @{ ModuleRequirement = "@{ ModuleName='$moduleName'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with name' }
                @{ ModuleRequirement = "@{ ModuleName='$modulePath'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with path' }
                @{ ModuleRequirement = "@{ ModuleName='$manifestPath'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with manifest path' }
            )
        }

        AfterAll {
            $env:PSModulePath = $oldModulePath
        }

        It "Successfully runs a script requiring a module on the module path by <Scenario>" -TestCases $testCases {
            param([string]$ModuleRequirement, [string]$Scenario)

            $script = "#requires -Modules $ModuleRequirement`n`nTest-RequiredModule"

            $null = New-Item -Path $scriptPath -Value $script -Force

            & $scriptPath | Should -BeExactly $success
        }
    }

    Context "Loading by absolute path" {
        BeforeAll {
            $testCases = @(
                @{ ModuleRequirement = "'$modulePath'"; Scenario = 'path' }
                @{ ModuleRequirement = "'$manifestPath'"; Scenario = 'manifest path' }
                @{ ModuleRequirement = "@{ ModuleName='$modulePath'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with path' }
                @{ ModuleRequirement = "@{ ModuleName='$manifestPath'; ModuleVersion='$moduleVersion' }"; Scenario = 'fully qualified name with manifest path' }
            )
        }

        It "Successfully runs a script requiring a module by absolute path by <Scenario>" -TestCases $testCases {
            param([string]$ModuleRequirement, [string]$Scenario)

            $script = "#requires -Modules $ModuleRequirement`n`nTest-RequiredModule"

            $null = New-Item -Path $scriptPath -Value $script -Force

            & $scriptPath | Should -BeExactly $success
        }
    }
}
