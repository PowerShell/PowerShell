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

    Context "Azure Linux RPM packaging" {
        BeforeAll {
            $afterInstallScript = Join-Path $TestDrive "after-install.sh"
            $afterRemoveScript = Join-Path $TestDrive "after-remove.sh"
            "echo installed" | Set-Content -Path $afterInstallScript
            "echo removed" | Set-Content -Path $afterRemoveScript
            $linkInfo = & (Get-Module packaging) {
                [LinkInfo] @{
                    Source = "/tmp/pwsh"
                    Destination = "/usr/bin/pwsh"
                }
            }
        }

        It "Should target Azure Linux 3 and 4 for framework-dependent RPMs" {
            $distributions = & (Get-Module packaging) { $Script:RedhatFddDistributions }
            $distributions | Should -Be @("cm", "azl4")
        }

        It "Should generate an Azure Linux 4 RPM file name for <HostArchitecture>" -TestCases @(
            @{
                HostArchitecture = "x86_64"
                Expected = "powershell-preview-7.6.0_preview.6-1.azl4.x86_64.rpm"
            }
            @{
                HostArchitecture = "aarch64"
                Expected = "powershell-preview-7.6.0_preview.6-1.azl4.aarch64.rpm"
            }
        ) {
            param($HostArchitecture, $Expected)

            Get-RpmPackageFileName `
                -Name "powershell-preview" `
                -Version "7.6.0-preview.6" `
                -Iteration "1" `
                -Distribution "azl4" `
                -HostArchitecture $HostArchitecture |
                Should -BeExactly $Expected
        }

        It "Should write the Azure Linux 4 RPM Release metadata for <HostArchitecture>" -TestCases @(
            @{ HostArchitecture = "x86_64" }
            @{ HostArchitecture = "aarch64" }
        ) {
            param($HostArchitecture)

            $spec = New-RpmSpec `
                -Name "powershell-preview" `
                -Version "7.6.0-preview.6" `
                -Iteration "1" `
                -Description "PowerShell test package" `
                -Staging "/tmp/staging" `
                -Destination "/opt/microsoft/powershell/7-preview" `
                -ManGzipFile "/tmp/pwsh.1.gz" `
                -ManDestination "/usr/share/man/man1/pwsh-preview.1.gz" `
                -LinkInfo $linkInfo `
                -Dependencies @("glibc") `
                -AfterInstallScript $afterInstallScript `
                -AfterRemoveScript $afterRemoveScript `
                -Distribution "azl4" `
                -HostArchitecture $HostArchitecture

            $spec | Should -Match "(?m)^Release:\s+1\.azl4\r?$"
            $spec | Should -Match "(?m)^\* .+ - 7\.6\.0_preview\.6-1\.azl4\r?$"
        }

        It "Should select the .NET 10 Azure Linux dependencies for <Distribution> with runtime <Runtime>" -TestCases @(
            @{ Distribution = "cm"; Runtime = "linux-x64"; IncludeDotnetRuntime = $false }
            @{ Distribution = "azl4"; Runtime = "linux-arm64"; IncludeDotnetRuntime = $false }
            @{ Distribution = "cm"; Runtime = "fxdependent-linux-x64"; IncludeDotnetRuntime = $true }
            @{ Distribution = "azl4"; Runtime = "fxdependent-linux-arm64"; IncludeDotnetRuntime = $true }
        ) {
            param($Distribution, $Runtime, $IncludeDotnetRuntime)

            $dependencies = & (Get-Module packaging) {
                param($Distribution, $Runtime)

                $previousOptions = $Script:Options
                try {
                    $Script:Options = [pscustomobject] @{ Runtime = $Runtime }
                    @(Get-PackageDependencies -Distribution $Distribution)
                }
                finally {
                    $Script:Options = $previousOptions
                }
            } $Distribution $Runtime

            $expectedDependencies = @(
                "ca-certificates"
                "glibc"
                "icu"
                "libgcc"
                "libstdc++"
                "openssl-libs"
                "tzdata"
            )
            if ($IncludeDotnetRuntime) {
                $expectedDependencies += "dotnet-runtime-10.0"
            }

            $dependencies | Should -BeExactly $expectedDependencies
        }
    }

    Context "Azure Linux PMC mappings" {
        BeforeAll {
            $mappingPath = Join-Path $PSScriptRoot "../../tools/packages.microsoft.com/mapping.json"
            $mapping = Get-Content -Path $mappingPath -Raw | ConvertFrom-Json
        }

        It "Should map <Channel> <Architecture> packages to <Url>" -TestCases @(
            @{
                Channel = "preview"
                Architecture = "x86_64"
                Url = "azurelinux-4-preview-microsoft-x86_64"
            }
            @{
                Channel = "preview"
                Architecture = "aarch64"
                Url = "azurelinux-4-preview-microsoft-aarch64"
            }
            @{
                Channel = "stable"
                Architecture = "x86_64"
                Url = "azurelinux-4-prod-microsoft-x86_64"
            }
            @{
                Channel = "stable"
                Architecture = "aarch64"
                Url = "azurelinux-4-prod-microsoft-aarch64"
            }
        ) {
            param($Channel, $Architecture, $Url)

            $mappedPackage = @($mapping.Packages | Where-Object url -EQ $Url)
            $mappedPackage.Count | Should -Be 1
            $mappedPackage[0].channel | Should -BeExactly $Channel
            $mappedPackage[0].distribution | Should -BeExactly @("azl")
            $mappedPackage[0].PackageFormat |
                Should -BeExactly "PACKAGE_NAME-POWERSHELL_RELEASE-1.azl4.$Architecture.rpm"
        }

        It "Should remove the Azure Linux 4 beta mappings" {
            @($mapping.Packages | Where-Object url -Like "azurelinux-4.0-beta-*").Count | Should -Be 0
        }

        It "Should retain all Azure Linux 3 .cm mappings" {
            $azureLinux3Mappings = @($mapping.Packages | Where-Object url -Like "azurelinux-3.0-*")
            $azureLinux3Mappings.Count | Should -Be 4
            $azureLinux3Mappings.PackageFormat | Should -Match "\.cm\.(x86_64|aarch64)\.rpm$"
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
}
