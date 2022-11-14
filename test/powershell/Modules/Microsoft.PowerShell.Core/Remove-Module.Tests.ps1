# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Remove-Module -Name | -FullyQualifiedName | -ModuleInfo" -Tags "CI" {
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

        $removeModuleByNameTestCases = @(
            # Simple patterns
            @{ PatternsToRemove = "Bar"; ShouldBeRemoved = "Bar"; ShouldBePresent = "Baz", "Foo", "Foo"}
            @{ PatternsToRemove = "Foo"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "Bar", "Foo"; ShouldBeRemoved = "Bar", "Foo"; ShouldBePresent = "Baz"}
            @{ PatternsToRemove = "Bar", "Baz", "Foo"; ShouldBeRemoved = "Bar", "Baz", "Foo"; ShouldBePresent = ""}
            @{ PatternsToRemove = "Foo", "Foo"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "Foo", "Foo", "Bar"; ShouldBeRemoved = "Bar", "Foo"; ShouldBePresent = "Baz"}
            @{ PatternsToRemove = "Fo", "Foo"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}

            # Regex patterns
            #@{ PatternsToRemove = "*"; ShouldBeRemoved = "Bar", "Baz", "Foo"; ShouldBePresent = ""} -> this breaks pester for some reason
            @{ PatternsToRemove = "B*"; ShouldBeRemoved = "Bar", "Baz"; ShouldBePresent = "Foo", "Foo"}
            @{ PatternsToRemove = "F*"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "Foo*"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "F*", "Bar"; ShouldBeRemoved = "Foo", "Bar"; ShouldBePresent = "Baz"}
            @{ PatternsToRemove = "F*", "F*"; ShouldBeRemoved = "Foo"; ShouldBePresent = "Bar", "Baz"}
            @{ PatternsToRemove = "FF*"; ShouldBeRemoved = ""; ShouldBePresent = "Bar", "Baz", "Foo", "Foo"}
        )

        $removeModuleByNameErrorTestCases = @(
            # Invalid patterns
            @{ PatternsToRemove = "Fo"; ShouldBeRemoved = ""; ShouldBePresent = "Bar", "Baz", "Foo", "Foo"}
            @{ PatternsToRemove = "Fo", "Ba"; ShouldBeRemoved = ""; ShouldBePresent = "Bar", "Baz", "Foo", "Foo"}
        )

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
    }

    AfterAll {
        Remove-Module -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue
    }

    It "Remove-Module -Name <PatternsToRemove>" -TestCases $removeModuleByNameTestCases {
        param([string[]]$PatternsToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"

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

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"

        { Remove-Module -Name $PatternsToRemove -ErrorAction Stop } | Should -Throw -ErrorId "Modules_NoModulesRemoved,Microsoft.PowerShell.Commands.RemoveModuleCommand"

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -FullyQualifiedName <FqnToRemove>" -TestCases $removeModuleByFQNTestCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification[]]$FqnToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"

        { Remove-Module -FullyQualifiedName $FqnToRemove } | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -FullyQualifiedName <FqnToRemove> (Error cases)" -TestCases $removeModuleByFQNErrorTestCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification[]]$FqnToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"

        { Remove-Module -FullyQualifiedName $FqnToRemove -ErrorAction Stop } | Should -Throw -ErrorId "Modules_NoModulesRemoved,Microsoft.PowerShell.Commands.RemoveModuleCommand"

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -ModuleInfo <FqnToRemove>" -TestCases $removeModuleByFQNTestCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification[]]$FqnToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"

        $modInfo = Get-Module -FullyQualifiedName $FqnToRemove
        { Remove-Module -ModuleInfo $modInfo } | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -ModuleInfo (removing twice works)" {
        (Get-Module -Name "Bar", "Baz", "Foo").Name | Should -BeExactly "Bar", "Baz", "Foo", "Foo"

        $modInfo = Get-Module -Name "Bar"
        # Contrary to -Name and -FullyQualifiedName removing a non imported module works using ModuleInfo
        { Remove-Module -ModuleInfo $modInfo } | Should -Not -Throw
        { Remove-Module -ModuleInfo $modInfo } | Should -Not -Throw
    }
}

Describe "Remove-Module : module is readOnly | Constant" -Tags "CI" {

    BeforeAll {
        Remove-Module -Force -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue

        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Bar\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Baz\" -Force > $null

        New-ModuleManifest -Path "$testdrive\Modules\Foo\Foo_ro.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Foo\Foo_rw.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Bar\Bar_rw.psd1"
        New-ModuleManifest -Path "$testdrive\Modules\Baz\Const_module.psd1"

        New-Item -ItemType File -Path "$testdrive\Modules\Foo\Foo_ro.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Foo\Foo_rw.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Bar_rw.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Baz\Const_module.psm1" > $null

        $removeReadOnlyModulesTestCases = @(
            # Simple patterns
            @{ NamesToRemove = "Foo_ro"; ShouldBeRemoved = ""; ShouldBePresent = "Bar_rw", "Const_module", "Foo_ro", "Foo_rw"}
            @{ NamesToRemove = "Foo_ro", "Foo_rw"; ShouldBeRemoved = "Foo_rw"; ShouldBePresent = "Bar_rw", "Const_module", "Foo_ro"}
            @{ NamesToRemove = "Bar_rw", "Foo_ro", "Foo_rw"; ShouldBeRemoved = "Bar_rw", "Foo_rw"; ShouldBePresent = "Const_module", "Foo_ro"}

            # Regex patterns
            @{ NamesToRemove = "Foo_*"; ShouldBeRemoved = "Foo_rw"; ShouldBePresent = "Bar_rw", "Const_module", "Foo_ro"}
            @{ NamesToRemove = "Foo_*", "Bar_*"; ShouldBeRemoved = "Bar_rw", "Foo_rw"; ShouldBePresent = "Const_module", "Foo_ro"}
        )

        $removeForceReadOnlyModulesTestCases = @(
            # Simple patterns
            @{ NamesToRemove = "Foo_ro"; ShouldBeRemoved = "Foo_ro"; ShouldBePresent = "Bar_rw", "Const_module", "Foo_rw"}
            @{ NamesToRemove = "Foo_ro", "Foo_rw"; ShouldBeRemoved = "Foo_ro", "Foo_rw"; ShouldBePresent = "Bar_rw", "Const_module"}
            @{ NamesToRemove = "Bar_rw", "Foo_ro", "Foo_rw"; ShouldBeRemoved = "Bar_rw", "Foo_ro", "Foo_rw"; ShouldBePresent = "Const_module"}

            # Regex patterns
            @{ NamesToRemove = "Foo_*"; ShouldBeRemoved = "Foo_ro", "Foo_rw"; ShouldBePresent = "Bar_rw", "Const_module"}
            @{ NamesToRemove = "Foo_*", "Bar_*"; ShouldBeRemoved = "Bar_rw", "Foo_ro", "Foo_rw"; ShouldBePresent = "Const_module"}
        )

        $removeConstantModulesTestCases = @(
            # Simple patterns
            @{ NamesToRemove = "Const_module"; ShouldBeRemoved = ""; ShouldBePresent = "Bar_rw", "Const_module", "Foo_ro", "Foo_rw"}
            @{ NamesToRemove = "Const_module", "Foo_ro", "Foo_rw"; ShouldBeRemoved = "Foo_ro", "Foo_rw"; ShouldBePresent = "Bar_rw", "Const_module"}

            # Regex patterns
            @{ NamesToRemove = "Foo_*", "Ba*", "Const*"; ShouldBeRemoved = "Bar_rw", "Foo_ro", "Foo_rw"; ShouldBePresent = "Const_module"}
        )
    }

    AfterAll {
        Remove-Module -Force -Name "Foo_*", "Ba*" -ErrorAction SilentlyContinue
    }

    BeforeEach {
        Import-Module -Name "$testdrive\Modules\Foo\Foo_ro.psd1" -Force
        (Get-Module -Name "Foo_ro").AccessMode = "readOnly"
        Import-Module -Name "$testdrive\Modules\Foo\Foo_rw.psd1" -Force
        Import-Module -Name "$testdrive\Modules\Bar\Bar_rw.psd1" -Force

        # If this test is run again in the same session, this test module
        # will already be loaded and can't be removed because it sets
        # AccessMode to Constant, so only import this test module and
        # set AccessMode if it isn't already loaded
        if ($null -eq (Get-Module -Name "Const_module")) {
            Import-Module -Name "$testdrive\Modules\Baz\Const_module.psd1" -Force
            (Get-Module -Name "Const_module").AccessMode = "Constant"
        }
    }

    It "Remove-Module (ReadOnly modules): <NamesToRemove>" -TestCases $removeReadOnlyModulesTestCases {
        param([string[]]$NamesToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar_rw", "Foo_ro", "Foo_rw").Name | Should -BeExactly "Bar_rw", "Foo_ro", "Foo_rw"

        { Remove-Module -Name $NamesToRemove -ErrorAction Stop } | Should -Throw -ErrorId "Modules_ModuleIsReadOnly,Microsoft.PowerShell.Commands.RemoveModuleCommand"
        { Remove-Module -Name $NamesToRemove -ErrorAction SilentlyContinue } | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -Force (ReadOnly modules): <NamesToRemove>" -TestCases $removeForceReadOnlyModulesTestCases {
        param([string[]]$NamesToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar_rw", "Foo_ro", "Foo_rw").Name | Should -BeExactly "Bar_rw", "Foo_ro", "Foo_rw"

        { Remove-Module -Force -Name $NamesToRemove -ErrorAction Stop } | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }

    It "Remove-Module -Force (Constant modules): <NamesToRemove>" -TestCases $removeConstantModulesTestCases {
        param([string[]]$NamesToRemove, [string[]]$ShouldBeRemoved, [string[]]$ShouldBePresent)

        (Get-Module -Name "Bar_rw", "Const_module", "Foo_ro", "Foo_rw").Name | Should -BeExactly "Bar_rw", "Const_module", "Foo_ro", "Foo_rw"

        { Remove-Module -Force -Name $NamesToRemove -ErrorAction Stop } | Should -Throw -ErrorId "Modules_ModuleIsConstant,Microsoft.PowerShell.Commands.RemoveModuleCommand"
        { Remove-Module -Force -Name $NamesToRemove -ErrorAction SilentlyContinue } | Should -Not -Throw

        if ($ShouldBeRemoved) {
            (Get-Module -Name $ShouldBeRemoved).Name | Should -BeNullOrEmpty
        }

        if ($ShouldBePresent) {
            (Get-Module -Name $ShouldBePresent).Name | Should -BeExactly $ShouldBePresent
        }
    }
}

Describe "Remove-Module : module provides the PSDrive for current PS Session" -Tags "CI" {
    It "Remove-Module : module provides the PSDrive for current PS Session" {

        $module = Get-Module (Join-Path $PSHOME "System.Management.Automation.dll") -ListAvailable
        { Remove-Module $module -ErrorAction Stop } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.RemoveModuleCommand"
    }
}

Describe "Remove-Module : module contains nested modules" -Tags "CI" {

    BeforeAll {
        Remove-Module -Force -Name "Foo", "Bar", "Baz" -ErrorAction SilentlyContinue

        New-Item -ItemType Directory -Path "$testdrive\Modules\Foo\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Bar\" -Force > $null
        New-Item -ItemType Directory -Path "$testdrive\Modules\Baz\" -Force > $null

        New-Item -ItemType File -Path "$testdrive\Modules\Foo\Foo.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Bar\Bar.psm1" > $null
        New-Item -ItemType File -Path "$testdrive\Modules\Baz\Baz.psm1" > $null

        Set-Content -Path "$testdrive\Modules\Foo\Foo.psm1" -Value "function FooFunc {}"
        Set-Content -Path "$testdrive\Modules\Bar\Bar.psm1" -Value "function BarFunc {}"
    }

    It "Remove-Module : module contains nested modules" {
        New-ModuleManifest "$testdrive\Modules\Bar\Bar.psd1" -RootModule "./Bar.psm1" -FunctionsToExport "BarFunc"
        New-ModuleManifest "$testdrive\Modules\Foo\Foo.psd1" -NestedModules "../Bar/Bar.psd1" -FunctionsToExport "BarFunc"

        Import-Module "$testdrive\Modules\Foo\Foo.psd1" -Force
        (Get-Module -Name "Foo").Name | Should -BeExactly "Foo"

        { Get-Command BarFunc -ErrorAction Stop } | Should -Not -Throw
        { Remove-Module -Name "Foo" -ErrorAction Stop } | Should -Not -Throw
        { Get-Command BarFunc -ErrorAction Stop } | Should -Throw
        (Get-Module -Name "Foo").Name | Should -BeNullOrEmpty
    }

    It "Remove-Module : module contains nested modules with circular dependencies" {
        New-ModuleManifest "$testdrive\Modules\Bar\Bar.psd1" -RootModule "Bar" -FunctionsToExport "BarFunc" -NestedModules "Bar"

        Import-Module "$testdrive\Modules\Bar\Bar.psd1" -Force
        (Get-Module -Name "Bar").Name | Should -BeExactly "Bar"

        { Get-Command BarFunc -ErrorAction Stop } | Should -Not -Throw
        { Remove-Module -Name "Bar" -ErrorAction Stop } | Should -Not -Throw
        { Get-Command BarFunc -ErrorAction Stop } | Should -Throw
        (Get-Module -Name "Bar").Name | Should -BeNullOrEmpty
    }

    It "Remove-Module : modules are required by other modules" {
        New-ModuleManifest "$testdrive\Modules\Bar\Bar.psd1"
        New-ModuleManifest "$testdrive\Modules\Foo\Foo.psd1" -RequiredModules "Bar"

        Import-Module "$testdrive\Modules\Bar\Bar.psd1" -Force
        Import-Module "$testdrive\Modules\Foo\Foo.psd1" -Force
        (Get-Module -Name "Bar").Name | Should -BeExactly "Bar"
        (Get-Module -Name "Foo").Name | Should -BeExactly "Foo"

        {Remove-Module "Foo" -ErrorAction Stop } | Should -Not -Throw
        Import-Module "$testdrive\Modules\Foo\Foo.psd1" -Force

        { Remove-Module "Bar" -ErrorAction Stop } | Should -Throw -ErrorId "Modules_ModuleIsRequired,Microsoft.PowerShell.Commands.RemoveModuleCommand"
    }
}

Describe "Remove-Module core module on module path by name" -Tags "CI" {
    $moduleName = "Microsoft.PowerShell.Security"

    BeforeEach {
        Import-Module -Name $moduleName -Force
    }

    It "should be able to remove a module with using Name switch" {
        (Get-Module -Name $moduleName).Name | Should -BeExactly $moduleName

        { Remove-Module -Name $moduleName } | Should -Not -Throw
        (Get-Module -Name $moduleName).Name | Should -BeNullOrEmpty
    }

    It "should be able to remove a module with using ModuleInfo switch" {
        (Get-Module -Name $moduleName).Name | Should -BeExactly $moduleName

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

        It "Removes a module with fully qualified name with path <ModPath>" -TestCases $testCases -Pending {
            param([string]$ModPath)

            Get-Module $moduleName | Remove-Module

            $m = Import-Module $modulePath -PassThru

            $m.Name | Should -Be $moduleName
            $m.Version | Should -Be $moduleVersion
            $m.Path | Should -Be $manifestPath

            Remove-Module -FullyQualifiedName @{ ModuleName = $ModPath; RequiredVersion = $moduleVersion } -ErrorAction Stop

            Get-Module $moduleName | Should -HaveCount 0 -Because "The module should have been removed by its path"
        }
    }
}
