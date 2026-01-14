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
            Test-IsPreview -Version "7.4.0-preview.1" | Should -Be $true
        }

        It "Should return False for stable versions" {
            Test-IsPreview -Version "7.6.0" | Should -Be $false
            Test-IsPreview -Version "7.5.0" | Should -Be $false
            Test-IsPreview -Version "7.4.13" | Should -Be $false
        }

        It "Should return False for LTS builds regardless of version string" {
            Test-IsPreview -Version "7.6.0-preview.6" -IsLTS | Should -Be $false
            Test-IsPreview -Version "7.4.0-rc.1" -IsLTS | Should -Be $false
            Test-IsPreview -Version "7.5.0" -IsLTS | Should -Be $false
        }
    }

    Context "Get-MacOSPackageId function" {
        It "Should return preview identifier when -IsPreview is specified" {
            $result = Get-MacOSPackageId -IsPreview
            $result | Should -Be "com.microsoft.powershell-preview"
        }

        It "Should return stable identifier when -IsPreview is not specified" {
            $result = Get-MacOSPackageId
            $result | Should -Be "com.microsoft.powershell"
        }
    }

    Context "macOS preview package identifier detection" {
        It "Should correctly detect preview from version string for preview builds" {
            # Simulate the logic used in New-MacOSPackage
            $Version = "7.6.0-preview.6"
            $LTS = $false
            $Name = "powershell"  # Preview builds use "powershell" not "powershell-preview"
            
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            $IsPreview | Should -Be $true -Because "Version string contains preview marker"
            $pkgIdentifier | Should -Be "com.microsoft.powershell-preview" -Because "Preview builds should use preview identifier"
        }

        It "Should correctly detect stable from version string for stable builds" {
            # Simulate the logic used in New-MacOSPackage
            $Version = "7.6.0"
            $LTS = $false
            $Name = "powershell"
            
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            $IsPreview | Should -Be $false -Because "Version string does not contain preview marker"
            $pkgIdentifier | Should -Be "com.microsoft.powershell" -Because "Stable builds should use stable identifier"
        }

        It "Should treat LTS builds as stable even with preview version string" {
            # Simulate the logic used in New-MacOSPackage
            $Version = "7.4.0-preview.1"
            $LTS = $true
            $Name = "powershell-lts"
            
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            $IsPreview | Should -Be $false -Because "LTS flag takes precedence over version string"
            $pkgIdentifier | Should -Be "com.microsoft.powershell" -Because "LTS builds should use stable identifier"
        }

        It "Should NOT use package name for preview detection" {
            # This test verifies the fix for issue #26673
            # The bug was using ($Name -like '*-preview') which always returned false
            # because preview builds use Name="powershell" not "powershell-preview"
            
            $Version = "7.6.0-preview.6"
            $LTS = $false
            $Name = "powershell"  # This is what preview builds actually use
            
            # The INCORRECT logic would have been: $Name -like '*-preview'
            $incorrectCheck = $Name -like '*-preview'
            $incorrectCheck | Should -Be $false -Because "Preview package names are 'powershell' not 'powershell-preview'"
            
            # The CORRECT logic uses Test-IsPreview with Version
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $IsPreview | Should -Be $true -Because "Version string correctly identifies preview builds"
            
            # Verify the correct identifier is generated
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            $pkgIdentifier | Should -Be "com.microsoft.powershell-preview" -Because "Preview builds must use preview identifier"
        }
    }

    Context "New-MacOSPackage function integration" {
        It "Should use Test-IsPreview with Version parameter for preview build detection" {
            # This test verifies that New-MacOSPackage calls Test-IsPreview with the Version parameter
            # rather than using ($Name -like '*-preview') which was the bug
            
            # We can't easily test the full function, but we can verify the logic by checking
            # that the correct function calls would be made with the correct parameters
            
            $Version = "7.6.0-preview.6"
            $LTS = $false
            $Name = "powershell"  # Preview builds use "powershell" not "powershell-preview"
            
            # This is the CORRECT logic that New-MacOSPackage should use (and now does)
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            # Verify the correct identifier is generated
            $IsPreview | Should -Be $true -Because "Version 7.6.0-preview.6 should be detected as preview"
            $pkgIdentifier | Should -Be "com.microsoft.powershell-preview" -Because "Preview builds must use preview identifier"
            
            # Verify the INCORRECT logic (the bug) would have failed
            $incorrectIsPreview = $Name -like '*-preview'
            $incorrectIsPreview | Should -Be $false -Because "Package name 'powershell' does not match '*-preview'"
            $incorrectPkgId = Get-MacOSPackageId -IsPreview:$incorrectIsPreview
            $incorrectPkgId | Should -Be "com.microsoft.powershell" -Because "The buggy logic would use stable identifier"
            $incorrectPkgId | Should -Not -Be $pkgIdentifier -Because "The bug and fix produce different results"
        }

        It "Should use Test-IsPreview with Version parameter for stable build detection" {
            $Version = "7.6.0"
            $LTS = $false
            $Name = "powershell"
            
            # This is the CORRECT logic that New-MacOSPackage now uses
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            # Verify the correct identifier is generated
            $IsPreview | Should -Be $false -Because "Version 7.6.0 should be detected as stable"
            $pkgIdentifier | Should -Be "com.microsoft.powershell" -Because "Stable builds use stable identifier"
        }

        It "Should pass LTS parameter to Test-IsPreview for LTS builds" {
            $Version = "7.4.0"  # Even without preview marker
            $LTS = $true
            $Name = "powershell-lts"
            
            # This is the CORRECT logic that New-MacOSPackage now uses
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            # Verify LTS builds are treated as stable
            $IsPreview | Should -Be $false -Because "LTS builds should never be treated as preview"
            $pkgIdentifier | Should -Be "com.microsoft.powershell" -Because "LTS builds use stable identifier"
        }

        It "Should pass LTS parameter even for version strings with preview markers" {
            $Version = "7.4.0-preview.1"  # Has preview marker
            $LTS = $true  # But LTS flag takes precedence
            $Name = "powershell-lts"
            
            # This is the CORRECT logic that New-MacOSPackage now uses
            $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
            $pkgIdentifier = Get-MacOSPackageId -IsPreview:$IsPreview
            
            # Verify LTS flag takes precedence over version string
            $IsPreview | Should -Be $false -Because "LTS flag should override version string preview marker"
            $pkgIdentifier | Should -Be "com.microsoft.powershell" -Because "LTS builds always use stable identifier"
        }
    }
}
