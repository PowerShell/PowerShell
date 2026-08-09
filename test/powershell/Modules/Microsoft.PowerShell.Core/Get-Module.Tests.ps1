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

Describe 'Get-Module -ListAvaiable -(FullyQualifiedName|Name) <path> when argument is absolute path' -Tags "CI" {
    BeforeAll {
        $psModulePath = ($env:PSModulePath -split ';')[0]
    }

    It 'wrongly returns module information instead of $null or error for missing script module' {
        $path = [System.IO.Path]::GetFullPath("$pwd\missing.psm1")
        Test-Path $path | Should -BeFalse
        Get-Module -ListAvailable -FullyQualifiedName $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -Name $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
    }

    It 'wrongly returns module information instead of $null or error for missing script module under $env:PSModulePath' {
        $path = [System.IO.Path]::GetFullPath("$psModulePath\missing.psm1")
        Test-Path $path | Should -BeFalse
        Get-Module -ListAvailable -FullyQualifiedName $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
        Get-Module -ListAvailable -Name $path | Should -BeOfType ([System.Management.Automation.PSModuleInfo])
    }

    It 'writes error for missing manifest module' {
        $path = [System.IO.Path]::GetFullPath("$pwd\missing")
        Test-Path $path | Should -BeFalse
        { Get-Module -ListAvailable -FullyQualifiedName $path -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        { Get-Module -ListAvailable -Name $path -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
    }

    It 'writes error for missing manifest module under $env:PSModulePath' {
        $path = [System.IO.Path]::GetFullPath("$psModulePath\missing")
        Test-Path $path | Should -BeFalse
        { Get-Module -ListAvailable -FullyQualifiedName $path -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        { Get-Module -ListAvailable -Name $path -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
    }

    Context 'Locating existing script module' {
        BeforeAll {
            $psModulePath = ($env:PSModulePath -split ';')[0]

            # Script modules
            #
            # Under $env:PSModulePath. TODO: Is this a supported scenario?
            $inPSModulePathLooseFilePath = Join-Path $psModulePath 'loose.psm1'
            New-Item -ItemType File -Force $inPSModulePathLooseFilePath > $null
            #
            # Under $pwd
            $inPSModulePathLooseFilePathPwd = Join-Path $pwd 'loose.psm1'
            New-Item -ItemType File -Force $inPSModulePathLooseFilePathPwd > $null
        }

        AfterAll {
            Remove-Item $inPSModulePathLooseFilePath
            Remove-Item $inPSModulePathLooseFilePathPwd
        }

        # TODO: This looks like a bug.
        It 'wrongly writes error instead of returning module information for existing script module under $env:PSModulePath using basename' {
            Test-Path "$psModulePath\loose.psm1" | Should -BeTrue
            { Get-Module -ListAvailable -Name "$psModulePath\loose" -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
            { Get-Module -ListAvailable -FullyQualifiedName "$psModulePath\loose" -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        }

        # TODO: This looks like a bug.
        It 'wrongly writes error instead of returning module information for existing script module using basename' {
            Test-Path "$pwd\loose.psm1" | Should -BeTrue
            { Get-Module -ListAvailable -Name "$pwd\loose" -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
            { Get-Module -ListAvailable -FullyQualifiedName "$pwd\loose" -ErrorAction Stop } | Should -Throw -Because '*Update the Name parameter*'
        }

        It 'returns module information for existing script module using file name' {
            Test-Path "$pwd\loose.psm1" | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name "$pwd\loose.psm1"
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly "$pwd\loose.psm1"

            $actual = Get-Module -ListAvailable -FullyQualifiedName "$pwd\loose.psm1"
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly "$pwd\loose.psm1"
        }

        It 'returns module information for existing script module under $env:PSModulePath using file name' {
            Test-Path "$psModulePath\loose.psm1" | Should -BeTrue

            $actual = Get-Module -ListAvailable -Name "$psModulePath\loose.psm1"
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly "$psModulePath\loose.psm1"

            $actual = Get-Module -ListAvailable -FullyQualifiedName "$psModulePath\loose.psm1"
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly "$psModulePath\loose.psm1"
        }
    }

    Context 'When argument contains wildcards' {
        BeforeAll {
            $psModulePath = ($env:PSModulePath -split ';')[0]

            # Manifest modules under $env:PSModulePath
            #
            # Versioned, manifest
            $inPSModulePathWithManifestFileName = 'existing'
            $inPSModulePatWithManifestDirectory = Join-Path $psModulePath $inPSModulePathWithManifestFileName '0.0.1'
            $inPSModulePathWithManifestFilePath = Join-Path $inPSModulePatWithManifestDirectory "$inPSModulePathWithManifestFileName.psm1"
            New-Item -ItemType File -Force $inPSModulePathWithManifestFilePath > $null
            New-ModuleManifest -Path (Join-Path $inPSModulePatWithManifestDirectory "$inPSModulePathWithManifestFileName.psd1")
            #
            # Versioned, manifest
            $inPSModulePathWithManifestFileName2 = 'existing2'
            $inPSModulePatWithManifestDirectory2 = Join-Path $psModulePath $inPSModulePathWithManifestFileName2 '0.0.1'
            $inPSModulePathWithManifestFilePath2 = Join-Path $inPSModulePatWithManifestDirectory2 "$inPSModulePathWithManifestFileName2.psm1"
            New-Item -ItemType File -Force $inPSModulePathWithManifestFilePath2 > $null
            New-ModuleManifest -Path (Join-Path $inPSModulePatWithManifestDirectory2 "$inPSModulePathWithManifestFileName2.psd1")

            # Manifest modules under $pwd
            #
            # Versioned, manifest
            $inPSModulePathWithManifestFileNamePwd = 'existing'
            $inPSModulePatWithManifestDirectoryPwd = Join-Path $pwd $inPSModulePathWithManifestFileNamePwd '0.0.1'
            $inPSModulePathWithManifestFilePathPwd = Join-Path $inPSModulePatWithManifestDirectoryPwd "$inPSModulePathWithManifestFileNamePwd.psm1"
            New-Item -ItemType File -Force $inPSModulePathWithManifestFilePathPwd > $null
            New-ModuleManifest -Path (Join-Path $inPSModulePatWithManifestDirectoryPwd "$inPSModulePathWithManifestFileNamePwd.psd1")
            #
            # Versioned, manifest
            $inPSModulePathWithManifestFileName2Pwd = 'existing2'
            $inPSModulePatWithManifestDirectory2Pwd = Join-Path $pwd $inPSModulePathWithManifestFileName2Pwd '0.0.1'
            $inPSModulePathWithManifestFilePath2Pwd = Join-Path $inPSModulePatWithManifestDirectory2Pwd "$inPSModulePathWithManifestFileName2Pwd.psm1"
            New-Item -ItemType File -Force $inPSModulePathWithManifestFilePath2Pwd > $null
            New-ModuleManifest -Path (Join-Path $inPSModulePatWithManifestDirectory2Pwd "$inPSModulePathWithManifestFileName2Pwd.psd1")

            # Script modules
            #
            # Under $env:PSModulePath. TODO: Is this a supported scenario?
            $inPSModulePathLooseFilePath = Join-Path $psModulePath 'loose.psm1'
            New-Item -ItemType File -Force $inPSModulePathLooseFilePath > $null
        }

        AfterAll {
            Remove-Item -Force -Recurse $inPSModulePatWithManifestDirectory
            Remove-Item -Force -Recurse $inPSModulePatWithManifestDirectory2

            Remove-Item -Force -Recurse $inPSModulePatWithManifestDirectoryPwd
            Remove-Item -Force -Recurse $inPSModulePatWithManifestDirectory2Pwd

            Remove-Item $inPSModulePathLooseFilePath
        }

        It 'returns existing manifest modules under $env:PSModulePath when using the -Name parameter' {
            $actual = Get-Module -ListAvailable -Name "$psModulePath\existing*"
            $actual | Should -HaveCount 2
            $actual[0].Path | Should -BeExactly "$psModulePath\existing\0.0.1\existing.psd1"
            $actual[1].Path | Should -BeExactly "$psModulePath\existing2\0.0.1\existing2.psd1"
        }

        # TODO: This looks like a bug.
        It 'wrongly returns $null for existing manifest modules under $env:PSModulePath when using the -FullyQualifiedName parameter' {
            $actual = Get-Module -ListAvailable -FullyQualifiedName "$psModulePath\existing*"
            $actual | Should -Be $null
        }

        It 'returns existing manifest modules when using the -Name parameter' {
            $actual = Get-Module -ListAvailable -Name "$pwd\existing*"
            $actual | Should -HaveCount 2
            $actual[0].Path | Should -BeExactly "$pwd\existing\0.0.1\existing.psd1"
            $actual[1].Path | Should -BeExactly "$pwd\existing2\0.0.1\existing2.psd1"
        }

        # TODO: This looks like a bug.
        It 'wrongly returns $null for existing manifest modules when using the -FullyQualifiedName parameter' {
            $actual = Get-Module -ListAvailable -FullyQualifiedName "$pwd\existing*"
            $actual | Should -Be $null
        }

        It 'returns module information for existing script module under $env:PSModulePath' {
            $actual = Get-Module -ListAvailable -Name "$psModulePath\loose*"
            $actual | Should -HaveCount 1
            $actual[0].Path | Should -BeExactly "$psModulePath\loose.psm1"
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
