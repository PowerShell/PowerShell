# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "New-ModuleManifest basic tests" -tags "CI" {
    BeforeAll {
        $moduleName = 'test'
        $modulePath = "$TestDrive/Modules/$moduleName"
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"

        New-Item -Path "$TestDrive/Modules/$moduleName" -ItemType Directory
    }

    AfterEach {
        Remove-Item -Path $manifestPath -Force -ErrorAction SilentlyContinue
    }

    AfterAll {
        Remove-Item -Path $modulePath -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "Verify manifest fields 1" {
        New-ModuleManifest -Path $manifestPath
        $module = Test-ModuleManifest -Path $manifestPath
        $module.Name | Should -BeExactly "test"
        $module.ModuleType | Should -BeExactly "Manifest"
        $module.Version | Should -BeExactly "0.0.1"
    }

    It "Verify manifest fields 2" {
        New-ModuleManifest -Path $manifestPath `
            -Author 'author' `
            -CompanyName 'company' `
            -Copyright 'copyright' `
            -ModuleVersion '1.2.3' `
            -Description 'description' `
            -PowerShellVersion '6.0' `
            -ClrVersion '1.2.3' `
            -DotNetFrameworkVersion '3.2.1' `
            -PowerShellHostVersion '1.2.3' `
            -Tags @('tag1', 'tag2') `
            -ReleaseNotes 'release note' `
            -RequiredModules @('PSReadline') `
            -ExternalModuleDependencies @('PSReadline') `
            -Prerelease 'prerelease' `
            -RequireLicenseAcceptance

        $module = Test-ModuleManifest -Path $manifestPath
        $module.Name | Should -BeExactly "test"
        $module.Author | Should -BeExactly "author"
        $module.Version | Should -BeExactly "1.2.3"
        $module.Description | Should -BeExactly "description"
        $module.PowerShellVersion | Should -BeExactly "6.0"
        $module.ClrVersion | Should -BeExactly "1.2.3"
        $module.DotNetFrameworkVersion | Should -BeExactly "3.2.1"
        $module.PowerShellHostVersion | Should -BeExactly "1.2.3"
        $module.Tags | Should -BeExactly @('tag1', 'tag2')
        $module.ReleaseNotes | Should -BeExactly 'release note'
        $module.RequiredModules | Should -BeExactly 'PSReadline'
        $module.PrivateData.PSData.ExternalModuleDependencies | Should -BeExactly 'PSReadline'
        $module.PrivateData.PSData.Prerelease | Should -BeExactly 'prerelease'
        $module.PrivateData.PSData.RequireLicenseAcceptance | Should -BeExactly $true
    }
}

Describe "New-ModuleManifest tests" -tags "CI" {
    BeforeAll {
        $moduleName = 'test'
        $modulePath = "$TestDrive/Modules/$moduleName"
        $manifestPath = Join-Path $modulePath "$moduleName.psd1"

        New-Item -Path "$TestDrive/Modules/$moduleName" -ItemType Directory

        if ($IsWindows) {
            $ExpectedManifestBytes = @(35,13) # CR
        } else {
            $ExpectedManifestBytes = @(35,10) # LF
        }
    }

    AfterEach {
        Remove-Item -Path $manifestPath -Force -ErrorAction SilentlyContinue
    }

    AfterAll {
        Remove-Item -Path $modulePath -Recurse -Force -ErrorAction SilentlyContinue
    }


    It "Uris with spaces are allowed and escaped correctly" {
        $testUri = [Uri]"http://foo.com/hello world"
        $absoluteUri = $testUri.AbsoluteUri

        New-ModuleManifest -Path $manifestPath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri -HelpInfoUri $testUri
        $module = Test-ModuleManifest -Path $manifestPath
        $module.HelpInfoUri | Should -BeExactly $absoluteUri
        $module.PrivateData.PSData.IconUri | Should -BeExactly $absoluteUri
        $module.PrivateData.PSData.LicenseUri | Should -BeExactly $absoluteUri
        $module.PrivateData.PSData.ProjectUri | Should -BeExactly $absoluteUri
    }

    function TestNewModuleManifestEncoding {
        param ([byte[]]$expected)
        New-ModuleManifest -Path $manifestPath
        (Get-Content -AsByteStream -Path $manifestPath -TotalCount $expected.Length) -join ',' | Should -Be ($expected -join ',')
    }

    It "Verify module manifest encoding" {

        # verify first line of the manifest:
        # 2 characters - '#' '\n' - in UTF-8 no BOM - this should be @(35,10)
        TestNewModuleManifestEncoding -expected $ExpectedManifestBytes
    }

    It "Relative URIs are not allowed" {
        $testUri = [Uri]"../foo"

        { New-ModuleManifest -Path $manifestPath -ProjectUri $testUri -LicenseUri $testUri -IconUri $testUri } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.NewModuleManifestCommand"
    }
}
