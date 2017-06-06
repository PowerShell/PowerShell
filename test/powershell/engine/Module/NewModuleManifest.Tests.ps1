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

    function TestNewModuleManifestEncoding {
        param ([byte[]]$expected)
        New-ModuleManifest -Path $testModulePath
        (Get-Content -Encoding Byte -Path $testModulePath -TotalCount $expected.Length) -join ',' | Should Be ($expected -join ',')
    }

    It "Verify module manifest encoding on Windows " -Skip:(-not $IsWindows) {
        
        # verify first line of the manifest - 3 characters - '#' '\r' '\n'
        # On Windows platforms - in UTF-16 with BOM - this should be @(255,254,35,0,13,0,10,0)
        TestNewModuleManifestEncoding -expected @(255,254,35,0,13,0,10,0)
    }

    It "Verify module manifest encoding on non-Windows " -Skip:($IsWindows) {
        
        # verify first line of the manifest - 3 characters - '#' '\r' '\n'
        # On non-Windows platforms - in UTF-8 no BOM - this should be @(35,13,10)
        TestNewModuleManifestEncoding -expected @(35,13,10)
    }

    It "Relative URIs are not allowed" {
        $testUri = [Uri]"../foo"

        { New-ModuleManifest -Path $testModulePath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.NewModuleManifestCommand"
    }
}
