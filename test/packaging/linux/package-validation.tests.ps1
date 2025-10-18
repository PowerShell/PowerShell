# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Linux Package Name Validation" {
    BeforeAll {
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
            
            if ($rpmPackages.Count -eq 0) {
                Set-ItResult -Skipped -Because "No RPM packages found in artifacts directory"
                return
            }
            
            $invalidPackages = @()
            foreach ($package in $rpmPackages) {
                # Pattern matches: powershell-preview-7.6.0_preview.5-1.cm.x86_64.rpm
                # Also matches: powershell-7.6.0-1.rh.x86_64.rpm, powershell-lts-7.6.0-1.rh.x86_64.rpm
                if ($package.Name -notmatch 'powershell\-(preview-|lts-)?\d+\.\d+\.\d+(_[a-z]*\.\d+)?-1\.(rh|cm)\.(x86_64|aarch64)\.rpm') {
                    $invalidPackages += "$($package.Name) is not a valid RPM package name"
                    Write-Warning "$($package.Name) is not a valid RPM package name"
                }
            }
            
            if ($invalidPackages.Count -gt 0) {
                throw ($invalidPackages | Out-String)
            }
            
            $rpmPackages.Count | Should -BeGreaterThan 0
        }
    }
    
    Context "Tar.Gz Package Names" {
        It "Should have valid tar.gz package names" {
            $tarPackages = Get-ChildItem -Path $artifactsDir -Recurse -Filter *.tar.gz -ErrorAction SilentlyContinue
            
            if ($tarPackages.Count -eq 0) {
                Set-ItResult -Skipped -Because "No tar.gz packages found in artifacts directory"
                return
            }
            
            $invalidPackages = @()
            foreach ($package in $tarPackages) {
                # Pattern matches: powershell-7.6.0-preview.6-linux-x64.tar.gz or powershell-7.6.0-linux-x64.tar.gz
                # Also matches various runtime configurations
                if ($package.Name -notmatch 'powershell-(lts-)?\d+\.\d+\.\d+\-([a-z]*.\d+\-)?(linux|osx|linux-musl)+\-(x64\-fxdependent|x64|arm32|arm64|x64\-musl-noopt\-fxdependent)\.(tar\.gz)') {
                    $invalidPackages += "$($package.Name) is not a valid tar.gz package name"
                    Write-Warning "$($package.Name) is not a valid tar.gz package name"
                }
            }
            
            if ($invalidPackages.Count -gt 0) {
                throw ($invalidPackages | Out-String)
            }
            
            $tarPackages.Count | Should -BeGreaterThan 0
        }
    }
    
    Context "Package Existence" {
        It "Should find at least one package in artifacts directory" {
            $allPackages = Get-ChildItem -Path $artifactsDir -Recurse -Include *.rpm, *.tar.gz, *.deb -ErrorAction SilentlyContinue
            
            $allPackages.Count | Should -BeGreaterThan 0 -Because "At least one package should exist in the artifacts directory"
        }
    }
}
