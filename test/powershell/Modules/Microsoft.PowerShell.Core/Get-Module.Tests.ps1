# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Module -ListAvailable" -Tags "CI" {

    BeforeAll {
        $originalPSModulePath = $env:PSModulePath

        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\1.1" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\2.0" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Bar\Download" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Zoo\Too" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Az" -Force > $null

        New-ModuleManifest -Path "$testdrive\Modules\Foo\1.1\Foo.psd1" -ModuleVersion 1.1
        New-ModuleManifest -Path "$testdrive\Modules\Foo\2.0\Foo.psd1" -ModuleVersion 2.0
        New-ModuleManifest -Path "$testdrive\Modules\Bar\Bar.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Zoo\Zoo.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Az\Az.psd1" -ModuleVersion 1.1

        New-Item -ItemType File -Path "$testdrive\Modules\Foo\1.1\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Foo\2.0\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Bar.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Download\Download.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Zoo\Zoo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Zoo\Too\Zoo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Az\Az.psm1" > $null

        $fullyQualifiedPathTestCases = @(
            @{ ModPath = "$TestDrive/Modules\Foo"; Name = 'Foo'; Version = '2.0'; Count = 1 }
            @{ ModPath = "$TestDrive\Modules/Foo\1.1/Foo.psd1"; Name = 'Foo'; Version = '1.1'; Count = 1 }
            @{ ModPath = "$TestDrive\Modules/Bar.psd1"; Name = 'Bar'; Version = '0.0'; Count = 1 }
            @{ ModPath = "$TestDrive\Modules\Zoo\Too\Zoo.psm1"; Name = 'Zoo'; Version = '0.0'; Count = 1 }
        )

        $listModuleNameTestCases = @(
            @{
                Name = 'Foo'
                TestCaseName = 'Match case'
                ExpectedName = 'Foo'
                ModuleVersion = '2.0'
            }
            @{
                Name = 'foo'
                TestCaseName = 'Mismatched case'
                ExpectedName = 'Foo'
                ModuleVersion = '2.0'
            }
        )
        $loadedModuleNameTestCases = @(
            @{
                Name = 'Microsoft.PowerShell.Managemen*'
                TestCaseName = 'Wildcard'
                ExpectedName = 'Microsoft.PowerShell.Management'
                ModuleVersion = '7.0.0.0'
            }
            @{
                Name = 'microsoft.powershell.managemen*'
                TestCaseName = 'Mismatched case'
                ExpectedName = 'Microsoft.PowerShell.Management'
                ModuleVersion = '7.0.0.0'
            }
        )

        $env:PSModulePath = Join-Path $testdrive "Modules"
    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
    }

    It "Get-Module -ListAvailable" {
        $modules = Get-Module -ListAvailable
        $modules.Count | Should -Be 5
        $modules = $modules | Sort-Object -Property Name, Version
        $modules.Name -join "," | Should -BeExactly "Az,Bar,Foo,Foo,Zoo"
        $modules[0].Version | Should -Be "1.1"
        $modules[1].Version | Should -Be "0.0.1"
        $modules[2].Version | Should -Be '1.1'
        $modules[3].Version | Should -Be '2.0'
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
        $modules.Count | Should -Be 12
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Az,Az,Bar,Bar,Download,Foo,Foo,Foo,Foo,Zoo,Zoo,Zoo"

        $modules[0].ModuleType | Should -BeExactly "Manifest"
        $modules[1].ModuleType | Should -BeExactly "Script"
        $modules[2].ModuleType | Should -BeExactly "Manifest"
        $modules[3].ModuleType | Should -BeExactly "Script"
        $modules[4].ModuleType | Should -BeExactly "Script"
        $modules[5].ModuleType | Should -BeExactly "Manifest"
        $modules[5].Version | Should -Be "1.1"
        $modules[6].ModuleType | Should -BeExactly "Script"
        $modules[7].ModuleType | Should -BeExactly "Manifest"
        $modules[7].Version | Should -Be "2.0"
        $modules[8].ModuleType | Should -BeExactly "Script"
        $modules[9].ModuleType | Should -BeExactly "Script"
        $modules[9].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
        $modules[10].ModuleType | Should -BeExactly "Manifest"
        $modules[10].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psd1").Path
        $modules[11].ModuleType | Should -BeExactly "Script"
        $modules[11].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Zoo.psm1").Path
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
        $modules[2].Version | Should -Be "1.1"
        $modules[3].Version | Should -Be '2.0'
    }

    It "Get-Module <Path> -ListAvailable -All" {
        $modules = Get-Module "$testdrive\Modules\*" -ListAvailable -All
        $modules.Count | Should -Be 6
        $modules = $modules | Sort-Object -Property Name, Path
        $modules.Name -join "," | Should -BeExactly "Az,Bar,Foo,Foo,Zoo,Zoo"
        $modules[4].Path | Should -BeExactly (Resolve-Path "$testdrive\Modules\Zoo\Too\Zoo.psm1").Path
    }

    It "Get-Module -FullyQualifiedName @{ModuleName = '<Name>' ; ModuleVersion = '<ModuleVersion>'} -ListAvailable - <TestCaseName>" -TestCases $listModuleNameTestCases {
        param(
            [Parameter(Mandatory = $true)]
            $Name,
            $TestCaseName,
            [Parameter(Mandatory = $true)]
            $ExpectedName,
            $ModuleVersion
        )

        $moduleSpecification  = @{ModuleName = $name ; ModuleVersion = $ModuleVersion}
        $modules = Get-Module -FullyQualifiedName $moduleSpecification -ListAvailable
        $modules | Should -HaveCount 1
        $modules.Name | Should -BeExactly $ExpectedName
        $modules.Version | Should -BeExactly $ModuleVersion
    }

    It "Get-Module -FullyQualifiedName @{ModuleName = '<Name>' ; ModuleVersion = '<ModuleVersion>'} - <TestCaseName>" -TestCases $loadedModuleNameTestCases {
        param(
            [Parameter(Mandatory = $true)]
            $Name,
            $TestCaseName,
            [Parameter(Mandatory = $true)]
            $ExpectedName,
            $ModuleVersion
        )

        $moduleSpecification  = @{ModuleName = $name ; ModuleVersion = $ModuleVersion}
        $modules = Get-Module -FullyQualifiedName $moduleSpecification
        $modules | Should -HaveCount 1
        $modules.Name | Should -BeExactly $ExpectedName
        $modules.Version | Should -BeExactly $ModuleVersion
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
            $result = & "$PSHOME/pwsh" -noprofile -c "`$env:PSModulePath = '$tempModulePath'; `$module = Get-Module -ListAvailable; `$fullName = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object Location -eq $assemblyPath | Foreach-Object FullName; `$module.Name + `$fullName"
            $result | Should -BeExactly "MyModuelTest"
        }
    }
}

Describe 'Get-Module -ListAvailable -(FullyQualifiedName|Name) <path> when argument is home-rooted' -Tags "CI" {
    It 'wrongly does not expand ''~'' to $HOME' {
        $path = Join-Path ~ missing.psm1
        Test-Path $path | Should -BeFalse

        Get-Module -ListAvailable -Name $path | ForEach-Object Path | Should -BeExactly (Join-Path $HOME missing.psm1)
        # TODO: This is a bug.
        Get-Module -ListAvailable -FullyQualifiedName $path | ForEach-Object Path | Should -BeExactly (Join-Path $pwd $path)
    }
}

Describe 'Get-Module -ListAvailable -(FullyQualifiedName|Name) <path> when argument is relative-rooted' -Tags "CI" {
    It 'wrongly returns module information instead of $null or error for missing script module' {
        $path1 = Join-Path . missing.psm1
        $path2 = Join-Path .. missing.psm1
        Test-Path $path1 | Should -BeFalse
        Test-Path $path2 | Should -BeFalse

        Get-Module -ListAvailable -Name $path1 | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -Name $path2 | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -FullyQualifiedName $path1 | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -FullyQualifiedName $path2 | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
    }

    It 'writes error for missing manifest module' {
        $path1 = Join-Path . missing
        $path2 = Join-Path .. missing
        Test-Path $path1 | Should -BeFalse
        Test-Path $path2 | Should -BeFalse

        { Get-Module -ListAvailable -Name $path1 -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        { Get-Module -ListAvailable -Name $path2 -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        { Get-Module -ListAvailable -FullyQualifiedName $path1 -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        { Get-Module -ListAvailable -FullyQualifiedName $path2 -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
    }

    Context 'Locating existing script module' {
        BeforeAll {
            # Script modules
            #
            # Under $env:PSModulePath. TODO: Is this a supported scenario?
            $inPSModulePathLooseFilePath = Join-Path . loose.psm1
            New-Item -ItemType File -Force $inPSModulePathLooseFilePath > $null
            #
            # Under $pwd
            $inPSModulePathLooseFilePathParent = Join-Path .. loose.psm1
            New-Item -ItemType File -Force $inPSModulePathLooseFilePathParent > $null
        }

        AfterAll {
            Remove-Item $inPSModulePathLooseFilePath
            Remove-Item $inPSModulePathLooseFilePathParent
        }

        # TODO: This looks like a bug.
        It 'wrongly writes error instead of returning module information for existing script module using basename' {
            $path1 = Join-Path . loose.psm1
            $path2 = Join-Path .. loose.psm1
            Test-Path $path1 | Should -BeTrue
            Test-Path $path2 | Should -BeTrue

            $name1 = Join-Path . loose
            $name2 = Join-Path .. loose

            $err = { Get-Module -ListAvailable -Name $name1 -ErrorAction Stop } | Should -Throw -PassThru
            $err.Exception.Message | Should -BeLike '*Update the Name parameter*'

            $err = { Get-Module -ListAvailable -Name $name2 -ErrorAction Stop } | Should -Throw -PassThru
            $err.Exception.Message | Should -BeLike '*Update the Name parameter*'
        }

        It 'returns module information for existing script module using file name' {
            $name1 = Join-Path . loose.psm1
            $name2 = Join-Path .. loose.psm1
            Test-Path $name1 | Should -BeTrue
            Test-Path $name2 | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name $name1
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath($name1))

            $actual = Get-Module -ListAvailable -Name $name2
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath($name2))

            $actual = Get-Module -ListAvailable -FullyQualifiedName $name1
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath($name1))

            $actual = Get-Module -ListAvailable -FullyQualifiedName $name2
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath($name2))
        }
    }

    Context 'When argument contains wildcards' {
        BeforeAll {
            # Manifest modules under '.'
            #
            # Versioned, manifest
            $inCwdWithManifestFileName = 'existing'
            $inCwdWithManifestDirectory = Join-Path '.' $inCwdWithManifestFileName '0.0.1'
            $inCwdWithManifestFilePath = Join-Path $inCwdWithManifestDirectory "$inCwdWithManifestFileName.psm1"
            New-Item -ItemType File -Force $inCwdWithManifestFilePath > $null
            New-ModuleManifest -Path (Join-Path $inCwdWithManifestDirectory "$inCwdWithManifestFileName.psd1")
            #
            # Versioned, manifest
            $inCwdWithManifestFileName2 = 'existing2'
            $inCwdWithManifestDirectory2 = Join-Path '.' $inCwdWithManifestFileName2 '0.0.1'
            $inCwdWithManifestFilePath2 = Join-Path $inCwdWithManifestDirectory2 "$inCwdWithManifestFileName2.psm1"
            New-Item -ItemType File -Force $inCwdWithManifestFilePath2 > $null
            New-ModuleManifest -Path (Join-Path $inCwdWithManifestDirectory2 "$inCwdWithManifestFileName2.psd1")

            # Manifest modules under '..'
            #
            # Versioned, manifest
            $inParentWithManifestFileName = 'existing'
            $inParentWithManifestDirectory = Join-Path '..' $inParentWithManifestFileName '0.0.1'
            $inParentWithManifestFilePath = Join-Path $inParentWithManifestDirectory "$inParentWithManifestFileName.psm1"
            New-Item -ItemType File -Force $inParentWithManifestFilePath > $null
            New-ModuleManifest -Path (Join-Path $inParentWithManifestDirectory "$inParentWithManifestFileName.psd1")
            #
            # Versioned, manifest
            $inParentWithManifestFileName2 = 'existing2'
            $inParentWithManifestDirectory2 = Join-Path '..' $inParentWithManifestFileName2 '0.0.1'
            $inParentWithManifestFilePath2 = Join-Path $inParentWithManifestDirectory2 "$inParentWithManifestFileName2.psm1"
            New-Item -ItemType File -Force $inParentWithManifestFilePath2 > $null
            New-ModuleManifest -Path (Join-Path $inParentWithManifestDirectory2 "$inParentWithManifestFileName2.psd1")

            # Script modules under '.' and '..'
            #
            # Under '.'
            $inCwdLooseFilePath = Join-Path '.' 'loose.psm1'
            New-Item -ItemType File -Force $inCwdLooseFilePath > $null
            #
            # Under '..'
            $inParentLooseFilePath = Join-Path '..' 'loose.psm1'
            New-Item -ItemType File -Force $inParentLooseFilePath > $null
        }

        AfterAll {
            Remove-Item -Force -Recurse $inCwdWithManifestDirectory
            Remove-Item -Force -Recurse $inCwdWithManifestDirectory2

            Remove-Item -Force -Recurse $inParentWithManifestDirectory
            Remove-Item -Force -Recurse $inParentWithManifestDirectory2

            Remove-Item $inCwdLooseFilePath
        }

        It 'returns existing manifest modules when using the -Name parameter' {
            $actual = Get-Module -ListAvailable -Name '.\existing*'
            $actual | Should -HaveCount 2
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath('.\existing\0.0.1\existing.psd1'))
            $actual[1].Path | Should -BeExactly ([System.IO.Path]::GetFullPath('.\existing2\0.0.1\existing2.psd1'))

            $actual = Get-Module -ListAvailable -Name '..\existing*'
            $actual | Should -HaveCount 2
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath('..\existing\0.0.1\existing.psd1'))
            $actual[1].Path | Should -BeExactly ([System.IO.Path]::GetFullPath('..\existing2\0.0.1\existing2.psd1'))
        }

        # TODO: This looks like a bug.
        It 'wrongly returns $null for existing manifest modules when using the -FullyQualifiedName parameter' {
            $actual = Get-Module -ListAvailable -FullyQualifiedName '.\existing*'
            $actual | Should -Be $null

            $actual = Get-Module -ListAvailable -FullyQualifiedName '..\existing*'
            $actual | Should -Be $null
        }

        It 'returns module information for existing script module' {
            $actual = Get-Module -ListAvailable -Name '.\loose*'
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath('.\loose.psm1'))

            $actual = Get-Module -ListAvailable -FullyQualifiedName '..\loose*'
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly ([System.IO.Path]::GetFullPath('..\loose.psm1'))
        }
    }
}

Describe 'Get-Module -ListAvailable -(FullyQualifiedName|Name) <path> when argument is absolute path' -Tags "CI" {
    BeforeAll {
        $oldPSModulePath = $env:PSModulePath
        $env:PSModulePath = New-Item -ItemType Directory (Join-Path $TestDrive modules)
    }

    AfterAll {
        $env:PSModulePath = $oldPSModulePath
    }

    It 'wrongly returns module information instead of $null or error for missing script module' {
        $path = [System.IO.Path]::GetFullPath((Join-Path $pwd missing.psm1))
        Test-Path $path | Should -BeFalse

        Get-Module -ListAvailable -FullyQualifiedName $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -Name $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
    }

    It 'wrongly returns module information instead of $null or error for missing script module under $env:PSModulePath' {
        $path = [System.IO.Path]::GetFullPath((Join-Path $env:PSModulePath missing.psm1))
        Test-Path $path | Should -BeFalse

        Get-Module -ListAvailable -FullyQualifiedName $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -Name $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
    }

    It 'writes error for missing manifest module' {
        $path = [System.IO.Path]::GetFullPath((Join-Path $pwd missing))
        Test-Path $path | Should -BeFalse

        $err = { Get-Module -ListAvailable -FullyQualifiedName $path -ErrorAction Stop } | Should -Throw -PassThru
        $err.Exception.Message | Should -BeLike '*Update the Name parameter*'

        $err = { Get-Module -ListAvailable -Name $path -ErrorAction Stop } | Should -Throw -PassThru
        $err.Exception.Message | Should -BeLike '*Update the Name parameter*'
    }

    It 'writes error for missing manifest module under $env:PSModulePath' {
        $path = [System.IO.Path]::GetFullPath((Join-Path $env:PSModulePath missing))
        Test-Path $path | Should -BeFalse

        $err = { Get-Module -ListAvailable -FullyQualifiedName $path -ErrorAction Stop } | Should -Throw -PassThru
        $err.Exception.Message | Should -BeLike '*Update the Name parameter*'

        $err = { Get-Module -ListAvailable -Name $path -ErrorAction Stop } | Should -Throw -PassThru
        $err.Exception.Message | Should -BeLike '*Update the Name parameter*'
    }

    Context 'Locating existing script module' {
        BeforeAll {
            # Script modules
            #
            # Under $env:PSModulePath. TODO: Is this a supported scenario?
            $inPSModulePathLooseFilePath = Join-Path $env:PSModulePath 'loose.psm1'
            New-Item -ItemType File -Force $inPSModulePathLooseFilePath > $null
            #
            # Under $pwd
            $inCwdLooseFilePath = Join-Path $pwd 'loose.psm1'
            New-Item -ItemType File -Force $inCwdLooseFilePath > $null
        }

        AfterAll {
            Remove-Item $inPSModulePathLooseFilePath
            Remove-Item $inCwdLooseFilePath
        }

        # TODO: This looks like a bug.
        It 'wrongly writes error instead of returning module information for existing script module under $env:PSModulePath using basename' {
            Test-Path (Join-Path $env:PSModulePath loose.psm1) | Should -BeTrue
            $path = Join-Path $env:PSModulePath loose

            $err = { Get-Module -ListAvailable -Name $path -ErrorAction Stop } | Should -Throw -PassThru
            $err.Exception.Message | Should -BeLike '*Update the Name parameter*'

            $err = { Get-Module -ListAvailable -FullyQualifiedName $path -ErrorAction Stop } | Should -Throw -PassThru
            $err.Exception.Message | Should -BeLike '*Update the Name parameter*'
        }

        # TODO: This looks like a bug.
        It 'wrongly writes error instead of returning module information for existing script module using basename' {
            Test-Path (Join-Path $pwd loose.psm1) | Should -BeTrue
            $path = Join-Path $pwd loose

            $err = { Get-Module -ListAvailable -Name $path -ErrorAction Stop } | Should -Throw -PassThru
            $err.Exception.Message | Should -BeLike '*Update the Name parameter*'

            $err = { Get-Module -ListAvailable -FullyQualifiedName $path -ErrorAction Stop } | Should -Throw -PassThru
            $err.Exception.Message | Should -BeLike '*Update the Name parameter*'
        }

        It 'returns module information for existing script module using file name' {
            $path = Join-Path $pwd loose.psm1
            Test-Path $path | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name $path
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly $path

            $actual = Get-Module -ListAvailable -FullyQualifiedName $path
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly $path
        }

        It 'returns module information for existing script module under $env:PSModulePath using file name' {
            $path = Join-Path $env:PSModulePath loose.psm1
            Test-Path $path | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name $path
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly $path

            $actual = Get-Module -ListAvailable -FullyQualifiedName $path
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly $path
        }
    }

    Context 'When argument contains wildcards' {
        BeforeAll {
            # Manifest modules under $env:PSModulePath
            #
            # Versioned, manifest
            $inPSModulePathWithManifestFileName = 'existing'
            $inPSModulePatWithManifestDirectory = Join-Path $env:PSModulePath $inPSModulePathWithManifestFileName '0.0.1'
            $inPSModulePathWithManifestFilePath = Join-Path $inPSModulePatWithManifestDirectory "$inPSModulePathWithManifestFileName.psm1"
            New-Item -ItemType File -Force $inPSModulePathWithManifestFilePath > $null
            New-ModuleManifest -Path (Join-Path $inPSModulePatWithManifestDirectory "$inPSModulePathWithManifestFileName.psd1")
            #
            # Versioned, manifest
            $inPSModulePathWithManifestFileName2 = 'existing2'
            $inPSModulePatWithManifestDirectory2 = Join-Path $env:PSModulePath $inPSModulePathWithManifestFileName2 '0.0.1'
            $inPSModulePathWithManifestFilePath2 = Join-Path $inPSModulePatWithManifestDirectory2 "$inPSModulePathWithManifestFileName2.psm1"
            New-Item -ItemType File -Force $inPSModulePathWithManifestFilePath2 > $null
            New-ModuleManifest -Path (Join-Path $inPSModulePatWithManifestDirectory2 "$inPSModulePathWithManifestFileName2.psd1")

            # Manifest modules under $pwd
            #
            # Versioned, manifest
            $inCwdhWithManifestFileName = 'existing'
            $inCwdWithManifestDirectory = Join-Path $pwd $inCwdhWithManifestFileName '0.0.1'
            $inCwdWithManifestFilePath = Join-Path $inCwdWithManifestDirectory "$inCwdhWithManifestFileName.psm1"
            New-Item -ItemType File -Force $inCwdWithManifestFilePath > $null
            New-ModuleManifest -Path (Join-Path $inCwdWithManifestDirectory "$inCwdhWithManifestFileName.psd1")
            #
            # Versioned, manifest
            $inCwdWithManifestFileName2 = 'existing2'
            $inCwdWithManifestDirectory2 = Join-Path $pwd $inCwdWithManifestFileName2 '0.0.1'
            $inCwdWithManifestFilePath2 = Join-Path $inCwdWithManifestDirectory2 "$inCwdWithManifestFileName2.psm1"
            New-Item -ItemType File -Force $inCwdWithManifestFilePath2 > $null
            New-ModuleManifest -Path (Join-Path $inCwdWithManifestDirectory2 "$inCwdWithManifestFileName2.psd1")

            # Script modules
            #
            # Under $env:PSModulePath. TODO: Is this a supported scenario?
            $inPSModulePathLooseFilePath = Join-Path $env:PSModulePath 'loose.psm1'
            New-Item -ItemType File -Force $inPSModulePathLooseFilePath > $null
            #
            #
            # Under $pwd
            $inCwdFilePath = Join-Path $pwd 'loose.psm1'
            New-Item -ItemType File -Force $inCwdFilePath > $null
        }

        AfterAll {
            Remove-Item -Force -Recurse $inPSModulePatWithManifestDirectory
            Remove-Item -Force -Recurse $inPSModulePatWithManifestDirectory2

            Remove-Item -Force -Recurse $inCwdWithManifestDirectory
            Remove-Item -Force -Recurse $inCwdWithManifestDirectory2

            Remove-Item $inPSModulePathLooseFilePath
            Remove-Item $inCwdFilePath
        }

        It 'returns existing manifest modules under $env:PSModulePath when using the -Name parameter' {
            $moduleManifestPath1 = Join-Path $env:PSModulePath existing 0.0.1,existing.psd1
            $moduleManifestPath2 = Join-Path $env:PSModulePath existing2 0.0.1,existing2.psd1
            Test-Path $moduleManifestPath1 | Should -BeTrue
            Test-Path $moduleManifestPath2 | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name (Join-Path $env:PSModulePath 'existing*')
            $actual | Should -HaveCount 2
            $actual[0].Path | Should -BeExactly $moduleManifestPath1
            $actual[1].Path | Should -BeExactly $moduleManifestPath2
        }

        # TODO: This looks like a bug.
        It 'wrongly returns $null for existing manifest modules under $env:PSModulePath when using the -FullyQualifiedName parameter' {
            $actual = Get-Module -ListAvailable -FullyQualifiedName (Join-Path $env:PSModulePath 'existing*')
            $actual | Should -Be $null
        }

        It 'returns existing manifest modules when using the -Name parameter' {
            $moduleManifestPath1 = Join-Path $pwd existing 0.0.1,existing.psd1
            $moduleManifestPath2 = Join-Path $pwd existing2 0.0.1,existing2.psd1
            Test-Path $moduleManifestPath1 | Should -BeTrue
            Test-Path $moduleManifestPath2 | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name (Join-Path $pwd 'existing*')
            $actual | Should -HaveCount 2
            $actual[0].Path | Should -BeExactly $moduleManifestPath1
            $actual[1].Path | Should -BeExactly $moduleManifestPath2
        }

        # TODO: This looks like a bug.
        It 'wrongly returns $null for existing manifest modules when using the -FullyQualifiedName parameter' {
            $actual = Get-Module -ListAvailable -FullyQualifiedName (Join-Path $pwd 'existing*')
            $actual | Should -Be $null
        }

        It 'returns module information for existing script module under $env:PSModulePath' {
            $actual = Get-Module -ListAvailable -Name (Join-Path $env:PSModulePath 'loose*')
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly (Join-Path $env:PSModulePath loose.psm1)
        }

        It 'returns module information for existing script module using file name' {
            $path = Join-Path $pwd loose.psm1
            Test-Path $path | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name $path
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly $path

            $actual = Get-Module -ListAvailable -FullyQualifiedName $path
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly $path
        }
    }
}

Describe 'Get-Module -ListAvailable with path' -Tags "CI" {
    BeforeAll {
        $moduleName = 'Banana'
        $modulePath = Join-Path $TestDrive $moduleName
        $v1 = '1.2.3'
        $v2 = '4.8.3'
        $v1DirPath = Join-Path $modulePath $v1
        $v2DirPath = Join-Path $modulePath $v2
        $manifestV1Path = Join-Path $v1DirPath "$moduleName.psd1"
        $manifestV2Path = Join-Path $v2DirPath "$moduleName.psd1"

        New-Item -ItemType Directory $modulePath
        New-Item -ItemType Directory -Path $v1DirPath
        New-Item -ItemType Directory -Path $v2DirPath
        New-ModuleManifest -Path $manifestV1Path -ModuleVersion $v1
        New-ModuleManifest -Path $manifestV2Path -ModuleVersion $v2
    }

    It "Gets all versions by path" {
        $modules = Get-Module -ListAvailable $modulePath | Sort-Object -Property Version

        $modules | Should -HaveCount 2
        $modules[0].Name | Should -BeExactly $moduleName
        $modules[0].Path | Should -BeExactly $manifestV1Path
        $modules[0].Version | Should -Be $v1
        $modules[1].Name | Should -BeExactly $moduleName
        $modules[1].Path | Should -BeExactly $manifestV2Path
        $modules[1].Version | Should -Be $v2
    }

    It "Gets all versions by FullyQualifiedName with path with lower version" {
        $modules = Get-Module -ListAvailable -FullyQualifiedName @{ ModuleName = $modulePath; ModuleVersion = '0.0' } | Sort-Object -Property Version

        $modules | Should -HaveCount 2
        $modules[0].Name | Should -BeExactly $moduleName
        $modules[0].Path | Should -BeExactly $manifestV1Path
        $modules[0].Version | Should -Be $v1
        $modules[1].Name | Should -BeExactly $moduleName
        $modules[1].Path | Should -BeExactly $manifestV2Path
        $modules[1].Version | Should -Be $v2
    }

    It "Gets high version by FullyQualifiedName with path with high version" {
        $modules = Get-Module -ListAvailable -FullyQualifiedName @{ ModuleName = $modulePath; ModuleVersion = '2.0' } | Sort-Object -Property Version

        $modules | Should -HaveCount 1
        $modules[0].Name | Should -BeExactly $moduleName
        $modules[0].Path | Should -BeExactly $manifestV2Path
        $modules[0].Version | Should -Be $v2
    }

    It "Gets low version by FullyQualifiedName with path with low maximum version" {
        $modules = Get-Module -ListAvailable -FullyQualifiedName @{ ModuleName = $modulePath; MaximumVersion = '2.0' } | Sort-Object -Property Version

        $modules | Should -HaveCount 1
        $modules[0].Name | Should -BeExactly $moduleName
        $modules[0].Path | Should -BeExactly $manifestV1Path
        $modules[0].Version | Should -Be $v1
    }

    It "Gets low version by FullyQualifiedName with path with low maximum version and version" {
        $modules = Get-Module -ListAvailable -FullyQualifiedName @{ ModuleName = $modulePath; MaximumVersion = '2.0'; ModuleVersion = '1.0' } | Sort-Object -Property Version

        $modules | Should -HaveCount 1
        $modules[0].Name | Should -BeExactly $moduleName
        $modules[0].Path | Should -BeExactly $manifestV1Path
        $modules[0].Version | Should -Be $v1
    }

    It "Gets correct version by FullyQualifiedName with path with required version" -TestCases @(
        @{ Version = $v1 }
        @{ Version = $v2 }
    ) {
        param([version]$Version)

        switch ($Version)
        {
            $v1
            {
                $expectedPath = $manifestV1Path
                break
            }

            $v2
            {
                $expectedPath = $manifestV2Path
            }
        }

        $modules = Get-Module -ListAvailable -FullyQualifiedName @{ ModuleName = $modulePath; RequiredVersion = $Version }

        $modules | Should -HaveCount 1
        $modules[0].Name | Should -BeExactly $moduleName
        $modules[0].Path | Should -BeExactly $expectedPath
        $modules[0].Version | Should -Be $Version
    }
}
