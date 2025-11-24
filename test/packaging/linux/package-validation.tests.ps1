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
            
            $rpmPackages.Count | Should -BeGreaterThan 0 -Because "At least one RPM package should exist in the artifacts directory"
            
            $invalidPackages = @()
            # Regex pattern for valid RPM package names.
            # Breakdown:
            # ^powershell\-           : Starts with 'powershell-'
            # (preview-|lts-)?        : Optionally 'preview-' or 'lts-'
            # \d+\.\d+\.\d+           : Version number (e.g., 7.6.0)
            # (_[a-z]*\.\d+)?         : Optional underscore, letters, dot, and digits (e.g., _alpha.1)
            # -1\.                    : Literal '-1.'
            # (preview\.\d+\.)?       : Optional 'preview.' and digits, followed by a dot
            # (rh|cm)\.               : Either 'rh.' or 'cm.'
            # (x86_64|aarch64)\.rpm$  : Architecture and file extension
            $rpmPackageNamePattern = 'powershell\-(preview-|lts-)?\d+\.\d+\.\d+(_[a-z]*\.\d+)?-1\.(preview\.\d+\.)?(rh|cm)\.(x86_64|aarch64)\.rpm'

            foreach ($package in $rpmPackages) {
                if ($package.Name -notmatch $rpmPackageNamePattern) {
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
            # Regex pattern for valid DEB package names.
            # Valid examples:
            # - powershell-preview_7.6.0-preview.6-1.deb_amd64.deb
            # - powershell-lts_7.4.13-1.deb_amd64.deb
            # - powershell_7.4.13-1.deb_amd64.deb
            # - powershell_7.6.0~rc.1-1.deb_amd64.deb
            # Breakdown:
            # ^powershell            : Starts with 'powershell'
            # (-preview|-lts)?       : Optionally '-preview' or '-lts'
            # _\d+\.\d+\.\d+         : Underscore followed by version number (e.g., _7.6.0)
            # ([\-~][a-z]*.d+)?      : Optional dash or tilde, letters, dot, and digits (e.g., -preview.6 or ~rc.1)
            # -\d                    : Dash followed by iteration digit
            # \.deb_                 : Literal '.deb_'
            # (amd64|arm64)          : Architecture
            # \.deb$                 : File extension
            $debPackageNamePattern = '^powershell(-preview|-lts)?_\d+\.\d+\.\d+([\-~][a-z]*\.\d+)?-\d\.deb_(amd64|arm64)\.deb$'

            foreach ($package in $debPackages) {
                if ($package.Name -notmatch $debPackageNamePattern) {
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
        }
    }
    
    Context "Package Existence" {
        It "Should find at least one package in artifacts directory" {
            $allPackages = Get-ChildItem -Path $artifactsDir -Recurse -Include *.rpm, *.tar.gz, *.deb -ErrorAction SilentlyContinue
            
            $allPackages.Count | Should -BeGreaterThan 0 -Because "At least one package should exist in the artifacts directory"
        }
    }
}
