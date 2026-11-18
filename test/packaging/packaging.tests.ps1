# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Packaging Module Functions" {
    BeforeAll {
        Import-Module $PSScriptRoot/../../build.psm1 -Force
        Import-Module $PSScriptRoot/../../tools/packaging/packaging.psm1 -Force
    }

    Context "Global tool nuspec metadata" {
        It "Should leave owners empty in the <TemplateName> template" -TestCases @(
            @{ TemplateName = "GlobalToolNuSpec" }
            @{ TemplateName = "GlobalToolPointerNuSpec" }
        ) {
            param($TemplateName)

            $strings = Import-PowerShellDataFile (Join-Path $PSScriptRoot "../../tools/packaging/packaging.strings.psd1")
            $nuspec = [xml] ($strings[$TemplateName] -f "PowerShell", "7.8.0-preview.1", "Powershell_black_64.png")
            $owners = $nuspec.package.metadata.SelectSingleNode("*[local-name()='owners']")
            $owners | Should -Not -BeNullOrEmpty
            $owners.InnerText | Should -BeExactly ""
        }

        It "Should populate metadata without changing <PackageId> identity or payload" -TestCases @(
            @{ PackageId = "PowerShell.win-x64"; PackageVersion = "7.7.0"; NamespaceYear = "2011/08" }
            @{ PackageId = "PowerShell.linux-arm64"; PackageVersion = "7.8.0-preview.2"; NamespaceYear = "2013/05" }
            @{ PackageId = "PowerShell.osx-arm64"; PackageVersion = "8.0.1"; NamespaceYear = "2013/05" }
        ) {
            param($PackageId, $PackageVersion, $NamespaceYear)

            $packageRoot = New-Item -Path (Join-Path $TestDrive $PackageId) -ItemType Directory
            $nuspecPath = Join-Path $packageRoot "$PackageId.nuspec"
            @"
<package xmlns="http://schemas.microsoft.com/packaging/$NamespaceYear/nuspec.xsd">
  <metadata>
    <id>$PackageId</id>
    <version>$PackageVersion</version>
    <authors>pwsh</authors>
    <owners>Microsoft,PowerShell</owners>
    <description>Package Description</description>
    <icon>Powershell_64.png</icon>
    <repository type="git" url="https://github.com/PowerShell/PowerShell" commit="abc123" />
    <packageTypes><packageType name="DotnetToolRidPackage" /></packageTypes>
  </metadata>
</package>
"@ | Set-Content -LiteralPath $nuspecPath
            $toolSettingsPath = Join-Path $packageRoot "DotnetToolSettings.xml"
            $toolSettings = '<DotNetCliTool Version="2"><Commands><Command Name="pwsh" Runner="executable" /></Commands></DotNetCliTool>'
            Set-Content -LiteralPath $toolSettingsPath -Value $toolSettings -NoNewline

            Set-GlobalToolNuspecMetadata -Path $nuspecPath
            Set-GlobalToolNuspecMetadata -Path $nuspecPath

            $nupkgPath = Join-Path $TestDrive "$PackageId.$PackageVersion.nupkg"
            [System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot.FullName, $nupkgPath)
            $archive = [System.IO.Compression.ZipFile]::OpenRead($nupkgPath)
            try {
                $reader = [System.IO.StreamReader]::new($archive.GetEntry("$PackageId.nuspec").Open())
                try {
                    $nuspec = [xml] $reader.ReadToEnd()
                }
                finally {
                    $reader.Dispose()
                }

                $metadata = $nuspec.package.metadata
                $metadata.id | Should -BeExactly $PackageId
                $metadata.version | Should -BeExactly $PackageVersion
                $metadata.authors | Should -BeExactly "Microsoft"
                $metadata.owners | Should -BeExactly ""
                $metadata.requireLicenseAcceptance | Should -BeExactly "false"
                $metadata.license.type | Should -BeExactly "expression"
                $metadata.license.InnerText | Should -BeExactly "MIT"
                $metadata.icon | Should -BeExactly "Powershell_black_64.png"
                $metadata.projectUrl | Should -BeExactly "https://github.com/PowerShell/PowerShell"
                $metadata.description | Should -BeExactly "PowerShell global tool"
                $metadata.copyright | Should -BeExactly "$([char]0xA9) Microsoft Corporation. All rights reserved."
                $metadata.language | Should -BeExactly "en-US"
                $metadata.tags | Should -BeExactly "PowerShell"
                $metadata.packageTypes.packageType.name | Should -BeExactly "DotnetToolRidPackage"
                $metadata.repository.commit | Should -BeExactly "abc123"
                @($metadata.ChildNodes | Where-Object NamespaceURI -NE $metadata.NamespaceURI).Count | Should -Be 0
                @($metadata.ChildNodes | Where-Object LocalName -EQ "authors").Count | Should -Be 1
                $archive.GetEntry($metadata.icon) | Should -Not -BeNullOrEmpty
                $reader = [System.IO.StreamReader]::new($archive.GetEntry("DotnetToolSettings.xml").Open())
                try {
                    $reader.ReadToEnd() | Should -BeExactly $toolSettings
                }
                finally {
                    $reader.Dispose()
                }
            }
            finally {
                $archive.Dispose()
            }

            (Get-FileHash (Join-Path $packageRoot "Powershell_black_64.png")).Hash |
                Should -BeExactly (Get-FileHash (Join-Path $PSScriptRoot "../../assets/Powershell_black_64.png")).Hash
        }

        It "Should reject a nuspec with no package identity" {
            $nuspecPath = Join-Path $TestDrive "invalid.nuspec"
            '<package><metadata><description>Invalid package</description></metadata></package>' |
                Set-Content -LiteralPath $nuspecPath

            { Set-GlobalToolNuspecMetadata -Path $nuspecPath } | Should -Throw "*must contain a package id and version*"
        }
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
                Expected = "powershell-preview-7.7.0_preview.5-1.azl4.x86_64.rpm"
            }
            @{
                HostArchitecture = "aarch64"
                Expected = "powershell-preview-7.7.0_preview.5-1.azl4.aarch64.rpm"
            }
        ) {
            param($HostArchitecture, $Expected)

            Get-RpmPackageFileName `
                -Name "powershell-preview" `
                -Version "7.7.0-preview.5" `
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
                -Version "7.7.0-preview.5" `
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
            $spec | Should -Match "(?m)^\* .+ - 7\.7\.0_preview\.5-1\.azl4\r?$"
        }

        It "Should select the current master dependencies for <Distribution> with runtime <Runtime>" -TestCases @(
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
                $expectedDependencies += "dotnet-runtime-11.0"
            }

            $dependencies | Should -BeExactly $expectedDependencies
        }
    }

    Context "Azure Linux PMC mappings" {
        BeforeAll {
            $mappingPath = Join-Path $PSScriptRoot "../../tools/packages.microsoft.com/mapping.json"
            $mapping = Get-Content -Path $mappingPath -Raw | ConvertFrom-Json
        }

        It "Should not map packages to Azure Linux 4" {
            $azureLinux4Mappings = @(
                $mapping.Packages |
                    Where-Object {
                        $_.url -like "azurelinux-4*" -or
                        $_.PackageFormat -match "\.azl4\."
                    }
            )

            $azureLinux4Mappings | Should -BeNullOrEmpty
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
