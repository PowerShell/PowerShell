# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Update-ModuleManifest tests" -tags "CI" {

    BeforeEach {
        $testModulePath = "testdrive:/module/test.psd1"
        New-Item -ItemType Directory -Path testdrive:/module > $null
    }

    AfterEach {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue testdrive:/module
    }

    It "Update should not clear out NestedModules: <variation>" -TestCases @(
        @{ variation = "export with wildcards"; exportValue = "*" },
        @{ variation = "export without wildcards"; exportValue = "@()"}
    ) {
        param($exportValue)

        New-Item -ItemType File -Path testdrive:/module/foo.psm1 > $null
        New-ModuleManifest -Path $testModulePath -NestedModules foo.psm1 -HelpInfoUri http://foo.com -AliasesToExport $exportValue -CmdletsToExport $exportValue -FunctionsToExport $exportValue -VariablesToExport $exportValue -DscResourcesToExport $exportValue
        $module = Test-ModuleManifest -Path $testModulePath
        $module.HelpInfoUri | Should -BeExactly "http://foo.com/"
        $module.NestedModules | Should -HaveCount 1
        $module.NestedModules.Name | Should -BeExactly foo
        Update-ModuleManifest -Path $testModulePath -HelpInfoUri https://bar.org
        $module = Test-ModuleManifest -Path $testModulePath
        $module.HelpInfoUri | Should -BeExactly "https://bar.org/"
        $module.NestedModules | Should -HaveCount 1
        $module.NestedModules.Name | Should -BeExactly foo
    }
}
