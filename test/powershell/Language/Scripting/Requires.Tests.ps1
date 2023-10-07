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

            $powerShellVersions = "1.0", "2.0", "3.0", "4.0", "5.0", "5.1", "6.0", "6.1", "6.2", "7.0", "7.1", "7.2", "7.3", "7.4"
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
