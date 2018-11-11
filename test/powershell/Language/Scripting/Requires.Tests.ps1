# Copyright (c) Microsoft Corporation. All rights reserved.
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
            $testCases = @(
                @{ ModuleRequirement = "'$badName'"; Scenario = 'name' }
                @{ ModuleRequirement = "'$badPath'"; Scenario = 'path' }
                @{ ModuleRequirement = "@{ ModuleName = '$badName'; ModuleVersion = '$version' }"; Scenario = 'fully qualified name with name' }
                @{ ModuleRequirement = "@{ ModuleName = '$badPath'; ModuleVersion = '$version' }"; Scenario = 'fully qualified name with path' }
            )
        }

        It "Fails parsing a script that requires module by <Scenario>" -TestCases $testCases {
            param([string]$ModuleRequirement, [string]$Scenario)

            $script = "#requires -Modules $ModuleRequirement`n`nWrite-Output 'failed'"
            $null = New-Item -Path $scriptPath -Value $script -Force

            { & $scriptPath } | Should -Throw -ErrorId 'ScriptRequiresMissingModules'
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
