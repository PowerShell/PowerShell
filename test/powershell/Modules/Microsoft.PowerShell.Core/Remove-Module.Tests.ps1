# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Remove-Module -Name" -Tags "CI" {
    BeforeAll {
        Remove-Module -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue

        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Bar\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Baz\" -Force > $null

        New-ModuleManifest -Path "$testdrive\Modules\Foo\Foo.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Bar\Bar.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Baz\Baz.psd1"

        New-Item -ItemType File -Path "$testdrive\Modules\Foo\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Bar.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Baz\Baz.psm1" > $null

        $removeModuleByNameTestCases = @(
            # Simple patterns
            @{ PatternsToRemove = "Foo"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "Bar", "Foo"; ShouldBeRemoved = "Bar", "Foo"; ShouldBePresent = "Baz"}
            @{ PatternsToRemove = "Bar", "Baz", "Foo"; ShouldBeRemoved = "Bar", "Baz", "Foo"; ShouldBePresent = ""}
            @{ PatternsToRemove = "Foo", "Foo"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "Foo", "Foo", "Bar"; ShouldBeRemoved = "Bar", "Foo"; ShouldBePresent = "Baz"}
            @{ PatternsToRemove = "Fo", "Foo"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}

            # Regex patterns
            #@{ PatternsToRemove = "*"; ShouldBeRemoved = "Bar", "Baz", "Foo"; ShouldBePresent = ""} -> this breaks pester for some reason
            @{ PatternsToRemove = "B*"; ShouldBeRemoved = "Bar", "Baz"; ShouldBePresent = "Foo"}
            @{ PatternsToRemove = "F*"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "Foo*"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "F*", "Bar"; ShouldBeRemoved = "Foo", "Bar"; ShouldBePresent = "Baz"}
            @{ PatternsToRemove = "F*", "F*"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "FF*"; ShouldBeRemoved = ""; ShouldBePresent = "Bar", "Baz", "Foo"}
        )

        $removeModuleByNameErrorTestCases = @(
            # Invalid patterns
            @{ PatternsToRemove = "Fo"; ShouldBeRemoved = ""; ShouldBePresent = "Bar", "Baz", "Foo"}
            @{ PatternsToRemove = "Fo", "Ba"; ShouldBeRemoved = ""; ShouldBePresent = "Bar", "Baz", "Foo"}
        )
    }

    BeforeEach {
        Import-Module -Name "$testdrive\Modules\Foo\Foo.psd1" -Force
        Import-Module -Name "$testdrive\Modules\Bar\Bar.psd1" -Force
        Import-Module -Name "$testdrive\Modules\Baz\Baz.psd1" -Force

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo"
    }

    AfterAll {
        Remove-Module -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue
    }

    It "Remove-Module -Name <PatternsToRemove>" -TestCases $removeModuleByNameTestCases {
        param([string[]]$PatternsToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        { Remove-Module -Name $PatternsToRemove} | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -Name <PatternsToRemove> (Error cases)" -TestCases $removeModuleByNameErrorTestCases {
        param([string[]]$PatternsToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        { Remove-Module -Name $PatternsToRemove -ErrorAction Stop } | Should -Throw -ErrorId "Modules_NoModulesRemoved,Microsoft.PowerShell.Commands.RemoveModuleCommand"

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }
}

Describe "Remove-Module -FullyQualifiedName" -Tags "CI" {
    BeforeAll {
        Remove-Module -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue

        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\1.0\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\2.0\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Bar\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Baz\" -Force > $null

        New-ModuleManifest -Path "$testdrive\Modules\Foo\1.0\Foo.psd1" -ModuleVersion 1.0
        New-ModuleManifest -Path "$testdrive\Modules\Foo\2.0\Foo.psd1" -ModuleVersion 2.0
        New-ModuleManifest -Path "$testdrive\Modules\Bar\Bar.psd1" -ModuleVersion 1.0
        New-ModuleManifest -Path "$testdrive\Modules\Baz\Baz.psd1" -ModuleVersion 1.0

        New-Item -ItemType File -Path "$testdrive\Modules\Foo\1.0\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Foo\2.0\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Bar.psm1" > $null

        $removeModuleByFQNTestCases = @(
            @{
                FqnToRemove = @{ModuleName = "Foo"; ModuleVersion = "1.0"};
                ShouldBeRemoved = "Foo";
                ShouldBePresent = "Bar", "Baz";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; RequiredVersion = "1.0"};
                ShouldBeRemoved = "";
                ShouldBePresent = "Bar", "Baz", "Foo";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; ModuleVersion = "2.0"};
                ShouldBeRemoved = "";
                ShouldBePresent = "Bar", "Baz", "Foo";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; RequiredVersion = "2.0"};
                ShouldBeRemoved = "";
                ShouldBePresent = "Bar", "Baz", "Foo";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; ModuleVersion = "1.0"};
                ShouldBeRemoved = "Foo";
                ShouldBePresent = "Bar", "Baz";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; ModuleVersion = "1.0"}, @{ModuleName = "Bar"; ModuleVersion = "1.0"};
                ShouldBeRemoved = "Foo", "Bar";
                ShouldBePresent = "Baz";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; ModuleVersion = "3.0"}, @{ModuleName = "Bar"; ModuleVersion = "1.0"};
                ShouldBeRemoved = "Bar";
                ShouldBePresent = "Baz", "Foo", "Foo";
            }
        )

        $removeModuleByFQNErrorTestCases = @(
            @{
                FqnToRemove = @{ModuleName = "Fo"; ModuleVersion = "1.0"};
                ShouldBeRemoved = "";
                ShouldBePresent = "Bar", "Baz", "Foo", "Foo";
            }
            @{
                FqnToRemove = @{ModuleName = "Foo"; ModuleVersion = "3.0"};
                ShouldBeRemoved = "";
                ShouldBePresent = "Bar", "Baz", "Foo", "Foo";
            }
            @{
                FqnToRemove = @{ModuleName = "Baz"; RequiredVersion = "3.0"};
                ShouldBeRemoved = "";
                ShouldBePresent = "Bar", "Baz", "Foo", "Foo";
            }
        )
    }

    BeforeEach {
        Import-Module -Name "$testdrive\Modules\Foo\1.0\Foo.psd1" -Force
        Import-Module -Name "$testdrive\Modules\Foo\2.0\Foo.psd1" -Force
        Import-Module -Name "$testdrive\Modules\Bar\Bar.psd1" -Force
        Import-Module -Name "$testdrive\Modules\Baz\Baz.psd1" -Force

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"
    }

    AfterAll {
        Remove-Module -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue
    }

    It "Remove-Module -FullyQualifiedName <FullyQualifiedName>" -TestCases $removeModuleByFQNTestCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification[]]$FqnToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        { Remove-Module -FullyQualifiedName $FqnToRemove } | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -FullyQualifiedName <FullyQualifiedName> (Error cases)" -TestCases $removeModuleByFQNErrorTestCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification[]]$FqnToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        { Remove-Module -FullyQualifiedName $FqnToRemove -ErrorAction Stop } | Should -Throw -ErrorId "Modules_NoModulesRemoved,Microsoft.PowerShell.Commands.RemoveModuleCommand"

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }
}

Describe "Remove-Module core module on module path by name" -Tags "CI" {
    $moduleName = "Microsoft.PowerShell.Security"

    BeforeEach {
        Import-Module -Name $moduleName -Force
        (Get-Module -Name $moduleName).Name | Should -BeExactly $moduleName
    }

    It "should be able to remove a module with using Name switch" {
        { Remove-Module -Name $moduleName } | Should -Not -Throw
        (Get-Module -Name $moduleName).Name | Should -BeNullOrEmpty
    }

    It "should be able to remove a module with using ModuleInfo switch" {
        $a = Get-Module -Name $moduleName
        { Remove-Module -ModuleInfo $a } | Should -Not -Throw
        (Get-Module -Name $moduleName).Name | Should -BeNullOrEmpty
    }

	AfterEach {
        Import-Module -Name $moduleName -Force
    }
}

Describe "Remove-Module custom module with FullyQualifiedName" -Tags "Feature" {
    BeforeAll {
        $moduleName = 'Banana'
        $moduleVersion = '1.0'

        New-Item -Path "$TestDrive/Modules/$moduleName" -ItemType Directory
        New-Item -Path "$TestDrive/Modules/$moduleName/Subanana" -ItemType Directory
        New-Item -Path "$TestDrive/Monkey" -ItemType Directory

        $modulePath = "$TestDrive/Modules/$moduleName"
        $moduleName = 'Banana'
        $moduleVersion = '1.0'
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -ModuleVersion $moduleVersion

        $testCases = @(
            @{ ModPath = "$TestDrive\Modules/$moduleName" }
            @{ ModPath = "$TestDrive/Modules\$moduleName/$moduleName.psd1" }
        )

        BeforeEach {
            Get-Module $moduleName | Remove-Module
        }

        It "Removes a module with fully qualified name with path <ModPath>" -TestCases $testCases -Pending {
            param([string]$ModPath)

            $m = Import-Module $modulePath -PassThru

            $m.Name | Should -Be $moduleName
            $m.Version | Should -Be $moduleVersion
            $m.Path | Should -Be $manifestPath

            Remove-Module -FullyQualifiedName @{ ModuleName = $ModPath; RequiredVersion = $moduleVersion } -ErrorAction Stop

            Get-Module $moduleName | Should -HaveCount 0 -Because "The module should have been removed by its path"
        }
    }
}
