# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "New-ModuleManifest tests" -tags "CI" {
    BeforeEach {
        $null = New-Item -ItemType Directory -Path testdrive:/module
        $testModulePath = "testdrive:/module/test.psd1"
    }

    AfterEach {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue testdrive:/module
    }

    BeforeAll {
        if ($IsWindows) {
            $ExpectedManifestBytes = @(35,13) # CR
        } else {
            $ExpectedManifestBytes = @(35,10) # LF
        }
    }

    It "Uris with spaces are allowed and escaped correctly" {
        $testUri = [Uri]"http://foo.com/hello world"
        $absoluteUri = $testUri.AbsoluteUri

        New-ModuleManifest -Path $testModulePath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri -HelpInfoUri $testUri
        $module = Test-ModuleManifest -Path $testModulePath
        $module.HelpInfoUri | Should -BeExactly $absoluteUri
        $module.PrivateData.PSData.IconUri | Should -BeExactly $absoluteUri
        $module.PrivateData.PSData.LicenseUri | Should -BeExactly $absoluteUri
        $module.PrivateData.PSData.ProjectUri | Should -BeExactly $absoluteUri
    }

    function TestNewModuleManifestEncoding {
        param ([byte[]]$expected)
        New-ModuleManifest -Path $testModulePath
        (Get-Content -AsByteStream -Path $testModulePath -TotalCount $expected.Length) -join ',' | Should -Be ($expected -join ',')
    }

    It "Verify module manifest encoding" {

        # verify first line of the manifest:
        # 2 characters - '#' '\n' - in UTF-8 no BOM - this should be @(35,10)
        TestNewModuleManifestEncoding -expected $ExpectedManifestBytes
    }

    It "Relative URIs are not allowed" {
        $testUri = [Uri]"../foo"

        { New-ModuleManifest -Path $testModulePath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.NewModuleManifestCommand"
    }
}
