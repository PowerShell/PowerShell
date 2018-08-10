# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function New-ModuleSpecification
{
    param(
        $ModuleName,
        $ModuleVersion,
        $MaximumVersion,
        $RequiredVersion,
        $Guid)

    $modSpec = @{}

    if ($ModuleName)
    {
        $modSpec.ModuleName = $ModuleName
    }

    if ($ModuleVersion)
    {
        $modSpec.ModuleVersion = $ModuleVersion
    }

    if ($MaximumVersion)
    {
        $modSpec.MaximumVersion = $MaximumVersion
    }

    if ($RequiredVersion)
    {
        $modSpec.RequiredVersion = $RequiredVersion
    }

    if ($Guid)
    {
        $modSpec.Guid = $Guid
    }

    return $modSpec
}

Describe "Module cmdlet version constraint checking" -Tags "Feature" {
    BeforeAll {
        $actualVersion = '2.3'

        $successCases = @(
            @{
                ModuleVersion = '2.0'
                MaximumVersion = $null
                RequiredVersion = $null
            },
            @{
                ModuleVersion = '1.0'
                MaximumVersion = '3.0'
                RequiredVersion = $null
            },
            @{
                ModuleVersion = $null
                MaximumVersion = '3.0'
                RequiredVersion = $null
            },
            @{
                ModuleVersion = $null
                MaximumVersion = $null
                RequiredVersion = $actualVersion
            }
        )

        $failCases = @(
            @{
                ModuleVersion = '2.5'
                MaximumVersion = $null
                RequiredVersion = $null
            },
            @{
                ModuleVersion = '2.0'
                MaximumVersion = '2.2'
                RequiredVersion = $null
            },
            @{
                ModuleVersion = '3.0'
                MaximumVersion = '3.1'
                RequiredVersion = $null
            },
            @{
                ModuleVersion = '3.0'
                MaximumVersion = '2.0'
                RequiredVersion = $null
            },
            @{
                ModuleVersion = $null
                MaximumVersion = '1.7'
                RequiredVersion = $null
            },
            @{
                ModuleVersion = $null
                MaximumVersion = $null
                RequiredVersion = '2.2'
            }
        )

        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion
    }

    Context "Checking preloaded modules" {
        BeforeAll {
            $oldPSModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            Import-Module $modulePath
        }

        AfterAll {
            Get-Module $moduleName | Remove-Module
            $env:PSModulePath = $oldPSModulePath
        }

        It "Gets the loaded module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            $mod = Get-Module -FullyQualifiedName $modSpec
            $mod.Name | Should -BeExactly $moduleName

            if ($ModuleVersion)
            {
                $mod.Version | Should -BeGreaterOrEqual $ModuleVersion
            }

            if ($MaximumVersion)
            {
                $mod.Version | Should -BeLessOrEqual $MaximumVersion
            }

            if ($RequiredVersion)
            {
                $mod.Version | Should -Be $RequiredVersion
            }
        }

        It "Does not get the loaded module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            Get-Module -FullyQualifiedName $modSpec | Should -Be $null
        }

        It "Imports the loaded module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            $importedModule = Import-Module -FullyQualifiedName $modSpec -PassThru
            (Get-Module $moduleName)[0] | Should -Be $importedModule
        }

        It "Does not import the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases -Pending {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Imports the loaded module from module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            $importedModule = Import-Module -FullyQualifiedName $modSpec -PassThru -ErrorAction Stop
            (Get-Module $moduleName)[0] | Should -Be $importedModule
        }

        It "Does not import the module from module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }
    }

    Context "Required modules" {
        BeforeAll {
            Import-Module $modulePath
            $reqModName = 'ReqMod'
            $reqModPath = Join-Path $TestDrive "$reqModName.psd1"
        }

        AfterEach {
            Get-Module $reqModName | Remove-Module
        }

        AfterAll {
            Get-Module $moduleName | Remove-Module
        }

        It "Successfully loads a module when the required module has ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            New-ModuleManifest -Path $reqModPath -RequiredModules $modSpec
            $reqMod = Import-Module $reqModPath -PassThru

            $reqMod.Name | Should -Be $reqModName
        }

        It "Does not load a module when the required module has ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            New-ModuleManifest -Path $reqModPath -RequiredModules $modSpec
            { Import-Module $reqModPath -ErrorAction Stop } | Should -Throw -ErrorId "Modules_InvalidManifest,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }
    }

    Context "Loading module with the same name but different version" {

    }
}
