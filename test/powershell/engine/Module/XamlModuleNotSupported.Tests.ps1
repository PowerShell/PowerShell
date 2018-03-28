# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Workflow .Xaml module is not supported in PSCore" -tags "CI" {
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

    It "Import a module with XAML root module should raise a 'NotSupportd' error" {
        { Import-Module $xamlRootModule -ErrorAction Stop } | Should -Throw -ErrorId "Modules_WorkflowModuleNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
    }

    It "Import a module with XAML nested module should raise a 'NotSupported' error" {
        { Import-Module $xamlNestedModule -ErrorAction Stop } | Should -Throw -ErrorId "Modules_WorkflowModuleNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
    }
}
