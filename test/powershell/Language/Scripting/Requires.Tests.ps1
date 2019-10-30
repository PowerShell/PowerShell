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

    Context "Version checks" {
        BeforeAll {
            $currentVersion = $PSVersionTable.PSVersion

            $powerShellVersions = "1.0", "2.0", "3.0", "4.0", "5.0", "5.1", "6.0", "6.1", "6.2", "7.0"
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

    Context "Maxmimum PS Version checks" {
        BeforeAll {
            $currentVersion = $PSVersionTable.PSVersion
        }
        It "Both current major and minor versions equals required maximum version." {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -MaximumPSVersion $($currentVersion.Major).$($currentVersion.Minor).$($currentVersion.Patch)" -Force
            { & $scriptPath } | Should -Not -Throw
        }
        if ($currentVersion.Minor -gt 0) {
            It "Current major version equals required maximum major version, and current minor version < required minor version." {
                $scriptPath = Join-Path $TestDrive 'script.ps1'
                $script = "#requires -MaximumPSVersion $($currentVersion.Major).0"
                $null = New-Item -Path $scriptPath -Value $script -Force
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            }
        }
        It "Current major version is greater than maximum major version" {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -MaximumPSVersion $($currentVersion.Major - 1)" -Force
            { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
        }

        It "Versions specified as bareword strings" {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -MaximumPSVersion 6" -Force
            if ($currentVersion.Major -gt 6) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } elseif ($currentVersion.Major -eq 6 -and $currentVersion.Minor -gt 0) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } else {
                { & $scriptPath } | Should -Not -Throw
            }
        }

        It "Versions specified as bareword strings" {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -MaximumPSVersion 6.2" -Force
            if ($currentVersion.Major -gt 6) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } elseif ($currentVersion.Major -eq 6 -and $currentVersion.Minor -gt 2) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } else {
                { & $scriptPath } | Should -Not -Throw
            }
        }

        It "Versions specified as bareword strings" {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -MaximumPSVersion 6.2.3" -Force
            if ($currentVersion.Major -gt 6) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } elseif ($currentVersion.Major -eq 6 -and $currentVersion.Minor -gt 2) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } elseif ($currentVersion.Major -eq 6 -and $currentVersion.Minor -eq 2 -and $currentVersion.Patch -gt 3) {
                { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresMaximumPSVersion"
            } else {
                { & $scriptPath } | Should -Not -Throw
            }
        }

    }

    Context "OS type checks" {
        It "OS type is in the supported OS types." {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -OS Linux,MacOS,Windows" -Force
            { & $scriptPath } | Should -Not -Throw
        }

        It "OS type is not in the supported OS types." {
            [System.Collections.ArrayList] $OSTypes = "Linux","MacOS","Windows"
            if ($IsMacOS) {
                $OSTypes.Remove("MacOS")
            } elseif ($IsLinux) {
                $OSTypes.Remove("Linux")
            } else {
                $OSTypes.Remove("Windows")
            }
            $requiredOSTypes = $OSTypes -join ','
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -OS $($requiredOSTypes)" -Force
            { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresOSTypeInvalid"
        }

        It "OS type is not a valid OS type." {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $null = New-Item -Path $scriptPath -Value "#requires -OS NonExistantOS" -Force
            { & $scriptPath } | Should -Throw -ErrorId "ScriptRequiresOSTypeInvalid"
        }

        It "OS type is not a string." {
            $scriptPath = Join-Path $TestDrive 'script.ps1'
            $requiredOSTypes = 1
            $null = New-Item -Path $scriptPath -Value "#requires -OS $($requiredOSTypes)" -Force
            { & $scriptPath } | Should -Throw -ErrorId "System.FormatException"
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

Describe "#requires warning emitting" -Tags "CI" {
    BeforeAll {
        [system.management.automation.internal.internaltesthooks]::SetTestHook('SilenceRequiresWarning', $true)
    }

    BeforeEach {
        [system.management.automation.internal.internaltesthooks]::SetTestHook('RequiresWarningCount', 0)
    }

    AfterEach {
        [system.management.automation.internal.internaltesthooks]::SetTestHook('RequiresWarningCount', 0)
    }

    AfterAll {
        [system.management.automation.internal.internaltesthooks]::SetTestHook('SilenceRequiresWarning', $false)
    }

    It "Emits no warning with no requires statement" {
        $tokens = $errors = $null
        $Statement = "`$foo = 1"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 0
        $errors.Count | Should -BeExactly 0
    }

    It "Emits no warning with one requires statement" {
        $tokens = $errors = $null
        $Statement = "#requires -OS Windows,MacOS,Linux"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 0
        $errors.Count | Should -BeExactly 0
    }

    It "Emits no warning with many requires statement" {
        $tokens = $errors = $null
        $currentVersion = $PSVersionTable.PSVersion
        $Statement = "#requires -OS Windows,MacOS,Linux`r`n#requires -Version 1.0`r`n#requires -MaximumPSVersion $($currentVersion.Major).$($currentVersion.Minor).$($currentVersion.Patch)"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 0
        $errors.Count | Should -BeExactly 0
    }

    It "Emits no warning when requires have blank lines between them as long as no other statements have been parsed" {
        $tokens = $errors = $null
        $Statement = "#requires -OS Windows,MacOS,Linux`r`n`r`n#requires -Version 1.0`r`n`$foo = 1"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 0
        $errors.Count | Should -BeExactly 0
    }

    It "Emits no warning when requires are at the top of the script but blank lines precede them" {
        $tokens = $errors = $null
        $Statement = "`r`n`r`n#requires -OS Windows,MacOS,Linux`r`n#requires -Version 1.0"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 0
        $errors.Count | Should -BeExactly 0
    }

    It "Emits a warning when a requires come after statements" {
        $tokens = $errors = $null
        $Statement = "`$foo = 1; #requires -OS Windows,MacOS,Linux"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 1
        $errors.Count | Should -BeExactly 0
    }

    It "Emits multiple warnings with different lines when multiple requires come after statements" {
        $tokens = $errors = $null
        $Statement = "`$foo = 1`r`n#requires -OS Windows,MacOS,Linux`r`n#requires -Version 1.0"
        [System.Management.Automation.Language.Parser]::ParseInput($Statement, [ref]$tokens, [ref]$errors)
        [system.management.automation.internal.internaltesthooks]::GetTestHookValue("RequiresWarningCount") | Should -BeExactly 2
        $errors.Count | Should -BeExactly 0
    }
}
