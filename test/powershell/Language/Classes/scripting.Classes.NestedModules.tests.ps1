# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'NestedModules' -Tags "CI" {
    BeforeAll {
        function New-TestModule {
            param(
                [string]$Name,
                [string]$Content,
                [string[]]$NestedContents
            )

            New-Item -type directory -Force "TestDrive:\$Name" > $null
            $manifestParams = @{
                Path = "TestDrive:\$Name\$Name.psd1"
            }

            if ($Content) {
                Set-Content -Path "${TestDrive}\$Name\$Name.psm1" -Value $Content
                $manifestParams['RootModule'] = "$Name.psm1"
            }

            if ($NestedContents) {
                $manifestParams['NestedModules'] = 1..$NestedContents.Count | ForEach-Object {
                    $null = New-Item -type directory TestDrive:\$Name\Nested$_
                    $null = Set-Content -Path "${TestDrive}\$Name\Nested$_\Nested$_.psm1" -Value $NestedContents[$_ - 1]
                    "Nested$_"
                }
            }

            New-ModuleManifest @manifestParams

            $resolvedTestDrivePath = Split-Path ((Get-ChildItem TestDrive:\)[0].FullName)
            if (-not ($env:PSModulePath -like "*$resolvedTestDrivePath*")) {
                $env:PSModulePath += "$([System.IO.Path]::PathSeparator)$resolvedTestDrivePath"
            }
        }

        $originalPSModulePath = $env:PSModulePath

        # Create modules in TestDrive:\
        New-TestModule -Name NoRoot -NestedContents @(
            'class A { [string] foo() { return "A1"} }',
            'class A { [string] foo() { return "A2"} }'
        )

        New-TestModule -Name WithRoot -NestedContents @(
            'class A { [string] foo() { return "A1"} }',
            'class A { [string] foo() { return "A2"} }'
        ) -Content 'class A { [string] foo() { return "A0"} }'

        New-TestModule -Name ABC -NestedContents @(
            'class A { [string] foo() { return "A"} }',
            'class B { [string] foo() { return "B"} }'
        ) -Content 'class C { [string] foo() { return "C"} }'
    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
        Get-Module @('ABC', 'NoRoot', 'WithRoot') | Remove-Module
    }


    It 'Get-Module is able to find types' {
        $module = Get-Module NoRoot -ListAvailable
        $module.GetExportedTypeDefinitions().Count | Should -Be 1

        $module = Get-Module WithRoot -ListAvailable
        $module.GetExportedTypeDefinitions().Count | Should -Be 1

        $module = Get-Module ABC -ListAvailable
        $module.GetExportedTypeDefinitions().Count | Should -Be 3
    }

    It 'Import-Module pick the right type' {
        $module = Import-Module ABC -PassThru
        $module.GetExportedTypeDefinitions().Count | Should -Be 3
        $module = Import-Module ABC -PassThru -Force
        $module.GetExportedTypeDefinitions().Count | Should -Be 3

        $module = Import-Module NoRoot -PassThru
        $module.GetExportedTypeDefinitions().Count | Should -Be 1
        $module = Import-Module NoRoot -PassThru -Force
        $module.GetExportedTypeDefinitions().Count | Should -Be 1
        [scriptblock]::Create(@'
using module NoRoot
[A]::new().foo()
'@
).Invoke() | Should -Be A2

        $module = Import-Module WithRoot -PassThru
        $module.GetExportedTypeDefinitions().Count | Should -Be 1
        $module = Import-Module WithRoot -PassThru -Force
        $module.GetExportedTypeDefinitions().Count | Should -Be 1
        [scriptblock]::Create(@'
using module WithRoot
[A]::new().foo()
'@
).Invoke() | Should -Be A0
    }

    Context 'execute type creation in the module context' {

        # let's define types to make it more fun
        class A { [string] foo() { return "local"} }
        class B { [string] foo() { return "local"} }
        class C { [string] foo() { return "local"} }

        # We need to think about it: should it work or not.
        # Currently, types are resolved in compile-time to the 'local' versions
        # So at runtime we don't call the module versions.
        It 'Can execute type creation in the module context with new()' -Pending {
            & (Get-Module ABC) { [C]::new().foo() } | Should -Be C
            & (Get-Module NoRoot) { [A]::new().foo() } | Should -Be A2
            & (Get-Module WithRoot) { [A]::new().foo() } | Should -Be A0
            & (Get-Module ABC) { [A]::new().foo() } | Should -Be A
        }

        It 'Can execute type creation in the module context with New-Object' {
            & (Get-Module ABC) { (New-Object C).foo() } | Should -Be C
            & (Get-Module NoRoot) { (New-Object A).foo() } | Should -Be A2
            & (Get-Module WithRoot) { (New-Object A).foo() } | Should -Be A0
            & (Get-Module ABC) { (New-Object A).foo() } | Should -Be A
        }
    }
}
