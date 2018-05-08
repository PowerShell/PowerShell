# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Module basic tests" -tags "CI" {
    Context "Circular nested module test" {
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
            Remove-Item -Path $moduleFolder -Force -Recurse
        }

        It "Loading the module should succeed and return a module with circular nested module" {
            $m = Import-Module $psdPath -PassThru
            $m.NestedModules[0] | Should -Be $m
        }
    }
}
