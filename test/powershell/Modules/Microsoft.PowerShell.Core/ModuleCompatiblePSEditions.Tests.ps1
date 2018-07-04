# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# NOTE: This test suite must be named/located so that it comes after the
# "Disable GAC loading" test in Import-Module.Tests.ps1. This is because that
# test will fail when the type it's trying to load is already in the cache and succeeds.

$script:oldModulePath = $env:PSModulePath

function Add-ModulePath
{
    param([string]$Path)

    $script:oldModulePath = $env:PSModulePath

    $env:PSModulePath = $env:PSModulePath + [System.IO.Path]::PathSeparator + $Path
}

function Restore-ModulePath
{
    $env:PSModulePath = $script:oldModulePath
}

# Creates a new dummy module compatible with the given PSEditions
function New-EditionCompatibleModule
{
    param(
        [Parameter(Mandatory = $true)][string]$ModuleName,
        [string]$DirPath,
        [string[]]$CompatiblePSEditions)

    $modulePath = Join-Path $DirPath $ModuleName

    $manifestPath = Join-Path $modulePath "$ModuleName.psd1"

    $psm1Name = "$ModuleName.psm1"
    $psm1Path = Join-Path $modulePath $psm1Name

    New-Item -Path $modulePath -ItemType Directory

    New-Item -Path $psm1Path -Value "function Test-$ModuleName { `$true }"

    if ($CompatiblePSEditions)
    {
        New-ModuleManifest -Path $manifestPath -CompatiblePSEditions $CompatiblePSEditions -RootModule $psm1Name
    }
    else
    {
        New-ModuleManifest -Path $manifestPath -RootModule $psm1Name
    }

    return $modulePath
}

function New-TestModules
{
    param([hashtable[]]$TestCases, [string]$BaseDir)

    for ($i = 0; $i -lt $TestCases.Count; $i++)
    {
        $path = New-EditionCompatibleModule -ModuleName $TestCases[$i].ModuleName -CompatiblePSEditions $TestCases[$i].Editions -Dir $BaseDir

        $TestCases[$i].Path = $path
        $TestCases[$i].Name = $TestCases[$i].Editions -join ","
    }
}

Describe "Get-Module with CompatiblePSEditions-checked paths" -Tag "CI" {

    BeforeAll {
        $successCases = @(
            @{ Editions = "Core","Desktop"; ModuleName = "BothModule" },
            @{ Editions = "Core"; ModuleName = "CoreModule" }
        )

        $failCases = @(
            @{ Editions = "Desktop"; ModuleName = "DesktopModule" },
            @{ Editions = $null; ModuleName = "NeitherModule" }
        )

        $basePath = Join-Path $TestDrive "EditionCompatibleModules"
        New-TestModules -TestCases $successCases -BaseDir $basePath
        New-TestModules -TestCases $failCases -BaseDir $basePath

        # Emulate the System32 module path for tests
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $basePath)
    }

    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $null)
    }

    Context "Loading from checked paths on the module path with no flags" {
        BeforeAll {
            Add-ModulePath $basePath
            $modules = Get-Module -ListAvailable
        }

        AfterAll {
            Restore-ModulePath
        }

        It "Lists compatible modules from the module path with -ListAvailable for PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }

        It "Does not list incompatible modules with -ListAvailable for PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules.Name | Should -Not -Contain $ModuleName
        }
    }

    Context "Loading from checked paths by absolute path with no flags" {
        It "Lists compatible modules with -ListAvailable for PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules = Get-Module -ListAvailable (Join-Path -Path $basePath -ChildPath $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }

        It "Does not list incompatible modules with -ListAvailable for PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules = Get-Module -ListAvailable (Join-Path -Path $basePath -ChildPath $ModuleName)

            $modules.Name | Should -Not -Contain $ModuleName
        }
    }

    Context "Loading from checked paths on the module path with -SkipEditionCheck" {
        BeforeAll {
            Add-ModulePath $basePath
            $modules = Get-Module -ListAvailable -SkipEditionCheck
        }

        AfterAll {
            Restore-ModulePath
        }

        It "Lists all modules from the module path with -ListAvailable for PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }
    }

    Context "Loading from checked paths by absolute path with -SkipEditionCheck" {
        It "Lists compatible modules with -ListAvailable for PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules = Get-Module -ListAvailable -SkipEditionCheck (Join-Path -Path $basePath -ChildPath $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }
    }
}

Describe "Import-Module from CompatiblePSEditions-checked paths" -Tag "CI" {
    BeforeAll {
        $successCases = @(
            @{ Editions = "Core","Desktop"; ModuleName = "BothModule"; Result = $true },
            @{ Editions = "Core"; ModuleName = "CoreModule"; Result = $true }
        )

        $failCases = @(
            @{ Editions = "Desktop"; ModuleName = "DesktopModule"; Result = $true },
            @{ Editions = $null; ModuleName = "NeitherModule"; Result = $true }
        )

        $basePath = Join-Path $TestDrive "EditionCompatibleModules"
        New-TestModules -TestCases $successCases -BaseDir $basePath
        New-TestModules -TestCases $failCases -BaseDir $basePath

        $allModules = ($successCases + $failCases).ModuleName

        # Emulate the System32 module path for tests
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $basePath)
    }

    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $null)
    }

    AfterEach {
        Get-Module $allModules | Remove-Module -Force
    }

    Context "Imports from module path" {
        BeforeAll {
            Add-ModulePath $basePath
        }

        AfterAll {
            Restore-ModulePath
        }

        It "Successfully imports compatible modules from the module path with PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            Import-Module $ModuleName -Force
            & "Test-$ModuleName" | Should -Be $Result
        }

        It "Fails to import incompatible modules from the module path with PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            { Import-Module $ModuleName -Force -ErrorAction 'Stop'; & "Test-$ModuleName" } | Should -Throw -ErrorId "Modules_PSEditionNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Imports an incompatible module from the module path with -SkipEditionCheck with PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            Import-Module $ModuleName -SkipEditionCheck -Force
            & "Test-$ModuleName" | Should -Be $Result
        }
    }

    Context "Imports from absolute path" {
        It "Successfully imports compatible modules from an absolute path with PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            $path = Join-Path -Path $basePath -ChildPath $ModuleName

            Import-Module $path -Force
            & "Test-$ModuleName" | Should -Be $Result
        }

        It "Fails to import incompatible modules from an absolute path with PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            $path = Join-Path -Path $basePath -ChildPath $ModuleName

            { Import-Module $path -Force -ErrorAction 'Stop'; & "Test-$ModuleName" } | Should -Throw -ErrorId "Modules_PSEditionNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Imports an incompatible module from an absolute path with -SkipEditionCheck with PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            $path = Join-Path -Path $basePath -ChildPath $ModuleName

            Import-Module $path -SkipEditionCheck -Force
            & "Test-$ModuleName" | Should -Be $Result
        }
    }
}
