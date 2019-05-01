# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Import-Module" -Tags "CI" {
    $moduleName = "Microsoft.PowerShell.Security"
    BeforeAll {
        $originalPSModulePath = $env:PSModulePath
        New-Item -ItemType Directory -Path "$testdrive\Modules\TestModule\1.1" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\TestModule\2.0" -Force > $null
        $env:PSModulePath += [System.IO.Path]::PathSeparator + "$testdrive\Modules"
        New-ModuleManifest -Path "$testdrive\Modules\TestModule\1.1\TestModule.psd1" -ModuleVersion 1.1
        New-ModuleManifest -Path "$testdrive\Modules\TestModule\2.0\TestModule.psd1" -ModuleVersion 2.0
    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
    }

    BeforeEach {
        Remove-Module -Name $moduleName -Force
        (Get-Module -Name $moduleName).Name | Should -BeNullOrEmpty
        Remove-Module -Name TestModule -Force -ErrorAction SilentlyContinue
    }

    AfterEach {
        Import-Module -Name $moduleName -Force
        (Get-Module -Name $moduleName).Name | Should -BeExactly $moduleName
    }

    It "should be able to add a module with using Name switch" {
        { Import-Module -Name $moduleName } | Should -Not -Throw
        (Get-Module -Name $moduleName).Name | Should -BeExactly $moduleName
    }

    It "should be able to load a module with a trailing directory separator: <modulePath>" -TestCases @(
        @{ modulePath = (Get-Module -ListAvailable $moduleName).ModuleBase + [System.IO.Path]::DirectorySeparatorChar; expectedName = $moduleName },
        @{ modulePath = Join-Path -Path $TestDrive -ChildPath "\Modules\TestModule\"; expectedName = "TestModule" }
    ) {
        param( $modulePath, $expectedName )
        { Import-Module -Name $modulePath -ErrorAction Stop } | Should -Not -Throw
        (Get-Module -Name $expectedName).Name | Should -BeExactly $expectedName
    }

    It "should be able to add a module with using ModuleInfo switch" {
        $a = Get-Module -ListAvailable $moduleName
        { Import-Module -ModuleInfo $a } | Should -Not -Throw
        (Get-Module -Name $moduleName).Name | Should -BeExactly $moduleName
    }

    It "should be able to load an already loaded module" {
        Import-Module $moduleName
        { $script:module = Import-Module $moduleName -PassThru -ErrorAction Stop } | Should -Not -Throw
        Get-Module -Name $moduleName | Should -BeExactly $script:module
    }

    It "should only load the specified version" {
        Import-Module TestModule -RequiredVersion 1.1
        (Get-Module TestModule).Version | Should -BeIn "1.1"
    }
}

Describe "Import-Module with ScriptsToProcess" -Tags "CI" {

    BeforeAll {
        $moduleRootPath = Join-Path $TestDrive 'TestModules'
        New-Item $moduleRootPath -ItemType Directory -Force | Out-Null
        Push-Location $moduleRootPath

        "1 | Out-File out.txt -Append -NoNewline" | Out-File script1.ps1
        "2 | Out-File out.txt -Append -NoNewline" | Out-File script2.ps1
        New-ModuleManifest module1.psd1 -ScriptsToProcess script1.ps1
        New-ModuleManifest module2.psd1 -ScriptsToProcess script2.ps1 -NestedModules module1.psd1
    }

    AfterAll {
        Pop-Location
    }

    BeforeEach {
        New-Item out.txt -ItemType File -Force | Out-Null
    }

    AfterEach {
        $m = @('module1','module2','script1','script2')
        remove-module $m -Force -ErrorAction SilentlyContinue
        Remove-Item out.txt -Force -ErrorAction SilentlyContinue
    }

    $testCases = @(
            @{ TestNameSuffix = 'for top-level module'; ipmoParms =  @{'Name'='.\module1.psd1'}; Expected = '1' }
            @{ TestNameSuffix = 'for top-level and nested module'; ipmoParms =  @{'Name'='.\module2.psd1'}; Expected = '21' }
            @{ TestNameSuffix = 'for top-level module when -Version is specified'; ipmoParms =  @{'Name'='.\module1.psd1'; 'Version'='0.0.1'}; Expected = '1' }
            @{ TestNameSuffix = 'for top-level and nested module when -Version is specified'; ipmoParms =  @{'Name'='.\module2.psd1'; 'Version'='0.0.1'}; Expected = '21' }
        )

    It "Verify ScriptsToProcess are executed <TestNameSuffix>" -TestCases $testCases {
        param($TestNameSuffix,$ipmoParms,$Expected)
        Import-Module @ipmoParms
        Get-Content out.txt | Should -BeExactly $Expected
    }
}

Describe "Import-Module for Binary Modules in GAC" -Tags 'Feature' {
    Context "Modules are not loaded from GAC" {
        It "Load PSScheduledJob from Windows Powershell Modules folder should fail" -Skip:(-not $IsWindows) {
            # NOTE:
            # This test attempts to load an assembly from the GAC and expects to fail.
            # However, if the assembly is already loaded, the assembly will be resolved before
            # the DisableGACLoading flag comes into effect, meaning no error is thrown and the test fails.
            #
            # Since the System32 module direction is now on the module path and Get-Module -ListAvailable
            # loads assemblies, the assembly is resolved and this test will fail if any other tests before
            # it call Get-Module -ListAvailable.
            #
            # So for now, the solution is to run this test in a fresh process.
            # Since disabling GAC loading is supposed to be a startup option, this is possibly appropriate.

            $testScript = {
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('DisableGACLoading', $true)
                $modulePath = Join-Path $env:windir "System32/WindowsPowershell/v1.0/Modules/PSScheduledJob"
                Import-Module $modulePath -SkipEditionCheck
            }
            $err = (& "$PSHOME\pwsh.exe" -c $testScript 2>&1 | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] })[0]
            $err.FullyQualifiedErrorId | Should -Be "FormatXmlUpdateException,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }
    }

    Context "Modules are loaded from GAC" {
        It "Load PSScheduledJob from Windows Powershell Modules folder" -Skip:(-not $IsWindows) {
            $modulePath = Join-Path $env:windir "System32/WindowsPowershell/v1.0/Modules/PSScheduledJob"
            Import-Module $modulePath -SkipEditionCheck
            (Get-Command New-JobTrigger).Name | Should -BeExactly 'New-JobTrigger'
        }
    }
}

Describe "Import-Module for Binary Modules" -Tags 'CI' {

    BeforeAll {
        $src = @"
            using System.Management.Automation;
            namespace ModuleCmdlets
            {
                [Cmdlet(VerbsDiagnostic.Test,"BinaryModuleCmdlet1")]
                public class TestBinaryModuleCmdlet1Command : Cmdlet
                {
                    protected override void BeginProcessing()
                    {
                        WriteObject("BinaryModuleCmdlet1 exported by the ModuleCmdlets module.");
                    }
                }
            }
"@
        Add-Type -TypeDefinition $src -OutputAssembly $TESTDRIVE\System.dll
        Add-Type -TypeDefinition $src -OutputAssembly $TESTDRIVE\System.exe
    }

    It "PS should try to load the binary module from a file path with extension: <extension>" -TestCases (
        @{extension = "dll"},
        @{extension = "exe"}
     ) {
         param ($extension)

        try {
            $TestModulePath = Join-Path $TESTDRIVE "System.$extension"
            $job = Start-Job -ScriptBlock {
                $module = Import-Module $using:TestModulePath -Passthru;
                $module.ImplementingAssembly.Location;
                Test-BinaryModuleCmdlet1
            }
            $assemblyLocation, $cmdletOutput = $job | Wait-Job -Timeout 30 | Receive-Job
        }
        finally {
            $job | Remove-Job -ErrorAction Ignore
        }

        $assemblyLocation | Should -BeExactly $TestModulePath
        $cmdletOutput | Should -BeExactly "BinaryModuleCmdlet1 exported by the ModuleCmdlets module."
    }

    It 'PS should be able to load the executable as a root module' {
        $psdFile = Join-Path $TESTDRIVE test.psd1
        $exe = Join-Path $TESTDRIVE System.exe
        New-ModuleManifest -Path $psdFile -RootModule $exe

        try {
            $job = Start-Job -ScriptBlock { Import-Module $using:psdFile -PassThru }
            $module = $job | Wait-Job -Timeout 30 | Receive-Job
        }
        finally {
            $job | Remove-Job -ErrorAction Ignore
        }

        $module | Should -Not -BeNullOrEmpty
        $module.ModuleType.ToString() | Should -Be 'Binary'
        $module.RootModule | Should -Be $exe
        $module.ExportedCmdlets['Test-BinaryModuleCmdlet1'] | Should -Be 'Test-BinaryModuleCmdlet1'
    }

    It 'PS should be able to load the executable as a nested module' {
        $psdFile = Join-Path $TESTDRIVE test.psd1
        $exe = Join-Path $TESTDRIVE System.exe
        New-ModuleManifest -Path $psdFile -NestedModules $exe

        try {
            $job = Start-Job -ScriptBlock {
                $module = Import-Module $using:psdFile -PassThru
                # return the module and the nested module assembly location
                $module
                $module.NestedModules[0].ImplementingAssembly.Location
            }
            $module, $location = $job | Wait-Job -Timeout 30 | Receive-Job
        }
        finally {
            $job | Remove-Job -ErrorAction Ignore
        }

        $module | Should -Not -BeNullOrEmpty
        $module.ModuleType.ToString() | Should -Be 'Manifest'
        $module.ExportedCmdlets['Test-BinaryModuleCmdlet1'] | Should -Be 'Test-BinaryModuleCmdlet1'
        $module.NestedModules | Should -Not -BeNullOrEmpty
        $location | Should -Be $exe
    }

    It "PS should try to load the assembly from assembly name if file path doesn't exist" {
        $psdFile = Join-Path $TESTDRIVE test.psd1
        $nestedModule = Join-Path NOExistedPath Microsoft.PowerShell.Commands.Utility.dll
        New-ModuleManifest -Path $psdFile -NestedModules $nestedModule

        try {
            $job = Start-Job -ScriptBlock {
                $module = Import-Module $using:psdFile -PassThru
                # return the module and the assembly location
                $module
                $module.NestedModules[0].ImplementingAssembly.Location
            }
            $module, $location = $job | Wait-Job -Timeout 30 | Receive-Job
        }
        finally {
            $job | Remove-Job -ErrorAction Ignore
        }

        $module.NestedModules | Should -Not -BeNullOrEmpty
        $assemblyLocation = [Microsoft.PowerShell.Commands.AddTypeCommand].Assembly.Location
        $location | Should -Be $assemblyLocation
    }

    It 'Should load from ModuleBase path before looking up in GAC' -Skip:(-not $IsWindows) {
        $module = Get-Module PSScheduledJob -ListAvailable -SkipEditionCheck
        $moduleBasePath = Split-Path $module.Path
        $destPath = New-Item -ItemType Directory -Path "$TestDrive/PSScheduledJob"
        $gacAssemblyPath = (Get-ChildItem "${env:WinDir}\Microsoft.NET\assembly\GAC_MSIL\Microsoft.PowerShell.ScheduledJob" -Recurse -Filter 'Microsoft.PowerShell.ScheduledJob.dll').FullName

        # Copy format, type and psd1
        Copy-Item "$moduleBasePath/PSScheduledJob*.*" -Destination $destPath -Force

        # Copy assembly to temp module folder"
        Copy-Item $gacAssemblyPath -Destination $destPath -Force

        # Use a different pwsh so that we do not have the PSScheduledJob module already loaded.
        $loadedAssemblyLocation = pwsh -noprofile -c "Import-Module $destPath -Force; [Microsoft.PowerShell.ScheduledJob.AddJobTriggerCommand].Assembly.Location"
        $loadedAssemblyLocation | Should -BeLike "$TestDrive*\Microsoft.PowerShell.ScheduledJob.dll"
    }
}

Describe "Import-Module should be case insensitive" -Tags 'CI' {
    BeforeAll {
        $defaultPSModuleAutoloadingPreference = $PSModuleAutoloadingPreference
        $originalPSModulePath = $env:PSModulePath.Clone()
        $modulesPath = "$TestDrive\Modules"
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $modulesPath
        $PSModuleAutoloadingPreference = "none"
    }

    AfterAll {
        $global:PSModuleAutoloadingPreference = $defaultPSModuleAutoloadingPreference
        $env:PSModulePath = $originalPSModulePath
    }

    AfterEach {
        Remove-Item -Recurse -Path $modulesPath -Force -ErrorAction SilentlyContinue
    }

    It "Import-Module can import a module using different casing using '<modulePath>' and manifest:<manifest>" -TestCases @(
        @{modulePath="TESTMODULE/1.1"; manifest=$true},
        @{modulePath="TESTMODULE"    ; manifest=$true},
        @{modulePath="TESTMODULE"    ; manifest=$false}
        ) {
        param ($modulePath, $manifest)
        New-Item -ItemType Directory -Path "$modulesPath/$modulePath" -Force > $null
        if ($manifest) {
            New-ModuleManifest -Path "$modulesPath/$modulePath/TESTMODULE.psd1" -RootModule "TESTMODULE.psm1" -ModuleVersion 1.1
        }
        Set-Content -Path "$modulesPath/$modulePath/TESTMODULE.psm1" -Value "function mytest { 'hello' }"
        Import-Module testMODULE
        $m = Get-Module TESTmodule
        $m | Should -BeOfType "System.Management.Automation.PSModuleInfo"
        $m.Name | Should -BeIn "TESTMODULE"
        mytest | Should -BeExactly "hello"
        Remove-Module TestModule
        Get-Module tESTmODULE | Should -BeNullOrEmpty
    }
}

Describe "Workflow .Xaml module is not supported in PSCore" -tags "Feature" {
    BeforeAll {
        $xamlFile = Join-Path $TestDrive "XamlTest.xaml"
        New-Item -Path $xamlFile -ItemType File -Force

        $xamlRootModule = Join-Path $TestDrive "XamlRootModule"
        New-Item -Path $xamlRootModule -ItemType Directory -Force
        Copy-Item $xamlFile $xamlRootModule
        $xamlRootModuleManifest = Join-Path $xamlRootModule "XamlRootModule.psd1"
        New-ModuleManifest -Path $xamlRootModuleManifest -RootModule "XamlTest.xaml"

        $xamlNestedModule = Join-Path $TestDrive "XamlNestedModule"
        New-Item -Path $xamlNestedModule -ItemType Directory -Force
        Copy-Item $xamlFile $xamlNestedModule
        $xamlNestedModuleManifest = Join-Path $xamlNestedModule "XamlNestedModule.psd1"
        New-ModuleManifest -Path $xamlNestedModuleManifest -NestedModules "XamlTest.xaml"
    }

    It "Import a XAML file directly should raise a 'NotSupported' error" {
        { Import-Module $xamlFile -ErrorAction Stop } | Should -Throw -ErrorId "Modules_WorkflowModuleNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
    }

    It "Import a module with XAML root module should raise a 'NotSupported' error" {
        { Import-Module $xamlRootModule -ErrorAction Stop } | Should -Throw -ErrorId "Modules_WorkflowModuleNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
    }

    It "Import a module with XAML nested module should raise a 'NotSupported' error" {
        { Import-Module $xamlNestedModule -ErrorAction Stop } | Should -Throw -ErrorId "Modules_WorkflowModuleNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
    }
}

Describe "Circular nested module test" -tags "CI" {
    BeforeAll {
        $moduleFolder = Join-Path $TestDrive CircularNestedModuleTest
        $psdPath = Join-Path $moduleFolder CircularNestedModuleTest.psd1
        $psmPath = Join-Path $moduleFolder CircularNestedModuleTest.psm1

        New-Item -Path $moduleFolder -ItemType Directory -Force > $null
        Set-Content -Path $psdPath -Value "@{ ModuleVersion = '0.0.1'; RootModule = 'CircularNestedModuleTest'; NestedModules = @('CircularNestedModuleTest') }" -Encoding Ascii
        Set-Content -Path $psmPath -Value "function bar {}" -Encoding Ascii
    }

    AfterAll {
        Remove-Module -Name CircularNestedModuleTest -Force -ErrorAction SilentlyContinue
    }

    It "Loading the module should succeed and return a module with circular nested module" {
        $m = Import-Module $psdPath -PassThru
        $m.NestedModules[0] | Should -Be $m
    }
}

Describe "Import-Module -Force behaviour" -Tag "CI" {
    BeforeAll {
        $moduleContent = 'function Test-One { return 101 }'
        $newModuleContent = "function Test-One { return 93 }`nfunction Test-Two { return 14 }"
        $modulePath = Join-Path $TestDrive 'ipmofTestModule.psm1'
    }

    AfterEach {
        Get-Module 'ipmofTestModule' | Remove-Module
    }

    It "Loads new functions when Import-Module is called with -Force" {
        Set-Content -Path $modulePath -Value $moduleContent
        Import-Module $modulePath

        Test-One | Should -Be 101
        { Test-Two } | Should -Throw -ErrorId 'CommandNotFoundException'

        Set-Content -Path $modulePath -Value $newModuleContent -Force
        Import-Module -Force $modulePath

        Test-One | Should -Be 93
        Test-Two | Should -Be 14
    }

    It "Does not load new functions when Import-Module is called without -Force" {
        Set-Content -Path $modulePath -Value $moduleContent
        Import-Module $modulePath

        Test-One | Should -Be 101
        { Test-Two } | Should -Throw -ErrorId 'CommandNotFoundException'

        Set-Content -Path $modulePath -Value $newModuleContent -Force
        Import-Module $modulePath

        Test-One | Should -Be 101
        { Test-Two } | Should -Throw -ErrorId 'CommandNotFoundException'
    }
}
