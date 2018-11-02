# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function New-GmoTestModule
{
    param(
        [Parameter()]
        [string]
        $Name,

        [Parameter()]
        [hashtable]
        $Structure,

        [Parameter()]
        [string]
        $ModuleDirPath
    )

    $modRootPath = Join-Path $ModuleDirPath $Name

    $null = New-Item -Path $modRootPAth -ItemType Directory

    $null = New-TestModuleStructure -Structure $Structure -Dir $modRootPath
}

function New-TestModuleStructure
{
    param(
        [Parameter()]
        [hashtable]
        $Structure,

        [Parameter()]
        [string]
        $Dir
    )

    foreach ($item in $Structure.Keys)
    {
        $itemPath = Join-Path $Dir $item

        if ($item.EndsWith('.psd1'))
        {
            $version = (Split-Path -Leaf $Dir) -as [Version]

            if ($version)
            {
                $null = New-ModuleManifest -Path $itemPath -ModuleVersion $version
            }
            else
            {
                $null = New-ModuleManifest -Path $itemPath
            }

            continue
        }

        if ($Structure[$item] -is [hashtable])
        {
            $null = New-Item -Path $itemPath -ItemType Directory
            $null = New-TestModuleStructure -Structure $Structure[$item] -Dir $itemPath
            continue
        }

        if ($item.EndsWith('.psm1'))
        {
            if ($Structure[$item])
            {
                $null = New-Item -ItemType File -Path $itemPath -Value $Structure[$item]
            }
            else
            {
                $null = New-Item -ItemType File -Path $itemPath
            }

            continue
        }
    }
}

$script:Modules = @(
    @{ Name = 'Foo'; Structure = @{ '1.1' = @{ 'Foo.psd1' = $null; 'Foo.psm1' = $null }; '2.0' = @{ 'Foo.psd1' = $null; 'Foo.psm1' = $null } } }
    @{ Name = 'Bar'; Structure = @{ 'Download' = @{ 'Download.psm1' = $null }; 'Bar.psd1' = $null; 'Bar.psm1' = $null } }
    @{ Name = 'Zoo'; Structure = @{ 'Zoo.psd1' = $null; 'Zoo.psm1' = $null; 'Too' = @{ 'Zoo.psm1' = $null } } }
    @{ Name = 'Az'; Structure = @{ 'Az.psd1' = $null } }
)

Describe "Get-Module" -Tags "CI" {

    BeforeAll {
        $originalPSModulePath = $env:PSModulePath

        $testModulePath = Join-Path $testdrive "Modules"

        foreach ($mod in $script:Modules)
        {
            New-GmoTestModule @mod -ModuleDirPath $testModulePath
        }

        $env:PSModulePath = $testModulePath
    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
    }

    It "Get-Module -ListAvailable" {
        $modules = Get-Module -ListAvailable
        $modules | Should -HaveCount 5
        $modules = $modules | Sort-Object -Property Name, Version
        $modules.Name -join "," | Should -BeExactly "Az,Bar,Foo,Foo,Zoo"
        $modules[2].Version | Should -BeExactly "1.1"
        $modules[3].Version | Should -BeExactly '2.0'
    }

    It "Get-Module <Name> -ListAvailable" {
        $modules = Get-Module F* -ListAvailable
        $modules.Count | Should -Be 2
        $modules = $modules | Sort-Object -Property Version
        $modules.Name -join "," | Should -BeExactly "Foo,Foo"
        $modules[0].Version | Should -Be "1.1"
        $modules[1].Version | Should -Be "2.0"
    }

    It "Get-Module -ListAvailable -All" {
        $modules = Get-Module -ListAvailable -All
        $modules.Count | Should -Be 11
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Az,Bar,Bar,Download,Foo,Foo,Foo,Foo,Zoo,Zoo,Zoo"

        $modules[0].ModuleType | Should -BeExactly "Manifest"
        $modules[1].ModuleType | Should -BeExactly "Manifest"
        $modules[2].ModuleType | Should -BeExactly "Script"
        $modules[3].ModuleType | Should -BeExactly "Script"
        $modules[4].ModuleType | Should -BeExactly "Manifest"
        $modules[4].Version | Should -BeExactly "1.1"
        $modules[5].ModuleType | Should -BeExactly "Script"
        $modules[6].ModuleType | Should -BeExactly "Manifest"
        $modules[6].Version | Should -BeExactly "2.0"
        $modules[7].ModuleType | Should -BeExactly "Script"
        $modules[8].ModuleType | Should -BeExactly "Script"
        $modules[8].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
        $modules[9].ModuleType | Should -BeExactly "Manifest"
        $modules[9].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psd1").Path
        $modules[10].ModuleType | Should -BeExactly "Script"
        $modules[10].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psm1").Path
    }

    It "Get-Module <Name> -ListAvailable -All" {
        $modules = Get-Module down*, zoo -ListAvailable -All
        $modules.Count | Should -Be 4
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Download,Zoo,Zoo,Zoo"

        $modules[0].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Bar\Download\Download.psm1").Path
        $modules[1].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
        $modules[2].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psd1").Path
        $modules[3].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psm1").Path
    }

    It "Get-Module <Path> -ListAvailable" {
        $modules = Get-Module "$testdrive\Modules\*" -ListAvailable
        $modules.Count | Should -Be 5
        $modules = $modules | Sort-Object -Property Name, Version
        $modules.Name -join "," | Should -BeExactly "Az,Bar,Foo,Foo,Zoo"
        $modules[2].Version | Should -BeExactly "1.1"
        $modules[3].Version | Should -BeExactly '2.0'
    }

    It "Get-Module <Path> -ListAvailable -All" {
        $modules = Get-Module "$testdrive\Modules\*" -ListAvailable -All
        $modules | Should -HaveCount 6
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Az,Bar,Foo,Foo,Zoo,Zoo"
        $modules[4].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
    }
}
