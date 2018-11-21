# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
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
