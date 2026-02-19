# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

. "$PSScriptRoot\..\buildCommon\startNativeExecution.ps1"

$Environment = Get-EnvironmentInformation
$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path

$packagingStrings = Import-PowerShellDataFile "$PSScriptRoot\packaging.strings.psd1"
Import-Module "$PSScriptRoot\..\Xml" -ErrorAction Stop -Force
$DebianDistributions = @("deb")
$RedhatFullDistributions = @("rh")
$RedhatFddDistributions = @("cm")
$RedhatDistributions = @()
$RedhatDistributions += $RedhatFullDistributions
$RedhatDistributions += $RedhatFddDistributions
$AllDistributions = @()
$AllDistributions += $DebianDistributions
$AllDistributions += $RedhatDistributions
$AllDistributions += 'macOs'
$script:netCoreRuntime = 'net11.0'
$script:iconFileName = "Powershell_black_64.png"
$script:iconPath = Join-Path -path $PSScriptRoot -ChildPath "../../assets/$iconFileName" -Resolve

class R2RVerification {
    [ValidateSet('NoR2R','R2R','SdkOnly')]
    [string]
    $R2RState = 'R2R'

    [System.Reflection.PortableExecutable.Machine]
    $Architecture = [System.Reflection.PortableExecutable.Machine]::Amd64

    [ValidateSet('Linux','Apple','Windows')]
    [string]
    $OperatingSystem = 'Windows'
}

function Start-PSPackage {
    [CmdletBinding(DefaultParameterSetName='Version',SupportsShouldProcess=$true)]
    param(
        # PowerShell packages use Semantic Versioning https://semver.org/
        [Parameter(ParameterSetName = "Version")]
        [string]$Version,

        [Parameter(ParameterSetName = "ReleaseTag")]
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag,

        # Package name
        [ValidatePattern("^powershell")]
        [string]$Name = "powershell",

        # Ubuntu, CentOS, Fedora, macOS, and Windows packages are supported
        [ValidateSet("msix", "deb", "osxpkg", "rpm", "rpm-fxdependent", "rpm-fxdependent-arm64", "msi", "zip", "zip-pdb", "tar", "tar-arm", "tar-arm64", "tar-alpine", "fxdependent", "fxdependent-win-desktop", "min-size", "tar-alpine-fxdependent")]
        [string[]]$Type,

        # Generate windows downlevel package
        [ValidateSet("win7-x86", "win7-x64", "win-arm", "win-arm64")]
        [string] $WindowsRuntime,

        [ValidateSet('osx-x64', 'osx-arm64')]
        [ValidateScript({$Environment.IsMacOS})]
        [string] $MacOSRuntime,

        [string] $PackageBinPath,

        [switch] $Private,

        [Switch] $Force,

        [Switch] $SkipReleaseChecks,

        [switch] $NoSudo,

        [switch] $LTS
    )

    DynamicParam {
        if ($Type -in ('zip', 'min-size') -or $Type -like 'fxdependent*') {
            # Add a dynamic parameter '-IncludeSymbols' when the specified package type is 'zip' only.
            # The '-IncludeSymbols' parameter can be used to indicate that the package should only contain powershell binaries and symbols.
            $ParameterAttr = New-Object "System.Management.Automation.ParameterAttribute"
            $Attributes = New-Object "System.Collections.ObjectModel.Collection``1[System.Attribute]"
            $Attributes.Add($ParameterAttr) > $null

            $Parameter = New-Object "System.Management.Automation.RuntimeDefinedParameter" -ArgumentList ("IncludeSymbols", [switch], $Attributes)
            $Dict = New-Object "System.Management.Automation.RuntimeDefinedParameterDictionary"
            $Dict.Add("IncludeSymbols", $Parameter) > $null
            return $Dict
        }
    }

    End {
        $IncludeSymbols = $null
        if ($PSBoundParameters.ContainsKey('IncludeSymbols')) {
            Write-Log 'setting IncludeSymbols'
            $IncludeSymbols = $PSBoundParameters['IncludeSymbols']
        }

        # Runtime and Configuration settings required by the package
        ($Runtime, $Configuration) = if ($WindowsRuntime) {
            $WindowsRuntime, "Release"
        } elseif ($MacOSRuntime) {
           $MacOSRuntime, "Release"
        } elseif ($Type.Count -eq 1 -and $Type[0] -eq "tar-alpine") {
            New-PSOptions -Configuration "Release" -Runtime "linux-musl-x64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type.Count -eq 1 -and $Type[0] -eq "tar-arm") {
            New-PSOptions -Configuration "Release" -Runtime "Linux-ARM" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type.Count -eq 1 -and $Type[0] -eq "tar-arm64") {
            if ($IsMacOS) {
                New-PSOptions -Configuration "Release" -Runtime "osx-arm64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
            } else {
                New-PSOptions -Configuration "Release" -Runtime "Linux-ARM64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
            }
        } elseif ($Type.Count -eq 1 -and $Type[0] -eq "rpm-fxdependent") {
            New-PSOptions -Configuration "Release" -Runtime 'fxdependent-linux-x64' -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type.Count -eq 1 -and $Type[0] -eq "rpm-fxdependent-arm64") {
            New-PSOptions -Configuration "Release" -Runtime 'fxdependent-linux-arm64' -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        }
        elseif ($Type.Count -eq 1 -and $Type[0] -eq "tar-alpine-fxdependent") {
            New-PSOptions -Configuration "Release" -Runtime 'fxdependent-noopt-linux-musl-x64' -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        }
        else {
            New-PSOptions -Configuration "Release" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        }

        if ($Environment.IsWindows) {
            # Runtime will be one of win7-x64, win7-x86, "win-arm" and "win-arm64" on Windows.
            # Build the name suffix for universal win-plat packages.
            switch ($Runtime) {
                "win-arm64" { $NameSuffix = "win-arm64" }
                default     { $NameSuffix = $_ -replace 'win\d+', 'win' }
            }
        }

        if ($Type -eq 'fxdependent') {
            $NameSuffix = "win-fxdependent"
            Write-Log "Packaging : '$Type'; Packaging Configuration: '$Configuration', Runtime: '$Runtime'"
        } elseif ($Type -eq 'fxdependent-win-desktop') {
            $NameSuffix = "win-fxdependentWinDesktop"
            Write-Log "Packaging : '$Type'; Packaging Configuration: '$Configuration', Runtime: '$Runtime'"
        } elseif ($MacOSRuntime) {
            $NameSuffix = $MacOSRuntime
            Write-Log "Packaging : '$Type'; Packaging Configuration: '$Configuration', Runtime: '$Runtime'"
        } else {
            Write-Log "Packaging RID: '$Runtime'; Packaging Configuration: '$Configuration'"
        }

        $Script:Options = Get-PSOptions
        $actualParams = @()

        $PSModuleRestoreCorrect = $false

        # Require PSModuleRestore for packaging without symbols
        # But Disallow it when packaging with symbols
        if (!$IncludeSymbols.IsPresent -and $Script:Options.PSModuleRestore) {
            $actualParams += '-PSModuleRestore'
            $PSModuleRestoreCorrect = $true
        }
        elseif ($IncludeSymbols.IsPresent -and !$Script:Options.PSModuleRestore) {
            $PSModuleRestoreCorrect = $true
        }
        else {
            $actualParams += '-PSModuleRestore'
        }

        $precheckFailed = if ($Type -like 'fxdependent*' -or $Type -eq 'tar-alpine') {
            ## We do not check on runtime for framework dependent package.
            -not $Script:Options -or                                ## Start-PSBuild hasn't been executed yet
            -not $PSModuleRestoreCorrect -or                        ## Last build didn't specify '-PSModuleRestore' correctly
            $Script:Options.Configuration -ne $Configuration -or    ## Last build was with configuration other than 'Release'
            $Script:Options.Framework -ne $script:netCoreRuntime    ## Last build wasn't for CoreCLR
        } else {
            -not $Script:Options -or                                ## Start-PSBuild hasn't been executed yet
            -not $PSModuleRestoreCorrect -or                        ## Last build didn't specify '-PSModuleRestore' correctly
            $Script:Options.Runtime -ne $Runtime -or                ## Last build wasn't for the required RID
            $Script:Options.Configuration -ne $Configuration -or    ## Last build was with configuration other than 'Release'
            $Script:Options.Framework -ne $script:netCoreRuntime    ## Last build wasn't for CoreCLR
        }

        # Make sure the most recent build satisfies the package requirement
        if ($precheckFailed) {
            # It's possible that the most recent build doesn't satisfy the package requirement but
            # an earlier build does.
            # It's also possible that the last build actually satisfies the package requirement but
            # then `Start-PSPackage` runs from a new PS session or `build.psm1` was reloaded.
            #
            # In these cases, the user will be asked to build again even though it's technically not
            # necessary. However, we want it that way -- being very explict when generating packages.
            # This check serves as a simple gate to ensure that the user knows what he is doing, and
            # also ensure `Start-PSPackage` does what the user asks/expects, because once packages
            # are generated, it'll be hard to verify if they were built from the correct content.

            Write-Warning -Message "Start-PSPackage: The build PreCheck has failed."
            if (-not $Script:Options) {
                Write-Warning -Message "Start-PSPackage: builid options variable is null indicating Start-PSBuild hasn't been run yet."
            }
            if (-not $PSModuleRestoreCorrect) {
                Write-Warning -Message "Start-PSPackage: PSModuleRestoreCorrect variable is null indicating build -PSModuleRestore was not performed."
            }
            if ($Script:Options.Configuration -ne $Configuration) {
                Write-Warning -Message "Start-PSPackage: Build configuration is incorrect: Expected: $Configuration Actual: $($Script:Options.Configuration)"
            }
            if ($Script:Options.Framework -ne $script:netCoreRuntime) {
                Write-Warning -Message "Start-PSPackage: Build .NET version is incorrect: Expected: $($script:netCoreRuntime) Actual: $($Script:Options.Framework)"
            }
            if (($Type -notlike 'fxdependent*' -and $Type -ne 'tar-alpine') -and ($Script:Options.Runtime -ne $Runtime)) {
                Write-Warning -Message "Start-PSPackage: Build RID does not match expected RID: Expected: $Runtime Actual: $($Script:Options.Runtime)"
            }

            $params = @('-Clean')

            if (!$IncludeSymbols.IsPresent) {
                $params += '-PSModuleRestore'
            }

            $actualParams += '-Runtime ' + $Script:Options.Runtime

            if ($Type -eq 'fxdependent') {
                $params += '-Runtime', 'fxdependent'
            } elseif ($Type -eq 'fxdependent-win-desktop') {
                $params += '-Runtime', 'fxdependent-win-desktop'
            } else {
                $params += '-Runtime', $Runtime
            }

            $params += '-Configuration', $Configuration
            $actualParams += '-Configuration ' + $Script:Options.Configuration

            Write-Warning "Build started with unexpected parameters 'Start-PSBuild $actualParams"
            throw "Please ensure you have run 'Start-PSBuild $params'!"
        }

        if ($SkipReleaseChecks.IsPresent) {
            Write-Warning "Skipping release checks."
        }
        elseif (!$Script:Options.RootInfo.IsValid){
            throw $Script:Options.RootInfo.Warning
        }

        # If ReleaseTag is specified, use the given tag to calculate Version
        if ($PSCmdlet.ParameterSetName -eq "ReleaseTag") {
            $Version = $ReleaseTag -Replace '^v'
        }

        # Use Git tag if not given a version
        if (-not $Version) {
            $Version = (git --git-dir="$RepoRoot/.git" describe) -Replace '^v'
        }

        $Source = if ($PackageBinPath) {
            $PackageBinPath
        }
        else {
            Split-Path -Path $Script:Options.Output -Parent
        }

        Write-Verbose -Verbose "Source: $Source"

        # Copy the ThirdPartyNotices.txt so it's part of the package
        Copy-Item "$RepoRoot/ThirdPartyNotices.txt" -Destination $Source -Force

        # Copy the default.help.txt so it's part of the package
        Copy-Item "$RepoRoot/assets/default.help.txt" -Destination "$Source/en-US" -Force

        if (-not $SkipGenerateReleaseFiles -and -not $env:TF_BUILD) {
            # Make sure psoptions.json file exists so appropriate files.wsx is generated
            $psOptionsPath = (Join-Path -Path $Source "psoptions.json")
            if (-not (Test-Path -Path $psOptionsPath)) {
                $createdOptionsFile = New-Item -Path $psOptionsPath -Force
                Write-Verbose -Verbose "Created psoptions file: $createdOptionsFile"
            }

            # Make sure _manifest\spdx_2.2\manifest.spdx.json file exists so appropriate files.wxs is generated
            $manifestSpdxPath = (Join-Path -Path $Source "_manifest\spdx_2.2\manifest.spdx.json")
            if (-not (Test-Path -Path $manifestSpdxPath)) {
                $createdSpdxPath = New-Item -Path $manifestSpdxPath -Force
                Write-Verbose -Verbose "Created manifest.spdx.json file: $createdSpdxPath"
            }

            $manifestSpdxPathSha = (Join-Path -Path $Source "_manifest\spdx_2.2\manifest.spdx.json.sha256")
            if (-not (Test-Path -Path $manifestSpdxPathSha)) {
                $createdSpdxPathSha = New-Item -Path $manifestSpdxPathSha -Force
                Write-Verbose -Verbose "Created manifest.spdx.json.sha256 file: $createdSpdxPathSha"
            }

            $bsiJsonPath = (Join-Path -Path $Source "_manifest\spdx_2.2\bsi.json")
            if (-not (Test-Path -Path $bsiJsonPath)) {
                $createdBsiJsonPath = New-Item -Path $bsiJsonPath -Force
                Write-Verbose -Verbose "Created bsi.json file: $createdBsiJsonPath"
            }

            $manifestCatPath = (Join-Path -Path $Source "_manifest\spdx_2.2\manifest.cat")
            if (-not (Test-Path -Path $manifestCatPath)) {
                $createdCatPath = New-Item -Path $manifestCatPath -Force
                Write-Verbose -Verbose "Created manifest.cat file: $createdCatPath"
            }
        }

        # If building a symbols package, we add a zip of the parent to publish
        if ($IncludeSymbols.IsPresent)
        {
            $publishSource = $Source
            $buildSource = Split-Path -Path $Source -Parent
            $Source = New-TempFolder
            $symbolsSource = New-TempFolder

            try
            {
                # Copy files which go into the root package
                Get-ChildItem -Path $publishSource | Copy-Item -Destination $Source -Recurse

                $signingXml = [xml] (Get-Content (Join-Path $PSScriptRoot "..\releaseBuild\signing.xml" -Resolve))
                # Only include the files we sign for compliance scanning, those are the files we build.
                $filesToInclude = $signingXml.SignConfigXML.job.file.src | Where-Object {  -not $_.endswith('pwsh.exe') -and ($_.endswith(".dll") -or $_.endswith(".exe")) } | ForEach-Object { ($_ -split '\\')[-1] }
                $filesToInclude += $filesToInclude | ForEach-Object { $_ -replace '.dll', '.pdb' }
                Get-ChildItem -Path $buildSource | Where-Object { $_.Name -in $filesToInclude } | Copy-Item -Destination $symbolsSource -Recurse

                # Zip symbols.zip to the root package
                $zipSource = Join-Path $symbolsSource -ChildPath '*'
                $zipPath = Join-Path -Path $Source -ChildPath 'symbols.zip'
                Save-PSOptions -PSOptionsPath (Join-Path -Path $source -ChildPath 'psoptions.json') -Options $Script:Options
                Compress-Archive -Path $zipSource -DestinationPath $zipPath
            }
            finally
            {
                Remove-Item -Path $symbolsSource -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        Write-Log "Packaging Source: '$Source'"

        # Decide package output type
        if (-not $Type) {
            $Type = if ($Environment.IsLinux) {
                if ($Environment.LinuxInfo.ID -match "ubuntu") {
                    "deb", "tar"
                } elseif ($Environment.IsRedHatFamily) {
                    "rpm"
                } elseif ($Environment.IsSUSEFamily) {
                    "rpm"
                } else {
                    throw "Building packages for $($Environment.LinuxInfo.PRETTY_NAME) is unsupported!"
                }
            } elseif ($Environment.IsMacOS) {
                "osxpkg", "tar"
            } elseif ($Environment.IsWindows) {
                "msi", "msix"
            }
            Write-Warning "-Type was not specified, continuing with $Type!"
        }
        Write-Log "Packaging Type: $Type"

        # Add the symbols to the suffix
        # if symbols are specified to be included
        if ($IncludeSymbols.IsPresent -and $NameSuffix) {
            $NameSuffix = "symbols-$NameSuffix"
        }
        elseif ($IncludeSymbols.IsPresent) {
            $NameSuffix = "symbols"
        }

        switch ($Type) {
            "zip" {
                $os, $architecture = ($Script:Options.Runtime -split '-')
                $peOS = ConvertTo-PEOperatingSystem -OperatingSystem $os
                $peArch =  ConvertTo-PEArchitecture -Architecture $architecture

                $Arguments = @{
                    PackageNameSuffix = $NameSuffix
                    PackageSourcePath = $Source
                    PackageVersion = $Version
                    Force = $Force
                }

                if ($architecture -in 'x86', 'x64', 'arm', 'arm64') {
                    $Arguments += @{ R2RVerification = [R2RVerification]@{
                            R2RState = 'R2R'
                            OperatingSystem = $peOS
                            Architecture = $peArch
                        }
                    }
                } else {
                    $Arguments += @{ R2RVerification = [R2RVerification]@{
                            R2RState = 'SdkOnly'
                            OperatingSystem = $peOS
                            Architecture = $peArch
                        }
                    }
                }

                if ($PSCmdlet.ShouldProcess("Create Zip Package")) {
                    New-ZipPackage @Arguments
                }
            }
            "zip-pdb" {
                $Arguments = @{
                    PackageNameSuffix = $NameSuffix
                    PackageSourcePath = $Source
                    PackageVersion = $Version
                    Force = $Force
                }

                if ($PSCmdlet.ShouldProcess("Create Symbols Zip Package")) {
                    New-PdbZipPackage @Arguments
                }
            }
            "min-size" {
                # Add suffix '-gc' because this package is for the Guest Config team.
                if ($Environment.IsWindows) {
                    $Arguments = @{
                        PackageNameSuffix = "$NameSuffix-gc"
                        PackageSourcePath = $Source
                        PackageVersion = $Version
                        Force = $Force
                        R2RVerification = [R2RVerification]@{
                            R2RState = 'SdkOnly'
                            OperatingSystem = "Windows"
                            Architecture = "amd64"
                        }
                    }

                    if ($PSCmdlet.ShouldProcess("Create Zip Package")) {
                        New-ZipPackage @Arguments
                    }
                }
                elseif ($Environment.IsLinux) {
                    $Arguments = @{
                        PackageSourcePath = $Source
                        Name = $Name
                        PackageNameSuffix = 'gc'
                        Version = $Version
                        Force = $Force
                        R2RVerification = [R2RVerification]@{
                            R2RState = 'SdkOnly'
                        }
                    }

                    if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                        New-TarballPackage @Arguments
                    }
                }
            }
            { $_ -like "fxdependent*"} {
                if ($Environment.IsWindows) {
                    $Arguments = @{
                        PackageNameSuffix = $NameSuffix
                        PackageSourcePath = $Source
                        PackageVersion = $Version
                        Force = $Force
                        R2RVerification = [R2RVerification]@{
                            R2RState = 'NoR2R'
                        }
                    }

                    if ($PSCmdlet.ShouldProcess("Create Zip Package")) {
                        New-ZipPackage @Arguments
                    }
                } elseif ($Environment.IsLinux) {
                    $Arguments = @{
                        PackageSourcePath = $Source
                        Name = $Name
                        PackageNameSuffix = 'fxdependent'
                        Version = $Version
                        Force = $Force
                        R2RVerification = [R2RVerification]@{
                            R2RState = 'NoR2R'
                        }
                    }

                    if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                        New-TarballPackage @Arguments
                    }
                }
            }
            "tar-alpine-fxdependent" {
                if ($Environment.IsLinux) {
                    $Arguments = @{
                        PackageSourcePath = $Source
                        Name = $Name
                        PackageNameSuffix = 'musl-noopt-fxdependent'
                        Version = $Version
                        Force = $Force
                        R2RVerification = [R2RVerification]@{
                            R2RState = 'NoR2R'
                            OperatingSystem = "Linux"
                        }
                    }

                    if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                        New-TarballPackage @Arguments
                    }
                }
            }
            "msi" {
                $TargetArchitecture = "x64"
                $r2rArchitecture = "amd64"
                if ($Runtime -match "-x86") {
                    $TargetArchitecture = "x86"
                    $r2rArchitecture = "i386"
                }
                elseif ($Runtime -match "-arm64")
                {
                    $TargetArchitecture = "arm64"
                    $r2rArchitecture = "arm64"
                }

                Write-Verbose "TargetArchitecture = $TargetArchitecture" -Verbose

                $Arguments = @{
                    ProductNameSuffix = $NameSuffix
                    ProductSourcePath = $Source
                    ProductVersion = $Version
                    AssetsPath = "$RepoRoot\assets"
                    ProductTargetArchitecture = $TargetArchitecture
                    Force = $Force
            }

                if ($PSCmdlet.ShouldProcess("Create MSI Package")) {
                    New-MSIPackage @Arguments
                }
            }
            "msix" {
                $Arguments = @{
                    ProductNameSuffix = $NameSuffix
                    ProductSourcePath = $Source
                    ProductVersion = $Version
                    Architecture = $WindowsRuntime.Split('-')[1]
                    Force = $Force
                    Private = $Private
                    LTS = $LTS
                }

                if ($PSCmdlet.ShouldProcess("Create MSIX Package")) {
                    New-MSIXPackage @Arguments
                }
            }
            "tar" {
                $Arguments = @{
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                }

                if ($MacOSRuntime) {
                    $architecture = $MacOSRuntime.Split('-')[1]
                    $Arguments['Architecture'] = $architecture
                }

                if ($Script:Options.Runtime -match '(linux|osx).*') {
                    $os, $architecture = ($Script:Options.Runtime -split '-')
                    $peOS = ConvertTo-PEOperatingSystem -OperatingSystem $os
                    $peArch =  ConvertTo-PEArchitecture -Architecture $architecture

                    $Arguments['R2RVerification'] = [R2RVerification]@{
                        R2RState        = "R2R"
                        OperatingSystem = $peOS
                        Architecture    = $peArch
                    }
                }

                if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                    New-TarballPackage @Arguments
                }
            }
            "tar-arm" {
                $peArch = ConvertTo-PEArchitecture -Architecture 'arm'
                $Arguments = @{
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    Architecture = "arm32"
                    ExcludeSymbolicLinks = $true
                    R2RVerification = [R2RVerification]@{
                        R2RState = 'R2R'
                        OperatingSystem = "Linux"
                        Architecture = $peArch
                    }
                }

                if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                    New-TarballPackage @Arguments
                }
            }
            "tar-arm64" {
                $Arguments = @{
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    Architecture = "arm64"
                    ExcludeSymbolicLinks = $true
                    R2RVerification = [R2RVerification]@{
                        R2RState = 'R2R'
                        OperatingSystem = "Linux"
                        Architecture = "arm64"
                    }
                }

                if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                    New-TarballPackage @Arguments
                }
            }
            "tar-alpine" {
                $Arguments = @{
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    Architecture = "musl-x64"
                    ExcludeSymbolicLinks = $true
                    R2RVerification = [R2RVerification]@{
                        R2RState = 'R2R'
                        OperatingSystem = "Linux"
                        Architecture = "amd64"
                    }
                }

                if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                    New-TarballPackage @Arguments
                }
            }
            'deb' {
                $Arguments = @{
                    Type = 'deb'
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    NoSudo = $NoSudo
                    LTS = $LTS
                    HostArchitecture = "amd64"
                }
                foreach ($Distro in $Script:DebianDistributions) {
                    $Arguments["Distribution"] = $Distro
                    if ($PSCmdlet.ShouldProcess("Create DEB Package for $Distro")) {
                        New-UnixPackage @Arguments
                    }
                }
            }
            'rpm' {
                $Arguments = @{
                    Type = 'rpm'
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    NoSudo = $NoSudo
                    LTS = $LTS
                    HostArchitecture = "x86_64"
                }
                foreach ($Distro in $Script:RedhatFullDistributions) {
                    $Arguments["Distribution"] = $Distro
                    if ($PSCmdlet.ShouldProcess("Create RPM Package for $Distro")) {
                        Write-Verbose -Verbose "Creating RPM Package for $Distro"
                        New-UnixPackage @Arguments
                    }
                }
            }
            'rpm-fxdependent' {
                $Arguments = @{
                    Type = 'rpm'
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    NoSudo = $NoSudo
                    LTS = $LTS
                    HostArchitecture = "x86_64"
                }
                foreach ($Distro in $Script:RedhatFddDistributions) {
                    $Arguments["Distribution"] = $Distro
                    if ($PSCmdlet.ShouldProcess("Create RPM Package for $Distro")) {
                        Write-Verbose -Verbose "Creating RPM Package for $Distro"
                        New-UnixPackage @Arguments
                    }
                }
            }
            'rpm-fxdependent-arm64' {
                $Arguments = @{
                    Type = 'rpm'
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    NoSudo = $NoSudo
                    LTS = $LTS
                    HostArchitecture = "aarch64"
                }
                foreach ($Distro in $Script:RedhatFddDistributions) {
                    $Arguments["Distribution"] = $Distro
                    if ($PSCmdlet.ShouldProcess("Create RPM Package for $Distro")) {
                        Write-Verbose -Verbose "Creating RPM Package for $Distro"
                        New-UnixPackage @Arguments
                    }
                }
            }
            'osxpkg' {
                $HostArchitecture = "x86_64"
                if ($MacOSRuntime -match "-arm64") {
                    $HostArchitecture = "arm64"
                }
                Write-Verbose "HostArchitecture = $HostArchitecture" -Verbose

                $Arguments = @{
                    Type = 'osxpkg'
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    NoSudo = $NoSudo
                    LTS = $LTS
                    HostArchitecture = $HostArchitecture
                }


                if ($PSCmdlet.ShouldProcess("Create macOS Package")) {
                    New-UnixPackage @Arguments
                }
            }
            default {
                $Arguments = @{
                    Type = $_
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    NoSudo = $NoSudo
                    LTS = $LTS
                    HostArchitecture = "all"
                }

                if ($PSCmdlet.ShouldProcess("Create $_ Package")) {
                    New-UnixPackage @Arguments
                }
            }
        }

        if ($IncludeSymbols.IsPresent)
        {
            # Source is a temporary folder when -IncludeSymbols is present.  So, we should remove it.
            Remove-Item -Path $Source -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function New-TarballPackage {
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory)]
        [string] $PackageSourcePath,

        # Must start with 'powershell' but may have any suffix
        [Parameter(Mandatory)]
        [ValidatePattern("^powershell")]
        [string] $Name,

        # Suffix of the Name
        [string] $PackageNameSuffix,

        [Parameter(Mandatory)]
        [string] $Version,

        [Parameter()]
        [string] $Architecture = "x64",

        [switch] $Force,

        [switch] $ExcludeSymbolicLinks,

        [string] $CurrentLocation = (Get-Location),

        [R2RVerification] $R2RVerification
    )

    if ($PackageNameSuffix) {
        $packageName = "$Name-$Version-{0}-$Architecture-$PackageNameSuffix.tar.gz"
    } else {
        $packageName = "$Name-$Version-{0}-$Architecture.tar.gz"
    }

    if ($Environment.IsWindows) {
        throw "Must be on Linux or macOS to build 'tar.gz' packages!"
    } elseif ($Environment.IsLinux) {
        $packageName = $packageName -f "linux"
    } elseif ($Environment.IsMacOS) {
        $packageName = $packageName -f "osx"
    }

    $packagePath = Join-Path -Path $CurrentLocation -ChildPath $packageName
    Write-Verbose "Create package $packageName"
    Write-Verbose "Package destination path: $packagePath"

    if (Test-Path -Path $packagePath) {
        if ($Force -or $PSCmdlet.ShouldProcess("Overwrite existing package file")) {
            Write-Verbose "Overwrite existing package file at $packagePath" -Verbose
            Remove-Item -Path $packagePath -Force -ErrorAction Stop -Confirm:$false
        }
    }

    $Staging = "$PSScriptRoot/staging"
    New-StagingFolder -StagingPath $Staging -PackageSourcePath $PackageSourcePath -R2RVerification $R2RVerification

    if (Get-Command -Name tar -CommandType Application -ErrorAction Ignore) {
        if ($Force -or $PSCmdlet.ShouldProcess("Create tarball package")) {
            $options = "-czf"
            if ($PSBoundParameters.ContainsKey('Verbose') -and $PSBoundParameters['Verbose'].IsPresent) {
                # Use the verbose mode '-v' if '-Verbose' is specified
                $options = "-czvf"
            }

            try {
                Push-Location -Path $Staging
                tar $options $packagePath *
            } finally {
                Pop-Location
            }

            if (Test-Path -Path $packagePath) {
                Write-Log "You can find the tarball package at $packagePath"
                return (Get-Item $packagePath)
            } else {
                throw "Failed to create $packageName"
            }
        }
    } else {
        throw "Failed to create the package because the application 'tar' cannot be found"
    }
}

function New-TempFolder
{
    $tempPath = [System.IO.Path]::GetTempPath()

    $tempFolder = Join-Path -Path $tempPath -ChildPath ([System.IO.Path]::GetRandomFileName())
    if (!(Test-Path -Path $tempFolder))
    {
        $null = New-Item -Path $tempFolder -ItemType Directory
    }

    return $tempFolder
}

function New-PSSignedBuildZip
{
    param(
        [Parameter(Mandatory)]
        [string]$BuildPath,
        [Parameter(Mandatory)]
        [string]$SignedFilesPath,
        [Parameter(Mandatory)]
        [string]$DestinationFolder,
        [parameter(HelpMessage='VSTS variable to set for path to zip')]
        [string]$VstsVariableName
    )

    Update-PSSignedBuildFolder -BuildPath $BuildPath -SignedFilesPath $SignedFilesPath

    # Remove '$signedFilesPath' now that signed binaries are copied
    if (Test-Path $signedFilesPath)
    {
        Remove-Item -Recurse -Force -Path $signedFilesPath
    }

    New-PSBuildZip -BuildPath $BuildPath -DestinationFolder $DestinationFolder -VstsVariableName $VstsVariableName
}

function New-PSBuildZip
{
    param(
        [Parameter(Mandatory)]
        [string]$BuildPath,
        [Parameter(Mandatory)]
        [string]$DestinationFolder,
        [parameter(HelpMessage='VSTS variable to set for path to zip')]
        [string]$VstsVariableName
    )

    $name = Split-Path -Path $BuildPath -Leaf
    $zipLocationPath = Join-Path -Path $DestinationFolder -ChildPath "$name-signed.zip"
    Compress-Archive -Path $BuildPath\* -DestinationPath $zipLocationPath
    if ($VstsVariableName)
    {
        # set VSTS variable with path to package files
        Write-Log "Setting $VstsVariableName to $zipLocationPath"
        Write-Host "##vso[task.setvariable variable=$VstsVariableName]$zipLocationPath"
    }
    else
    {
        return $zipLocationPath
    }
}


function Update-PSSignedBuildFolder
{
    param(
        [Parameter(Mandatory)]
        [string]$BuildPath,
        [Parameter(Mandatory)]
        [string]$SignedFilesPath,
        [string[]] $RemoveFilter = ('*.pdb', '*.zip', '*.r2rmap'),
        [bool]$OfficialBuild = $true
    )

    $BuildPathNormalized = (Get-Item $BuildPath).FullName
    $SignedFilesPathNormalized = (Get-Item $SignedFilesPath).FullName

    Write-Verbose -Verbose "BuildPath = $BuildPathNormalized"
    Write-Verbose -Verbose "SignedFilesPath = $signedFilesPath"

    # Replace unsigned binaries with signed
    $signedFilesFilter = Join-Path -Path $SignedFilesPathNormalized -ChildPath '*'
    Write-Verbose -Verbose "signedFilesFilter = $signedFilesFilter"

    $signedFilesList = Get-ChildItem -Path $signedFilesFilter -Recurse -File
    foreach ($signedFileObject in $signedFilesList) {
        # completely skip replacing pwsh on non-windows systems (there is no .exe extension here)
        # and it may not be signed correctly

        # The Shim will not be signed in CI.

        if ($signedFileObject.Name -eq "pwsh" -or ($signedFileObject.Name -eq "Microsoft.PowerShell.GlobalTool.Shim.exe" -and $env:BUILD_REASON -eq 'PullRequest')) {
            Write-Verbose -Verbose "Skipping $signedFileObject"
            continue
        }

        $signedFilePath = $signedFileObject.FullName
        Write-Verbose -Verbose "Processing $signedFilePath"

        # Agents seems to be on a case sensitive file system
        if ($IsLinux) {
            $relativePath = $signedFilePath.Replace($SignedFilesPathNormalized, '')
        } else {
            $relativePath = $signedFilePath.ToLowerInvariant().Replace($SignedFilesPathNormalized.ToLowerInvariant(), '')
        }

        Write-Verbose -Verbose "relativePath = $relativePath"
        $destination = (Get-Item (Join-Path -Path $BuildPathNormalized -ChildPath $relativePath)).FullName
        Write-Verbose -Verbose "destination = $destination"
        Write-Log "replacing $destination with $signedFilePath"

        if (-not (Test-Path $destination)) {
            $parent = Split-Path -Path $destination -Parent
            $exists = Test-Path -Path $parent

            if ($exists) {
                Write-Verbose -Verbose "Parent:"
                Get-ChildItem -Path $parent | Select-Object -ExpandProperty FullName | Write-Verbose -Verbose
            }

            Write-Error "File not found: $destination, parent - $parent exists: $exists"
        }

        # Get-AuthenticodeSignature will only work on Windows
        if ($IsWindows)
        {
            $signature = Get-AuthenticodeSignature -FilePath $signedFilePath

            if ($signature.Status -ne 'Valid' -and $OfficialBuild) {
                Write-Host "Certificate Issuer: $($signature.SignerCertificate.Issuer)"
                Write-Host "Certificate Subject: $($signature.SignerCertificate.Subject)"
                Write-Error "Invalid signature for $signedFilePath"
            } elseif ($OfficialBuild -eq $false) {
                if ($signature.Status -eq 'NotSigned') {
                    Write-Warning "File is not signed: $signedFilePath"
                } elseif ($signature.SignerCertificate.Issuer -notmatch '^CN=(Microsoft|TestAzureEngBuildCodeSign|Windows Internal Build Tools).*') {
                    Write-Warning "File signed with test certificate: $signedFilePath"
                    Write-Host "Certificate Issuer: $($signature.SignerCertificate.Issuer)"
                    Write-Host "Certificate Subject: $($signature.SignerCertificate.Subject)"
                } else {
                    Write-Verbose -Verbose "File properly signed: $signedFilePath"
                }
            }
        }
        else
        {
            Write-Verbose -Verbose "Skipping certificate check of $signedFilePath on non-Windows"
        }

        Copy-Item -Path $signedFilePath -Destination $destination -Force

    }

    foreach($filter in $RemoveFilter) {
        $removePath = Join-Path -Path $BuildPathNormalized -ChildPath $filter
        Remove-Item -Path $removePath -Recurse -Force
    }
}


function Expand-PSSignedBuild
{
    param(
        [Parameter(Mandatory)]
        [string]$BuildZip,

        [Switch]$SkipPwshExeCheck
    )

    $psModulePath = Split-Path -Path $PSScriptRoot
    # Expand signed build
    $buildPath = Join-Path -Path $psModulePath -ChildPath 'ExpandedBuild'
    $null = New-Item -Path $buildPath -ItemType Directory -Force
    Expand-Archive -Path $BuildZip -DestinationPath $buildPath -Force
    # Remove the zip file that contains only those files from the parent folder of 'publish'.
    # That zip file is used for compliance scan.
    Remove-Item -Path (Join-Path -Path $buildPath -ChildPath '*.zip') -Recurse

    if ($SkipPwshExeCheck) {
        $executablePath = (Join-Path $buildPath -ChildPath 'pwsh.dll')
    } else {
        if ($IsMacOS -or $IsLinux) {
            $executablePath = (Join-Path $buildPath -ChildPath 'pwsh')
        } else {
            $executablePath = (Join-Path $buildPath -ChildPath 'pwsh.exe')
        }
    }

    Restore-PSModuleToBuild -PublishPath $buildPath

    $psOptionsPath = Join-Path $buildPath -ChildPath 'psoptions.json'
    Restore-PSOptions -PSOptionsPath $psOptionsPath

    $options = Get-PSOptions

    $options.PSModuleRestore = $true

    if (Test-Path -Path $executablePath) {
        $options.Output = $executablePath
    } else {
        throw 'Could not find pwsh'
    }

    Set-PSOptions -Options $options
}

function New-UnixPackage {
    [CmdletBinding(SupportsShouldProcess=$true)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet("deb", "osxpkg", "rpm")]
        [string]$Type,

        [Parameter(Mandatory)]
        [string]$PackageSourcePath,

        # Must start with 'powershell' but may have any suffix
        [Parameter(Mandatory)]
        [ValidatePattern("^powershell")]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        # Package iteration version (rarely changed)
        # This is a string because strings are appended to it
        [string]$Iteration = "1",

        # Host architecture values allowed for deb type packages: amd64
        # Host architecture values allowed for rpm type packages include: x86_64, aarch64, native, all, noarch, any
        # Host architecture values allowed for osxpkg type packages include: x86_64, arm64
        [string]
        [ValidateSet("x86_64", "amd64", "aarch64", "arm64", "native", "all", "noarch", "any")]
        $HostArchitecture,

        [Switch]
        $Force,

        [switch]
        $NoSudo,

        [switch]
        $LTS,

        [string]
        $CurrentLocation = (Get-Location)
    )

    DynamicParam {
        if ($Type -eq "deb" -or $Type -like 'rpm*') {
            # Add a dynamic parameter '-Distribution' when the specified package type is 'deb'.
            # The '-Distribution' parameter can be used to indicate which Debian distro this package is targeting.
            $ParameterAttr = New-Object "System.Management.Automation.ParameterAttribute"
            if($type -eq 'deb')
            {
                $ValidateSetAttr = New-Object "System.Management.Automation.ValidateSetAttribute" -ArgumentList $Script:DebianDistributions
            }
            else
            {
                $ValidateSetAttr = New-Object "System.Management.Automation.ValidateSetAttribute" -ArgumentList $Script:RedHatDistributions
            }
            $Attributes = New-Object "System.Collections.ObjectModel.Collection``1[System.Attribute]"
            $Attributes.Add($ParameterAttr) > $null
            $Attributes.Add($ValidateSetAttr) > $null
            $Dict = New-Object "System.Management.Automation.RuntimeDefinedParameterDictionary"
            $Parameter = New-Object "System.Management.Automation.RuntimeDefinedParameter" -ArgumentList ("Distribution", [string], $Attributes)

            $Dict.Add("Distribution", $Parameter) > $null
            return $Dict
        }
    }

    End {
        # This allows sudo install to be optional; needed when running in containers / as root
        # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
        $sudo = if (!$NoSudo) { "sudo" }

        # Validate platform
        $ErrorMessage = "Must be on {0} to build '$Type' packages!"
        switch ($Type) {
            "deb" {
                $packageVersion = Get-LinuxPackageSemanticVersion -Version $Version
                if (!$Environment.IsUbuntu -and !$Environment.IsDebian -and !$Environment.IsMariner) {
                    throw ($ErrorMessage -f "Ubuntu or Debian")
                }

                if ($PSBoundParameters.ContainsKey('Distribution')) {
                    $DebDistro = $PSBoundParameters['Distribution']
                } elseif ($Environment.IsUbuntu16) {
                    $DebDistro = "ubuntu.16.04"
                } elseif ($Environment.IsUbuntu18) {
                    $DebDistro = "ubuntu.18.04"
                } elseif ($Environment.IsUbuntu20) {
                    $DebDistro = "ubuntu.20.04"
                } elseif ($Environment.IsDebian9) {
                    $DebDistro = "debian.9"
                } else {
                    throw "The current Debian distribution is not supported."
                }

                # iteration is "debian_revision"
                # usage of this to differentiate distributions is allowed by non-standard
                $Iteration += ".$DebDistro"
            }
            "rpm" {
                if ($PSBoundParameters.ContainsKey('Distribution')) {
                    $DebDistro = $PSBoundParameters['Distribution']

                } elseif ($Environment.IsRedHatFamily) {
                    $DebDistro = "rhel.7"
                } else {
                    throw "The current distribution is not supported."
                }

                $packageVersion = Get-LinuxPackageSemanticVersion -Version $Version
            }
            "osxpkg" {
                $packageVersion = $Version
                if (!$Environment.IsMacOS) {
                    throw ($ErrorMessage -f "macOS")
                }

                $DebDistro = 'macOS'
            }
        }

        # Determine if the version is a preview version
        # Only LTS packages get a prefix in the name
        # Preview versions are identified by the version string itself (e.g., 7.6.0-preview.6)
        # Rebuild versions are also identified by the version string (e.g., 7.4.13-rebuild.5)
        $Name = if($LTS) {
            "powershell-lts"
        }
        else {
            "powershell"
        }

        # Verify dependencies are installed and in the path
        Test-Dependencies

        $Description = $packagingStrings.Description

        # Break the version down into its components, we are interested in the major version
        $VersionMatch = [regex]::Match($Version, '(\d+)(?:.(\d+)(?:.(\d+)(?:-preview(?:.(\d+))?)?)?)?')
        $MajorVersion = $VersionMatch.Groups[1].Value

        # Suffix is used for side-by-side preview/release package installation
        $Suffix = if ($IsPreview) { $MajorVersion + "-preview" } elseif ($LTS) { $MajorVersion + "-lts" } else { $MajorVersion }

        # Setup staging directory so we don't change the original source directory
        $Staging = "$PSScriptRoot/staging"
        if ($PSCmdlet.ShouldProcess("Create staging folder")) {
            New-StagingFolder -StagingPath $Staging -PackageSourcePath $PackageSourcePath
        }

        # Follow the Filesystem Hierarchy Standard for Linux and macOS
        $Destination = if ($Environment.IsLinux) {
            "/opt/microsoft/powershell/$Suffix"
        } elseif ($Environment.IsMacOS) {
            "/usr/local/microsoft/powershell/$Suffix"
        }

        # Destination for symlink to powershell executable
        $Link = Get-PwshExecutablePath -IsPreview:$IsPreview

        $links = @(New-LinkInfo -LinkDestination $Link -LinkTarget "$Destination/pwsh")

        if($LTS) {
            $links += New-LinkInfo -LinkDestination (Get-PwshExecutablePath -IsLTS:$LTS) -LinkTarget "$Destination/pwsh"
        }

        if ($PSCmdlet.ShouldProcess("Create package file system"))
        {
            # Generate After Install and After Remove scripts
            $AfterScriptInfo = New-AfterScripts -Link $Link -Distribution $DebDistro -Destination $Destination

            # Generate gzip of man file
            $ManGzipInfo = New-ManGzip -IsPreview:$IsPreview -IsLTS:$LTS

            # Change permissions for packaging
            Write-Log "Setting permissions..."
            Start-NativeExecution {
                find $Staging -type d | xargs chmod 755
                find $Staging -type f | xargs chmod 644
                chmod 644 $ManGzipInfo.GzipFile
                # refers to executable, does not vary by channel
                chmod 755 "$Staging/pwsh" #only the executable file should be granted the execution permission
            }
        }

        # Add macOS powershell launcher
        if ($Type -eq "osxpkg")
        {
            Write-Log "Adding macOS launch application..."
            if ($PSCmdlet.ShouldProcess("Add macOS launch application"))
            {
                # Generate launcher app folder
                $AppsFolder = New-MacOSLauncher -Version $Version
            }
        }

        $packageDependenciesParams = @{}
        if ($DebDistro)
        {
            $packageDependenciesParams['Distribution']=$DebDistro
        }

        # Setup package dependencies
        $Dependencies = @(Get-PackageDependencies @packageDependenciesParams)

        # Build package
        try {
            if ($Type -eq 'rpm') {
                # Use rpmbuild directly for RPM packages
                if ($PSCmdlet.ShouldProcess("Create RPM package with rpmbuild")) {
                    Write-Log "Creating RPM package with rpmbuild..."

                    # Create rpmbuild directory structure
                    $rpmBuildRoot = Join-Path $env:HOME "rpmbuild"
                    $specsDir = Join-Path $rpmBuildRoot "SPECS"
                    $rpmsDir = Join-Path $rpmBuildRoot "RPMS"

                    New-Item -ItemType Directory -Path $specsDir -Force | Out-Null
                    New-Item -ItemType Directory -Path $rpmsDir -Force | Out-Null

                    # Generate RPM spec file
                    $specContent = New-RpmSpec `
                        -Name $Name `
                        -Version $packageVersion `
                        -Iteration $Iteration `
                        -Description $Description `
                        -Dependencies $Dependencies `
                        -AfterInstallScript $AfterScriptInfo.AfterInstallScript `
                        -AfterRemoveScript $AfterScriptInfo.AfterRemoveScript `
                        -Staging $Staging `
                        -Destination $Destination `
                        -ManGzipFile $ManGzipInfo.GzipFile `
                        -ManDestination $ManGzipInfo.ManFile `
                        -LinkInfo $Links `
                        -Distribution $DebDistro `
                        -HostArchitecture $HostArchitecture

                    $specFile = Join-Path $specsDir "$Name.spec"
                    $specContent | Out-File -FilePath $specFile -Encoding ascii
                    Write-Verbose "Generated spec file: $specFile" -Verbose

                    # Log the spec file content
                    if ($env:GITHUB_ACTIONS -eq 'true') {
                        Write-Host "::group::RPM Spec File Content"
                        Write-Host $specContent
                        Write-Host "::endgroup::"
                    } else {
                        Write-Verbose "RPM Spec File Content:`n$specContent" -Verbose
                    }

                    # Build RPM package
                    try {
                        # Use bash to properly handle rpmbuild arguments
                        # Add --target for cross-architecture builds
                        $targetArch = ""
                        if ($HostArchitecture -ne "x86_64" -and $HostArchitecture -ne "noarch") {
                            $targetArch = "--target $HostArchitecture"
                        }
                        $buildCmd = "rpmbuild -bb --quiet $targetArch --define '_topdir $rpmBuildRoot' --buildroot '$rpmBuildRoot/BUILDROOT' '$specFile'"
                        Write-Verbose "Running: $buildCmd" -Verbose
                        $Output = bash -c $buildCmd 2>&1
                        $exitCode = $LASTEXITCODE

                        if ($exitCode -ne 0) {
                            throw "rpmbuild failed with exit code $exitCode"
                        }

                        # Find the generated RPM
                        $rpmFile = Get-ChildItem -Path (Join-Path $rpmsDir $HostArchitecture) -Filter "*.rpm" -ErrorAction Stop |
                            Sort-Object -Property LastWriteTime -Descending |
                            Select-Object -First 1

                        if ($rpmFile) {
                            # Copy RPM to current location
                            Copy-Item -Path $rpmFile.FullName -Destination $CurrentLocation -Force
                            $Output = @("Created package {:path=>""$($rpmFile.Name)""}")
                        } else {
                            throw "RPM file not found after build"
                        }
                    }
                    catch {
                        Write-Verbose -Message "!!!Handling error in rpmbuild!!!" -Verbose -ErrorAction SilentlyContinue
                        if ($Output) {
                            Write-Verbose -Message "$Output" -Verbose -ErrorAction SilentlyContinue
                        }
                        Get-Error -InputObject $_
                        throw
                    }
                }
            } elseif ($Type -eq 'deb') {
                # Use native DEB package builder
                if ($PSCmdlet.ShouldProcess("Create DEB package natively")) {
                    Write-Log "Creating DEB package natively..."
                    try {
                        $result = New-NativeDeb `
                            -Name $Name `
                            -Version $packageVersion `
                            -Iteration $Iteration `
                            -Description $Description `
                            -Staging $Staging `
                            -Destination $Destination `
                            -ManGzipFile $ManGzipInfo.GzipFile `
                            -ManDestination $ManGzipInfo.ManFile `
                            -LinkInfo $Links `
                            -Dependencies $Dependencies `
                            -AfterInstallScript $AfterScriptInfo.AfterInstallScript `
                            -AfterRemoveScript $AfterScriptInfo.AfterRemoveScript `
                            -HostArchitecture $HostArchitecture `
                            -CurrentLocation $CurrentLocation

                        $Output = @("Created package {:path=>""$($result.PackageName)""}")
                    }
                    catch {
                        Write-Verbose -Message "!!!Handling error in native DEB creation!!!" -Verbose -ErrorAction SilentlyContinue
                    }
                }
            } elseif ($Type -eq 'osxpkg') {
                # Use native macOS packaging tools
                if ($PSCmdlet.ShouldProcess("Create macOS package with pkgbuild/productbuild")) {
                    Write-Log "Creating macOS package with native tools..."

                    $macPkgArgs = @{
                        Name = $Name
                        Version = $packageVersion
                        Iteration = $Iteration
                        Staging = $Staging
                        Destination = $Destination
                        ManGzipFile = $ManGzipInfo.GzipFile
                        ManDestination = $ManGzipInfo.ManFile
                        LinkInfo = $Links
                        AfterInstallScript = $AfterScriptInfo.AfterInstallScript
                        AppsFolder = $AppsFolder
                        HostArchitecture = $HostArchitecture
                        CurrentLocation = $CurrentLocation
                        LTS = $LTS
                    }

                    try {
                        $packageFile = New-MacOSPackage @macPkgArgs
                        $Output = @("Created package {:path=>""$($packageFile.Name)""}")
                    }
                    catch {
                        Write-Verbose -Message "!!!Handling error in macOS packaging!!!" -Verbose -ErrorAction SilentlyContinue
                        Get-Error -InputObject $_
                        throw
                    }
                }
            } else {
                # Nothing should reach here
                throw "Unknown package type: $Type"
            }
        } finally {
            if ($Environment.IsMacOS) {
                Write-Log "Starting Cleanup for mac packaging..."
                if ($PSCmdlet.ShouldProcess("Cleanup macOS launcher"))
                {
                    Clear-MacOSLauncher
                }
            }

            # Clean up rpmbuild directory if it was created
            if ($Type -eq 'rpm') {
                $rpmBuildRoot = Join-Path $env:HOME "rpmbuild"
                if (Test-Path $rpmBuildRoot) {
                    Write-Verbose "Cleaning up rpmbuild directory: $rpmBuildRoot" -Verbose
                    Remove-Item -Path $rpmBuildRoot -Recurse -Force -ErrorAction SilentlyContinue
                }
            }

            if ($AfterScriptInfo.AfterInstallScript) {
                Remove-Item -ErrorAction 'silentlycontinue' $AfterScriptInfo.AfterInstallScript -Force
            }
            if ($AfterScriptInfo.AfterRemoveScript) {
                Remove-Item -ErrorAction 'silentlycontinue' $AfterScriptInfo.AfterRemoveScript -Force
            }
            Remove-Item -Path $ManGzipInfo.GzipFile -Force -ErrorAction SilentlyContinue
        }

        # Magic to get path output
        $createdPackage = Get-Item (Join-Path $CurrentLocation (($Output[-1] -split ":path=>")[-1] -replace '["{}]'))

        # For macOS with native tools, the package is already in the correct format
        # For other platforms, the package name from dpkg-deb/rpmbuild is sufficient

        if (Test-Path $createdPackage)
        {
            Write-Verbose "Created package: $createdPackage" -Verbose
            return $createdPackage
        }
        else
        {
            throw "Failed to create $createdPackage"
        }
    }
}

Function New-LinkInfo
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param(
        [Parameter(Mandatory)]
        [string]
        $LinkDestination,
        [Parameter(Mandatory)]
        [string]
        $linkTarget
    )

    $linkDir = Join-Path -Path '/tmp' -ChildPath ([System.IO.Path]::GetRandomFileName())
    $null = New-Item -ItemType Directory -Path $linkDir
    $linkSource = Join-Path -Path $linkDir -ChildPath 'pwsh'

    Write-Log "Creating link to target '$LinkTarget', with a temp source of '$LinkSource' and a Package Destination of '$LinkDestination'"
    if ($PSCmdlet.ShouldProcess("Create package symbolic from $linkDestination to $linkTarget"))
    {
        # refers to executable, does not vary by channel
        New-Item -Force -ItemType SymbolicLink -Path $linkSource -Target $LinkTarget > $null
    }

    [LinkInfo] @{
        Source = $linkSource
        Destination = $LinkDestination
    }
}

function New-MacOsDistributionPackage
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param(
        [Parameter(Mandatory,HelpMessage='The FileInfo of the component package')]
        [System.IO.FileInfo]$ComponentPackage,

        [Parameter(Mandatory,HelpMessage='Package name for the output file')]
        [string]$PackageName,

        [Parameter(Mandatory,HelpMessage='Package version')]
        [string]$Version,

        [Parameter(Mandatory,HelpMessage='Output directory for the final package')]
        [string]$OutputDirectory,

        [Parameter(HelpMessage='x86_64 for Intel or arm64 for Apple Silicon')]
        [ValidateSet("x86_64", "arm64")]
        [string] $HostArchitecture = "x86_64",

        [Parameter(HelpMessage='Package identifier')]
        [string]$PackageIdentifier,

        [Switch] $IsPreview
    )

    if (!$Environment.IsMacOS)
    {
        throw 'New-MacOsDistributionPackage is only supported on macOS!'
    }

    # Create a temp directory to store the needed files
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir -Force > $null

    $resourcesDir = Join-Path -Path $tempDir -ChildPath 'resources'
    New-Item -ItemType Directory -Path $resourcesDir -Force > $null

    # Copy background file to temp directory
    $backgroundFile = "$RepoRoot/assets/macDialog.png"
    if (Test-Path $backgroundFile) {
        Copy-Item -Path $backgroundFile -Destination $resourcesDir -Force
    }

    # Copy the component package to temp directory
    $componentFileName = Split-Path -Leaf -Path $ComponentPackage
    $tempComponentPath = Join-Path -Path $tempDir -ChildPath $componentFileName
    Copy-Item -Path $ComponentPackage -Destination $tempComponentPath -Force

    # Create the distribution xml
    $distributionXmlPath = Join-Path -Path $tempDir -ChildPath 'powershellDistribution.xml'

    # Get package ID if not provided
    if (-not $PackageIdentifier) {
        if ($IsPreview.IsPresent) {
            $PackageIdentifier = 'com.microsoft.powershell-preview'
        }
        else {
            $PackageIdentifier = 'com.microsoft.powershell'
        }
    }

    # Minimum OS version
    $minOSVersion = "11.0"  # macOS Big Sur minimum

    # format distribution template with:
    # 0 - title
    # 1 - version
    # 2 - package path (component package filename)
    # 3 - minimum os version
    # 4 - Package Identifier
    # 5 - host architecture (x86_64 for Intel or arm64 for Apple Silicon)
    $PackagingStrings.OsxDistributionTemplate -f $PackageName, $Version, $componentFileName, $minOSVersion, $PackageIdentifier, $HostArchitecture | Out-File -Encoding utf8 -FilePath $distributionXmlPath -Force

    # Build final package path
    # Rename x86_64 to x64 for compatibility
    $packageArchName = if ($HostArchitecture -eq "x86_64") { "x64" } else { $HostArchitecture }
    $finalPackagePath = Join-Path $OutputDirectory "$PackageName-$Version-osx-$packageArchName.pkg"

    # Remove existing package if it exists
    if (Test-Path $finalPackagePath) {
        Write-Warning "Removing existing package: $finalPackagePath"
        Remove-Item $finalPackagePath -Force
    }

    if ($PSCmdlet.ShouldProcess("Build product package with productbuild")) {
        Write-Log "Applying distribution.xml to package..."
        Push-Location $tempDir
        try
        {
            # productbuild is an xcode command line tool
            Start-NativeExecution -VerboseOutputOnError {
                productbuild --distribution $distributionXmlPath `
                    --package-path $tempDir `
                    --resources $resourcesDir `
                    $finalPackagePath
            }

            if (Test-Path $finalPackagePath) {
                Write-Log "Successfully created macOS package: $finalPackagePath"
            }
            else {
                throw "Package was not created at expected location: $finalPackagePath"
            }
        }
        finally
        {
            Pop-Location
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    return (Get-Item $finalPackagePath)
}

Class LinkInfo
{
    [string] $Source
    [string] $Destination
}

function New-RpmSpec
{
    param(
        [Parameter(Mandatory,HelpMessage='Package Name')]
        [String]$Name,

        [Parameter(Mandatory,HelpMessage='Package Version')]
        [String]$Version,

        [Parameter(Mandatory)]
        [String]$Iteration,

        [Parameter(Mandatory,HelpMessage='Package description')]
        [String]$Description,

        [Parameter(Mandatory,HelpMessage='Staging folder for installation files')]
        [String]$Staging,

        [Parameter(Mandatory,HelpMessage='Install path on target machine')]
        [String]$Destination,

        [Parameter(Mandatory,HelpMessage='The built and gzipped man file.')]
        [String]$ManGzipFile,

        [Parameter(Mandatory,HelpMessage='The destination of the man file')]
        [String]$ManDestination,

        [Parameter(Mandatory,HelpMessage='Symlink to powershell executable')]
        [LinkInfo[]]$LinkInfo,

        [Parameter(Mandatory,HelpMessage='Packages required to install this package')]
        [String[]]$Dependencies,

        [Parameter(Mandatory,HelpMessage='Script to run after the package installation.')]
        [String]$AfterInstallScript,

        [Parameter(Mandatory,HelpMessage='Script to run after the package removal.')]
        [String]$AfterRemoveScript,

        [String]$Distribution = 'rhel.7',
        [string]$HostArchitecture
    )

    # RPM doesn't allow hyphens in version, so convert them to underscores
    # e.g., "7.6.0-preview.6" becomes Version: 7.6.0_preview.6
    $rpmVersion = $Version -replace '-', '_'

    # Build Release field with distribution suffix (e.g., "1.cm" or "1.rh")
    # Don't use RPM macros - build the full release string in PowerShell
    $rpmRelease = "$Iteration.$Distribution"

    $specContent = @"
# RPM spec file for PowerShell
# Generated by PowerShell build system

Name:           $Name
Version:        $rpmVersion
Release:        $rpmRelease
Summary:        PowerShell - Cross-platform automation and configuration tool/framework
License:        MIT
URL:            https://microsoft.com/powershell
AutoReq:        no

"@

    # Only add BuildArch if not doing cross-architecture build
    # For cross-arch builds, we'll rely on --target option
    if ($HostArchitecture -eq "x86_64" -or $HostArchitecture -eq "noarch") {
        $specContent += "BuildArch:      $HostArchitecture`n`n"
    } else {
        # For cross-architecture builds, don't specify BuildArch in spec
        # The --target option will handle the architecture

        # Disable automatic binary stripping for cross-arch builds
        # The native /bin/strip on x86_64 cannot process ARM64 binaries and would fail with:
        # "Unable to recognise the format of the input file"
        # See: https://rpm-software-management.github.io/rpm/manual/macros.html
        # __strip: This macro controls the command used for stripping binaries during the build process.
        # /bin/true: A command that does nothing and always exits successfully, effectively bypassing the stripping process.
        $specContent += "%define __strip /bin/true`n"

        # Disable debug package generation to prevent strip-related errors
        # Debug packages require binary stripping which fails for cross-arch builds
        # See: https://rpm-packaging-guide.github.io/#debugging
        # See: https://docs.fedoraproject.org/en-US/packaging-guidelines/Debuginfo/#_useless_or_incomplete_debuginfo_packages_due_to_other_reasons
        $specContent += "%global debug_package %{nil}`n`n"
    }

    # Add dependencies
    foreach ($dep in $Dependencies) {
        $specContent += "Requires:       $dep`n"
    }

    $specContent += @"

%description
$Description

%prep
# No prep needed - files are already staged

%build
# No build needed - binaries are pre-built

%install
rm -rf `$RPM_BUILD_ROOT
mkdir -p `$RPM_BUILD_ROOT$Destination
mkdir -p `$RPM_BUILD_ROOT$(Split-Path -Parent $ManDestination)

# Copy all files from staging to destination
cp -r $Staging/* `$RPM_BUILD_ROOT$Destination/

# Copy man page
cp $ManGzipFile `$RPM_BUILD_ROOT$ManDestination

"@

    # Add symlinks - we need to get the target of the temp symlink
    foreach ($link in $LinkInfo) {
        $linkDir = Split-Path -Parent $link.Destination
        $specContent += "mkdir -p `$RPM_BUILD_ROOT$linkDir`n"
        # For RPM, we copy the symlink itself.
        # The symlink at $link.Source points to the actual target, so we'll copy it.
        # The -P flag preserves symlinks rather than copying their targets, which is critical for this operation.
        $specContent += "cp -P $($link.Source) `$RPM_BUILD_ROOT$($link.Destination)`n"
    }

    # Post-install script
    $postInstallContent = Get-Content -Path $AfterInstallScript -Raw
    $specContent += "`n%post`n"
    $specContent += $postInstallContent
    $specContent += "`n"

    # Post-uninstall script
    $postUninstallContent = Get-Content -Path $AfterRemoveScript -Raw
    $specContent += "%postun`n"
    $specContent += $postUninstallContent
    $specContent += "`n"

    # Files section
    $specContent += "%files`n"
    $specContent += "%defattr(-,root,root,-)`n"
    $specContent += "$Destination/*`n"
    $specContent += "$ManDestination`n"

    # Add symlinks to files
    foreach ($link in $LinkInfo) {
        $specContent += "$($link.Destination)`n"
    }

    # Changelog with correct date format for RPM
    $changelogDate = Get-Date -Format "ddd MMM dd yyyy"
    $specContent += "`n%changelog`n"
    $specContent += "* $changelogDate PowerShell Team <PowerShellTeam@hotmail.com> - $rpmVersion-$rpmRelease`n"
    $specContent += "- Automated build`n"

    return $specContent
}

function New-NativeDeb
{
    param(
        [Parameter(Mandatory, HelpMessage='Package Name')]
        [String]$Name,

        [Parameter(Mandatory, HelpMessage='Package Version')]
        [String]$Version,

        [Parameter(Mandatory)]
        [String]$Iteration,

        [Parameter(Mandatory, HelpMessage='Package description')]
        [String]$Description,

        [Parameter(Mandatory, HelpMessage='Staging folder for installation files')]
        [String]$Staging,

        [Parameter(Mandatory, HelpMessage='Install path on target machine')]
        [String]$Destination,

        [Parameter(Mandatory, HelpMessage='The built and gzipped man file.')]
        [String]$ManGzipFile,

        [Parameter(Mandatory, HelpMessage='The destination of the man file')]
        [String]$ManDestination,

        [Parameter(Mandatory, HelpMessage='Symlink to powershell executable')]
        [LinkInfo[]]$LinkInfo,

        [Parameter(HelpMessage='Packages required to install this package.')]
        [String[]]$Dependencies,

        [Parameter(HelpMessage='Script to run after the package installation.')]
        [String]$AfterInstallScript,

        [Parameter(HelpMessage='Script to run after the package removal.')]
        [String]$AfterRemoveScript,

        [string]$HostArchitecture,

        [string]$CurrentLocation
    )

    Write-Log "Creating native DEB package..."

    # Create temporary build directory
    $debBuildRoot = Join-Path $env:HOME "debbuild-$(Get-Random)"
    $debianDir = Join-Path $debBuildRoot "DEBIAN"
    $dataDir = Join-Path $debBuildRoot "data"

    try {
        New-Item -ItemType Directory -Path $debianDir -Force | Out-Null
        New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

        # Calculate installed size (in KB)
        $installedSize = 0
        Get-ChildItem -Path $Staging -Recurse -File | ForEach-Object { $installedSize += $_.Length }
        $installedSize += (Get-Item $ManGzipFile).Length
        $installedSizeKB = [Math]::Ceiling($installedSize / 1024)

        # Create control file with all fields in proper order
        # Description must be single line (first line) followed by extended description with leading space
        $descriptionLines = $Description -split "`n"
        $shortDescription = $descriptionLines[0]
        $extendedDescription = if ($descriptionLines.Count -gt 1) {
            ($descriptionLines[1..($descriptionLines.Count-1)] | ForEach-Object { " $_" }) -join "`n"
        }

        $controlContent = @"
Package: $Name
Version: $Version-$Iteration
Architecture: $HostArchitecture
Maintainer: PowerShell Team <PowerShellTeam@hotmail.com>
Installed-Size: $installedSizeKB
Priority: optional
Section: shells
Homepage: https://microsoft.com/powershell
Depends: $(if ($Dependencies) { $Dependencies -join ', ' })
Description: $shortDescription
$(if ($extendedDescription) { $extendedDescription + "`n" })
"@

        $controlFile = Join-Path $debianDir "control"
        $controlContent | Out-File -FilePath $controlFile -Encoding ascii -NoNewline

        Write-Verbose "Control file created: $controlFile" -Verbose
        Write-LogGroup -Title "DEB Control File Content" -Message $controlContent

        # Copy postinst script if provided
        if ($AfterInstallScript -and (Test-Path $AfterInstallScript)) {
            $postinstFile = Join-Path $debianDir "postinst"
            Copy-Item -Path $AfterInstallScript -Destination $postinstFile -Force
            Start-NativeExecution { chmod 755 $postinstFile }
            Write-Verbose "Postinst script copied to: $postinstFile" -Verbose
        }

        # Copy postrm script if provided
        if ($AfterRemoveScript -and (Test-Path $AfterRemoveScript)) {
            $postrmFile = Join-Path $debianDir "postrm"
            Copy-Item -Path $AfterRemoveScript -Destination $postrmFile -Force
            Start-NativeExecution { chmod 755 $postrmFile }
            Write-Verbose "Postrm script copied to: $postrmFile" -Verbose
        }

        # Copy staging files to data directory
        $targetPath = Join-Path $dataDir $Destination.TrimStart('/')
        New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
        Copy-Item -Path "$Staging/*" -Destination $targetPath -Recurse -Force
        Write-Verbose "Copied staging files to: $targetPath" -Verbose

        # Copy man page
        $manDestPath = Join-Path $dataDir $ManDestination.TrimStart('/')
        $manDestDir = Split-Path $manDestPath -Parent
        New-Item -ItemType Directory -Path $manDestDir -Force | Out-Null
        Copy-Item -Path $ManGzipFile -Destination $manDestPath -Force
        Write-Verbose "Copied man page to: $manDestPath" -Verbose

        # Copy symlinks from temporary locations
        foreach ($link in $LinkInfo) {
            $linkPath = Join-Path $dataDir $link.Destination.TrimStart('/')
            $linkDir = Split-Path $linkPath -Parent
            New-Item -ItemType Directory -Path $linkDir -Force | Out-Null

            # Copy the temporary symlink file that was created by New-LinkInfo
            # The Source contains a temporary symlink that points to the correct target
            if (Test-Path $link.Source) {
                # Use cp to preserve the symlink
                Start-NativeExecution { cp -P $link.Source $linkPath }
                Write-Verbose "Copied symlink: $linkPath (from $($link.Source))" -Verbose
            } else {
                Write-Warning "Symlink source not found: $($link.Source)"
            }
        }

        # Set proper permissions
        Write-Verbose "Setting file permissions..." -Verbose
        # 755 = rwxr-xr-x (owner can read/write/execute, group and others can read/execute)
        Get-ChildItem $dataDir -Directory -Recurse | ForEach-Object {
            Start-NativeExecution { chmod 755 $_.FullName }
        }
        # 644 = rw-r--r-- (owner can read/write, group and others can read only)
        # Exclude symlinks to avoid "cannot operate on dangling symlink" error
        Get-ChildItem $dataDir -File -Recurse |
            Where-Object { -not $_.Target } |
            ForEach-Object {
                Start-NativeExecution { chmod 644 $_.FullName }
            }

        # Set executable permission for pwsh if it exists
        # 755 = rwxr-xr-x (executable permission)
        $pwshPath = "$targetPath/pwsh"
        if (Test-Path $pwshPath) {
            Start-NativeExecution { chmod 755 $pwshPath }
        }

        # Calculate md5sums for all files in data directory (excluding symlinks)
        $md5sumsFile = Join-Path $debianDir "md5sums"
        $md5Content = ""
        Get-ChildItem -Path $dataDir -Recurse -File |
            Where-Object { -not $_.Target } |
            ForEach-Object {
                $relativePath = $_.FullName.Substring($dataDir.Length + 1)
                $md5Hash = (Get-FileHash -Path $_.FullName -Algorithm MD5).Hash.ToLower()
                $md5Content += "$md5Hash  $relativePath`n"
            }
        $md5Content | Out-File -FilePath $md5sumsFile -Encoding ascii -NoNewline
        Write-Verbose "MD5 sums file created: $md5sumsFile" -Verbose

        # Build the package using dpkg-deb
        $debFileName = "${Name}_${Version}-${Iteration}_${HostArchitecture}.deb"
        $debFilePath = Join-Path $CurrentLocation $debFileName

        Write-Verbose "Building DEB package: $debFileName" -Verbose

        # Copy DEBIAN directory and data files to build root
        $buildDir = Join-Path $debBuildRoot "build"
        New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

        Write-Verbose "debianDir: $debianDir" -Verbose
        Write-Verbose "dataDir: $dataDir" -Verbose
        Write-Verbose "buildDir: $buildDir" -Verbose

        # Use cp to preserve symlinks
        Start-NativeExecution { cp -a $debianDir "$buildDir/DEBIAN" }
        Start-NativeExecution { cp -a $dataDir/* $buildDir }

        # Build package with dpkg-deb
        Start-NativeExecution -VerboseOutputOnError {
            dpkg-deb --build $buildDir $debFilePath
        }

        if (Test-Path $debFilePath) {
            Write-Log "Successfully created DEB package: $debFileName"
            return @{
                PackagePath = $debFilePath
                PackageName = $debFileName
            }
        } else {
            throw "DEB package file not found after build: $debFilePath"
        }
    }
    finally {
        # Cleanup temporary directory
        if (Test-Path $debBuildRoot) {
            Write-Verbose "Cleaning up temporary build directory: $debBuildRoot" -Verbose
            Remove-Item -Path $debBuildRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function New-MacOSPackage
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$Iteration,

        [Parameter(Mandatory)]
        [string]$Staging,

        [Parameter(Mandatory)]
        [string]$Destination,

        [Parameter(Mandatory)]
        [string]$ManGzipFile,

        [Parameter(Mandatory)]
        [string]$ManDestination,

        [Parameter(Mandatory)]
        [LinkInfo[]]$LinkInfo,

        [Parameter(Mandatory)]
        [string]$AfterInstallScript,

        [Parameter(Mandatory)]
        [string]$AppsFolder,

        [Parameter(Mandatory)]
        [string]$HostArchitecture,

        [string]$CurrentLocation = (Get-Location),

        [switch]$LTS
    )

    Write-Log "Creating macOS package using pkgbuild and productbuild..."

    # Create a temporary directory for package building
    $tempRoot = New-TempFolder
    $componentPkgPath = Join-Path $tempRoot "component.pkg"
    $scriptsDir = Join-Path $tempRoot "scripts"
    $resourcesDir = Join-Path $tempRoot "resources"
    $distributionFile = Join-Path $tempRoot "distribution.xml"

    try {
        # Create scripts directory
        New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null

        # Copy and prepare the postinstall script
        $postInstallPath = Join-Path $scriptsDir "postinstall"
        Copy-Item -Path $AfterInstallScript -Destination $postInstallPath -Force
        Start-NativeExecution {
            chmod 755 $postInstallPath
        }

        # Create a temporary directory for the package root
        $pkgRoot = Join-Path $tempRoot "pkgroot"
        New-Item -ItemType Directory -Path $pkgRoot -Force | Out-Null

        # Copy staging files to destination path in package root
        $destInPkg = Join-Path $pkgRoot $Destination
        New-Item -ItemType Directory -Path $destInPkg -Force | Out-Null
        Write-Verbose "Copying staging files from $Staging to $destInPkg" -Verbose
        Copy-Item -Path "$Staging/*" -Destination $destInPkg -Recurse -Force

        # Create man page directory structure
        $manDir = Join-Path $pkgRoot (Split-Path $ManDestination -Parent)
        New-Item -ItemType Directory -Path $manDir -Force | Out-Null
        Copy-Item -Path $ManGzipFile -Destination (Join-Path $pkgRoot $ManDestination) -Force

        # Create symlinks in package root
        # The LinkInfo contains Source (a temp file that IS a symlink) and Destination (where to install it)
        foreach ($link in $LinkInfo) {
            $linkDestDir = Join-Path $pkgRoot (Split-Path $link.Destination -Parent)
            New-Item -ItemType Directory -Path $linkDestDir -Force | Out-Null
            $finalLinkPath = Join-Path $pkgRoot $link.Destination

            Write-Verbose "Creating symlink at $finalLinkPath" -Verbose

            # Remove if exists
            if (Test-Path $finalLinkPath) {
                Remove-Item $finalLinkPath -Force
            }

            # Get the target of the original symlink and recreate it in the package root
            if (Test-Path $link.Source) {
                $linkTarget = (Get-Item $link.Source).Target
                if ($linkTarget) {
                    Write-Verbose "Creating symlink to target: $linkTarget" -Verbose
                    New-Item -ItemType SymbolicLink -Path $finalLinkPath -Target $linkTarget -Force | Out-Null
                } else {
                    Write-Warning "Could not determine target for symlink at $($link.Source), copying file instead"
                    Copy-Item -Path $link.Source -Destination $finalLinkPath -Force
                }
            } else {
                Write-Warning "Source symlink $($link.Source) does not exist"
            }
        }

        # Copy launcher app folder if provided
        if ($AppsFolder) {
            $appsInPkg = Join-Path $pkgRoot "Applications"
            New-Item -ItemType Directory -Path $appsInPkg -Force | Out-Null
            Write-Verbose "Copying launcher app from $AppsFolder to $appsInPkg" -Verbose
            Copy-Item -Path "$AppsFolder/*" -Destination $appsInPkg -Recurse -Force
        }

        # Get package identifier info based on version and LTS flag
        $packageInfo = Get-MacOSPackageIdentifierInfo -Version $Version -LTS:$LTS
        $IsPreview = $packageInfo.IsPreview
        $pkgIdentifier = $packageInfo.PackageIdentifier

        if ($PSCmdlet.ShouldProcess("Build component package with pkgbuild")) {
            Write-Log "Running pkgbuild to create component package..."

            Start-NativeExecution -VerboseOutputOnError {
                pkgbuild --root $pkgRoot `
                    --identifier $pkgIdentifier `
                    --version $Version `
                    --scripts $scriptsDir `
                    --install-location "/" `
                    $componentPkgPath
            }

            Write-Verbose "Component package created: $componentPkgPath" -Verbose
        }

        # Create the final distribution package using the refactored function
        $distributionPackage = New-MacOsDistributionPackage `
            -ComponentPackage (Get-Item $componentPkgPath) `
            -PackageName $Name `
            -Version $Version `
            -OutputDirectory $CurrentLocation `
            -HostArchitecture $HostArchitecture `
            -PackageIdentifier $pkgIdentifier `
            -IsPreview:$IsPreview

        return $distributionPackage
    }
    finally {
        # Clean up temporary directory
        if (Test-Path $tempRoot) {
            Write-Verbose "Cleaning up temporary directory: $tempRoot" -Verbose
            Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Get-PackageDependencies
{
    [CmdletBinding()]
    param()
    DynamicParam {
        # Add a dynamic parameter '-Distribution' when the specified package type is 'deb'.
        # The '-Distribution' parameter can be used to indicate which Debian distro this package is targeting.
        $ParameterAttr = New-Object "System.Management.Automation.ParameterAttribute"
        $ParameterAttr.Mandatory = $true
        $ValidateSetAttr = New-Object "System.Management.Automation.ValidateSetAttribute" -ArgumentList $Script:AllDistributions
        $Attributes = New-Object "System.Collections.ObjectModel.Collection``1[System.Attribute]"
        $Attributes.Add($ParameterAttr) > $null
        $Attributes.Add($ValidateSetAttr) > $null

        $Parameter = New-Object "System.Management.Automation.RuntimeDefinedParameter" -ArgumentList ("Distribution", [string], $Attributes)
        $Dict = New-Object "System.Management.Automation.RuntimeDefinedParameterDictionary"
        $Dict.Add("Distribution", $Parameter) > $null
        return $Dict
    }

    End {
        if ($PSBoundParameters.ContainsKey('Distribution')) {
            $Distribution = $PSBoundParameters['Distribution']
        }

        # These should match those in the Dockerfiles, but exclude tools like Git, which, and curl
        $Dependencies = @()

        # ICU version range follows .NET runtime policy.
        # See: https://github.com/dotnet/runtime/blob/3fe8518d51bbcaa179bbe275b2597fbe1b88bc5a/src/native/libs/System.Globalization.Native/pal_icushim.c#L235-L243
        #
        # Version range rationale:
        # - The runtime supports ICU versions >= the version it was built against
        #   and <= that version + 30, to allow sufficient headroom for future releases.
        # - ICU typically releases about twice per year, so +30 provides roughly
        #   15 years of forward compatibility.
        # - On some platforms, the minimum supported version may be lower
        #   than the build version and we know that older versions just works.
        #
        $MinICUVersion = 60                    # runtime minimum supported
        $BuildICUVersion = Get-IcuLatestRelease
        $MaxICUVersion = $BuildICUVersion + 30 # headroom

        if ($Distribution -eq 'deb') {
            $Dependencies = @(
                "libc6",
                "libgcc1",
                "libgssapi-krb5-2",
                "libstdc++6",
                "zlib1g",
                (($MaxICUVersion..$MinICUVersion).ForEach{ "libicu$_" } -join '|'),
                "libssl3|libssl1.1|libssl1.0.2|libssl1.0.0"
            )
        } elseif ($Distribution -eq 'rh') {
            $Dependencies = @(
                "openssl-libs",
                "libicu"
            )
        } elseif ($Distribution -eq 'cm') {
            # Taken from the list here:
            # https://github.com/dotnet/dotnet-docker/blob/d451d6e9427f58c8508f1297c862663a27eb609f/src/runtime-deps/6.0/cbl-mariner1.0/amd64/Dockerfile#L6
            $Dependencies = @(
                "glibc"
                "libgcc"
                "krb5"
                "libstdc++"
                "zlib"
                "icu"
                "openssl-libs"
            )
            if($Script:Options.Runtime -like 'fx*') {
                $Dependencies += @(
                    "dotnet-runtime-10.0"
                )
            }
        } elseif ($Distribution -eq 'macOS') {
            # do nothing
        } else {
            throw "Unknown distribution $Distribution"
        }

        return $Dependencies
    }
}

function Test-Dependencies
{
    # RPM packages use rpmbuild directly.
    # DEB packages use dpkg-deb directly.
    # macOS packages use pkgbuild and productbuild from Xcode Command Line Tools.
    $Dependencies = @()

    # Check for 'rpmbuild' and 'dpkg-deb' on Azure Linux.
    if ($Environment.IsMariner) {
        $Dependencies += "dpkg-deb"
        $Dependencies += "rpmbuild"
    }

    # Check for macOS packaging tools
    if ($Environment.IsMacOS) {
        $Dependencies += "pkgbuild"
        $Dependencies += "productbuild"
    }

    foreach ($Dependency in $Dependencies) {
        if (!(precheck $Dependency "Package dependency '$Dependency' not found. Run Start-PSBootstrap -Scenario Package")) {
            throw "Dependency precheck failed!"
        }
    }
}

function New-AfterScripts
{
    param(
        [Parameter(Mandatory)]
        [string]
        $Link,

        [Parameter(Mandatory)]
        [string]
        $Distribution,

        [Parameter(Mandatory)]
        [string]
        $Destination
    )

    Write-Verbose -Message "AfterScript Distribution: $Distribution" -Verbose

    if ($Distribution -in $script:RedHatDistributions) {
        $AfterInstallScript = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        $AfterRemoveScript = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        $packagingStrings.RedHatAfterInstallScript -f "$Link", $Destination  | Out-File -FilePath $AfterInstallScript -Encoding ascii
        $packagingStrings.RedHatAfterRemoveScript -f "$Link", $Destination | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }
    elseif ($Environment.IsDebianFamily -or $Environment.IsSUSEFamily -or $Distribution -in $script:DebianDistributions) {
        $AfterInstallScript = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        $AfterRemoveScript = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        $packagingStrings.UbuntuAfterInstallScript -f "$Link", $Destination | Out-File -FilePath $AfterInstallScript -Encoding ascii
        $packagingStrings.UbuntuAfterRemoveScript -f "$Link", $Destination | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }
    elseif ($Environment.IsMacOS) {
        # NOTE: The macos pkgutil doesn't support uninstall actions so we did not implement it.
        # Handling uninstall can be done in Homebrew so we'll take advantage of that in the brew formula.
        $AfterInstallScript = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        $packagingStrings.MacOSAfterInstallScript -f "$Link" | Out-File -FilePath $AfterInstallScript -Encoding ascii
    }

    return [PSCustomObject] @{
        AfterInstallScript = $AfterInstallScript
        AfterRemoveScript = $AfterRemoveScript
    }
}

function New-ManGzip
{
    param(
        [switch]
        $IsPreview,

        [switch]
        $IsLTS
    )

    Write-Log "Creating man gz..."

    # run roff to convert man page to roff
    $RoffFile = "$RepoRoot/assets/manpage/pwsh.1"

    if ($IsPreview.IsPresent -or $IsLTS.IsPresent) {
        $prodName = if ($IsLTS) { 'pwsh-lts' } else { 'pwsh-preview' }
        $newRoffFile = $RoffFile -replace 'pwsh', $prodName
        Copy-Item -Path $RoffFile -Destination $newRoffFile -Force -Verbose
        $RoffFile = $newRoffFile
    }

    # gzip in assets directory
    $GzipFile = "$RoffFile.gz"
    Write-Log "Creating man gz - running gzip..."
    Start-NativeExecution { gzip -kf $RoffFile } -VerboseOutputOnError

    if($Environment.IsMacOS) {
        $ManFile = Join-Path "/usr/local/share/man/man1" (Split-Path -Leaf $GzipFile)
    }
    else {
        $ManFile = Join-Path "/usr/share/man/man1" (Split-Path -Leaf $GzipFile)
    }

    return [PSCustomObject ] @{
        GZipFile = $GzipFile
        ManFile = $ManFile
    }
}

<#
    .SYNOPSIS
        Determines the package identifier and preview status for macOS packages.
    .DESCRIPTION
        This function determines if a package is a preview build based on the version string
        and LTS flag, then returns the appropriate package identifier.
    .PARAMETER Version
        The version string (e.g., "7.6.0-preview.6" or "7.6.0")
    .PARAMETER LTS
        Whether this is an LTS build
    .OUTPUTS
        Hashtable with IsPreview (boolean) and PackageIdentifier (string) properties
    .EXAMPLE
        Get-MacOSPackageIdentifierInfo -Version "7.6.0-preview.6" -LTS:$false
        Returns @{ IsPreview = $true; PackageIdentifier = "com.microsoft.powershell-preview" }
#>
function Get-MacOSPackageIdentifierInfo
{
    param(
        [Parameter(Mandatory)]
        [string]$Version,

        [switch]$LTS
    )

    $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS

    # Determine package identifier based on preview status
    if ($IsPreview) {
        $PackageIdentifier = 'com.microsoft.powershell-preview'
    }
    else {
        $PackageIdentifier = 'com.microsoft.powershell'
    }

    return @{
        IsPreview = $IsPreview
        PackageIdentifier = $PackageIdentifier
    }
}

# Dynamically build macOS launcher application.
function New-MacOSLauncher
{
    param(
        [Parameter(Mandatory)]
        [String]$Version,

        [switch]$LTS
    )

    $packageInfo = Get-MacOSPackageIdentifierInfo -Version $Version -LTS:$LTS
    $IsPreview = $packageInfo.IsPreview
    $packageId = $packageInfo.PackageIdentifier

    # Define folder for launcher application.
    $suffix = if ($IsPreview) { "-preview" } elseif ($LTS) { "-lts" }
    $macosapp = "$PSScriptRoot/macos/launcher/ROOT/Applications/PowerShell$suffix.app"

    # Create folder structure for launcher application.
    New-Item -Force -ItemType Directory -Path "$macosapp/Contents/MacOS" | Out-Null
    New-Item -Force -ItemType Directory -Path "$macosapp/Contents/Resources" | Out-Null

    # Define icns file information.
    if ($IsPreview)
    {
        $iconfile = "$RepoRoot/assets/Powershell-preview.icns"
    }
    else
    {
        $iconfile = "$RepoRoot/assets/Powershell.icns"
    }
    $iconfilebase = (Get-Item -Path $iconfile).BaseName

    # Copy icns file.
    Copy-Item -Force -Path $iconfile -Destination "$macosapp/Contents/Resources"

    # Create plist file.
    $plist = "$macosapp/Contents/Info.plist"
    $plistcontent = $packagingStrings.MacOSLauncherPlistTemplate -f $packageId, $Version, $iconfilebase
    $plistcontent | Out-File -Force -Path $plist -Encoding utf8

    # Create shell script.
    $executablepath = Get-PwshExecutablePath -IsPreview:$IsPreview -IsLTS:$LTS
    $shellscript = "$macosapp/Contents/MacOS/PowerShell.sh"
    $shellscriptcontent = $packagingStrings.MacOSLauncherScript -f $executablepath
    $shellscriptcontent | Out-File -Force -Path $shellscript -Encoding utf8

    # Set permissions for plist and shell script.
    Start-NativeExecution {
        chmod 644 $plist
    }
    Start-NativeExecution {
        chmod 755 $shellscript
    }

    # Return the app folder path for packaging
    $appsfolder = (Resolve-Path -Path "$macosapp/..").Path

    return $appsfolder
}

function Get-PwshExecutablePath
{
    param(
        [switch] $IsPreview,
        [switch] $IsLTS
    )

    if ($IsPreview -and $IsLTS)
    {
        throw "Cannot be LTS and Preview"
    }

    $executableName = if ($IsPreview) {
        "pwsh-preview"
    } elseif ($IsLTS) {
        "pwsh-lts"
    } else {
        "pwsh"
    }

    if ($Environment.IsLinux) {
        "/usr/bin/$executableName"
    } elseif ($Environment.IsMacOS) {
        "/usr/local/bin/$executableName"
    }
}

function Clear-MacOSLauncher
{
    # This is needed to prevent installer from picking up
    # the launcher app in the build structure and updating
    # it which locks out subsequent package builds due to
    # increase permissions.

    # Remove launcher application.
    $macosfolder = "$PSScriptRoot/macos"
    Remove-Item -Force -Recurse -Path $macosfolder
}

function New-StagingFolder
{
    param(
        [Parameter(Mandatory)]
        [string]
        $StagingPath,

        [Parameter(Mandatory)]
        [string]
        $PackageSourcePath,

        [string]
        $Filter = '*',

        [R2RVerification]
        $R2RVerification
    )

    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $StagingPath
    Copy-Item -Recurse $PackageSourcePath $StagingPath -Filter $Filter

    $smaPath = Join-Path $StagingPath 'System.Management.Automation.dll'
    if ($R2RVerification) {
        $smaInfo = Get-PEInfo -File $smaPath
        switch ($R2RVerification.R2RState) {
            $null {
                Write-Verbose "Skipping R2R verification" -Verbose
            }
            'R2R' {
                Write-Verbose "Verifying R2R was done..." -Verbose
                if (!$smaInfo.CrossGen -or $smaInfo.Architecture -ne $R2RVerification.Architecture -or $smaInfo.OS -ne $R2RVerification.OperatingSystem) {
                    throw "System.Management.Automation.dll is not ReadyToRun for $($R2RVerification.OperatingSystem) $($R2RVerification.Architecture).  Actually ($($smaInfo.CrossGen) $($smaInfo.OS) $($smaInfo.Architecture) )"
                }
                $mismatchedCrossGenedFiles = @(Get-ChildItem -Path $StagingPath -Filter '*.dll' -Recurse |
                    Get-PEInfo |
                    Where-Object { $_.CrossGen -and $_.OS -ne $R2RVerification.OperatingSystem -and $_.Architecture -ne $R2RVerification.Architecture })
                if ($mismatchedCrossGenedFiles.Count -gt 0) {
                    foreach ($file in $mismatchedCrossGenedFiles) {
                        Write-Warning "Misconfigured ReadyToRun file found.  Expected $($R2RVerification.OperatingSystem) $($R2RVerification.Architecture).  Actual ($($file.OS) $($file.Architecture) ) "
                    }
                    throw "Unexpected ReadyToRun files found."
                }
            }
            'NoR2R' {
                Write-Verbose "Verifying no R2R was done..." -Verbose
                $crossGenedFiles = @(Get-ChildItem -Path $StagingPath -Filter '*.dll' -Recurse |
                    Get-PEInfo |
                    Where-Object { $_.CrossGen })
                if ($crossGenedFiles.Count -gt 0) {
                    throw "Unexpected ReadyToRun files found: $($crossGenedFiles | ForEach-Object { $_.Path })"
                }
            }
            'SdkOnly' {
                Write-Verbose "Verifying no R2R was done on SMA..." -Verbose
                if ($smaInfo.CrossGen) {
                    throw "System.Management.Automation.dll should not be ReadyToRun"
                }
            }
        }
    }
}

# Function to create a zip file for Nano Server and xcopy deployment
function New-ZipPackage
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (

        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $PackageName = 'PowerShell',

        # Suffix of the Name
        [string] $PackageNameSuffix,

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageVersion,

        # Source Path to the Product Files - required to package the contents into an Zip
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageSourcePath,

        [switch] $Force,

        [string] $CurrentLocation = (Get-Location),

        [R2RVerification] $R2RVerification = [R2RVerification]::new()
    )

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $PackageVersion

    $zipPackageName = $PackageName + "-" + $ProductSemanticVersion
    if ($PackageNameSuffix) {
        $zipPackageName = $zipPackageName, $PackageNameSuffix -join "-"
    }

    Write-Verbose "Create Zip for Product $zipPackageName"

    $zipLocationPath = Join-Path $CurrentLocation "$zipPackageName.zip"

    if ($Force.IsPresent)
    {
        if (Test-Path $zipLocationPath)
        {
            Remove-Item $zipLocationPath
        }
    }

    if (Get-Command Compress-Archive -ErrorAction Ignore)
    {
        if ($PSCmdlet.ShouldProcess("Create zip package"))
        {
            $staging = "$PSScriptRoot/staging"
            New-StagingFolder -StagingPath $staging -PackageSourcePath $PackageSourcePath -R2RVerification $R2RVerification

            Compress-Archive -Path $staging\* -DestinationPath $zipLocationPath
        }

        if (Test-Path $zipLocationPath)
        {
            Write-Log "You can find the Zip @ $zipLocationPath"
            $zipLocationPath
        }
        else
        {
            throw "Failed to create $zipLocationPath"
        }
    }
    else
    {
        Write-Error -Message "Compress-Archive cmdlet is missing in this PowerShell version"
    }
}

# Function to create a zip file of PDB
function New-PdbZipPackage
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (

        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $PackageName = 'PowerShell-Symbols',

        # Suffix of the Name
        [string] $PackageNameSuffix,

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        # Source Path to the Product Files - required to package the contents into an Zip
        [Parameter(Mandatory = $true)]
        [string] $PackageSourcePath,

        [switch] $Force,

        [string] $CurrentLocation = (Get-Location)
    )

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $PackageVersion

    $zipPackageName = $PackageName + "-" + $ProductSemanticVersion
    if ($PackageNameSuffix) {
        $zipPackageName = $zipPackageName, $PackageNameSuffix -join "-"
    }

    Write-Verbose "Create Symbols Zip for Product $zipPackageName"

    $zipLocationPath = Join-Path $CurrentLocation "$zipPackageName.zip"

    if ($Force.IsPresent)
    {
        if (Test-Path $zipLocationPath)
        {
            Remove-Item $zipLocationPath
        }
    }

    if (Get-Command Compress-Archive -ErrorAction Ignore)
    {
        if ($PSCmdlet.ShouldProcess("Create zip package"))
        {
            $staging = "$PSScriptRoot/staging"

            # We should NOT R2R verify the PDB zip
            New-StagingFolder -StagingPath $staging -PackageSourcePath $PackageSourcePath -Filter *.pdb

            Compress-Archive -Path $staging\* -DestinationPath $zipLocationPath
        }

        if (Test-Path $zipLocationPath)
        {
            Write-Log "You can find the Zip @ $zipLocationPath"
            $zipLocationPath
        }
        else
        {
            throw "Failed to create $zipLocationPath"
        }
    }
    else
    {
        Write-Error -Message "Compress-Archive cmdlet is missing in this PowerShell version"
    }
}


function CreateNugetPlatformFolder
{
    param(
        [Parameter(Mandatory = $true)]
        [string] $FileName,

        [Parameter(Mandatory = $true)]
        [string] $Platform,

        [Parameter(Mandatory = $true)]
        [string] $PackageRuntimesFolder,

        [Parameter(Mandatory = $true)]
        [string] $PlatformBinPath
    )

    $destPath = New-Item -ItemType Directory -Path (Join-Path $PackageRuntimesFolder "$Platform/lib/$script:netCoreRuntime")
    $fullPath = Join-Path $PlatformBinPath $FileName

    if (-not(Test-Path $fullPath)) {
        throw "File not found: $fullPath"
    }

    Copy-Item -Path $fullPath -Destination $destPath
    Write-Log "Copied $FileName to $Platform at path: $destPath"
}

<#
.SYNOPSIS
Creates a CGManifest file containing package dependencies for specified file.

.PARAMETER FilePath
File path name of CGManifest file to be created.

.PARAMETER Dependencies
Array list of dependency tuples:
[tuple[ [tuple[string, string]], [tuple[string, string]] ] []]
["Id", "Microsoft.PowerShell.SecretStore"], ["Version", "1.1.1.0"]
#>
function New-CGManifest
{
    param (
        [parameter(Mandatory = $true)]
        [string] $FilePath,

        [parameter(Mandatory = $false)]
        [tuple[ [tuple[string, string]], [tuple[string, string]] ] []] $Dependencies
    )

    Write-Verbose -Verbose -Message "Creating CGManifest for SBOM: $Filepath"

    $Registrations = @()

    foreach ($dependency in $Dependencies) {
        $component = @{
            Component = @{
                Type = "nuget";
                NuGet = @{
                    Name = ($dependency.Item1.Item2); Version = ($dependency.Item2.Item2)
                }
            };
            DevelopmentDependency = "true"
        }

        $Registrations += $component
    }

    $manifest = @{ Registrations = $Registrations }
    $jsonManifest = $manifest | ConvertTo-Json -Depth 10

    $jsonManifest | Out-File -FilePath $FilePath
}

function New-FileDependencies
{
    param (
        [parameter(Mandatory = $true)]
        [string] $FileBaseName,

        [parameter(Mandatory = $true)]
        [string] $PackageVersion
    )

    # Filed a tracking bug for automating generation of dependecy list: https://github.com/PowerShell/PowerShell/issues/6247
    $deps = [System.Collections.ArrayList]::new()

    switch ($FileBaseName) {
        'Microsoft.Management.Infrastructure.CimCmdlets' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
        }

        'Microsoft.PowerShell.Commands.Diagnostics' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
        }

        'Microsoft.PowerShell.Commands.Management' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Security'), [tuple]::Create('version', $PackageVersion))) > $null
            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
        }

        'Microsoft.PowerShell.Commands.Utility' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null

            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
        }

        'Microsoft.PowerShell.ConsoleHost' {
            $deps.Add([tuple]::Create( [tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
        }

        'Microsoft.PowerShell.CoreCLR.Eventing' {
            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
        }

        'Microsoft.PowerShell.SDK' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Commands.Management'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Commands.Utility'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.ConsoleHost'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Security'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.WSMan.Management'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Commands.Diagnostics'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.Management.Infrastructure.CimCmdlets'), [tuple]::Create('version', $PackageVersion))) > $null
        }

        'Microsoft.PowerShell.Security' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
        }

        'Microsoft.WSMan.Management' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.WSMan.Runtime'), [tuple]::Create('version', $PackageVersion))) > $null
            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
        }

        'Microsoft.WSMan.Runtime' {
            ## No dependencies
        }

        'System.Management.Automation' {
            $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.CoreCLR.Eventing'), [tuple]::Create('version', $PackageVersion))) > $null
            foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $FileBaseName))
            {
                $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
            }
        }
    }

    Write-Output $deps
}

<#
.SYNOPSIS
Creates nuget package sources for a single provided binary file.

.DESCRIPTION
Creates IL assemblies, for a single binary file, to be packaged in a NuGet file.
Includes runtime assemblies for linux and Windows runtime assemblies.

.PARAMETER FileName
File name of binary to create nuget sources for.

.PARAMETER PackagePath
Path where the package source files will be created.

.PARAMETER PackageVersion
Version of the created package.

.PARAMETER WinFxdBinPath
Path to source folder containing Windows framework dependent assemblies.

.PARAMETER LinuxFxdBinPath
Path to source folder containing Linux framework dependent assemblies.

.PARAMETER RefAssemblyPath
Path to the reference assemblies.

.PARAMETER CGManifestPath
Path to the CGManifest.json file.
#>
function New-ILNugetPackageSource
{
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [Parameter(Mandatory = $true)]
        [string] $FileName,

        [Parameter(Mandatory = $true)]
        [string] $PackagePath,

        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        [Parameter(Mandatory = $true)]
        [string] $WinFxdBinPath,

        [Parameter(Mandatory = $true)]
        [string] $LinuxFxdBinPath,

        [Parameter(Mandatory = $true)]
        [string] $RefAssemblyPath,

        [string] $CGManifestPath

    )

    if (! $Environment.IsWindows)
    {
        throw "New-ILNugetPackageSource can be only executed on Windows platform."
    }

    if (! $PSCmdlet.ShouldProcess("Create nuget packages at: $PackagePath"))
    {
        return
    }

    $fileList = @(
        "Microsoft.Management.Infrastructure.CimCmdlets.dll",
        "Microsoft.PowerShell.Commands.Diagnostics.dll",
        "Microsoft.PowerShell.Commands.Management.dll",
        "Microsoft.PowerShell.Commands.Utility.dll",
        "Microsoft.PowerShell.ConsoleHost.dll",
        "Microsoft.PowerShell.CoreCLR.Eventing.dll",
        "Microsoft.PowerShell.Security.dll",
        "Microsoft.PowerShell.SDK.dll",
        "Microsoft.WSMan.Management.dll",
        "Microsoft.WSMan.Runtime.dll",
        "System.Management.Automation.dll")

    $linuxExceptionList = @(
        "Microsoft.Management.Infrastructure.CimCmdlets.dll",
        "Microsoft.PowerShell.Commands.Diagnostics.dll",
        "Microsoft.PowerShell.CoreCLR.Eventing.dll",
        "Microsoft.WSMan.Management.dll",
        "Microsoft.WSMan.Runtime.dll")

    $SnkFilePath = "$RepoRoot\src\signing\visualstudiopublic.snk"

    if (! (Test-Path $PackagePath)) {
        $null = New-Item -Path $PackagePath -ItemType Directory
    }

    # Remove '.dll' at the end
    $fileBaseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    $filePackageFolder = New-Item (Join-Path $PackagePath $fileBaseName) -ItemType Directory -Force
    $packageRuntimesFolder = New-Item (Join-Path $filePackageFolder.FullName 'runtimes') -ItemType Directory

    Write-Verbose -Verbose "New-ILNugetPackageSource: Creating package source folder for file: $FileName at: $filePackageFolder"

    #region ref
    $refFolder = New-Item (Join-Path $filePackageFolder.FullName "ref/$script:netCoreRuntime") -ItemType Directory -Force
    CopyReferenceAssemblies -assemblyName $fileBaseName -refBinPath $RefAssemblyPath -refNugetPath $refFolder -assemblyFileList $fileList -winBinPath $WinFxdBinPath
    #endregion ref

    $packageRuntimesFolderPath = $packageRuntimesFolder.FullName

    CreateNugetPlatformFolder -FileName $FileName -Platform 'win' -PackageRuntimesFolder $packageRuntimesFolderPath -PlatformBinPath $WinFxdBinPath

    Write-Verbose -Verbose "Done creating Windows runtime assemblies for $FileName"

    if ($linuxExceptionList -notcontains $FileName )
    {
        CreateNugetPlatformFolder -FileName $FileName -Platform 'unix' -PackageRuntimesFolder $packageRuntimesFolderPath -PlatformBinPath $LinuxFxdBinPath
        Write-Verbose -Verbose "Done creating Linux runtime assemblies for $FileName"
    }
    else {
        Write-Verbose -Verbose "Skipping creating Linux runtime assemblies for $FileName"
    }

    if ($FileName -eq "Microsoft.PowerShell.SDK.dll")
    {
        # Copy the '$PSHOME\ref' folder to the NuGet package, so 'dotnet publish' can deploy the 'ref' folder to the publish folder.
        # This is to make 'Add-Type' work in application that hosts PowerShell.

        $contentFolder = New-Item (Join-Path $filePackageFolder "contentFiles\any\any") -ItemType Directory -Force
        $dotnetRefAsmFolder = Join-Path -Path $WinFxdBinPath -ChildPath "ref"
        Copy-Item -Path $dotnetRefAsmFolder -Destination $contentFolder -Recurse -Force
        Write-Log "Copied the reference assembly folder to contentFiles for the SDK package"

        # Copy the built-in module folders to the NuGet package, so 'dotnet publish' can deploy those modules to the $pshome module path.
        # This is for enabling applications that hosts PowerShell to ship the built-in modules.

        $winBuiltInModules = @(
            "CimCmdlets",
            "Microsoft.PowerShell.Diagnostics",
            "Microsoft.PowerShell.Host",
            "Microsoft.PowerShell.Management",
            "Microsoft.PowerShell.Security",
            "Microsoft.PowerShell.Utility",
            "Microsoft.WSMan.Management",
            "PSDiagnostics"
        )

        $unixBuiltInModules = @(
            "Microsoft.PowerShell.Host",
            "Microsoft.PowerShell.Management",
            "Microsoft.PowerShell.Security",
            "Microsoft.PowerShell.Utility"
        )

        $winModuleFolder = New-Item (Join-Path $contentFolder "runtimes\win\lib\$script:netCoreRuntime\Modules") -ItemType Directory -Force
        $unixModuleFolder = New-Item (Join-Path $contentFolder "runtimes\unix\lib\$script:netCoreRuntime\Modules") -ItemType Directory -Force

        foreach ($module in $winBuiltInModules) {
            $source = Join-Path $WinFxdBinPath "Modules\$module"
            Copy-Item -Path $source -Destination $winModuleFolder -Recurse -Force
        }

        foreach ($module in $unixBuiltInModules) {
            $source = Join-Path $LinuxFxdBinPath "Modules\$module"
            Copy-Item -Path $source -Destination $unixModuleFolder -Recurse -Force
        }

        Write-Log "Copied the built-in modules to contentFiles for the SDK package"
    }
    else {
        Write-Verbose -Verbose "Skipping copying the built-in modules and reference assemblies for $FileName"
    }

    if (-not $PSBoundParameters.ContainsKey("CGManifestPath")) {
        Write-Verbose -Verbose "CGManifestPath is not provided. Skipping CGManifest creation."
        return
    }

    # Create a CGManifest file that lists all dependencies for this package, which is used when creating the SBOM.
    if (! (Test-Path -Path $CGManifestPath)) {
        $null = New-Item -Path $CGManifestPath -ItemType Directory
    }
    $deps = New-FileDependencies -FileBaseName $fileBaseName -PackageVersion $PackageVersion
    New-CGManifest -FilePath (Join-Path -Path $CGManifestPath -ChildPath "CGManifest.json") -Dependencies $deps
}

<#
.SYNOPSIS
Creates a nuget package file from the provided source path.

.PARAMETER FileName
File name of binary to create nuget package for.

.PARAMETER PackagePath
Path for the source files and the created NuGet package file.
#>
function New-ILNugetPackageFromSource
{
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [Parameter(Mandatory = $true)]
        [string] $FileName,

        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        [Parameter(Mandatory = $true)]
        [string] $PackagePath
    )

    if (! $Environment.IsWindows)
    {
        throw "New-ILNugetPackageFromSource can be only executed on Windows platform."
    }

    if (! $PSCmdlet.ShouldProcess("Create nuget package for file $FileName at: $PackagePath"))
    {
        return
    }

    $fileBaseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)

    $deps = New-FileDependencies -FileBaseName $fileBaseName -PackageVersion $PackageVersion

    $srcFilePackagePath = Join-Path $PackagePath $fileBaseName

    Write-Verbose -Verbose "New-ILNugetPackageFromSource: Creating nuget package for file: $FileName from source path: $srcFilePackagePath"

    if (! (Test-Path $srcFilePackagePath)) {
        $msg = "Expected nuget source path $srcFilePackagePath for file $fileBaseName does not exist."
        Write-Verbose -Verbose -Message $msg
        throw $msg
    }

    # Remove the CGManifest file used to create the SBOM.
    $cgManifestPath = Join-Path -Path $PackagePath -ChildPath 'CGManifest'
    $cgManifestFilePath = Join-Path -Path $cgManifestPath -ChildPath 'CGManifest.json'
    if (Test-Path -Path $cgManifestFilePath)
    {
        Write-Verbose -Verbose "Removing CGManifest file: $cgManifestFilePath"
        Remove-Item -Path $cgManifestFilePath -Force -ErrorAction Continue
    }

    New-NuSpec -PackageId $fileBaseName -PackageVersion $PackageVersion -Dependency $deps -FilePath (Join-Path $srcFilePackagePath "$fileBaseName.nuspec")

    # Copy icon file to package
    Copy-Item -Path $iconPath -Destination "$srcFilePackagePath/$iconFileName" -Verbose

    New-NugetPackage -NuSpecPath $srcFilePackagePath -PackageDestinationPath $PackagePath

    # Remove file nuget package source directory
    Remove-Item $srcFilePackagePath -Recurse -Force -ErrorAction SilentlyContinue
}

<#
  Copy the generated reference assemblies to the 'ref/net8.0' folder properly.
  This is a helper function used by 'New-ILNugetPackageSource'.
#>
function CopyReferenceAssemblies
{
    param(
        [string] $assemblyName,
        [string] $refBinPath,
        [string] $refNugetPath,
        [string[]] $assemblyFileList,
        [string] $winBinPath
    )

    $supportedRefList = @(
        "Microsoft.PowerShell.Commands.Utility",
        "Microsoft.PowerShell.ConsoleHost",
        "Microsoft.PowerShell.Commands.Management",
        "Microsoft.PowerShell.Commands.Security",
        "System.Management.Automation"
        )

    switch ($assemblyName) {
        "Microsoft.PowerShell.SDK" {
            foreach ($asmFileName in $assemblyFileList) {
                $fileName = [System.IO.Path]::GetFileNameWithoutExtension($asmFileName)

                if ($fileName -in $supportedRefList) {
                    $refFile = Join-Path -Path $refBinPath -ChildPath $asmFileName
                    if (Test-Path -Path $refFile) {
                        $refDoc = Join-Path -Path $refBinPath -ChildPath ([System.IO.Path]::ChangeExtension($asmFileName, "xml"))
                        Copy-Item $refFile, $refDoc -Destination $refNugetPath -Force
                        Write-Log "Copied file '$refFile' and '$refDoc' to '$refNugetPath'"
                    }
                }
            }
        }

        default {
            $refDll = Join-Path -Path $refBinPath -ChildPath "$assemblyName.dll"
            $refDoc = Join-Path -Path $refBinPath -ChildPath "$assemblyName.xml"
            Copy-Item $refDll, $refDoc -Destination $refNugetPath -Force
            Write-Log "Copied file '$refDll' and '$refDoc' to '$refNugetPath'"
        }
    }
}

<#
.SYNOPSIS
Return the list of packages and versions used by a project

.PARAMETER ProjectName
The name of the project to get the projects for.
#>
function Get-ProjectPackageInformation
{
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $ProjectName
    )

    $csproj = "$RepoRoot\src\$ProjectName\$ProjectName.csproj"
    [xml] $csprojXml = (Get-Content -Raw -Path $csproj)

    # get the package references
    $packages=$csprojXml.Project.ItemGroup.PackageReference

    # check to see if there is a newer package for each refernce
    foreach($package in $packages)
    {
        if ($package.Version -notmatch '\*' -and $package.Include)
        {
            # Get the name of the package
            [PSCustomObject] @{
                Name = $package.Include
                Version = $package.Version
            }
        }
    }
}

<#
.SYNOPSIS
Creates a nuspec file.

.PARAMETER PackageId
ID of the package.

.PARAMETER PackageVersion
Version of the package.

.PARAMETER Dependency
Depedencies of the package.

.PARAMETER FilePath
Path to create the nuspec file.
#>
function New-NuSpec {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackageId,

        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        [Parameter(Mandatory = $false)]
        # An array of tuples of tuples to define the dependencies.
        # First tuple defines 'id' and value eg: ["id", "System.Data.SqlClient"]
        # Second tuple defines 'version' and value eg: ["version", "4.4.2"]
        # Both these tuples combined together define one dependency.
        # An array represents all the dependencies.
        [tuple[ [tuple[string, string]], [tuple[string, string]] ] []] $Dependency,

        [Parameter(Mandatory = $true)]
        [string] $FilePath
    )

    if (-not $Environment.IsWindows)
    {
        throw "New-NuSpec can be only executed on Windows platform."
    }

    $nuspecTemplate = $packagingStrings.NuspecTemplate -f $PackageId,$PackageVersion,$iconFileName
    $nuspecObj = [xml] $nuspecTemplate

    if ( ($null -ne $Dependency) -and $Dependency.Count -gt 0 ) {

        foreach($dep in $Dependency) {
            # Each item is [tuple[ [tuple[string, string]], [tuple[string, string]] ]
            $d = $nuspecObj.package.metadata.dependencies.group.AppendChild($nuspecObj.CreateElement("dependency"))

            # 'id' and value
            $d.SetAttribute($dep.Item1.Item1, $dep.Item1.Item2)

            # 'version' and value
            $d.SetAttribute($dep.Item2.Item1, $dep.Item2.Item2)
        }
    }

    $nuspecObj.Save($filePath)
}

<#
.SYNOPSIS
Create a reference assembly from System.Management.Automation.dll

.DESCRIPTION
A unix variant of System.Management.Automation.dll is converted to a reference assembly.
GenAPI.exe generated the CS file containing the APIs.
This file is cleaned up and then compiled into a dll.

.PARAMETER Unix64BinPath
Path to the folder containing unix 64 bit assemblies.

.PARAMETER RefAssemblyDestinationPath
Path to the folder where the reference assembly is created.

.PARAMETER RefAssemblyVersion
Version of the reference assembly.

.PARAMETER GenAPIToolPath
Path to GenAPI.exe. Tool from https://www.nuget.org/packages/Microsoft.DotNet.BuildTools.GenAPI/

.PARAMETER SnkFilePath
Path to the snk file for strong name signing.
#>

function New-ReferenceAssembly
{
    param(
        [Parameter(Mandatory = $true)]
        [string] $Linux64BinPath,

        [Parameter(Mandatory = $true)]
        [string] $RefAssemblyDestinationPath,

        [Parameter(Mandatory = $true)]
        [string] $RefAssemblyVersion,

        [Parameter(Mandatory = $true)]
        [string] $GenAPIToolPath,

        [Parameter(Mandatory = $true)]
        [string] $SnkFilePath
    )

    if (-not $Environment.IsWindows)
    {
        throw "New-ReferenceAssembly can be only executed on Windows platform."
    }

    $genAPIExe = Get-ChildItem -Path "$GenAPIToolPath/*GenAPI.exe" -Recurse

    if (-not (Test-Path $genAPIExe))
    {
        throw "GenAPI.exe was not found at: $GenAPIToolPath"
    }

    Write-Log "GenAPI nuget package saved and expanded."

    $genAPIFolder = New-TempFolder
    Write-Log "Working directory: $genAPIFolder."

    $SMAReferenceAssembly = $null
    $assemblyNames = @(
        "System.Management.Automation",
        "Microsoft.PowerShell.Commands.Utility",
        "Microsoft.PowerShell.ConsoleHost"
    )

    # Ensure needed dotNet version is available.  Find-DotNet does this, and is part of build.psm1 which should already be imported.
    Find-DotNet -Verbose

    foreach ($assemblyName in $assemblyNames) {

        Write-Log "Building reference assembly for '$assemblyName'"
        $projectFolder = New-Item -Path "$genAPIFolder/$assemblyName" -ItemType Directory -Force
        $generatedSource = Join-Path $projectFolder "$assemblyName.cs"
        $filteredSource = Join-Path $projectFolder "${assemblyName}_Filtered.cs"

        $linuxDllPath = Join-Path $Linux64BinPath "$assemblyName.dll"
        if (-not (Test-Path $linuxDllPath)) {
            throw "$assemblyName.dll was not found at: $Linux64BinPath"
        }

        $dllXmlDoc = Join-Path $Linux64BinPath "$assemblyName.xml"
        if (-not (Test-Path $dllXmlDoc)) {
            throw "$assemblyName.xml was not found at: $Linux64BinPath"
        }

        $genAPIArgs = "$linuxDllPath","-libPath:$Linux64BinPath,$Linux64BinPath\ref"
        Write-Log "GenAPI cmd: $genAPIExe $genAPIArgs"

        Start-NativeExecution { & $genAPIExe $genAPIArgs } | Out-File $generatedSource -Force
        Write-Log "Reference assembly file generated at: $generatedSource"

        CleanupGeneratedSourceCode -assemblyName $assemblyName -generatedSource $generatedSource -filteredSource $filteredSource

        try
        {
            Push-Location $projectFolder

            $sourceProjectRoot = Join-Path $PSScriptRoot "projects/reference/$assemblyName"
            $sourceProjectFile = Join-Path $sourceProjectRoot "$assemblyName.csproj"

            $destProjectFile = Join-Path $projectFolder "$assemblyName.csproj"
            $nugetConfigFile = Join-Path $PSScriptRoot "../../nuget.config"

            Copy-Item -Path $sourceProjectFile -Destination $destProjectFile -Force -Verbose
            Copy-Item -Path $nugetConfigFile -Destination $projectFolder -Verbose

            Send-AzdoFile -Path $destProjectFile
            Send-AzdoFile -Path $generatedSource

            $arguments = GenerateBuildArguments -AssemblyName $assemblyName -RefAssemblyVersion $RefAssemblyVersion -SnkFilePath $SnkFilePath -SMAReferencePath $SMAReferenceAssembly

            Write-Log "Running: dotnet $arguments"
            Start-NativeExecution -sb {dotnet $arguments}

            $refBinPath = Join-Path $projectFolder "bin/Release/$script:netCoreRuntime/$assemblyName.dll"
            if ($null -eq $refBinPath) {
                throw "Reference assembly was not built."
            }

            Copy-Item $refBinPath $RefAssemblyDestinationPath -Force
            Write-Log "Reference assembly '$assemblyName.dll' built and copied to $RefAssemblyDestinationPath"

            Copy-Item $dllXmlDoc $RefAssemblyDestinationPath -Force
            Write-Log "Xml document '$assemblyName.xml' copied to $RefAssemblyDestinationPath"

            if ($assemblyName -eq "System.Management.Automation") {
                $SMAReferenceAssembly = $refBinPath
            }
        }
        finally
        {
            Pop-Location
        }
    }

    if (Test-Path $genAPIFolder)
    {
        Remove-Item $genAPIFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

<#
  Helper function for New-ReferenceAssembly to further clean up the
  C# source code generated from GenApi.exe.
#>
function CleanupGeneratedSourceCode
{
    param(
        [string] $assemblyName,
        [string] $generatedSource,
        [string] $filteredSource
    )

    $patternsToRemove = @(
        '[System.Management.Automation.ArgumentToEncodingTransformationAttribute]'
        'typeof(System.Security.AccessControl.FileSecurity)'
        '[System.Management.Automation.ArgumentTypeConverterAttribute'
        '[System.Runtime.CompilerServices.IteratorStateMachineAttribute'
        '[Microsoft.PowerShell.Commands.ArgumentToModuleTransformationAttribute]'
        '[Microsoft.PowerShell.Commands.SetStrictModeCommand.ArgumentToVersionTransformationAttribute]'
        '[Microsoft.PowerShell.Commands.SetStrictModeCommand.ValidateVersionAttribute]'
        '[System.Management.Automation.OutputTypeAttribute(typeof(System.Management.Automation.PSRemotingJob))]'
        'typeof(System.Management.Automation.LanguagePrimitives.EnumMultipleTypeConverter)'
        '[System.Management.Automation.Internal.CommonParameters.ValidateVariableName]'
        '[System.Management.Automation.ArgumentEncodingCompletionsAttribute]'
        '[Microsoft.PowerShell.Commands.AddMemberCommand'
        '[System.Management.Automation.ArgumentCompleterAttribute(typeof(Microsoft.PowerShell.Commands.Utility.JoinItemCompleter))]'
        '[System.Management.Automation.ArgumentCompleterAttribute(typeof(System.Management.Automation.PropertyNameCompleter))]'
        '[Microsoft.PowerShell.Commands.ArgumentToTypeNameTransformationAttribute]'
        '[System.Management.Automation.Internal.ArchitectureSensitiveAttribute]'
        '[Microsoft.PowerShell.Commands.SelectStringCommand.FileinfoToStringAttribute]'
        '[System.Runtime.CompilerServices.IsReadOnlyAttribute]'
        '[System.Runtime.CompilerServices.NullableContextAttribute('
        '[System.Runtime.CompilerServices.NullableAttribute((byte)0)]'
        '[System.Runtime.CompilerServices.NullableAttribute(new byte[]{ (byte)2, (byte)1, (byte)1})]'
        '[System.Runtime.CompilerServices.AsyncStateMachineAttribute'
        '[Microsoft.PowerShell.Commands.SetStrictModeCommand.ArgumentToPSVersionTransformationAttribute]'
        '[Microsoft.PowerShell.Commands.HttpVersionCompletionsAttribute]'
        '[System.Management.Automation.ArgumentToVersionTransformationAttribute]'
        '[Microsoft.PowerShell.Commands.InvokeCommandCommand.ArgumentToPSVersionTransformationAttribute]'
        '[Microsoft.PowerShell.Commands.InvokeCommandCommand.ValidateVersionAttribute]',
        '[System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.Internal.Format.FormatStartData), typeof(Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData), typeof(Microsoft.PowerShell.Commands.Internal.Format.FormatEndData), typeof(Microsoft.PowerShell.Commands.Internal.Format.GroupStartData), typeof(Microsoft.PowerShell.Commands.Internal.Format.GroupEndData)})]'
        )

    $patternsToReplace = @(
        @{
            ApplyTo = @("Microsoft.PowerShell.Commands.Utility")
            Pattern = "[System.Runtime.CompilerServices.IsReadOnlyAttribute]ref Microsoft.PowerShell.Commands.JsonObject.ConvertToJsonContext"
            Replacement = "in Microsoft.PowerShell.Commands.JsonObject.ConvertToJsonContext"
        },
        @{
            ApplyTo = @("Microsoft.PowerShell.Commands.Utility")
            Pattern = "public partial struct ConvertToJsonContext"
            Replacement = "public readonly struct ConvertToJsonContext"
        },
        @{
            ApplyTo = @("Microsoft.PowerShell.Commands.Utility")
            Pattern = "Unable to resolve assembly 'Assembly(Name=Newtonsoft.Json"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=Newtonsoft.Json"
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "Unable to resolve assembly 'Assembly(Name=System.Security.Principal.Windows"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=System.Security.Principal.Windows"
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "Unable to resolve assembly 'Assembly(Name=Microsoft.Management.Infrastructure"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=Microsoft.Management.Infrastructure"
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "Unable to resolve assembly 'Assembly(Name=System.Security.AccessControl"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=System.Security.AccessControl"
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "[System.Runtime.CompilerServices.NullableAttribute(new byte[]{ (byte)1, (byte)2, (byte)1})]"
            Replacement = "/* [System.Runtime.CompilerServices.NullableAttribute(new byte[]{ (byte)1, (byte)2, (byte)1})] */ "
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "[System.Runtime.CompilerServices.NullableAttribute(new byte[]{ (byte)2, (byte)1})]"
            Replacement = "/* [System.Runtime.CompilerServices.NullableAttribute(new byte[]{ (byte)2, (byte)1})] */ "
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "[System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Runtime.CompilerServices.NullableContextAttribute((byte)2)]"
            Replacement = "/* [System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Runtime.CompilerServices.NullableContextAttribute((byte)2)] */ "
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "[System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Runtime.CompilerServices.IsReadOnlyAttribute]"
            Replacement = "/* [System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Runtime.CompilerServices.IsReadOnlyAttribute] */ "
        },
        @{
            ApplyTo = @("System.Management.Automation")
            Pattern = "[System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Runtime.CompilerServices.NullableContextAttribute((byte)1)]"
            Replacement = "/* [System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Runtime.CompilerServices.NullableContextAttribute((byte)1)] */ "
        },
        @{
            ApplyTo = @("System.Management.Automation", "Microsoft.PowerShell.ConsoleHost")
            Pattern = "[System.Runtime.CompilerServices.NullableAttribute((byte)2)]"
            Replacement = "/* [System.Runtime.CompilerServices.NullableAttribute((byte)2)] */"
        },
        @{
            ApplyTo = @("System.Management.Automation", "Microsoft.PowerShell.ConsoleHost")
            Pattern = "[System.Runtime.CompilerServices.NullableAttribute((byte)1)]"
            Replacement = "/* [System.Runtime.CompilerServices.NullableAttribute((byte)1)] */"
        }
    )

    $reader = [System.IO.File]::OpenText($generatedSource)
    $writer = [System.IO.File]::CreateText($filteredSource)

    while($null -ne ($line = $reader.ReadLine()))
    {
        $lineWasProcessed = $false
        foreach ($patternToReplace in $patternsToReplace)
        {
            if ($assemblyName -in $patternToReplace.ApplyTo -and $line.Contains($patternToReplace.Pattern)) {
                $line = $line.Replace($patternToReplace.Pattern, $patternToReplace.Replacement)
                $lineWasProcessed = $true
            }
        }

        if (!$lineWasProcessed) {
            $match = Select-String -InputObject $line -Pattern $patternsToRemove -SimpleMatch
            if ($null -ne $match)
            {
                $line = "//$line"
            }
        }

        $writer.WriteLine($line)
    }

    if ($null -ne $reader)
    {
        $reader.Close()
    }

    if ($null -ne $writer)
    {
        $writer.Close()
    }

    Move-Item $filteredSource $generatedSource -Force
    Write-Log "Code cleanup complete for reference assembly '$assemblyName'."
}

<#
  Helper function for New-ReferenceAssembly to get the arguments
  for building reference assemblies.
#>
function GenerateBuildArguments
{
    param(
        [string] $AssemblyName,
        [string] $RefAssemblyVersion,
        [string] $SnkFilePath,
        [string] $SMAReferencePath
    )

    $arguments = @('build')
    $arguments += @('-c','Release')
    $arguments += "/p:RefAsmVersion=$RefAssemblyVersion"
    $arguments += "/p:SnkFile=$SnkFilePath"

    if ($AssemblyName -ne "System.Management.Automation") {
        $arguments += "/p:SmaRefFile=$SMAReferencePath"
    }

    return $arguments
}

<#
.SYNOPSIS
Create a NuGet package from a nuspec.

.DESCRIPTION
Creates a NuGet using the nuspec at the specified folder.
It is expected that the lib / ref / runtime folders are welformed.
The genereated NuGet package is copied over to the $PackageDestinationPath

.PARAMETER NuSpecPath
Path to the folder containing the nuspec file.

.PARAMETER PackageDestinationPath
Path to which NuGet package should be copied. Destination is created if it does not exist.
#>

function New-NugetPackage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $NuSpecPath,

        [Parameter(Mandatory = $true)]
        [string] $PackageDestinationPath
    )

    $nuget = Get-Command -Type Application nuget -ErrorAction SilentlyContinue

    if ($null -eq $nuget)
    {
        throw 'nuget application is not available in PATH'
    }

    Push-Location $NuSpecPath

    Start-NativeExecution { nuget pack . } > $null

    if (-not (Test-Path $PackageDestinationPath))
    {
        New-Item $PackageDestinationPath -ItemType Directory -Force > $null
    }

    Copy-Item *.nupkg $PackageDestinationPath -Force -Verbose
    Pop-Location
}

<#
.SYNOPSIS
Publish the specified Nuget Package to MyGet feed.

.DESCRIPTION
The specified nuget package is published to the powershell.myget.org/powershell-core feed.

.PARAMETER PackagePath
Path to the NuGet Package.

.PARAMETER ApiKey
API key for powershell.myget.org
#>
function Publish-NugetToMyGet
{
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackagePath,

        [Parameter(Mandatory = $true)]
        [string] $ApiKey
    )

    $nuget = Get-Command -Type Application nuget -ErrorAction SilentlyContinue

    if ($null -eq $nuget)
    {
        throw 'nuget application is not available in PATH'
    }

    Get-ChildItem $PackagePath | ForEach-Object {
        Write-Log "Pushing $_ to PowerShell Myget"
        Start-NativeExecution { nuget push $_.FullName -Source 'https://powershell.myget.org/F/powershell-core/api/v2/package' -ApiKey $ApiKey } > $null
    }
}

function New-SubFolder
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param(
        [string]
        $Path,

        [String]
        $ChildPath,

        [switch]
        $Clean
    )

    $subFolderPath = Join-Path -Path $Path -ChildPath $ChildPath
    if ($Clean.IsPresent -and (Test-Path $subFolderPath))
    {
        Remove-Item -Path $subFolderPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (!(Test-Path $subFolderPath))
    {
        $null = New-Item -Path $subFolderPath -ItemType Directory
    }
    return $subFolderPath
}

# Builds coming out of this project can have version number as 'a.b.c-stringf.d-e-f' OR 'a.b.c.d-e-f'
# This function converts the above version into semantic version major.minor[.build-quality[.revision]] format
function Get-PackageSemanticVersion
{
    [CmdletBinding()]
    param (
        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Version,
        [switch] $NuGet
        )

    Write-Verbose "Extract the semantic version in the form of major.minor[.build-quality[.revision]] for $Version"
    $packageVersionTokens = $Version.Split('.')

    if ($packageVersionTokens.Count -eq 3) {
        # In case the input is of the form a.b.c, we use the same form
        $packageSemanticVersion = $Version
    } elseif ($packageVersionTokens.Count -eq 4) {
        # We have all the four fields
        $packageRevisionTokens = ($packageVersionTokens[3].Split('-'))[0]
        if ($NuGet.IsPresent)
        {
            $packageRevisionTokens = $packageRevisionTokens.Replace('.','-')
        }
        $packageSemanticVersion = $packageVersionTokens[0],$packageVersionTokens[1],$packageVersionTokens[2],$packageRevisionTokens -join '.'
    } else {
        throw "Cannot create Semantic Version from the string $Version containing 4 or more tokens"
    }

    $packageSemanticVersion
}

# Builds coming out of this project can have version number as 'M.m.p-previewName[Number]' OR 'M.m.p'
# This function converts the above version into semantic version major.minor.patch[-previewName[Number]] format
function Get-LinuxPackageSemanticVersion
{
    [CmdletBinding()]
    param (
        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidatePattern("^\d+\.\d+\.\d+(-\w+(\.\d+)?)?$")]
        [ValidateNotNullOrEmpty()]
        [string] $Version
        )

    Write-Verbose "Extract the semantic version in the form of major.minor[.build-quality[.revision]] for $Version"
    $packageVersionTokens = $Version.Split('-')

    if ($packageVersionTokens.Count -eq 1) {
        # In case the input is of the form a.b.c, we use the same form
        $packageSemanticVersion = $Version
    } elseif ($packageVersionTokens.Count -ge 2) {
        $packageRevisionTokens = ($packageVersionTokens[1..($packageVersionTokens.Count-1)] -join '-')
        $packageSemanticVersion = ('{0}-{1}' -f  $packageVersionTokens[0], $packageRevisionTokens)
    }

    $packageSemanticVersion
}

# Builds coming out of this project can have version number as 'a.b.c-stringf.d-e-f' OR 'a.b.c.d-e-f'
# This function converts the above version into semantic version major.minor[.build-quality[-revision]] format needed for nuget
function Get-NugetSemanticVersion
{
    [CmdletBinding()]
    param (
        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Version
        )

    $packageVersionTokens = $Version.Split('.')

    Write-Verbose "Extract the semantic version in the form of major.minor[.build-quality[-revision]] for $Version"
    $versionPartTokens = @()
    $identifierPortionTokens = @()
    $inIdentifier = $false
    foreach($token in $packageVersionTokens) {
        $tokenParts = $null
        if ($token -match '-') {
            $tokenParts = $token.Split('-')
        }
        elseif ($inIdentifier) {
            $tokenParts = @($token)
        }

        # If we don't have token parts, then it's a versionPart
        if (!$tokenParts) {
            $versionPartTokens += $token
        }
        else {
            foreach($idToken in $tokenParts) {
                # The first token after we detect the id Part is still
                # a version part
                if (!$inIdentifier) {
                    $versionPartTokens += $idToken
                    $inIdentifier = $true
                }
                else {
                    $identifierPortionTokens += $idToken
                }
            }
        }
    }

    if ($versionPartTokens.Count -gt 3) {
        throw "Cannot create Semantic Version from the string $Version containing 4 or more version tokens"
    }

    $packageSemanticVersion = ($versionPartTokens -join '.')
    if ($identifierPortionTokens.Count -gt 0) {
        $packageSemanticVersion += '-' + ($identifierPortionTokens -join '-')
    }

    $packageSemanticVersion
}

# Get the paths to various WiX tools
function Get-WixPath
{
    [CmdletBinding()]
    param (
        [bool] $IsProductArchitectureArm = $false
    )

    $wixToolsetBinPath = $IsProductArchitectureArm ? "${env:ProgramFiles(x86)}\Arm Support WiX Toolset *\bin" : "${env:ProgramFiles(x86)}\WiX Toolset *\bin"

    Write-Verbose -Verbose "Ensure Wix Toolset is present on the machine @ $wixToolsetBinPath"
    if (-not (Test-Path $wixToolsetBinPath))
    {
        if (!$IsProductArchitectureArm)
        {
            throw "The latest version of Wix Toolset 3.11 is required to create MSI package. Please install it from https://github.com/wixtoolset/wix3/releases"
        }
        else {
            throw "The latest version of Wix Toolset 3.14 is required to create MSI package for arm. Please install it from https://aka.ms/ps-wix-3-14-zip"
        }
    }

    ## Get the latest if multiple versions exist.
    $wixToolsetBinPath = (Get-ChildItem $wixToolsetBinPath).FullName | Sort-Object -Descending | Select-Object -First 1

    Write-Verbose "Initialize Wix executables..."
    $wixHeatExePath = Join-Path $wixToolsetBinPath "heat.exe"
    $wixMeltExePath = Join-Path $wixToolsetBinPath "melt.exe"
    $wixTorchExePath = Join-Path $wixToolsetBinPath "torch.exe"
    $wixPyroExePath = Join-Path $wixToolsetBinPath "pyro.exe"
    $wixCandleExePath = Join-Path $wixToolsetBinPath "Candle.exe"
    $wixLightExePath = Join-Path $wixToolsetBinPath "Light.exe"
    $wixInsigniaExePath = Join-Path $wixToolsetBinPath "Insignia.exe"

    return [PSCustomObject] @{
        WixHeatExePath     = $wixHeatExePath
        WixMeltExePath     = $wixMeltExePath
        WixTorchExePath    = $wixTorchExePath
        WixPyroExePath     = $wixPyroExePath
        WixCandleExePath   = $wixCandleExePath
        WixLightExePath    = $wixLightExePath
        WixInsigniaExePath = $wixInsigniaExePath
    }
}

<#
    .Synopsis
        Creates a Windows installer MSI package and assumes that the binaries are already built using 'Start-PSBuild'.
        This only works on a Windows machine due to the usage of WiX.
    .EXAMPLE
        # This example shows how to produce a Debug-x64 installer for development purposes.
        cd $RootPathOfPowerShellRepo
        Import-Module .\build.psm1; Import-Module .\tools\packaging\packaging.psm1
        New-MSIPackage -Verbose -ProductSourcePath '.\src\powershell-win-core\bin\Debug\net8.0\win7-x64\publish' -ProductTargetArchitecture x64 -ProductVersion '1.2.3'
#>
function New-MSIPackage
{
    [CmdletBinding()]
    param (

        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $ProductName = 'PowerShell',

        # Suffix of the Name
        [string] $ProductNameSuffix,

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductVersion,

        # Source Path to the Product Files - required to package the contents into an MSI
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductSourcePath,

        # File describing the MSI Package creation semantics
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $ProductWxsPath = "$RepoRoot\assets\wix\Product.wxs",

        # File describing the MSI Package creation semantics
        [ValidateNotNullOrEmpty()]
        [ValidateScript({Test-Path $_})]
        [string] $BundleWxsPath = "$RepoRoot\assets\wix\bundle.wxs",

        # Path to Assets folder containing artifacts such as icons, images
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $AssetsPath = "$RepoRoot\assets",

        # Architecture to use when creating the MSI
        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64", "arm64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture,

        # Force overwrite of package
        [Switch] $Force,

        [string] $CurrentLocation = (Get-Location)
    )

    $wixPaths = Get-WixPath -IsProductArchitectureArm ($ProductTargetArchitecture -eq "arm64")

    $windowsNames = Get-WindowsNames -ProductName $ProductName -ProductNameSuffix $ProductNameSuffix -ProductVersion $ProductVersion
    $productSemanticVersionWithName = $windowsNames.ProductSemanticVersionWithName
    $ProductSemanticVersion = $windowsNames.ProductSemanticVersion
    $packageName = $windowsNames.PackageName
    $ProductVersion = $windowsNames.ProductVersion
    Write-Verbose "Create MSI for Product $productSemanticVersionWithName" -Verbose
    Write-Verbose "ProductSemanticVersion =  $productSemanticVersion" -Verbose
    Write-Verbose "packageName =  $packageName" -Verbose
    Write-Verbose "ProductVersion =  $ProductVersion" -Verbose

    $simpleProductVersion = [string]([Version]$ProductVersion).Major
    $isPreview = Test-IsPreview -Version $ProductSemanticVersion
    if ($isPreview)
    {
        $simpleProductVersion += '-preview'
    }

    $staging = "$PSScriptRoot/staging"
    New-StagingFolder -StagingPath $staging -PackageSourcePath $ProductSourcePath

    $assetsInSourcePath = Join-Path $staging 'assets'

    New-Item $assetsInSourcePath -type directory -Force | Write-Verbose

    Write-Verbose "Place dependencies such as icons to $assetsInSourcePath"
    Copy-Item "$AssetsPath\*.ico" $assetsInSourcePath -Force



    $fileArchitecture = 'amd64'
    $ProductProgFilesDir = "ProgramFiles64Folder"
    if ($ProductTargetArchitecture -eq "x86")
    {
        $fileArchitecture = 'x86'
        $ProductProgFilesDir = "ProgramFilesFolder"
    }
    elseif ($ProductTargetArchitecture -eq "arm64")
    {
        $fileArchitecture = 'arm64'
        $ProductProgFilesDir = "ProgramFiles64Folder"
    }

    $wixFragmentPath = Join-Path $env:Temp "Fragment.wxs"

    # cleanup any garbage on the system
    Remove-Item -ErrorAction SilentlyContinue $wixFragmentPath -Force

    $msiLocationPath = Join-Path $CurrentLocation "$packageName.msi"
    $msiPdbLocationPath = Join-Path $CurrentLocation "$packageName.wixpdb"

    if (!$Force.IsPresent -and (Test-Path -Path $msiLocationPath)) {
        Write-Error -Message "Package already exists, use -Force to overwrite, path:  $msiLocationPath" -ErrorAction Stop
    }

    Write-Log "Generating wxs file manifest..."
    $arguments = @{
        IsPreview              = $isPreview
        ProductSourcePath      = $staging
        ProductName            = $ProductName
        ProductVersion         = $ProductVersion
        SimpleProductVersion   = $simpleProductVersion
        ProductSemanticVersion = $ProductSemanticVersion
        ProductVersionWithName = $productVersionWithName
        ProductProgFilesDir    = $ProductProgFilesDir
        FileArchitecture       = $fileArchitecture
    }

    $buildArguments = New-MsiArgsArray -Argument $arguments

    Test-Bom -Path $staging -BomName windows -Architecture $ProductTargetArchitecture -Verbose
    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixHeatExePath dir $staging -dr  VersionFolder -cg ApplicationFiles -ag -sfrag -srd -scom -sreg -out $wixFragmentPath -var var.ProductSourcePath $buildArguments -v}

    Send-AzdoFile -Path $wixFragmentPath

    $wixObjFragmentPath = Join-Path $env:Temp "Fragment.wixobj"

    # cleanup any garbage on the system
    Remove-Item -ErrorAction SilentlyContinue $wixObjFragmentPath -Force

    Start-MsiBuild -WxsFile $ProductWxsPath, $wixFragmentPath -ProductTargetArchitecture $ProductTargetArchitecture -Argument $arguments -MsiLocationPath $msiLocationPath -MsiPdbLocationPath $msiPdbLocationPath

    Remove-Item -ErrorAction SilentlyContinue $wixFragmentPath -Force

    if ((Test-Path $msiLocationPath) -and (Test-Path $msiPdbLocationPath))
    {
        Write-Verbose "You can find the WixPdb @ $msiPdbLocationPath" -Verbose
        Write-Verbose "You can find the MSI @ $msiLocationPath" -Verbose
        [pscustomobject]@{
            msi=$msiLocationPath
            wixpdb=$msiPdbLocationPath
        }
    }
    else
    {
        $errorMessage = "Failed to create $msiLocationPath"
        throw $errorMessage
    }
}

function Get-WindowsNames {
    param(
        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $ProductName = 'PowerShell',

        # Suffix of the Name
        [string] $ProductNameSuffix,

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductVersion
    )

    Write-Verbose -Message "Getting Windows Names for ProductName: $ProductName; ProductNameSuffix: $ProductNameSuffix; ProductVersion: $ProductVersion" -Verbose

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $ProductVersion
    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion -IncrementBuildNumber

    $productVersionWithName = $ProductName + '_' + $ProductVersion
    $productSemanticVersionWithName = $ProductName + '-' + $ProductSemanticVersion

    $packageName = $productSemanticVersionWithName
    if ($ProductNameSuffix) {
        $packageName += "-$ProductNameSuffix"
    }

    return [PSCustomObject]@{
        PackageName                    = $packageName
        ProductVersionWithName         = $productVersionWithName
        ProductSemanticVersion         = $ProductSemanticVersion
        ProductSemanticVersionWithName = $productSemanticVersionWithName
        ProductVersion                 = $ProductVersion
    }
}

function New-ExePackage {
    param(
        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $ProductName = 'PowerShell',

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]

        [string] $ProductVersion,

        # File describing the MSI Package creation semantics
        [ValidateNotNullOrEmpty()]
        [ValidateScript({Test-Path $_})]
        [string] $BundleWxsPath = "$RepoRoot\assets\wix\bundle.wxs",

        # Architecture to use when creating the MSI
        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64", "arm64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture,

        # Location of the signed MSI
        [Parameter(Mandatory = $true)]
        [string]
        $MsiLocationPath,

        [string] $CurrentLocation = (Get-Location)
    )

    $productNameSuffix = "win-$ProductTargetArchitecture"

    $windowsNames = Get-WindowsNames -ProductName $ProductName -ProductNameSuffix $productNameSuffix -ProductVersion $ProductVersion
    $productSemanticVersionWithName = $windowsNames.ProductSemanticVersionWithName
    $packageName = $windowsNames.PackageName
    $isPreview = Test-IsPreview -Version $windowsNames.ProductSemanticVersion

    Write-Verbose "Create EXE for Product $productSemanticVersionWithName" -verbose
    Write-Verbose "packageName =  $packageName" -Verbose

    $exeLocationPath = Join-Path $CurrentLocation "$packageName.exe"
    $exePdbLocationPath = Join-Path $CurrentLocation "$packageName.exe.wixpdb"
    $windowsVersion = Get-WindowsVersion -packageName $packageName

    Start-MsiBuild -WxsFile $BundleWxsPath -ProductTargetArchitecture $ProductTargetArchitecture -Argument @{
        IsPreview      = $isPreview
        TargetPath     = $MsiLocationPath
        WindowsVersion = $windowsVersion
    }  -MsiLocationPath $exeLocationPath -MsiPdbLocationPath $exePdbLocationPath

    return $exeLocationPath
}

<#
Allows you to extract the engine of exe package, mainly for signing
Any existing signature will be removed.
 #>
function Expand-ExePackageEngine {
    param(
        # Location of the unsigned EXE
        [Parameter(Mandatory = $true)]
        [string]
        $ExePath,

        # Location to put the expanded engine.
        [Parameter(Mandatory = $true)]
        [string]
        $EnginePath,

        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64", "arm64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture
    )

    <#
    2. detach the engine from TestInstaller.exe:
    insignia -ib TestInstaller.exe -o engine.exe
    #>

    $wixPaths = Get-WixPath -IsProductArchitectureArm ($ProductTargetArchitecture -eq "arm64")

    $resolvedExePath = (Resolve-Path -Path $ExePath).ProviderPath
    $resolvedEnginePath = [System.IO.Path]::GetFullPath($EnginePath)

    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixInsigniaExePath -ib $resolvedExePath -o $resolvedEnginePath}
}

<#
Allows you to replace the engine (installer) in the exe package.
Used to replace the engine with a signed version
#>
function Compress-ExePackageEngine {
    param(
        # Location of the unsigned EXE
        [Parameter(Mandatory = $true)]
        [string]
        $ExePath,

        # Location of the signed engine
        [Parameter(Mandatory = $true)]
        [string]
        $EnginePath,

        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64", "arm64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture
    )


    <#
    4. re-attach the signed engine.exe to the bundle:
    insignia -ab engine.exe TestInstaller.exe -o TestInstaller.exe
    #>

    $wixPaths = Get-WixPath -IsProductArchitectureArm ($ProductTargetArchitecture -eq "arm64")

    $resolvedEnginePath = (Resolve-Path -Path $EnginePath).ProviderPath
    $resolvedExePath = (Resolve-Path -Path $ExePath).ProviderPath

    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixInsigniaExePath -ab $resolvedEnginePath $resolvedExePath -o $resolvedExePath}
}

function New-MsiArgsArray {
    param(
        [Parameter(Mandatory)]
        [Hashtable]$Argument
    )

    $buildArguments = @()
    foreach ($key in $Argument.Keys) {
        $buildArguments += "-d$key=$($Argument.$key)"
    }

    return $buildArguments
}

function Start-MsiBuild {
    param(
        [string[]] $WxsFile,
        [string[]] $Extension = @('WixUIExtension', 'WixUtilExtension', 'WixBalExtension'),
        [string] $ProductTargetArchitecture,
        [Hashtable] $Argument,
        [string] $MsiLocationPath,
        [string] $MsiPdbLocationPath
    )

    $outDir = $env:Temp

    $wixPaths = Get-WixPath -IsProductArchitectureArm ($ProductTargetArchitecture -eq "arm64")

    $extensionArgs = @()
    foreach ($extensionName in $Extension) {
        $extensionArgs += '-ext'
        $extensionArgs += $extensionName
    }

    $buildArguments = New-MsiArgsArray -Argument $Argument

    $objectPaths = @()
    foreach ($file in $WxsFile) {
        $fileName = [system.io.path]::GetFileNameWithoutExtension($file)
        $objectPaths += Join-Path $outDir -ChildPath "${filename}.wixobj"
    }

    foreach ($file in $objectPaths) {
        Remove-Item -ErrorAction SilentlyContinue $file -Force
        Remove-Item -ErrorAction SilentlyContinue $file -Force
    }

    $resolvedWxsFiles = @()
    foreach ($file in $WxsFile) {
        $resolvedWxsFiles += (Resolve-Path -Path $file).ProviderPath
    }

    Write-Verbose "$resolvedWxsFiles" -Verbose

    Write-Log "running candle..."
    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixCandleExePath $resolvedWxsFiles -out "$outDir\\" $extensionArgs -arch $ProductTargetArchitecture $buildArguments -v}

    Write-Log "running light..."
    # suppress ICE61, because we allow same version upgrades
    # suppress ICE57, this suppresses an error caused by our shortcut not being installed per user
    # suppress ICE40, REINSTALLMODE is defined in the Property table.
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixLightExePath -sice:ICE61 -sice:ICE40 -sice:ICE57 -out $msiLocationPath -pdbout $msiPdbLocationPath $objectPaths $extensionArgs }

    foreach($file in $objectPaths)
    {
        Remove-Item -ErrorAction SilentlyContinue $file -Force
        Remove-Item -ErrorAction SilentlyContinue $file -Force
    }
}

<#
    .Synopsis
        Creates a Windows AppX MSIX package and assumes that the binaries are already built using 'Start-PSBuild'.
        This only works on a Windows machine due to the usage of makeappx.exe.
    .EXAMPLE
        # This example shows how to produce a Debug-x64 installer for development purposes.
        cd $RootPathOfPowerShellRepo
        Import-Module .\build.psm1; Import-Module .\tools\packaging\packaging.psm1
        New-MSIXPackage -Verbose -ProductSourcePath '.\src\powershell-win-core\bin\Debug\net8.0\win7-x64\publish' -ProductTargetArchitecture x64 -ProductVersion '1.2.3'
#>
function New-MSIXPackage
{
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact='Low')]
    param (

        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $ProductName = 'PowerShell',

        # Suffix of the Name
        [string] $ProductNameSuffix,

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductVersion,

        # Source Path to the Product Files - required to package the contents into an MSIX
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductSourcePath,

        # Processor Architecture
        [Parameter(Mandatory = $true)]
        [ValidateSet('x64','x86','arm','arm64')]
        [string] $Architecture,

        # Produce private package for testing in Store
        [Switch] $Private,

        # Produce LTS package
        [Switch] $LTS,

        # Force overwrite of package
        [Switch] $Force,

        [string] $CurrentLocation = (Get-Location)
    )

    $makeappx = Get-Command makeappx -CommandType Application -ErrorAction Ignore
    if ($null -eq $makeappx) {
        # This is location in our dockerfile
        $dockerPath = Join-Path $env:SystemDrive "makeappx"
        if (Test-Path $dockerPath) {
            $makeappx = Get-ChildItem $dockerPath -Include makeappx.exe -Recurse | Select-Object -First 1
        }

        if ($null -eq $makeappx) {
            # Try to find in well known location
            $makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64" -Include makeappx.exe -Recurse | Select-Object -First 1
            if ($null -eq $makeappx) {
                throw "Could not locate makeappx.exe, make sure Windows 10 SDK is installed"
            }
        }
    }

    $makepri = Get-Item (Join-Path $makeappx.Directory "makepri.exe") -ErrorAction Stop

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $ProductVersion
    $productSemanticVersionWithName = $ProductName + '-' + $ProductSemanticVersion
    $packageName = $productSemanticVersionWithName
    if ($Private) {
        $ProductNameSuffix = 'Private'
    }

    if ($ProductNameSuffix) {
        $packageName += "-$ProductNameSuffix"
    }

    $displayName = $productName

    if ($Private) {
        $ProductName = 'PowerShell-Private'
        $displayName = 'PowerShell-Private'
    } elseif ($ProductSemanticVersion.Contains('-')) {
        $ProductName += 'Preview'
        $displayName += ' Preview'
    } elseif ($LTS) {
        $ProductName += '-LTS'
        $displayName += '-LTS'
    }

    Write-Verbose -Verbose "ProductName: $productName"
    Write-Verbose -Verbose "DisplayName: $displayName"

    $ProductVersion = Get-WindowsVersion -PackageName $packageName

    # Any app that is submitted to the Store must have a PhoneIdentity in its appxmanifest.
    # If you submit a package without this information to the Store, the Store will silently modify your package to include it.
    # To find the PhoneProductId value, you need to run a package through the Store certification process,
    # and use the PhoneProductId value from the Store certified package to update the manifest in your source code.
    # This is the PhoneProductId for the "Microsoft.PowerShell" package.
    $PhoneProductId = "5b3ae196-2df7-446e-8060-94b4ad878387"

    $isPreview = Test-IsPreview -Version $ProductSemanticVersion
    if ($isPreview) {
        # This is the PhoneProductId for the "Microsoft.PowerShellPreview" package.
        $PhoneProductId = "67859fd2-b02a-45be-8fb5-62c569a3e8bf"
        Write-Verbose "Using Preview assets" -Verbose
    } elseif ($LTS) {
        # This is the PhoneProductId for the "Microsoft.PowerShell-LTS" package.
        $PhoneProductId = "a9af273a-c636-47ac-bc2a-775edf80b2b9"
        Write-Verbose "Using LTS assets" -Verbose
    }

    # Appx manifest needs to be in root of source path, but the embedded version needs to be updated
    # cp-459155 is 'CN=Microsoft Windows Store Publisher (Store EKU), O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
    # authenticodeFormer is 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
    $releasePublisher = 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'

    $appxManifest = Get-Content "$RepoRoot\assets\AppxManifest.xml" -Raw
    $appxManifest = $appxManifest.
        Replace('$VERSION$', $ProductVersion).
        Replace('$ARCH$', $Architecture).
        Replace('$PRODUCTNAME$', $productName).
        Replace('$DISPLAYNAME$', $displayName).
        Replace('$PUBLISHER$', $releasePublisher).
        Replace('$PHONEPRODUCTID$', $PhoneProductId)

    $xml = [xml]$appxManifest
    if ($isPreview) {
        Write-Verbose -Verbose "Adding pwsh-preview.exe alias"
        $aliasNode = $xml.Package.Applications.Application.Extensions.Extension.AppExecutionAlias.ExecutionAlias.Clone()
        $aliasNode.alias = "pwsh-preview.exe"
        $xml.Package.Applications.Application.Extensions.Extension.AppExecutionAlias.AppendChild($aliasNode) | Out-Null
    }
    $xml.Save("$ProductSourcePath\AppxManifest.xml")

    # Necessary image assets need to be in source assets folder
    $assets = @(
        'Square150x150Logo'
        'Square44x44Logo'
        'Square44x44Logo.targetsize-48'
        'Square44x44Logo.targetsize-48_altform-unplated'
        'StoreLogo'
    )

    if (!(Test-Path "$ProductSourcePath\assets")) {
        $null = New-Item -ItemType Directory -Path "$ProductSourcePath\assets"
    }

    $assets | ForEach-Object {
        if ($isPreview) {
            Copy-Item -Path "$RepoRoot\assets\$_-Preview.png" -Destination "$ProductSourcePath\assets\$_.png"
        }
        else {
            Copy-Item -Path "$RepoRoot\assets\$_.png" -Destination "$ProductSourcePath\assets\"
        }

    }

    if ($PSCmdlet.ShouldProcess("Create .msix package?")) {
        Write-Verbose "Creating priconfig.xml" -Verbose
        Start-NativeExecution -VerboseOutputOnError { & $makepri createconfig /o /cf (Join-Path $ProductSourcePath "priconfig.xml") /dq en-US }
        Write-Verbose "Creating resources.pri" -Verbose
        Push-Location $ProductSourcePath
        Start-NativeExecution -VerboseOutputOnError { & $makepri new /v /o /pr $ProductSourcePath /cf (Join-Path $ProductSourcePath "priconfig.xml") }
        Pop-Location
        Write-Verbose "Creating msix package" -Verbose
        Start-NativeExecution -VerboseOutputOnError { & $makeappx pack /o /v /h SHA256 /d $ProductSourcePath /p (Join-Path -Path $CurrentLocation -ChildPath "$packageName.msix") }
        Write-Verbose "Created $packageName.msix" -Verbose
    }
}

# Removes a ComponentRef node in the files.wxs Xml Doc
function Remove-ComponentRefNode
{
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument]
        $XmlDoc,
        [Parameter(Mandatory)]
        [System.Xml.XmlNamespaceManager]
        $XmlNsManager,
        [Parameter(Mandatory)]
        [string]
        $Id
    )

    $compRefXPath = '//Wix:ComponentRef[@Id="{0}"]' -f $Id
    $node = Get-XmlNodeByXPath -XmlDoc $XmlDoc -XmlNsManager $XmlNsManager -XPath $compRefXPath
    if ($node)
    {
        Remove-XmlElement -element $node
    }
    else
    {
        Write-Warning "could not remove node"
    }
}

# Get the ComponentGroup node in the files.wxs Xml Doc
function Get-ComponentGroupNode
{
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument]
        $XmlDoc,
        [Parameter(Mandatory)]
        [System.Xml.XmlNamespaceManager]
        $XmlNsManager
    )

    if (!$XmlNsManager.HasNamespace('Wix'))
    {
        throw 'Namespace manager must have "wix" defined.'
    }

    $compGroupXPath = '//Wix:ComponentGroup'
    $node = Get-XmlNodeByXPath -XmlDoc $XmlDoc -XmlNsManager $XmlNsManager -XPath $compGroupXPath
    return $node
}

# Gets the Directory Node the files.wxs Xml Doc
# Creates it if it does not exist
function Get-DirectoryNode
{
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlElement]
        $Node,
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument]
        $XmlDoc,
        [Parameter(Mandatory)]
        [System.Xml.XmlNamespaceManager]
        $XmlNsManager
    )

    if (!$XmlNsManager.HasNamespace('Wix'))
    {
        throw 'Namespace manager must have "wix" defined.'
    }

    $pathStack =  [System.Collections.Stack]::new()

    [System.Xml.XmlElement] $dirNode = $Node.ParentNode.ParentNode
    $dirNodeType = $dirNode.LocalName
    if ($dirNodeType -eq 'DirectoryRef')
    {
        return Get-XmlNodeByXPath -XmlDoc $XmlDoc -XmlNsManager $XmlNsManager -XPath "//Wix:DirectoryRef"
    }
    if ($dirNodeType -eq 'Directory')
    {
        while($dirNode.LocalName -eq 'Directory') {
            $pathStack.Push($dirNode.Name)
            $dirNode = $dirNode.ParentNode
        }
        $path = "//"
        [System.Xml.XmlElement] $lastNode = $null
        while($pathStack.Count -gt 0){
            $dirName = $pathStack.Pop()
            $path += 'Wix:Directory[@Name="{0}"]' -f $dirName
            $node = Get-XmlNodeByXPath -XmlDoc $XmlDoc -XmlNsManager $XmlNsManager -XPath $path

            if (!$node)
            {
                if (!$lastNode)
                {
                    # Inserting at the root
                    $lastNode = Get-XmlNodeByXPath -XmlDoc $XmlDoc -XmlNsManager $XmlNsManager -XPath "//Wix:DirectoryRef"
                }

                $newDirectory = New-XmlElement -XmlDoc $XmlDoc -LocalName 'Directory' -Node $lastNode -PassThru -NamespaceUri 'http://schemas.microsoft.com/wix/2006/wi'
                New-XmlAttribute -XmlDoc $XmlDoc -Element $newDirectory -Name 'Name' -Value $dirName
                New-XmlAttribute -XmlDoc $XmlDoc -Element $newDirectory -Name 'Id' -Value (New-WixId -Prefix 'dir')
                $lastNode = $newDirectory
            }
            else
            {
                $lastNode = $node
            }
            if ($pathStack.Count -gt 0)
            {
                $path += '/'
            }
        }
        return $lastNode
    }

    throw "unknown element type: $dirNodeType"
}

# Creates a new Wix Id in the proper format
function New-WixId
{
    param(
        [Parameter(Mandatory)]
        [string]
        $Prefix
    )

    $guidPortion = (New-Guid).Guid.ToUpperInvariant() -replace '\-' ,''
    "$Prefix$guidPortion"
}

function Get-WindowsVersion {
    param (
        [parameter(Mandatory)]
        [string]$PackageName
    )

    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion
    if (([Version]$ProductVersion).Revision -eq -1) {
        $ProductVersion += ".0"
    }

    # The Store requires the last digit of the version to be 0 so we swap the build and revision
    # This only affects Preview versions where the last digit is the preview number
    # For stable versions, the last digit is already zero so no changes
    $pversion = [version]$ProductVersion
    if ($pversion.Revision -ne 0) {
        $revision = $pversion.Revision
        if ($packageName.Contains('-rc')) {
            # For Release Candidates, we use numbers in the 100 range
            $revision += 100
        }

        $pversion = [version]::new($pversion.Major, $pversion.Minor, $revision, 0)
        $ProductVersion = $pversion.ToString()
    }

    Write-Verbose "Version: $productversion" -Verbose
    return $productversion
}

# Builds coming out of this project can have version number as 'a.b.c' OR 'a.b.c-d-f'
# This function converts the above version into major.minor[.build[.revision]] format
function Get-PackageVersionAsMajorMinorBuildRevision
{
    [CmdletBinding()]
    param (
        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Version,
        [switch] $IncrementBuildNumber
    )

    Write-Verbose "Extract the version in the form of major.minor[.build[.revision]] for $Version"
    $packageVersionTokens = $Version.Split('-')
    $packageVersion = ([regex]::matches($Version, "\d+(\.\d+)+"))[0].value

    if (1 -eq $packageVersionTokens.Count -and ([Version]$packageVersion).Revision -eq -1) {
        # In case the input is of the form a.b.c, add a '0' at the end for revision field
        $packageVersion = $packageVersion + '.0'
    } elseif (1 -lt $packageVersionTokens.Count) {
        # We have all the four fields
        $packageBuildTokens = ([regex]::Matches($packageVersionTokens[1], "\d+"))[0].value

        if ($packageBuildTokens)
        {
            if($packageBuildTokens.length -gt 4)
            {
                # MSIX will fail if it is more characters
                $packageBuildTokens = $packageBuildTokens.Substring(0,4)
            }

            if ($packageVersionTokens[1] -match 'rc' -and $IncrementBuildNumber) {
                $packageBuildTokens = [int]$packageBuildTokens + 100
            }

            $packageVersion = $packageVersion + '.' + $packageBuildTokens
        }
        else
        {
            $packageVersion = $packageVersion
        }
    }

    $packageVersion
}

<#
.SYNOPSIS
Create a smaller framework dependent package based off fxdependent package for dotnet-sdk container images.

.PARAMETER Path
Path to the folder containing the fxdependent package.

.PARAMETER KeepWindowsRuntimes
Specify this switch if the Windows runtimes are to be kept.
#>
function ReduceFxDependentPackage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [switch] $KeepWindowsRuntimes
    )

    if (-not (Test-Path $path))
    {
        throw "Path not found: $Path"
    }

    ## Remove unnecessary files
    $localeFolderToRemove = 'cs', 'de', 'es', 'fr', 'it', 'ja', 'ko', 'pl', 'pt-BR', 'ru', 'tr', 'zh-Hans', 'zh-Hant'
    Get-ChildItem $Path -Recurse -Directory | Where-Object { $_.Name -in $localeFolderToRemove } | ForEach-Object { Remove-Item $_.FullName -Force -Recurse -Verbose }

    Write-Log -message "Starting to cleanup runtime folders"

    $runtimeFolder = Get-ChildItem $Path -Recurse -Directory -Filter 'runtimes'

    $runtimeFolderPath = $runtimeFolder | Out-String

    if ($runtimeFolder.Count -eq 0)
    {
        Write-Log -message "No runtimes folder found under $Path. Completing cleanup."
        return
    }

    Write-Log -message $runtimeFolderPath

    if ($runtimeFolder.Count -eq 0)
    {
        throw "runtimes folder not found under $Path"
    }

    Write-Log -message (Get-ChildItem $Path | Out-String)

    # donet SDK container image microsoft/dotnet:2.2-sdk supports the following:
    # win10-x64 (Nano Server)
    # win-arm (Nano Server)
    # win-x64 to get PowerShell.Native components
    # linux-musl-x64 (Alpine 3.8)
    # linux-x64 (bionic / stretch)
    # unix, linux, win for dependencies
    # linux-arm and linux-arm64 for arm containers
    # osx to run global tool on macOS
    $runtimesToKeep = if ($KeepWindowsRuntimes) {
        'win10-x64', 'win-arm', 'win-x64', 'win'
    } else {
        'linux-x64', 'linux-musl-x64', 'unix', 'linux', 'linux-arm', 'linux-arm64', 'osx'
    }

    $runtimeFolder | ForEach-Object {
        Get-ChildItem -Path $_.FullName -Directory -Exclude $runtimesToKeep | Remove-Item -Force -Recurse -Verbose
    }

    ## Remove the shim layer assemblies
    Get-ChildItem -Path $Path -Filter "Microsoft.PowerShell.GlobalTool.Shim.*" | Remove-Item -Verbose
}

<#
.SYNOPSIS
Performs clean up work for preparation to running New-GlobalToolNupkgSource package source creation.

.DESCRIPTION
Unnecessary package source files are removed.

.PARAMETER LinuxBinPath
Path to the folder containing the fxdependent package for Linux.

.PARAMETER WindowsBinPath
Path to the folder containing the fxdependent package for Windows.

.PARAMETER WindowsDesktopBinPath
Path to the folder containing desktop framework package for Windows.
#>
function Start-PrepForGlobalToolNupkg
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $LinuxBinPath,
        [Parameter(Mandatory)] [string] $WindowsBinPath,
        [Parameter(Mandatory)] [string] $WindowsDesktopBinPath,
        [Parameter(Mandatory)] [string] $AlpineBinPath
    )

    Write-Log "Start-PrepForGlobalToolNupkg: Running clean up for New-GlobalToolNupkg package creation."

    $libCryptoPath = Join-Path $LinuxBinPath 'libcrypto.so.1.0.0'
    $libSSLPath = Join-Path $LinuxBinPath 'libssl.so.1.0.0'

    if (Test-Path $libCryptoPath) {
        Remove-Item -Path $libCryptoPath -Verbose -Force
    }

    if (Test-Path $libSSLPath) {
        Remove-Item -Path $libSSLPath -Verbose -Force
    }

    # Remove unnecessary xml files
    Get-ChildItem -Path $LinuxBinPath, $WindowsBinPath, $WindowsDesktopBinPath, $AlpineBinPath -Filter *.xml | Remove-Item -Verbose
}

<#
.SYNOPSIS
Create a single PowerShell Global tool nuget package NuSpec source directory for the provied
package type.

.DESCRIPTION
A single NuSpec source directory is created for the individual package type, and the created
directory path is set to the environement variable name: "GlobaToolNuSpecSourcePath_${PackageType}".

.PARAMETER PackageType
Global tool package type to create.

.PARAMETER LinuxBinPath
Path to the folder containing the fxdependent package for Linux.

.PARAMETER WindowsBinPath
Path to the folder containing the fxdependent package for Windows.

.PARAMETER WindowsDesktopBinPath
Path to the folder containing desktop framework package for Windows.

.PARAMETER PackageVersion
Version for the NuGet package that will be generated.
#>
function New-GlobalToolNupkgSource
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $PackageType,
        [Parameter(Mandatory)] [string] $LinuxBinPath,
        [Parameter(Mandatory)] [string] $WindowsBinPath,
        [Parameter(Mandatory)] [string] $WindowsDesktopBinPath,
        [Parameter(Mandatory)] [string] $AlpineBinPath,
        [Parameter(Mandatory)] [string] $PackageVersion,
        [Parameter()] [switch] $SkipCGManifest
    )

    if ($PackageType -ne "Unified")
    {
        Write-Log "New-GlobalToolNupkgSource: Reducing package size for non-unified packages."

        Write-Log "New-GlobalToolNupkgSource: Reducing size of Linux package"
        ReduceFxDependentPackage -Path $LinuxBinPath

        Write-Log "New-GlobalToolNupkgSource: Reducing size of Alpine package"
        ReduceFxDependentPackage -Path $AlpineBinPath

        Write-Log "New-GlobalToolNupkgSource: Reducing size of Windows package"
        ReduceFxDependentPackage -Path $WindowsBinPath -KeepWindowsRuntimes

        Write-Log "New-GlobalToolNupkgSource: Reducing size of WindowsDesktop package"
        ReduceFxDependentPackage -Path $WindowsDesktopBinPath -KeepWindowsRuntimes
    }

    Write-Log "New-GlobalToolNupkgSource: Creating package: $PackageType"

    switch ($PackageType)
    {
        "Unified"
        {
            $ShimDllPath = Join-Path $WindowsDesktopBinPath "Microsoft.PowerShell.GlobalTool.Shim.dll"

            $PackageName = "PowerShell"
            $RootFolder = New-TempFolder

            Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

            $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory
            $winFolder = New-Item (Join-Path $ridFolder "win") -ItemType Directory
            $unixFolder = New-Item (Join-Path $ridFolder "unix") -ItemType Directory

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $WindowsDesktopBinPath"
            Copy-Item "$WindowsDesktopBinPath\*" -Destination $winFolder -Recurse

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $LinuxBinPath"
            Copy-Item "$LinuxBinPath\*" -Destination $unixFolder -Recurse

            Write-Log "New-GlobalToolNupkgSource: Copying shim dll from $ShimDllPath"
            Copy-Item $ShimDllPath -Destination $ridFolder

            $shimConfigFile = Join-Path (Split-Path $ShimDllPath -Parent) 'Microsoft.PowerShell.GlobalTool.Shim.runtimeconfig.json'
            Write-Log "New-GlobalToolNupkgSource: Copying shim config file from $shimConfigFile"
            Copy-Item $shimConfigFile -Destination $ridFolder -ErrorAction Stop

            $toolSettings = $packagingStrings.GlobalToolSettingsFile -f (Split-Path $ShimDllPath -Leaf)
        }

        "PowerShell.Linux.Alpine"
        {
            $PackageName = "PowerShell.Linux.Alpine"
            $RootFolder = New-TempFolder

            Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

            $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $AlpineBinPath for $PackageType"
            Copy-Item "$AlpineBinPath/*" -Destination $ridFolder -Recurse
            $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
        }

        "PowerShell.Linux.x64"
        {
            $PackageName = "PowerShell.Linux.x64"
            $RootFolder = New-TempFolder

            Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

            $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $LinuxBinPath for $PackageType"
            Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
            Remove-Item -Path $ridFolder/runtimes/linux-arm -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/linux-arm64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/linux-musl-x64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
            $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
        }

        "PowerShell.Linux.arm32"
        {
            $PackageName = "PowerShell.Linux.arm32"
            $RootFolder = New-TempFolder

            Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

            $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $LinuxBinPath for $PackageType"
            Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
            Remove-Item -Path $ridFolder/runtimes/linux-arm64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/linux-musl-x64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/linux-x64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
            $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
        }

        "PowerShell.Linux.arm64"
        {
            $PackageName = "PowerShell.Linux.arm64"
            $RootFolder = New-TempFolder

            Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

            $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $LinuxBinPath for $PackageType"
            Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
            Remove-Item -Path $ridFolder/runtimes/linux-arm -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/linux-musl-x64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/linux-x64 -Recurse -Force
            Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
            $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
        }

        # Due to needing a signed shim for the global tool, we build the global tool in build instead of packaging.
        # keeping the code for reference.
        # "PowerShell.Windows.x64"
        # {
        #     $PackageName = "PowerShell.Windows.x64"
        #     $RootFolder = New-TempFolder

        #     Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

        #     $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

        #     Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $WindowsDesktopBinPath for $PackageType"
        #     Copy-Item "$WindowsDesktopBinPath/*" -Destination $ridFolder -Recurse
        #     Remove-Item -Path $ridFolder/runtimes/win-arm -Recurse -Force
        #     $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
        # }

        "PowerShell.Windows.arm32"
        {
            $PackageName = "PowerShell.Windows.arm32"
            $RootFolder = New-TempFolder

            Copy-Item -Path $iconPath -Destination "$RootFolder/$iconFileName" -Verbose

            $ridFolder = New-Item -Path (Join-Path $RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

            Write-Log "New-GlobalToolNupkgSource: Copying runtime assemblies from $WindowsBinPath for $PackageType"
            Copy-Item "$WindowsBinPath/*" -Destination $ridFolder -Recurse
            Remove-Item -Path $ridFolder/runtimes/win-x64 -Recurse -Force
            $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
        }

        default { throw "New-GlobalToolNupkgSource: Unknown package type: $PackageType" }
    }

    $nuSpec = $packagingStrings.GlobalToolNuSpec -f $PackageName, $PackageVersion, $iconFileName
    $nuSpec | Out-File -FilePath (Join-Path $RootFolder "$PackageName.nuspec") -Encoding ascii
    $toolSettings | Out-File -FilePath (Join-Path $ridFolder "DotnetToolSettings.xml") -Encoding ascii

    # Source created.
    Write-Log "New-GlobalToolNupkgSource: Global tool package ($PackageName) source created at: $RootFolder"

    # Set VSTS environment variable for package NuSpec source path.
    $pkgNuSpecSourcePathVar = "GlobalToolNuSpecSourcePath"
    Write-Log "New-GlobalToolNupkgSource: Creating NuSpec source path VSTS variable: $pkgNuSpecSourcePathVar"
    Write-Verbose -Verbose "sending: [task.setvariable variable=$pkgNuSpecSourcePathVar]$RootFolder"
    Write-Host "##vso[task.setvariable variable=$pkgNuSpecSourcePathVar]$RootFolder"
    $global:GlobalToolNuSpecSourcePath = $RootFolder

    # Set VSTS environment variable for package Name.
    $pkgNameVar = "GlobalToolPkgName"
    Write-Log "New-GlobalToolNupkgSource: Creating current package name variable: $pkgNameVar"
    Write-Verbose -Verbose "sending: vso[task.setvariable variable=$pkgNameVar]$PackageName"
    Write-Host "##vso[task.setvariable variable=$pkgNameVar]$PackageName"
    $global:GlobalToolPkgName = $PackageName

    if ($SkipCGManifest.IsPresent) {
        Write-Verbose -Verbose "New-GlobalToolNupkgSource: Skipping CGManifest creation."
        return
    }

    # Set VSTS environment variable for CGManifest file path.
    $globalToolCGManifestPFilePath = Join-Path -Path "$env:REPOROOT" -ChildPath "tools\cgmanifest.json"
    $globalToolCGManifestFilePath = Resolve-Path -Path $globalToolCGManifestPFilePath -ErrorAction SilentlyContinue
    if (($null -eq $globalToolCGManifestFilePath) -or (! (Test-Path -Path $globalToolCGManifestFilePath)))
    {
        throw "New-GlobalToolNupkgSource: Invalid build source CGManifest file path: $globalToolCGManifestPFilePath"
    }
    $globalToolCGManifestSourceRoot = New-TempFolder
    Write-Log "New-GlobalToolNupkgSource: Creating new CGManifest.json file at: $globalToolCGManifestSourceRoot"
    Copy-Item -Path $globalToolCGManifestFilePath -Destination $globalToolCGManifestSourceRoot -Force

    $globalToolCGManifestPathVar = "GlobalToolCGManifestPath"
    Write-Log "New-GlobalToolNupkgSource: Creating CGManifest path variable, $globalToolCGManifestPathVar, for path: $globalToolCGManifestSourceRoot"
    Write-Host "##vso[task.setvariable variable=$globalToolCGManifestPathVar]$globalToolCGManifestSourceRoot"
}

<#
.SYNOPSIS
Create a single PowerShell Global tool nuget package from the provied package source folder.

.DESCRIPTION
Creates a single PowerShell Global tool nuget package based on the provided package NuSpec source
folder (created by New-GlobalNupkgSource), and places the created package in the provided destination
folder.

.PARAMETER PackageNuSpecPath
Location of NuSpec path containing source for package creation.

.PARAMETER PackageName
Name of Global Tool package being created.

.PARAMETER DestinationPath
Path to the folder where the generated package is placed.
#>
function New-GlobalToolNupkgFromSource
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)] [string] $PackageNuSpecPath,
        [Parameter(Mandatory)] [string] $PackageName,
        [Parameter(Mandatory)] [string] $DestinationPath,
        [Parameter()] [string] $CGManifestPath
    )

    if (! (Test-Path -Path $PackageNuSpecPath))
    {
        throw "New-GlobalToolNupkgFromSource: failed because NuSpec path does not exist: $PackageNuSpecPath"
    }

    Write-Log "New-GlobalToolNupkgFromSource: Creating package: $PackageName"
    New-NugetPackage -NuSpecPath $PackageNuSpecPath -PackageDestinationPath $DestinationPath

    Write-Log "New-GlobalToolNupkgFromSource: Removing GlobalTool NuSpec source directory: $PackageNuSpecPath"
    Remove-Item -Path $PackageNuSpecPath -Recurse -Force -ErrorAction SilentlyContinue

    if (-not ($PSBoundParameters.ContainsKey('CGManifestPath')))
    {
        Write-Verbose -Verbose "New-GlobalToolNupkgFromSource: CGManifest file path not provided."
        return
    }

    Write-Log "New-GlobalToolNupkgFromSource: Removing GlobalTool CGManifest source directory: $CGManifestPath"
    if (! (Test-Path -Path $CGManifestPath))
    {
        Write-Verbose -Verbose -Message "New-GlobalToolNupkgFromSource: CGManifest file does not exist: $CGManifestPath"
        return
    }
    Remove-Item -Path $CGManifestPath -Recurse -Force -ErrorAction SilentlyContinue
}

${mainLinuxBuildFolder} = 'pwshLinuxBuild'
${minSizeLinuxBuildFolder} = 'pwshLinuxBuildMinSize'
${arm32LinuxBuildFolder} = 'pwshLinuxBuildArm32'
${arm64LinuxBuildFolder} = 'pwshLinuxBuildArm64'
${amd64MarinerBuildFolder} = 'pwshMarinerBuildAmd64'
${amd64AlpineFxdBuildFolder} = 'pwshAlpineFxdBuildAmd64'
${arm64MarinerBuildFolder} = 'pwshMarinerBuildArm64'

<#
    Used in Azure DevOps Yaml to package all the linux packages for a channel.
#>
function Invoke-AzDevOpsLinuxPackageCreation {
    param(
        [switch]
        $LTS,

        [Parameter(Mandatory)]
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag,

        [Parameter(Mandatory)]
        [ValidateSet('fxdependent', 'alpine', 'deb', 'rpm')]
        [String]$BuildType
    )

    if (!${env:SYSTEM_ARTIFACTSDIRECTORY}) {
        throw "Must be run in Azure DevOps"
    }

    try {
        Write-Verbose "Packaging '$BuildType'; LTS:$LTS for $ReleaseTag ..." -Verbose

        Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}-meta\psoptions.json"

        $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }

        switch ($BuildType) {
            'fxdependent' {
                $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}-meta\linuxFilePermission.json"
                Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}"
                Start-PSPackage -Type 'fxdependent' @releaseTagParam -LTS:$LTS
            }
            'alpine' {
                $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}-meta\linuxFilePermission.json"
                Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}"
                Start-PSPackage -Type 'tar-alpine' @releaseTagParam -LTS:$LTS
            }
            'rpm' {
                Start-PSPackage -Type 'rpm' @releaseTagParam -LTS:$LTS
            }
            default {
                $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}-meta\linuxFilePermission.json"
                Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${mainLinuxBuildFolder}"
                Start-PSPackage @releaseTagParam -LTS:$LTS -Type 'deb', 'tar'
            }
        }

        if ($BuildType -eq 'deb') {
            Start-PSPackage -Type tar @releaseTagParam -LTS:$LTS

            Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${minSizeLinuxBuildFolder}-meta\psoptions.json"

            $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${minSizeLinuxBuildFolder}-meta\linuxFilePermission.json"
            Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${minSizeLinuxBuildFolder}"

            Write-Verbose -Verbose "---- Min-Size ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"

            Start-PSPackage -Type min-size @releaseTagParam -LTS:$LTS

            ## Create 'linux-arm' 'tar.gz' package.
            ## Note that 'linux-arm' can only be built on Ubuntu environment.
            Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm32LinuxBuildFolder}-meta\psoptions.json"
            $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm32LinuxBuildFolder}-meta\linuxFilePermission.json"
            Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm32LinuxBuildFolder}"
            Start-PSPackage -Type tar-arm @releaseTagParam -LTS:$LTS

            ## Create 'linux-arm64' 'tar.gz' package.
            ## Note that 'linux-arm64' can only be built on Ubuntu environment.
            Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm64LinuxBuildFolder}-meta\psoptions.json"
            $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm64LinuxBuildFolder}-meta\linuxFilePermission.json"
            Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm64LinuxBuildFolder}"
            Start-PSPackage -Type tar-arm64 @releaseTagParam -LTS:$LTS
        } elseif ($BuildType -eq 'rpm') {
            # Generate mariner amd64 package
            Write-Verbose -Verbose "Generating mariner amd64 package"
            Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${amd64MarinerBuildFolder}-meta\psoptions.json"
            $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${amd64MarinerBuildFolder}-meta\linuxFilePermission.json"
            Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${amd64MarinerBuildFolder}"

            Write-Verbose -Verbose "---- rpm-fxdependent ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"

            Start-PSPackage -Type rpm-fxdependent @releaseTagParam -LTS:$LTS

            # Generate mariner arm64 package
            Write-Verbose -Verbose "Generating mariner arm64 package"
            Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm64MarinerBuildFolder}-meta\psoptions.json"
            $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm64MarinerBuildFolder}-meta\linuxFilePermission.json"
            Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${arm64MarinerBuildFolder}"

            Write-Verbose -Verbose "---- rpm-fxdependent-arm64 ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"

            Start-PSPackage -Type rpm-fxdependent-arm64 @releaseTagParam -LTS:$LTS
        } elseif ($BuildType -eq 'alpine') {
            Restore-PSOptions -PSOptionsPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${amd64AlpineFxdBuildFolder}-meta\psoptions.json"
            $filePermissionFile = "${env:SYSTEM_ARTIFACTSDIRECTORY}\${amd64AlpineFxdBuildFolder}-meta\linuxFilePermission.json"
            Set-LinuxFilePermission -FilePath $filePermissionFile -RootPath "${env:SYSTEM_ARTIFACTSDIRECTORY}\${amd64AlpineFxdBuildFolder}"

            Write-Verbose -Verbose "---- tar-alpine-fxdependent ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"

            Start-PSPackage -Type tar-alpine-fxdependent @releaseTagParam -LTS:$LTS
        }
    }
    catch {
        Get-Error -InputObject $_
        throw
    }
}

<#
    Used in Azure DevOps Yaml to do all the builds needed for all Linux packages for a channel.
#>
function Invoke-AzDevOpsLinuxPackageBuild {
    param (
        [Parameter(Mandatory)]
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag,

        [Parameter(Mandatory)]
        [ValidateSet('fxdependent', 'alpine', 'deb', 'rpm')]
        [String]$BuildType
    )

    if (!${env:SYSTEM_ARTIFACTSDIRECTORY}) {
        throw "Must be run in Azure DevOps"
    }

    try {

        Write-Verbose "Building '$BuildType' for $ReleaseTag ..." -Verbose

        $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }

        $buildParams = @{ Configuration = 'Release'; PSModuleRestore = $true; Restore = $true }

        switch ($BuildType) {
            'fxdependent' {
                $buildParams.Add("Runtime", "fxdependent")
            }
            'alpine' {
                $buildParams.Add("Runtime", 'linux-musl-x64')
            }
        }

        $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${mainLinuxBuildFolder}"
        Start-PSBuild @buildParams @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
        Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force

        # Remove symbol files.
        Remove-Item "${buildFolder}\*.pdb" -Force

        if ($BuildType -eq 'deb') {
            ## Build 'min-size'
            $options = Get-PSOptions
            Write-Verbose -Verbose "---- Min-Size ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"
            $binDir = Join-Path -Path $options.Top -ChildPath 'bin'
            if (Test-Path -Path $binDir) {
                Write-Verbose -Verbose "Remove $binDir, to get a clean build for min-size package"
                Remove-Item -Path $binDir -Recurse -Force
            }

            $buildParams['ForMinimalSize'] = $true
            $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${minSizeLinuxBuildFolder}"
            Start-PSBuild -Clean @buildParams @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
            # Remove symbol files, xml document files.
            Remove-Item "${buildFolder}\*.pdb", "${buildFolder}\*.xml" -Force
            Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force

            ## Build 'linux-arm' and create 'tar.gz' package for it.
            ## Note that 'linux-arm' can only be built on Ubuntu environment.
            $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${arm32LinuxBuildFolder}"
            Start-PSBuild -Configuration Release -Restore -Runtime linux-arm -PSModuleRestore @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
            # Remove symbol files.
            Remove-Item "${buildFolder}\*.pdb" -Force
            Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force

            $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${arm64LinuxBuildFolder}"
            Start-PSBuild -Configuration Release -Restore -Runtime linux-arm64 -PSModuleRestore @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
            # Remove symbol files.
            Remove-Item "${buildFolder}\*.pdb" -Force
            Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force
        } elseif ($BuildType -eq 'rpm') {
            ## Build for Mariner amd64
            $options = Get-PSOptions
            Write-Verbose -Verbose "---- Mariner x64 ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"
            $binDir = Join-Path -Path $options.Top -ChildPath 'bin'
            if (Test-Path -Path $binDir) {
                Write-Verbose -Verbose "Remove $binDir, to get a clean build for Mariner x64 package"
                Remove-Item -Path $binDir -Recurse -Force
            }

            $buildParams['Runtime'] = 'fxdependent-linux-x64'
            $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${amd64MarinerBuildFolder}"
            Start-PSBuild -Clean @buildParams @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
            # Remove symbol files, xml document files.
            Remove-Item "${buildFolder}\*.pdb", "${buildFolder}\*.xml" -Force
            Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force

            ## Build for Mariner arm64
            $options = Get-PSOptions
            Write-Verbose -Verbose "---- Mariner arm64 ----"

            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"
            $binDir = Join-Path -Path $options.Top -ChildPath 'bin'
            if (Test-Path -Path $binDir) {
                Write-Verbose -Verbose "Remove $binDir, to get a clean build for Mariner arm64 package"
                Remove-Item -Path $binDir -Recurse -Force
            }

            $buildParams['Runtime'] = 'fxdependent-linux-arm64'
            $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${arm64MarinerBuildFolder}"

            Start-PSBuild -Clean @buildParams @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
            # Remove symbol files, xml document files.
            Remove-Item "${buildFolder}\*.pdb", "${buildFolder}\*.xml" -Force
            Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force
        } elseif ($BuildType -eq 'alpine') {
            ## Build for alpine fxdependent
            $options = Get-PSOptions
            Write-Verbose -Verbose "---- fxdependent alpine x64 ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"
            $binDir = Join-Path -Path $options.Top -ChildPath 'bin'
            if (Test-Path -Path $binDir) {
                Write-Verbose -Verbose "Remove $binDir, to get a clean build for Mariner package"
                Remove-Item -Path $binDir -Recurse -Force
            }

            $buildParams['Runtime'] = 'fxdependent-noopt-linux-musl-x64'
            $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/${amd64AlpineFxdBuildFolder}"
            Start-PSBuild -Clean @buildParams @releaseTagParam -Output $buildFolder -PSOptionsPath "${buildFolder}-meta/psoptions.json"
            # Remove symbol files, xml document files.
            Remove-Item "${buildFolder}\*.pdb", "${buildFolder}\*.xml" -Force
            Get-ChildItem -Path $buildFolder -Recurse -File | Export-LinuxFilePermission -FilePath "${buildFolder}-meta/linuxFilePermission.json" -RootPath ${buildFolder} -Force
        }
    }
    catch {
        Get-Error -InputObject $_
        throw
    }
}

<#
    Apply the file permissions specified in the json file $FilePath to the files under $RootPath.
    The format of the json file is like:

    {
        "System.Net.WebClient.dll": "744",
        "Schemas/PSMaml/developer.xsd": "644",
        "ref/System.Security.AccessControl.dll": "744",
        "ref/System.IO.dll": "744",
        "cs/Microsoft.CodeAnalysis.resources.dll": "744",
        "Schemas/PSMaml/base.xsd": "644",
        "Schemas/PSMaml/structureProcedure.xsd": "644",
        "ref/System.Net.Security.dll": "744"
    }
#>
function Set-LinuxFilePermission {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string] $RootPath
    )

    if (-not (Test-Path $FilePath)) {
        throw "File does not exist: $FilePath"
    }

    if (-not (Test-Path $RootPath)) {
        throw "File does not exist: $RootPath"
    }

    try {
        Push-Location $RootPath
        $filePermission = Get-Content $FilePath -Raw | ConvertFrom-Json -AsHashtable

        Write-Verbose -Verbose -Message "Got file permission: $($filePermission.Count) for $FilePath"

        $filePermission.GetEnumerator() | ForEach-Object {
            $file = $_.Name
            $permission = $_.Value
            $fileFullName = Join-Path -Path $RootPath -ChildPath $file
            Write-Verbose "Set permission $permission to $fileFullName" -Verbose
            chmod $permission $fileFullName
        }
    }
    finally {
        Pop-Location
    }
}

<#
    Store the linux file permissions for all the files under root path $RootPath to the json file $FilePath.
    The json file stores them as relative paths to the root.

    The format of the json file is like:

    {
        "System.Net.WebClient.dll": "744",
        "Schemas/PSMaml/developer.xsd": "644",
        "ref/System.Security.AccessControl.dll": "744",
        "ref/System.IO.dll": "744",
        "cs/Microsoft.CodeAnalysis.resources.dll": "744",
        "Schemas/PSMaml/base.xsd": "644",
        "Schemas/PSMaml/structureProcedure.xsd": "644",
        "ref/System.Net.Security.dll": "744"
    }

#>
function Export-LinuxFilePermission {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string] $RootPath,
        [Parameter(Mandatory, ValueFromPipeline = $true)] [System.IO.FileInfo[]] $InputObject,
        [Parameter()] [switch] $Force
    )

    begin {
        if (Test-Path $FilePath) {
          if (-not $Force) {
            throw "File '$FilePath' already exists."
          }
          else {
            Remove-Item $FilePath -Force
          }
        }

        $fileData = @{}
    }

    process {
        foreach ($object in $InputObject) {
            Write-Verbose "Processing $($object.FullName)"
            # This gets the unix stat information for the file in the format that chmod expects, like '644'.
            $filePerms = [convert]::ToString($object.unixstat.mode, 8).substring(3)
            $relativePath = [System.IO.Path]::GetRelativePath($RootPath, $_.FullName)
            $fileData.Add($relativePath, $filePerms)
        }
    }

    end {
        $fileData | ConvertTo-Json -Depth 10 | Out-File -FilePath $FilePath
    }
}

enum PackageManifestResultStatus {
    Mismatch
    Match
    MissingFromManifest
    MissingFromPackage
}

class PackageManifestResult {
    [string] $File
    [string] $ExpectedHash
    [string] $ActualHash
    [PackageManifestResultStatus] $Status
}

function Test-PackageManifest {
    param (
        [Parameter(Mandatory)]
        [string]
        $PackagePath
    )

    Begin {
        $spdxManifestPath = Join-Path $PackagePath -ChildPath "/_manifest/spdx_2.2/manifest.spdx.json"
        $man = Get-Content $spdxManifestPath -ErrorAction Stop | convertfrom-json
        $inManifest = @()
    }

    Process {
        Write-Verbose "Processing $($man.files.count) files..." -verbose
        $man.files | ForEach-Object {
            $filePath = Join-Path $PackagePath -childPath $_.fileName
            $checksumObj = $_.checksums | Where-Object {$_.algorithm -eq 'sha256'}
            $sha256 = $checksumObj.checksumValue
            $actualHash = $null
            $actualHash = (Get-FileHash -Path $filePath -Algorithm sha256 -ErrorAction SilentlyContinue).Hash
            $inManifest += $filePath
            if($actualHash -ne $sha256) {
                $status = [PackageManifestResultStatus]::Mismatch
                if (!$actualHash) {
                    $status = [PackageManifestResultStatus]::MissingFromPackage
                }
                [PackageManifestResult] $result = @{
                    File         = $filePath
                    ExpectedHash = $sha256
                    ActualHash   = $actualHash
                    Status       = $status
                }
                Write-Output $result
            }
            else {
                [PackageManifestResult] $result = @{
                    File         = $filePath
                    ExpectedHash = $sha256
                    ActualHash   = $actualHash
                    Status       = [PackageManifestResultStatus]::Match
                }
                Write-Output $result
            }
        }


        Get-ChildItem $PackagePath -recurse | Select-Object -ExpandProperty FullName | foreach-object {
            if(!$inManifest -contains $_) {
                $actualHash = (get-filehash -Path $_ -algorithm sha256 -erroraction silentlycontinue).Hash
                [PackageManifestResult] $result = @{
                    File         = $_
                    ExpectedHash = $null
                    ActualHash   = $actualHash
                    Status       = [PackageManifestResultStatus]::MissingFromManifest
                }
                Write-Output $result
            }
        }
    }
}

# Get the PE information for a file
function Get-PEInfo {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline = $true)][string] $File)
    BEGIN {
        # retrieved from ILCompiler.PEWriter.MachineOSOverride
        enum MachineOSOverride {
            Windows = 0
            SunOS = 6546
            NetBSD = 6547
            Apple = 17988
            Linux = 31609
            FreeBSD = 44484
        }

        # The information we want
        class PsPeInfo {
            [string]$File
            [bool]$CrossGen
            [Nullable[MachineOSOverride]]$OS
            [System.Reflection.PortableExecutable.Machine]$Architecture
            [Nullable[System.Reflection.PortableExecutable.CorFlags]]$Flags
        }

    }
    PROCESS {
        $filePath = (get-item $file).fullname
        $CrossGenFlag = 4
        try {
            $stream = [System.IO.FileStream]::new($FilePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
            $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
            $flags = $peReader.PEHeaders.CorHeader.Flags
            if (-not $flags) {
                Write-Warning "$filePath is not a managed assembly"
            }
            $machine = $peReader.PEHeaders.CoffHeader.Machine
            if (-not $machine) {
                throw "Null Machine"
            }
        } catch {
            $er = [system.management.automation.errorrecord]::new(([InvalidOperationException]::new($_)), "Get-PEInfo:InvalidOperation", "InvalidOperation", $filePath)
            $PSCmdlet.WriteError($er)
            return
        } finally {
            if ($peReader) {
                $peReader.Dispose()
            }
        }

        [ushort]$r2rOsArch = $machine

        $RealOS = $null
        $realarch = "unknown"
        foreach ($os in [enum]::GetValues([MachineOSOverride])) {
            foreach ($architecture in [Enum]::GetValues([System.Reflection.PortableExecutable.Machine])) {
                if (([ushort]$architecture -BXOR [ushort]$os) -eq [ushort]$r2rOsArch) {
                    $realOS = $os
                    $realArch = $architecture

                    [PsPeInfo]@{
                        File         = $File
                        OS           = $realos
                        Architecture = $realarch
                        CrossGen     = [bool]($flags -band $CrossGenFlag)
                        Flags        = $flags
                    }
                    return
                }
            }
        }
    }
}

function ConvertTo-PEArchitecture {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline = $true)]
        [string]
        $Architecture
    )

    PROCESS {
        switch ($Architecture) {
            "x86" { "I386" }
            "x64" { "AMD64" }
            "arm" { "ArmThumb2" }
            default { $Architecture }
        }
    }
}

function ConvertTo-PEOperatingSystem {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline = $true)]
        [string]
        $OperatingSystem
    )

    PROCESS {
        switch -regex ($OperatingSystem) {
            "win.*" { "Windows" }
            "Linux" { "Linux" }
            "OSX" { "Apple" }
            default { $OperatingSystem }
        }
    }
}

# Upload an artifact in Azure DevOps
# On other systems will just log where the file was placed
function Send-AzdoFile {
    param (
        [parameter(Mandatory, ParameterSetName = 'contents')]
        [string[]]
        $Contents,
        [parameter(Mandatory, ParameterSetName = 'contents')]
        [string]
        $LogName,
        [parameter(Mandatory, ParameterSetName = 'path')]
        [ValidateScript({ Test-Path -Path $_ })]
        [string]
        $Path
    )

    $logFolder = Join-Path -Path $PWD -ChildPath 'logfile'
    if (!(Test-Path -Path $logFolder)) {
        $null = New-Item -Path $logFolder -ItemType Directory
        if ($IsMacOS -or $IsLinux) {
            $null = chmod a+rw $logFolder
        }
    }

    if ($LogName) {
        $effectiveLogName = $LogName + '.txt'
    } else {
        $effectiveLogName = Split-Path -Leaf -Path $Path
    }

    $newName = ([System.Io.Path]::GetRandomFileName() + "-$effectiveLogName")
    if ($Contents) {
        $logFile = Join-Path -Path $logFolder -ChildPath $newName

        $Contents | Out-File -path $logFile -Encoding ascii
    } else {
        $logFile = Join-Path -Path $logFolder -ChildPath $newName
        Copy-Item -Path $Path -Destination $logFile
    }

    Write-Verbose "Capture the log file as '$newName'" -Verbose
    if($env:TF_BUILD) {
        ## In Azure DevOps
        Write-Host "##vso[artifact.upload containerfolder=$newName;artifactname=$newName]$logFile"
    } elseif ($env:GITHUB_WORKFLOW -and $env:SYSTEM_ARTIFACTSDIRECTORY) {
        ## In GitHub Actions
        $destinationPath = $env:SYSTEM_ARTIFACTSDIRECTORY
        Write-Verbose "Upload '$logFile' to '$destinationPath' in GitHub Action" -Verbose

        # Create the folder if it does not exist
        if (!(Test-Path -Path $destinationPath)) {
            $null = New-Item -ItemType Directory -Path $destinationPath -Force
        }

        Copy-Item -Path $logFile -Destination $destinationPath -Force -Verbose
    } else {
        Write-Warning "This environment is neither Azure Devops nor GitHub Actions. Cannot capture the log file in this environment."
    }
}

# Class used for serializing and deserialing a BOM into Json
class BomRecord {
    hidden
    [string]
    $Pattern

    [ValidateSet("Product", "NonProduct")]
    [string]
    $FileType = "NonProduct"

    [string[]]
    $Architecture

    # Add methods to normalize Pattern to use `/` as the directory separator,
    # but give a Pattern that is usable on the current platform
    [string]
    GetPattern () {
        # Get the directory separator character for the current OS
        $dirSeparator = [System.io.path]::DirectorySeparatorChar

        # If the directory separator character is not a slash, then replace all slashes in the pattern with the OS-specific directory separator character
        if ($dirSeparator -ne '/') {
            return $this.Pattern.replace('/', $dirSeparator)
        }

        # If the directory separator character is a slash, then return the pattern as-is
        return $this.Pattern
    }

    [void]
    SetPattern ([string]$Pattern) {
        # Get the directory separator character for the current OS
        $dirSeparator = [System.io.path]::DirectorySeparatorChar

        # If the directory separator character is not a slash, then replace all instances of the OS-specific directory separator character with slashes in the pattern
        if ($dirSeparator -ne '/') {
            $this.Pattern = $Pattern.Replace($dirSeparator, '/')
        }

        # If the directory separator character is a slash, then set the pattern as-is
        $this.Pattern = $Pattern
    }

    [void]
    EnsureArchitecture([string[]]$DefaultArchitecture = @("x64","x86","arm64")) {
        if (-not $this.PSObject.Properties.Match("Architecture")) {
            $this.Architecture = $DefaultArchitecture
        }
    }
}

# Verify a folder based on a BOM json.
# Use -Fix to update the BOM, Please review the file types.
function Test-Bom {
    param(
        [ValidateSet('mac','windows','linux')]
        [string]
        $BomName,
        [ValidateScript({ Test-Path $_ })]
        [string]
        $Path,
        [switch]
        $Fix,
        [string]
        $Architecture
    )

    Write-Log "verifying no unauthorized files have been added or removed..."
    $root = (Resolve-Path $Path).ProviderPath -replace "\$([System.io.path]::DirectorySeparatorChar)$"

    $bomFile = Join-Path -Path $PSScriptRoot -ChildPath "Boms\$BomName.json"
    Write-Verbose "bomFile: $bomFile" -Verbose
    [BomRecord[]]$bomRecords = Get-Content -Path $bomFile | ConvertFrom-Json
    $bomList = [System.Collections.Generic.List[BomRecord]]::new($bomRecords)
    $noMatch = @()
    $patternsUsed = @()
    $files = @(Get-ChildItem -File -Path $Path -Recurse)
    $totalFiles = $files.Count
    $currentFileCount = 0

    # Test each file if it is a match for a pattern in the BOM
    # Add patters found to $patternsUsed
    # Generate a list of new BOMs in $noMatch
    $files | ForEach-Object {
        [System.IO.FileInfo] $file = $_
        $fileName = $file.Name
        $filePath = $file.FullName
        $currentFileCount++

        Write-Progress -Activity "Testing $BomName BOM" -PercentComplete (100*$currentFileCount/$totalFiles) -Status "Processing $fileName"

        $match = $false
        [BomRecord] $matchingRecord = $null

        # Test file against each BOM that can still have a match
        foreach ($bom in $bomList) {
            $pattern = $root + [system.io.path]::DirectorySeparatorChar + $bom.GetPattern()
            if ($filePath -like $pattern) {
                $matchingRecord = $bom
                $match = $true
                if ($patternsUsed -notcontains $bom) {
                    $patternsUsed += $bom
                }
                break
            }
        }

        # if we didn't find a match, create a record in the noMatch list.
        if (!$match) {
            $relativePath = $_.FullName.Replace($root, "").Substring(1)
            $isProduct = Test-IsProductFile -Path $relativePath
            $fileType = "NonProduct"
            if ($isProduct) {
                $fileType = "Product"
            }

            [BomRecord] $newBomRecord = [BomRecord] @{
                FileType = $fileType
            }

            $newBomRecord.SetPattern([WildcardPattern]::Escape($_.FullName.Replace($root, "").Substring(1)))
            $noMatch += $newBomRecord
        }
        elseif ($matchingRecord -and ![WildcardPattern]::ContainsWildcardCharacters($matchingRecord.GetPattern())) {
            # remove any exact pattern which have been matched to speed up file processing,
            # because they should not have additional matches.
            if ($matchingRecord -is [BomRecord]) {
                $null = $bomList.Remove($matchingRecord)
            } else {
                Write-Warning "Cannot remove matchingRecord $($matchingRecord.GetPattern())"
            }
        }
    }

    Write-Progress -Activity "Testing $BomName BOM" -Completed

    Write-Verbose "$($noMatch.count) records need to be added to $bomFile" -Verbose

    # Create the complete new manifest
    $currentRecords = @()
    # Add BOMs for all the files that didn't match
    $currentRecords += $noMatch
    # Add BOMs for all the patterns that did match
    $currentRecords += $patternsUsed

    # Generate a name for the updated BOM
    $newBom = Join-Path -Path ([system.io.path]::GetTempPath()) -ChildPath ("${bomName}-" +  [system.io.path]::GetRandomFileName() + "-bom.json")

    # Sort and serialize the BOM
    $currentRecords | Sort-Object -Property FileType, Pattern | ConvertTo-Json | Out-File -Encoding utf8NoBOM -FilePath $newBom

    # check if we removed any BOMs
    $needsRemoval = $bom | Where-Object {
        $_ -notin $patternsUsed
    }

    Write-Verbose "$($needsRemoval.count) need removal from $bomFile" -Verbose

    # If we added or removed BOMs, log the new file and throw
    if ($noMatch.count -gt 0 -or $needsRemoval.Count -gt 0) {
        Send-AzdoFile -Path $newBom

        # If -Fix was specified, update the original BOM
        if ($Fix) {
            Copy-Item -Path $newBom -Destination $bomFile -Force -Verbose
        }

        throw "Please update $bomFile per the above instructions"
    }
}

# Simple test to guess if a file is a product file
function Test-IsProductFile {
    param(
        $Path
    )

    $itemsToCopy = @(
        "*.ps1"
        "*Microsoft.PowerShell*.dll"
        "*Microsoft.PowerShell*.psd1"
        "*Microsoft.PowerShell*.ps1xml"
        "*Microsoft.WSMan.Management*.psd1"
        "*Microsoft.WSMan.Management*.ps1xml"
        "*pwsh.dll"
        "*System.Management.Automation.dll"
        "*PSDiagnostics.ps?1"
        "*pwsh"
        "*pwsh.exe"
    )

    $itemsToExclude = @(
        # This package is retrieved from https://www.github.com/powershell/MarkdownRender
        "*Microsoft.PowerShell.MarkdownRender.dll"

        )
    if ($Path -like $itemsToExclude) {
        return $false
    }

    foreach ($pattern in $itemsToCopy) {
        if ($Path -like $pattern) {
            return $true
        }
    }

    return $false
}

# Get major version from latest ICU release (latest: stable version)
function Get-IcuLatestRelease {
    $response = Invoke-WebRequest -Uri "https://github.com/unicode-org/icu/releases/latest"
    $tagUrl = ($response.Links | Where-Object href -like "*releases/tag/release-*")[0].href

    if ($tagUrl -match 'release-(\d+)\.') {
       return [int]$Matches[1]
    }

    throw "Unable to determine the latest ICU release version."
}
