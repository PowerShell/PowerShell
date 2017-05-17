Describe "New-ModuleManifest tests" -tags "CI" {
    BeforeEach {
        New-Item -ItemType Directory -Path testdrive:/module
        $testModulePath = "testdrive:/module/test.psd1"
    }

    AfterEach {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue testdrive:/module
    }

    It "Uris with spaces are allowed and escaped correctly" {
        $testUri = [Uri]"http://foo.com/hello world"
        $absoluteUri = $testUri.AbsoluteUri

        New-ModuleManifest -Path $testModulePath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri -HelpInfoUri $testUri
        $module = Test-ModuleManifest -Path $testModulePath
        $module.HelpInfoUri | Should BeExactly $absoluteUri
        $module.PrivateData.PSData.IconUri | Should BeExactly $absoluteUri
        $module.PrivateData.PSData.LicenseUri | Should BeExactly $absoluteUri
        $module.PrivateData.PSData.ProjectUri | Should BeExactly $absoluteUri
    }

    It "Relative URIs are not allowed" {
        $testUri = [Uri]"../foo"

        { New-ModuleManifest -Path $testModulePath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.NewModuleManifestCommand"
    }
}
