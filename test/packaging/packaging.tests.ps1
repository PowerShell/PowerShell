# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Packaging Module Functions" {
    BeforeAll {
        Import-Module $PSScriptRoot/../../build.psm1 -Force
        Import-Module $PSScriptRoot/../../tools/packaging/packaging.psm1 -Force
    }

    Context "Test-IsPreview function" {
        It "Should return True for preview versions" {
            Test-IsPreview -Version "7.6.0-preview.6" | Should -Be $true
            Test-IsPreview -Version "7.5.0-rc.1" | Should -Be $true
        }

        It "Should return False for stable versions" {
            Test-IsPreview -Version "7.6.0" | Should -Be $false
            Test-IsPreview -Version "7.5.0" | Should -Be $false
        }

        It "Should return False for LTS builds regardless of version string" {
            Test-IsPreview -Version "7.6.0-preview.6" -IsLTS | Should -Be $false
            Test-IsPreview -Version "7.5.0" -IsLTS | Should -Be $false
        }
    }

    Context "Get-MacOSPackageId function" {
        It "Should return preview identifier when -IsPreview is specified" {
            Get-MacOSPackageId -IsPreview | Should -Be "com.microsoft.powershell-preview"
        }

        It "Should return stable identifier when -IsPreview is not specified" {
            Get-MacOSPackageId | Should -Be "com.microsoft.powershell"
        }
    }

    Context "Get-MacOSPackageIdentifierInfo function (New-MacOSPackage logic)" {
        It "Should detect preview builds and return preview identifier" {
            $result = Get-MacOSPackageIdentifierInfo -Version "7.6.0-preview.6" -LTS:$false
            
            $result.IsPreview | Should -Be $true
            $result.PackageIdentifier | Should -Be "com.microsoft.powershell-preview"
        }

        It "Should detect stable builds and return stable identifier" {
            $result = Get-MacOSPackageIdentifierInfo -Version "7.6.0" -LTS:$false
            
            $result.IsPreview | Should -Be $false
            $result.PackageIdentifier | Should -Be "com.microsoft.powershell"
        }

        It "Should treat LTS builds as stable even with preview version string" {
            $result = Get-MacOSPackageIdentifierInfo -Version "7.4.0-preview.1" -LTS:$true
            
            $result.IsPreview | Should -Be $false
            $result.PackageIdentifier | Should -Be "com.microsoft.powershell"
        }

        It "Should NOT use package name for preview detection (bug fix verification)" {
            # This test verifies the fix for issue #26673
            # The bug was using ($Name -like '*-preview') which always returned false
            # because preview builds use Name="powershell" not "powershell-preview"
            
            $Version = "7.6.0-preview.6"
            $Name = "powershell"  # Preview builds use "powershell" not "powershell-preview"
            
            # The INCORRECT logic (the bug): $Name -like '*-preview'
            $incorrectCheck = $Name -like '*-preview'
            $incorrectCheck | Should -Be $false -Because "Package name is 'powershell' not 'powershell-preview'"
            
            # The CORRECT logic (the fix): uses version string
            $result = Get-MacOSPackageIdentifierInfo -Version $Version -LTS:$false
            $result.IsPreview | Should -Be $true -Because "Version string correctly identifies preview"
            $result.PackageIdentifier | Should -Be "com.microsoft.powershell-preview"
        }
    }
}
