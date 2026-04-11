# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Linux Package Name Validation" {
    BeforeAll {
        Import-Module $PSScriptRoot/../../tools/packaging/packaging.psm1 -Force
        
        # Determine artifacts directory (GitHub Actions or Azure DevOps)
        $artifactsDir = if ($env:GITHUB_ACTIONS -eq 'true') {
            "$env:GITHUB_WORKSPACE/../packages"
        } else {
            $env:SYSTEM_ARTIFACTSDIRECTORY
        }
        
        if (-not $artifactsDir) {
            throw "Artifacts directory not found. GITHUB_WORKSPACE or SYSTEM_ARTIFACTSDIRECTORY must be set."
        }
        
        Write-Verbose "Artifacts directory: $artifactsDir" -Verbose
    }
    
    Context "RPM Package Names" {
        It "Should have valid RPM package names" {
            $rpmPackages = Get-ChildItem -Path $artifactsDir -Recurse -Filter *.rpm -ErrorAction SilentlyContinue
            
            $rpmPackages.Count | Should -BeGreaterThan 0 -Because "At least one RPM package should exist in the artifacts directory"
            
            $invalidPackages = @()
            
            foreach ($package in $rpmPackages) {
                if (-not (Test-PackageNameRpm -PackageName $package.Name)) {
                    $invalidPackages += "$($package.Name) is not a valid RPM package name"
                    Write-Warning "$($package.Name) is not a valid RPM package name"
                }
            }
            
            if ($invalidPackages.Count -gt 0) {
                throw ($invalidPackages | Out-String)
            }
        }
    }
    
    Context "DEB Package Names" {
        It "Should have valid DEB package names" {
            $debPackages = Get-ChildItem -Path $artifactsDir -Recurse -Filter *.deb -ErrorAction SilentlyContinue
            
            $debPackages.Count | Should -BeGreaterThan 0 -Because "At least one DEB package should exist in the artifacts directory"
            
            $invalidPackages = @()
            
            foreach ($package in $debPackages) {
                if (-not (Test-PackageNameDeb -PackageName $package.Name)) {
                    $invalidPackages += "$($package.Name) is not a valid DEB package name"
                    Write-Warning "$($package.Name) is not a valid DEB package name"
                }
            }
            
            if ($invalidPackages.Count -gt 0) {
                throw ($invalidPackages | Out-String)
            }
        }
    }
    
    Context "Tar.Gz Package Names" {
        It "Should have valid tar.gz package names" {
            $tarPackages = Get-ChildItem -Path $artifactsDir -Recurse -Filter *.tar.gz -ErrorAction SilentlyContinue
            
            $tarPackages.Count | Should -BeGreaterThan 0 -Because "At least one tar.gz package should exist in the artifacts directory"
            
            $invalidPackages = @()
            foreach ($package in $tarPackages) {
                if (-not (Test-PackageNameTarGz -PackageName $package.Name)) {
                    $invalidPackages += "$($package.Name) is not a valid tar.gz package name"
                    Write-Warning "$($package.Name) is not a valid tar.gz package name"
                }
            }
            
            if ($invalidPackages.Count -gt 0) {
                throw ($invalidPackages | Out-String)
            }
        }
    }
    
    Context "Package Existence" {
        It "Should find at least one package in artifacts directory" {
            $allPackages = Get-ChildItem -Path $artifactsDir -Recurse -Include *.rpm, *.tar.gz, *.deb -ErrorAction SilentlyContinue
            
            $allPackages.Count | Should -BeGreaterThan 0 -Because "At least one package should exist in the artifacts directory"
        }
    }
}
