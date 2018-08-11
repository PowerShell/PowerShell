# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$PID

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

function Invoke-ImportModule
{
    param(
        $Module,
        $MinimumVersion,
        $MaximumVersion,
        $RequiredVersion,
        [switch]$PassThru,
        [switch]$AsCustomObject)

    $ipmoCmd = "Import-Module $Module "

    if ($MinimumVersion)
    {
        $ipmoCmd += "-MinimumVersion '$MinimumVersion' "
    }

    if ($MaximumVersion)
    {
        $ipmoCmd += "-MaximumVersion '$MaximumVersion' "
    }

    if ($RequiredVersion)
    {
        $ipmoCmd += "-RequiredVersion '$RequiredVersion' "
    }

    if ($PassThru)
    {
        $ipmoCmd += "-PassThru "
    }

    if ($AsCustomObject)
    {
        $ipmoCmd += "-AsCustomObject "
    }

    $ipmoCmd += '-ErrorAction Stop '

    return (Invoke-Expression $ipmoCmd)
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

    Context "Checking preloaded modules with FullyQualifiedName" {
        BeforeAll {
            $oldPSModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            $moduleInfo = Import-Module $modulePath -PassThru
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

        It "Successfully loads module from manifest path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
        }

        It "Does not load the module from manifest path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases -Pending {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }
    }

    Context "Checking preloaded modules with version parameters" {
        BeforeAll {
            $oldPSModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            $moduleInfo = Import-Module $modulePath -PassThru
        }

        AfterAll {
            Get-Module $moduleName | Remove-Module
            $env:PSModulePath = $oldPSModulePath
        }

        It "Imports the loaded module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $importedModule = Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -PassThru

            (Get-Module $moduleName)[0] | Should -Be $importedModule
        }

        It "Does not import the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $invocation = {
                Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            }

            if ($ModuleVersion -and $MaximumVersion -and [version]$ModuleVersion -gt [version]$MaximumVersion)
            {
                $invocation | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
                return
            }

            $invocation | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Imports the loaded module from module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)
            $importedModule = Invoke-ImportModule -Module $moduleName -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -PassThru

            (Get-Module $moduleName)[0] | Should -Be $importedModule
        }

        It "Does not import the module from module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $invocation = {
                Invoke-ImportModule -Module $moduleName -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            }

            if ($ModuleVersion -and $MaximumVersion -and [version]$ModuleVersion -gt [version]$MaximumVersion)
            {
                $invocation | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
                return
            }

            $invocation | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Successfully loads module from manifest path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $mod = Invoke-ImportModule -Module $manifestPath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -PassThru

            $mod.Name | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
        }

        It "Does not load the module from manifest path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases -Pending {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $invocation = {
                Invoke-ImportModule -Module $manifestPath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            }

            if ($ModuleVersion -and $MaximumVersion -and [version]$ModuleVersion -gt [version]$MaximumVersion)
            {
                $invocation | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
                return
            }

            $invocation | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Successfully loads module from module info when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $successCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $mod = Invoke-ImportModule -Module $moduleInfo -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -PassThru

            $mod.Name | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
        }

        It "Does not load the module from module info when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $invocation = {
                Invoke-ImportModule -Module $moduleInfo -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            }

            if ($ModuleVersion -and $MaximumVersion -and [version]$ModuleVersion -gt [version]$MaximumVersion)
            {
                $invocation | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
                return
            }

            $invocation | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
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

            $reqMod | Should -Not -Be $null
            $reqMod.Name | Should -Be $reqModName
        }

        It "Does not load a module when the required module has ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
            New-ModuleManifest -Path $reqModPath -RequiredModules $modSpec
            { Import-Module $reqModPath -ErrorAction Stop } | Should -Throw -ErrorId "Modules_InvalidManifest,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }
    }
}
