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

function Assert-ModuleIsCorrect
{
    param(
        $Module,
        [string]$Name = $moduleName,
        [guid]$Guid = $actualGuid,
        [version]$Version = $actualVersion,
        [version]$MinVersion,
        [version]$MaxVersion,
        [version]$RequiredVersion
    )

    $Module      | Should -Not -Be $null
    $Module.Name | Should -Be $ModuleName
    $Module.Guid | Should -Be $Guid
    if ($Version)
    {
        $Module.Version | Should -Be $Version
    }
    if ($ModuleVersion)
    {
        $Module.Version | Should -BeGreaterOrEqual $ModuleVersion
    }
    if ($MaximumVersion)
    {
        $Module.Version | Should -BeLessOrEqual $MaximumVersion
    }
    if ($RequiredVersion)
    {
        $Module.Version | Should -Be $RequiredVersion
    }
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

$guidSuccessCases = [System.Collections.ArrayList]::new()
foreach ($case in $successCases)
{
    [void]$guidSuccessCases.Add($case + @{ Guid = $null })
    [void]$guidSuccessCases.Add(($case + @{ Guid = $actualGuid }))
}

$guidFailCases = [System.Collections.ArrayList]::new()
foreach ($case in $failCases)
{
    [void]$guidFailCases.Add($case + @{ Guid = $null })
    [void]$guidFailCases.Add($case + @{ Guid = $actualGuid })
    [void]$guidFailCases.Add($case + @{ Guid = [guid]::NewGuid() })
}

Describe "Module loading with version constraints" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -Guid $actualGuid

        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
    }

    AfterEach {
        Get-Module $moduleName | Remove-Module
    }

    It "Loads the module by FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        $mod | Should -Be $null
    }

    It "Does not load the module with FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }
}

Describe "Versioned directory loading with module constraints" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $versionPath = Join-Path $modulePath $actualVersion
        New-Item -Path $versionPath -ItemType Directory
        $manifestPath = Join-Path $versionPath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -Guid $actualGuid

        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
    }

    AfterEach {
        Get-Module $moduleName | Remove-Module
    }

    It "Loads the module by FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        $mod | Should -Be $null
    }

    It "Does not load the module with FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }
}

Describe "Rooted module loading with module constraints" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $rootModuleName = 'RootModule.psm1'
        $rootModulePath = Join-Path $modulePath $rootModuleName
        New-Item -Path $rootModulePath -ItemType File -Value 'function Test-RootModule { 178 }'
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -Guid $actualGuid -RootModule $rootModuleName
        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
    }

    AfterEach {
        Get-Module $moduleName | Remove-Module
    }

    It "Loads the module by FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        $mod | Should -Be $null
    }

    It "Does not load the module with FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }
}

Describe "Preloaded module specification checking" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -Guid $actualGuid

        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive

        Import-Module $modulePath

        $relativePathCases = @(
            @{ Location = $TestDrive; ModPath = (Join-Path "." $moduleName) }
            @{ Location = $TestDrive; ModPath = (Join-Path "." $moduleName "$moduleName.psd1") }
            @{ Location = (Join-Path $TestDrive $moduleName); ModPath = (Join-Path "." "$moduleName.psd1") }
            @{ Location = (Join-Path $TestDrive $moduleName); ModPath = (Join-Path ".." $moduleName) }
        )
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
        Get-Module $moduleName | Remove-Module
    }

    It "Gets the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Gets the module when a relative path is used in a module specification: <ModPath>" -TestCases $relativePathCases -Pending {
        param([string]$Location, [string]$ModPath)

        Push-Location $Location
        try
        {
            $modSpec = New-ModuleSpecification -ModuleName $ModPath -ModuleVersion $actualVersion
            $mod = Get-Module -FullyQualifiedName $modSpec
            Assert-ModuleIsCorrect `
                -Module $mod `
                -Name $moduleName
                -Guid $actualGuid
                -RequiredVersion $actualVersion
        }
        finally
        {
            Pop-Location
        }
    }

    It "Loads the module by FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        $mod | Should -Be $null
    }

    It "Does not load the module with FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    Context "Required modules" {
        BeforeAll {
            $reqModName = 'ReqMod'
            $reqModPath = Join-Path $TestDrive "$reqModName.psd1"
        }

        AfterEach {
            Get-Module $reqModName | Remove-Module
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

Describe "Preloaded modules with versioned directory version checking" -Tag "Feature" {
    BeforeAll {
        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $versionPath = Join-Path $modulePath $actualVersion
        New-Item -Path $versionPath -ItemType Directory
        $manifestPath = Join-Path $versionPath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -Guid $actualGuid

        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive

        Import-Module $modulePath
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
        Get-Module $moduleName | Remove-Module
    }

    It "Gets the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        $mod | Should -Be $null
    }

    It "Does not load the module with FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    Context "Required modules" {
        BeforeAll {
            $reqModName = 'ReqMod'
            $reqModPath = Join-Path $TestDrive "$reqModName.psd1"
        }

        AfterEach {
            Get-Module $reqModName | Remove-Module
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

Describe "Preloaded rooted module specification checking" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'TestModule'
        $modulePath = Join-Path $TestDrive $moduleName
        New-Item -Path $modulePath -ItemType Directory
        $rootModuleName = 'RootModule.psm1'
        $rootModulePath = Join-Path $modulePath $rootModuleName
        New-Item -Path $rootModulePath -ItemType File -Value 'function Test-RootModule { 43 }'
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $actualVersion -Guid $actualGuid -RootModule $rootModuleName

        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive

        Import-Module $modulePath
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
        Get-Module $moduleName | Remove-Module
    }

    It "Gets the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module by FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidSuccessCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Import-Module -FullyQualifiedName $modSpec -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Loads the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $successCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $mod = Invoke-ImportModule -Module $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid -PassThru

        Assert-ModuleIsCorrect `
            -Module $mod `
            -MinVersion $ModuleVersion `
            -MaxVersion $MaximumVersion `
            -RequiredVersion $RequiredVersion
    }

    It "Does not get the module when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        $mod = Get-Module -FullyQualifiedName $modSpec

        $mod | Should -Be $null
    }

    It "Does not load the module with FullyQualifiedName from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $modulePath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $moduleName -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with FullyQualifiedName from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $guidFailCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $modSpec = New-ModuleSpecification -ModuleName $manifestPath -ModuleVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion -Guid $Guid

        { Import-Module -FullyQualifiedName $modSpec -ErrorAction Stop } | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from absolute path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the module path when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    It "Does not load the module with version constraints from the manifest when ModuleVersion=<ModuleVersion>, MaximumVersion=<MaximumVersion>, RequiredVersion=<RequiredVersion>, Guid=<Guid>" -TestCases $failCases {
        param($ModuleVersion, $MaximumVersion, $RequiredVersion, $Guid)

        $sb = {
            Invoke-ImportModule -Module $modulePath -MinimumVersion $ModuleVersion -MaximumVersion $MaximumVersion -RequiredVersion $RequiredVersion
        }

        if ($ModuleVersion -and $MaximumVersion -and ($ModuleVersion -ge $MaximumVersion))
        {
            $sb | Should -Throw -ErrorId 'ArgumentOutOfRange,Microsoft.PowerShell.Commands.ImportModuleCommand'
            return
        }
        $sb | Should -Throw -ErrorId 'Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand'
    }

    Context "Required modules" {
        BeforeAll {
            $reqModName = 'ReqMod'
            $reqModPath = Join-Path $TestDrive "$reqModName.psd1"
        }

        AfterEach {
            Get-Module $reqModName | Remove-Module
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
