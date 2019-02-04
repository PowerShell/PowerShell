# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-Module -ListAvailable" -Tags "CI" {

    BeforeAll {
        $originalPSModulePath = $env:PSModulePath

        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\1.1" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\2.0" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Bar\Download" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Zoo\Too" -Force > $null

        New-ModuleManifest -Path "$testdrive\Modules\Foo\1.1\Foo.psd1" -ModuleVersion 1.1
        New-ModuleManifest -Path "$testdrive\Modules\Foo\2.0\Foo.psd1" -ModuleVersion 2.0
        New-ModuleManifest -Path "$testdrive\Modules\Bar\Bar.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Zoo\Zoo.psd1"

        New-Item -ItemType File -Path "$testdrive\Modules\Foo\1.1\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Foo\2.0\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Bar.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Download\Download.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Zoo\Zoo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Zoo\Too\Zoo.psm1" > $null

        $fullyQualifiedPathTestCases = @(
            # The current behaviour in PowerShell is that version gets ignored when using Get-Module -FullyQualifiedName with a path
            @{ ModPath = "$TestDrive/Modules\Foo"; Name = 'Foo'; Version = '2.0'; Count = 2 }
            @{ ModPath = "$TestDrive\Modules/Foo\1.1/Foo.psd1"; Name = 'Foo'; Version = '1.1'; Count = 1 }
            @{ ModPath = "$TestDrive\Modules/Bar.psd1"; Name = 'Bar'; Version = '0.0'; Count = 1 }
            @{ ModPath = "$TestDrive\Modules\Zoo\Too\Zoo.psm1"; Name = 'Zoo'; Version = '0.0'; Count = 1 }
        )

        $env:PSModulePath = Join-Path $testdrive "Modules"
    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
    }

    It "Get-Module -ListAvailable" {
        $modules = Get-Module -ListAvailable
        $modules.Count | Should -Be 4
        $modules = $modules | Sort-Object -Property Name, Version
        $modules.Name -join "," | Should -BeExactly "Bar,Foo,Foo,Zoo"
        $modules[1].Version | Should -Be "1.1"
        $modules[2].Version | Should -Be '2.0'
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
        $modules.Count | Should -Be 10
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Bar,Bar,Download,Foo,Foo,Foo,Foo,Zoo,Zoo,Zoo"

        $modules[0].ModuleType | Should -BeExactly "Manifest"
        $modules[1].ModuleType | Should -BeExactly "Script"
        $modules[2].ModuleType | Should -BeExactly "Script"
        $modules[3].ModuleType | Should -BeExactly "Manifest"
        $modules[3].Version | Should -Be "1.1"
        $modules[4].ModuleType | Should -BeExactly "Script"
        $modules[5].ModuleType | Should -BeExactly "Manifest"
        $modules[5].Version | Should -Be "2.0"
        $modules[6].ModuleType | Should -BeExactly "Script"
        $modules[7].ModuleType | Should -BeExactly "Script"
        $modules[7].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
        $modules[8].ModuleType | Should -BeExactly "Manifest"
        $modules[8].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psd1").Path
        $modules[9].ModuleType | Should -BeExactly "Script"
        $modules[9].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psm1").Path
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
        $modules.Count | Should -Be 4
        $modules = $modules | Sort-Object -Property Name, Version
        $modules.Name -join "," | Should -BeExactly "Bar,Foo,Foo,Zoo"
        $modules[1].Version | Should -Be "1.1"
        $modules[2].Version | Should -Be '2.0'
    }

    It "Get-Module <Path> -ListAvailable -All" {
        $modules = Get-Module "$testdrive\Modules\*" -ListAvailable -All
        $modules.Count | Should -Be 5
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Bar,Foo,Foo,Zoo,Zoo"
        $modules[3].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
    }

    It "Get-Module -FullyQualifiedName <FullyQualifiedName> -ListAvailable" {
        $moduleSpecification  = @{ModuleName = "Foo"; ModuleVersion = "2.0"}
        $modules = Get-Module -FullyQualifiedName $moduleSpecification -ListAvailable
        $modules | Should -HaveCount 1
        $modules.Name | Should -BeExactly "Foo"
        $modules.Version | Should -BeExactly "2.0"
    }

    It "Get-Module <Name> -Refresh -ListAvailable" {
        $modules = Get-Module -Name 'Zoo' -ListAvailable
        $modules | Should -HaveCount 1
        $modules.Name | Should -BeExactly "Zoo"
        $modules.ExportedFunctions.Count | Should -Be 0 -Because 'No exports were defined'

        New-ModuleManifest -Path "$testdrive\Modules\Zoo\Zoo.psd1" -FunctionsToExport 'Test-ZooFunction'

        $modules = Get-Module -Name 'Zoo' -ListAvailable -Refresh
        $modules | Should -HaveCount 1
        $modules.Name | Should -BeExactly "Zoo"
        $modules.ExportedFunctions.Count | Should -Be 1 -Because 'We added a new function to export'
    }

    It "Get-Module respects absolute paths in module specifications: <ModPath>" -TestCases $fullyQualifiedPathTestCases {
        param([string]$ModPath, [string]$Name, [string]$Version, [int]$Count)

        $modSpec = @{
            ModuleName = $ModPath
            RequiredVersion = $Version
        }

        $modules = Get-Module -ListAvailable -FullyQualifiedName $modSpec
        $modules | Should -HaveCount $Count
        $modules[0].Name | Should -BeExactly $Name
        $modules.Version | Should -Contain $Version
    }

    Context "PSEdition" {

        BeforeAll {
            New-Item -ItemType Directory -Path "$testdrive\Modules\DesktopOnlyModule" -Force > $null
            New-Item -ItemType Directory -Path "$testdrive\Modules\CoreOnlyModule" -Force > $null
            New-Item -ItemType Directory -Path "$testdrive\Modules\CoreAndDesktopModule" -Force > $null

            New-ModuleManifest -Path "$testdrive\Modules\DesktopOnlyModule\DesktopOnlyModule.psd1" -CompatiblePSEditions Desktop
            New-ModuleManifest -Path "$testdrive\Modules\CoreOnlyModule\CoreOnlyModule.psd1" -CompatiblePSEditions Core
            New-ModuleManifest -Path "$testdrive\Modules\CoreAndDesktopModule\CoreAndDesktopModule.psd1" -CompatiblePSEditions Core, Desktop

            New-Item -ItemType File -Path "$testdrive\Modules\DesktopOnlyModule\DesktopOnlyModule.psm1" > $null
            New-Item -ItemType File -Path "$testdrive\Modules\CoreOnlyModule\CoreOnlyModule.psm1" > $null
            New-Item -ItemType File -Path "$testdrive\Modules\CoreAndDesktopModule\CoreAndDesktopModule.psm1" > $null
        }

        It "Get-Module -PSEdition <CompatiblePSEditions> -ListAvailable" -TestCases @(
            @{ CompatiblePSEditions = 'Desktop'; ExpectedModule = 'CoreAndDesktopModule', 'DesktopOnlyModule' },
            @{ CompatiblePSEditions = 'Core'   ; ExpectedModule = 'CoreAndDesktopModule', 'CoreOnlyModule' }
        ) {
            param ($CompatiblePSEditions, $ExpectedModule)
            $modules = Get-Module -PSEdition $CompatiblePSEditions -ListAvailable
            $modules | Should -HaveCount $ExpectedModule.Count
            $modules.Name | Sort-Object | Should -BeExactly $ExpectedModule
        }
    }

    Context "Module analysis shouldn't load assembly" {
        BeforeAll {
            $tempModulePath = Join-Path $TestDrive "TempModules"
            $testModuleDir = Join-Path $tempModulePath "MyModuelTest"
            $moduleManifest = Join-Path $testModuleDir "MyModuelTest.psd1"
            $assemblyPath = Join-Path $testModuleDir "MyModuelTestCommandAssembly.dll"

            $null = New-Item $testModuleDir -ItemType Directory -ErrorAction SilentlyContinue
            if (-not (Test-Path $moduleManifest))
            {
                Set-Content $moduleManifest -Value @'
    @{
        RootModule = 'MyModuelTestCommandAssembly.dll'
        ModuleVersion = '0.0.1'
        GUID = '5776ed43-1607-4e64-be76-acacdf8e9c8c'
        FunctionsToExport = @()
        CmdletsToExport = @("Get-Test")
        AliasesToExport = @()
    }
'@
            }

            $code = @'
    using System.Management.Automation;

    [Cmdlet("Get", "Test")]
    public class MyModuelTestCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject("BLAH");
        }
    }
'@
            if (-not (Test-Path $assemblyPath))
            {
                Add-Type -TypeDefinition $code -OutputAssembly $assemblyPath
            }
        }

        It "'Get-Module -ListAvailable' should not load the module assembly" {
            ## $fullName should be null and thus the result should just be the module's name.
            $result = pwsh -c "`$env:PSModulePath = '$tempModulePath'; `$module = Get-Module -ListAvailable; `$fullName = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object Location -eq $assemblyPath | Foreach-Object FullName; `$module.Name + `$fullName"
            $result | Should -BeExactly "MyModuelTest"
        }
    }
}
