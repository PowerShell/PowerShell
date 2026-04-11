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

        It "Should NOT use package name for preview detection (bug fix verification) - <Name>" -TestCases @(
            @{ Version = "7.6.0-preview.6"; Name = "Preview" }
            @{ Version = "7.6.0-rc.1"; Name = "RC" }
        ) {
            # This test verifies the fix for issue #26673
            # The bug was using ($Name -like '*-preview') which always returned false
            # because preview builds use Name="powershell" not "powershell-preview"
            param($Version)

            # The CORRECT logic (the fix): uses version string
            $result = Get-MacOSPackageIdentifierInfo -Version $Version -LTS:$false
            $result.IsPreview | Should -Be $true -Because "Version string correctly identifies preview"
            $result.PackageIdentifier | Should -Be "com.microsoft.powershell-preview"
        }
    }

    Context "Package Name Validation Functions" {
        Context "Test-PackageNameRpm" {
            It "Should accept valid stable RPM names" {
                Test-PackageNameRpm -PackageName "powershell-7.6.0-1.rh.x86_64.rpm" | Should -Be $true
                Test-PackageNameRpm -PackageName "powershell-7.4.13-1.cm.aarch64.rpm" | Should -Be $true
            }

            It "Should accept valid LTS RPM names" {
                Test-PackageNameRpm -PackageName "powershell-lts-7.4.13-1.rh.x86_64.rpm" | Should -Be $true
                Test-PackageNameRpm -PackageName "powershell-lts-7.2.0-1.cm.aarch64.rpm" | Should -Be $true
            }

            It "Should accept valid preview RPM names" {
                Test-PackageNameRpm -PackageName "powershell-preview-7.6.0-1.rh.x86_64.rpm" | Should -Be $true
            }

            It "Should accept RPM names with suffixes" {
                Test-PackageNameRpm -PackageName "powershell-7.6.0_alpha.1-1.rh.x86_64.rpm" | Should -Be $true
                Test-PackageNameRpm -PackageName "powershell-7.6.0_preview.6-1.cm.aarch64.rpm" | Should -Be $true
            }

            It "Should reject RPM names with invalid characters in suffix" {
                Test-PackageNameRpm -PackageName "powershell-7.6.0_preview@6-1.rh.x86_64.rpm" | Should -Be $false
                Test-PackageNameRpm -PackageName "powershell-7.6.0_preview-6-1.rh.x86_64.rpm" | Should -Be $false
            }

            It "Should reject RPM names with empty label in suffix" {
                Test-PackageNameRpm -PackageName "powershell-7.6.0_.6-1.rh.x86_64.rpm" | Should -Be $false
            }

            It "Should reject RPM names with invalid architecture" {
                Test-PackageNameRpm -PackageName "powershell-7.6.0-1.rh.arm64.rpm" | Should -Be $false
            }

            It "Should reject RPM names with invalid distribution" {
                Test-PackageNameRpm -PackageName "powershell-7.6.0-1.ubuntu.x86_64.rpm" | Should -Be $false
            }
        }

        Context "Test-PackageNameTarGz" {
            It "Should accept valid stable tar.gz names" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linux-x64.tar.gz" | Should -Be $true
                Test-PackageNameTarGz -PackageName "powershell-7.4.13-osx-arm64.tar.gz" | Should -Be $true
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linux-musl-x64.tar.gz" | Should -Be $true
            }

            It "Should accept valid LTS tar.gz names" {
                Test-PackageNameTarGz -PackageName "powershell-lts-7.4.13-linux-x64.tar.gz" | Should -Be $true
            }

            It "Should accept tar.gz names with preview suffix" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-preview.6-linux-x64.tar.gz" | Should -Be $true
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-alpha.1-osx-arm64.tar.gz" | Should -Be $true
            }

            It "Should accept tar.gz names with various runtimes" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linux-x64-fxdependent.tar.gz" | Should -Be $true
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linux-arm32.tar.gz" | Should -Be $true
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linux-musl-x64-musl-noopt-fxdependent.tar.gz" | Should -Be $true
            }

            It "Should reject tar.gz names with invalid characters in suffix" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-preview@6-linux-x64.tar.gz" | Should -Be $false
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-preview-6-linux-x64.tar.gz" | Should -Be $false
            }

            It "Should reject tar.gz names with empty label in suffix" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-.6-linux-x64.tar.gz" | Should -Be $false
            }

            It "Should reject tar.gz names with repeated platform tokens" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linuxlinux-x64.tar.gz" | Should -Be $false
            }

            It "Should reject tar.gz names with invalid platform" {
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-windows-x64.tar.gz" | Should -Be $false
            }

            It "Should reject tar.gz names without anchoring (extra text)" {
                Test-PackageNameTarGz -PackageName "extra-powershell-7.6.0-linux-x64.tar.gz" | Should -Be $false
                Test-PackageNameTarGz -PackageName "powershell-7.6.0-linux-x64.tar.gz-extra" | Should -Be $false
            }
        }

        Context "Test-PackageNamePkg" {
            It "Should accept valid stable PKG names" {
                Test-PackageNamePkg -PackageName "powershell-7.6.0-osx-x64.pkg" | Should -Be $true
                Test-PackageNamePkg -PackageName "powershell-7.4.13-osx-arm64.pkg" | Should -Be $true
            }

            It "Should accept valid LTS PKG names" {
                Test-PackageNamePkg -PackageName "powershell-lts-7.4.13-osx-x64.pkg" | Should -Be $true
            }

            It "Should accept PKG names with preview suffix" {
                Test-PackageNamePkg -PackageName "powershell-7.6.0-preview.6-osx-x64.pkg" | Should -Be $true
                Test-PackageNamePkg -PackageName "powershell-7.4.13-rebuild.5-osx-arm64.pkg" | Should -Be $true
            }

            It "Should reject PKG names with invalid characters in suffix" {
                Test-PackageNamePkg -PackageName "powershell-7.6.0-preview@6-osx-x64.pkg" | Should -Be $false
                Test-PackageNamePkg -PackageName "powershell-7.6.0-preview-6-osx-x64.pkg" | Should -Be $false
            }

            It "Should reject PKG names with empty label in suffix" {
                Test-PackageNamePkg -PackageName "powershell-7.6.0-.6-osx-x64.pkg" | Should -Be $false
            }

            It "Should reject PKG names with invalid architecture" {
                Test-PackageNamePkg -PackageName "powershell-7.6.0-osx-x86.pkg" | Should -Be $false
            }

            It "Should reject PKG names without anchoring (extra text)" {
                Test-PackageNamePkg -PackageName "extra-powershell-7.6.0-osx-x64.pkg" | Should -Be $false
                Test-PackageNamePkg -PackageName "powershell-7.6.0-osx-x64.pkg-extra" | Should -Be $false
            }
        }

        Context "Test-PackageNameWindowsMsiZip" {
            It "Should accept valid stable MSI names" {
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-win-x64.msi" | Should -Be $true
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.4.13-win-arm64.msi" | Should -Be $true
            }

            It "Should accept valid stable ZIP names" {
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-win-x64.zip" | Should -Be $true
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.4.13-win-x86.zip" | Should -Be $true
            }

            It "Should accept MSI/ZIP names with preview suffix" {
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-preview.6-win-x64.msi" | Should -Be $true
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-alpha.1-win-x64.zip" | Should -Be $true
            }

            It "Should accept MSI/ZIP names with fxdependent runtime" {
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-win-fxdependent.zip" | Should -Be $true
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-win-fxdependentWinDesktop.zip" | Should -Be $true
            }

            It "Should reject MSI/ZIP names with invalid characters in suffix" {
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-preview@6-win-x64.msi" | Should -Be $false
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-preview-6-win-x64.zip" | Should -Be $false
            }

            It "Should reject MSI/ZIP names with empty label in suffix" {
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-.6-win-x64.msi" | Should -Be $false
            }

            It "Should reject MSI/ZIP names with wrong case" {
                Test-PackageNameWindowsMsiZip -PackageName "powershell-7.6.0-win-x64.msi" | Should -Be $false
            }

            It "Should reject MSI/ZIP names without anchoring (extra text)" {
                Test-PackageNameWindowsMsiZip -PackageName "extra-PowerShell-7.6.0-win-x64.msi" | Should -Be $false
                Test-PackageNameWindowsMsiZip -PackageName "PowerShell-7.6.0-win-x64.msi-extra" | Should -Be $false
            }
        }

        Context "Test-PackageNameDeb" {
            It "Should accept valid stable DEB names" {
                Test-PackageNameDeb -PackageName "powershell_7.6.0-1.deb_amd64.deb" | Should -Be $true
                Test-PackageNameDeb -PackageName "powershell_7.4.13-1.deb_arm64.deb" | Should -Be $true
            }

            It "Should accept valid preview DEB names" {
                Test-PackageNameDeb -PackageName "powershell-preview_7.6.0-1.deb_amd64.deb" | Should -Be $true
            }

            It "Should accept valid LTS DEB names" {
                Test-PackageNameDeb -PackageName "powershell-lts_7.4.13-1.deb_amd64.deb" | Should -Be $true
            }

            It "Should accept DEB names with dash suffix" {
                Test-PackageNameDeb -PackageName "powershell-preview_7.6.0-preview.6-1.deb_amd64.deb" | Should -Be $true
            }

            It "Should accept DEB names with tilde suffix" {
                Test-PackageNameDeb -PackageName "powershell_7.6.0~preview.6-1.deb_amd64.deb" | Should -Be $true
            }

            It "Should reject DEB names with invalid characters in suffix" {
                Test-PackageNameDeb -PackageName "powershell_7.6.0-preview@6-1.deb_amd64.deb" | Should -Be $false
                Test-PackageNameDeb -PackageName "powershell_7.6.0-preview-6-1.deb_amd64.deb" | Should -Be $false
            }

            It "Should reject DEB names with empty label in suffix" {
                Test-PackageNameDeb -PackageName "powershell_7.6.0-.6-1.deb_amd64.deb" | Should -Be $false
            }

            It "Should reject DEB names with invalid architecture" {
                Test-PackageNameDeb -PackageName "powershell_7.6.0-1.deb_x86.deb" | Should -Be $false
            }

            It "Should reject DEB names without anchoring (extra text)" {
                Test-PackageNameDeb -PackageName "extra-powershell_7.6.0-1.deb_amd64.deb" | Should -Be $false
                Test-PackageNameDeb -PackageName "powershell_7.6.0-1.deb_amd64.deb-extra" | Should -Be $false
            }
        }
    }
}
