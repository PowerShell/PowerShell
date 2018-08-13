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

function Invoke-ImportModule
{
    param(
        $Module,
        $MinimumVersion,
        $MaximumVersion,
        $RequiredVersion,
        [switch]$PassThru,
        [switch]$AsCustomObject)

    $cmdArgs =  @{
        Name = $Module
        ErrorAction = 'Stop'
    }

    if ($MinimumVersion)
    {
        $cmdArgs.MinimumVersion = $MinimumVersion
    }

    if ($MaximumVersion)
    {
        $cmdArgs.MaximumVersion = $MaximumVersion
    }

    if ($RequiredVersion)
    {
        $cmdArgs.RequiredVersion = $RequiredVersion
    }

    if ($PassThru)
    {
        $cmdArgs.PassThru = $true
    }

    if ($AsCustomObject)
    {
        $cmdArgs.AsCustomObject = $true
    }

    return Import-Module @cmdArgs
}

$actualVersion = '2.3'
$actualGuid = [guid]'9b945229-65fd-4629-ae99-88e2618377ff'

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

Describe "Module cmdlet version constraint checking" -Tags "Feature" {
    BeforeAll {
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
            Import-Module $modulePath
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

Describe "Root module version checking" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'RootedModule'
        $modulePath = Join-Path $TestDrive $moduleName
        $rootModuleName = 'Root.psm1'
        $rootModulePath = Join-Path $modulePath $rootModuleName
        New-Item -Path $modulePath -ItemType Directory
        New-Item -Force -Path $rootModulePath -ItemType File -Value 'function Test-RootModule { 87 }'
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -RootModule $rootModuleName
    }

    Context "Checking preloaded modules with FullyQualifiedName" {
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

        It "Does not import the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
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

        It "Does not load the module from manifest path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }
    }

    Context "Checking preloaded modules with version parameters" {
        BeforeAll {
            $oldPSModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            Import-Module $modulePath
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

        It "Does not load the module from manifest path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
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

Describe "Versioned directory module version checking" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'VersionedModule'
        $modulePath = Join-Path $TestDrive $moduleName
        $versionPath = Join-Path $modulePath $actualVersion
        New-Item -Path $versionPath -ItemType Directory
        $manifestPath = Join-Path $versionPath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion
    }

    Context "Checking preloaded modules with FullyQualifiedName" {
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

        It "Does not import the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>" -TestCases $failCases {
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
            Import-Module $modulePath
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

Describe "Module GUID checking logic" -Tags "Feature" {
    BeforeAll {
        $guidSuccessCases = [System.Collections.ArrayList]::new()
        foreach ($case in $successCases)
        {
            $guidSuccessCases.Add($case)
            $guidSuccessCases.Add(($case + @{ Guid = $actualGuid }))
        }

        $guidFailCases = [System.Collections.ArrayList]::new()
        foreach ($case in $failCases)
        {
            $guidFailCases.Add($case)
            $guidFailCases.Add(($case + @{ Guid = $actualGuid }))
            $guidFailCases.Add(($case + @{ Guid = [guid]::NewGuid() }))
        }
    }

    Context "FullyQualifiedName finding a loaded a psd1 module" {
        BeforeAll {
            $moduleName = 'GuidModule'
            $modulePath = Join-Path $TestDrive $moduleName
            $manifestPath = Join-Path $modulePath "$moduleName.psd1"
            New-Item -Path $modulePath -ItemType Directory
            New-ModuleManifest -Path $manifestPath -Guid $actualGuid -ModuleVersion $actualVersion

            $oldModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            Import-Module $modulePath
        }

        AfterAll {
            Get-Module $moduleName | Remove-Module
            $env:PSModulePath = $oldModulePath
        }

        It "Gets the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Get-Module -FullyQualifiedName $modSpec

            $mod.Name | Should -Be $moduleName
            $mod.Guid | Should -Be $actualGuid
            $mod.Version | Should -Be $actualVersion
        }

        It "Loads the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Loads the module from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Loads the module from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Get-Module -FullyQualifiedName $modSpec

            $mod | Should -Be $null
        }

        It "Does not load the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Does not load the module from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Does not load the module from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases -Pending {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }
    }

    Context "FullyQualifiedName loading a rooted module" {
        BeforeAll {
            $moduleName = 'GuidRootedModule'
            $modulePath = Join-Path $TestDrive $moduleName
            $manifestPath = Join-Path $modulePath "$moduleName.psd1"
            New-Item -Path $modulePath -ItemType Directory
            $rootModuleName = 'RootModule.psm1'
            $rootModulePath = Join-Path $modulePath $rootModuleName
            New-Item -Path $rootModulePath -ItemType File -Value "function Test-GuidRootModule { 128 }"
            New-ModuleManifest -Path $manifestPath -Guid $actualGuid -ModuleVersion $actualVersion -RootModule $rootModuleName

            $oldModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            Import-Module $modulePath
        }

        AfterAll {
            Get-Module $moduleName | Remove-Module
            $env:PSModulePath = $oldModulePath
        }

        It "Gets the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Get-Module -FullyQualifiedName $modSpec

            $mod.Name | Should -Be $moduleName
            $mod.Guid | Should -Be $actualGuid
            $mod.Version | Should -Be $actualVersion
        }

        It "Loads the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Loads the module from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Loads the module from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Get-Module -FullyQualifiedName $modSpec

            $mod | Should -Be $null
        }

        It "Does not load the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Does not load the module from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Does not load the module from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }
    }

    Context "FullyQualifiedName loading a module in a versioned directory" {
        BeforeAll {
            $moduleName = 'GuidModule'
            $modulePath = Join-Path $TestDrive $moduleName
            $versionPath = Join-Path $modulePath $actualVersion
            $manifestPath = Join-Path $versionPath "$moduleName.psd1"
            New-Item -Path $versionPath -ItemType Directory
            New-ModuleManifest -Path $manifestPath -Guid $actualGuid -ModuleVersion $actualVersion

            $oldModulePath = $env:PSModulePath
            $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
            Import-Module $modulePath
        }

        AfterAll {
            Get-Module $moduleName | Remove-Module
            $env:PSModulePath = $oldModulePath
        }

        It "Gets the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Get-Module -FullyQualifiedName $modSpec

            $mod.Name | Should -Be $moduleName
            $mod.Guid | Should -Be $actualGuid
            $mod.Version | Should -Be $actualVersion
        }

        It "Loads the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Loads the module from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Loads the module from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

            $mod.Name    | Should -Be $moduleName
            $mod.Version | Should -Be $actualVersion
            $mod.Guid    | Should -Be $actualGuid
        }

        It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            $mod = Get-Module -FullyQualifiedName $modSpec

            $mod | Should -Be $null
        }

        It "Does not load the module from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Does not load the module from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

        It "Does not load the module from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases -Pending {
            param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

            $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

            { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
        }

    }
}
