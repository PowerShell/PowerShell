# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Isolated module scenario - load the whole module in custom ALC" -Tag 'CI' {
    It "Loading 'IsolatedModule' should work as expected" {
        ## The 'IsolatedModule' module can be found at '<repo-root>\test\tools\Modules'.
        ## The module assemblies are created and deployed by '<repo-root>\test\tools\TestAlc'.
        ## The module defines its own custom ALC and has its module structure organized in a special way that allows the module to be loaded in that custom ALC.
        ## The file structure of this module is as follows:
        ## │   IsolatedModule.psd1
        ## │   Test.Isolated.Init.dll (contains the custom ALC and code to setup 'Resolving' handler)
        ## │
        ## └───Dependencies
        ##        Newtonsoft.Json.dll (version 10.0.0.0 dependency)
        ##        Test.Isolated.Nested.dll (nested binary module)
        ##        Test.Isolated.Root.dll (root binary module)
        $module = Import-Module IsolatedModule -PassThru
        $nestedCmd = Get-Command Test-NestedCommand
        $rootCmd = Get-Command Test-RootCommand

        $module.ModuleType | Should -Be "Binary"
        $module.RootModule | Should -Not -BeNullOrEmpty

        ## The type 'Test.Isolated.Nested.Foo' from the nested module can be resolved and should be from the same load context.
        $context1 = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($nestedCmd.ImplementingType.Assembly)
        $context2 = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext([Test.Isolated.Nested.Foo].Assembly)
        $context1.Name | Should -BeExactly "MyCustomALC"
        $context1 | Should -Be $context2

        ## Test-NestedCommand depends on NewtonSoft.Json 10.0.3.0 while PowerShell depends on 13.0.0.0 or higher.
        ## The exact version of NewtonSoft.Json should be loaded to the custom ALC.
        $foo = [Test.Isolated.Nested.Foo]::new("Hello", "World")
        $version = Test-NestedCommand -Param $foo
        $version | Should -BeExactly "Hello-World-Newtonsoft.Json, Version=13.0.1.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed" -Because $version

        ## The type 'Test.Isolated.Root.Red' from the root module can be resolved and should be from the same load context.
        $context3 = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($rootCmd.ImplementingType.Assembly)
        $context4 = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext([Test.Isolated.Root.Red].Assembly)
        $context3.Name | Should -BeExactly "MyCustomALC"
        $context3 | Should -Be $context4
        $context3 | Should -Be $context1

        ## No type identity issue in parameter binding because they are from the same assembly instance.
        $red = [Test.Isolated.Root.Red]::new("RED!")
        Test-RootCommand -Param $red | Should -BeExactly "RED!"

        ## Removing the module should have its assemblies removed from 'Context.AssemblyCache' and results in the 'TypeNotFound'
        ## error when trying to resolve the following 2 types:
        ##  - 'Test.Isolated.Nested.Bar', from 'Test.Isolated.Nested.dll'
        ##  - 'Test.Isolated.Root.Yellow', from 'Test.Isolated.Root.dll'
        ## The types cannot be found because:
        ##  1. they are from a load context that is not visible to PowerShell,
        ##  2. those two assemblies have been removed from the cache when unloading the module.
        ## [Test.Isolated.Nested.Foo] and [Test.Isolated.Root.Red] can still be found because they were added to type cache when
        ## successfully resolved above.
        Remove-Module IsolatedModule
        { [Test.Isolated.Nested.Bar] } | Should -Throw -ErrorId "TypeNotFound"
        { [Test.Isolated.Root.Yellow] } | Should -Throw -ErrorId "TypeNotFound"
    }

    It "WSMan and Certificate providers should reference the manifest module instead of the nested module" -Skip:(!$IsWindows) {
        $wsManModule = Import-Module Microsoft.WSMan.Management -PassThru
        $securityModule = Import-Module Microsoft.PowerShell.Security -PassThru

        $wsManModule.ModuleType | Should -Be "Manifest"
        $securityModule.ModuleType | Should -Be "Manifest"

        ## For engine providers, the 'Module' property should point to top-level module, instead of the nested module.
        $wsManProvider = Get-PSProvider WSMan
        $certificateProvider = Get-PSProvider Certificate

        $wsManProvider.Module | Should -Be $wsManModule
        $certificateProvider.Module | Should -Be $securityModule
    }
}
