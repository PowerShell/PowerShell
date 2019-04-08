# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$Environment = Get-EnvironmentInformation
$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path

$packagingStrings = Import-PowerShellDataFile "$PSScriptRoot\packaging.strings.psd1"
Import-Module "$PSScriptRoot\..\Xml" -ErrorAction Stop -Force
$DebianDistributions = @("ubuntu.16.04", "ubuntu.18.04", "debian.9")

function Start-PSPackage {
    [CmdletBinding(DefaultParameterSetName='Version',SupportsShouldProcess=$true)]
    param(
        # PowerShell packages use Semantic Versioning https://semver.org/
        [Parameter(ParameterSetName = "Version")]
        [string]$Version,

        [Parameter(ParameterSetName = "ReleaseTag")]
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d+)?)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag,

        # Package name
        [ValidatePattern("^powershell")]
        [string]$Name = "powershell",

        # Ubuntu, CentOS, Fedora, macOS, and Windows packages are supported
        [ValidateSet("deb", "osxpkg", "rpm", "msi", "zip", "nupkg", "tar", "tar-arm", "tar-arm64", "tar-alpine", "fxdependent")]
        [string[]]$Type,

        # Generate windows downlevel package
        [ValidateSet("win7-x86", "win7-x64", "win-arm", "win-arm64")]
        [ValidateScript({$Environment.IsWindows})]
        [string] $WindowsRuntime,

        [Switch] $Force,

        [Switch] $SkipReleaseChecks,

        [switch] $NoSudo
    )

    DynamicParam {
        if ("zip" -eq $Type -or "fxdependent" -eq $Type) {
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
        } elseif ($Type -eq "tar-alpine") {
            New-PSOptions -Configuration "Release" -Runtime "alpine-x64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type -eq "tar-arm") {
            New-PSOptions -Configuration "Release" -Runtime "Linux-ARM" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type -eq "tar-arm64") {
            New-PSOptions -Configuration "Release" -Runtime "Linux-ARM64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } else {
            New-PSOptions -Configuration "Release" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        }

        if ($Environment.IsWindows) {
            # Runtime will be one of win7-x64, win7-x86, "win-arm" and "win-arm64" on Windows.
            # Build the name suffix for universal win-plat packages.
            switch ($Runtime) {
                "win-arm"   { $NameSuffix = "win-arm32" }
                "win-arm64" { $NameSuffix = "win-arm64" }
                default     { $NameSuffix = $_ -replace 'win\d+', 'win' }
            }
        }

        if ($Type -eq 'fxdependent') {
            $NameSuffix = "win-fxdependent"
            Write-Log "Packaging : '$Type'; Packaging Configuration: '$Configuration'"
        } else {
            Write-Log "Packaging RID: '$Runtime'; Packaging Configuration: '$Configuration'"
        }

        $Script:Options = Get-PSOptions
        $actualParams = @()

        $crossGenCorrect = $false
        if ($Runtime -match "arm") {
            # crossgen doesn't support arm32/64
            $crossGenCorrect = $true
        }
        elseif ($Script:Options.CrossGen) {
            $actualParams += '-CrossGen'
            $crossGenCorrect = $true
        }

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

        $precheckFailed = if ($Type -eq 'fxdependent' -or $Type -eq 'tar-alpine') {
            ## We do not check for runtime and crossgen for framework dependent package.
            -not $Script:Options -or                                ## Start-PSBuild hasn't been executed yet
            -not $PSModuleRestoreCorrect -or                        ## Last build didn't specify '-PSModuleRestore' correctly
            $Script:Options.Configuration -ne $Configuration -or    ## Last build was with configuration other than 'Release'
            $Script:Options.Framework -ne "netcoreapp2.1"           ## Last build wasn't for CoreCLR
        } else {
            -not $Script:Options -or                                ## Start-PSBuild hasn't been executed yet
            -not $crossGenCorrect -or                               ## Last build didn't specify '-CrossGen' correctly
            -not $PSModuleRestoreCorrect -or                        ## Last build didn't specify '-PSModuleRestore' correctly
            $Script:Options.Runtime -ne $Runtime -or                ## Last build wasn't for the required RID
            $Script:Options.Configuration -ne $Configuration -or    ## Last build was with configuration other than 'Release'
            $Script:Options.Framework -ne "netcoreapp2.1"           ## Last build wasn't for CoreCLR
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


            $params = @('-Clean')

            # CrossGen cannot be done for framework dependent package as it is runtime agnostic.
            if ($Type -ne 'fxdependent') {
                $params += '-CrossGen'
            }

            if (!$IncludeSymbols.IsPresent) {
                $params += '-PSModuleRestore'
            }

            $actualParams += '-Runtime ' + $Script:Options.Runtime

            if ($Type -eq 'fxdependent') {
                $params += '-Runtime', 'fxdependent'
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

        # If ReleaseTag is specified, use the given tag to calculate Vesrion
        if ($PSCmdlet.ParameterSetName -eq "ReleaseTag") {
            $Version = $ReleaseTag -Replace '^v'
        }

        # Use Git tag if not given a version
        if (-not $Version) {
            $Version = (git --git-dir="$RepoRoot/.git" describe) -Replace '^v'
        }

        $Source = Split-Path -Path $Script:Options.Output -Parent

        # Copy the ThirdPartyNotices.txt so it's part of the package
        Copy-Item "$RepoRoot/ThirdPartyNotices.txt" -Destination $Source -Force

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

                # files not to include as individual files.  These files will be included in the root package
                # pwsh.exe is just dotnet.exe renamed by dotnet.exe during the build.
                $toExclude = @(
                    'hostfxr.dll'
                    'hostpolicy.dll'
                    'libhostfxr.so'
                    'libhostpolicy.so'
                    'libhostfxr.dylib'
                    'libhostpolicy.dylib'
                    'Publish'
                    'pwsh.exe'
                    )
                # Copy file which go into symbols.zip
                Get-ChildItem -Path $buildSource | Where-Object {$toExclude -inotcontains $_.Name} | Copy-Item -Destination $symbolsSource -Recurse

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
                    "deb", "nupkg", "tar"
                } elseif ($Environment.IsRedHatFamily) {
                    "rpm", "nupkg", "tar"
                } elseif ($Environment.IsSUSEFamily) {
                    "rpm", "nupkg", "tar"
                } else {
                    throw "Building packages for $($Environment.LinuxInfo.PRETTY_NAME) is unsupported!"
                }
            } elseif ($Environment.IsMacOS) {
                "osxpkg", "nupkg", "tar"
            } elseif ($Environment.IsWindows) {
                "msi", "nupkg"
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
                $Arguments = @{
                    PackageNameSuffix = $NameSuffix
                    PackageSourcePath = $Source
                    PackageVersion = $Version
                    Force = $Force
                }

                if ($PSCmdlet.ShouldProcess("Create Zip Package")) {
                    New-ZipPackage @Arguments
                }
            }
            "fxdependent" {
                ## Remove PDBs from package to reduce size.
                if(-not $IncludeSymbols.IsPresent) {
                    Get-ChildItem $Source -Filter *.pdb | Remove-Item -Force
                }

                if ($Environment.IsWindows) {
                    $Arguments = @{
                        PackageNameSuffix = $NameSuffix
                        PackageSourcePath = $Source
                        PackageVersion = $Version
                        Force = $Force
                    }

                    if ($PSCmdlet.ShouldProcess("Create Zip Package")) {
                        New-ZipPackage @Arguments
                    }
                } elseif ($IsLinux) {
                    $Arguments = @{
                        PackageSourcePath = $Source
                        Name = $Name
                        PackageNameSuffix = 'fxdependent'
                        Version = $Version
                        Force = $Force
                    }

                    if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                        New-TarballPackage @Arguments
                    }
                }
            }
            "msi" {
                $TargetArchitecture = "x64"
                if ($Runtime -match "-x86") {
                    $TargetArchitecture = "x86"
                }

                $Arguments = @{
                    ProductNameSuffix = $NameSuffix
                    ProductSourcePath = $Source
                    ProductVersion = $Version
                    AssetsPath = "$RepoRoot\assets"
                    LicenseFilePath = "$RepoRoot\assets\license.rtf"
                    # Product Code needs to be unique for every PowerShell version since it is a unique identifier for the particular product release
                    ProductCode = New-Guid
                    ProductTargetArchitecture = $TargetArchitecture
                    Force = $Force
                }

                if ($PSCmdlet.ShouldProcess("Create MSI Package")) {
                    New-MSIPackage @Arguments
                }
            }
            'nupkg' {
                $Arguments = @{
                    PackageNameSuffix = $NameSuffix
                    PackageSourcePath = $Source
                    PackageVersion = $Version
                    PackageRuntime = $Runtime
                    PackageConfiguration = $Configuration
                    Force = $Force
                }

                if ($PSCmdlet.ShouldProcess("Create NuPkg Package")) {
                    New-NugetContentPackage @Arguments
                }
            }
            "tar" {
                $Arguments = @{
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                }

                if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                    New-TarballPackage @Arguments
                }
            }
            "tar-arm" {
                $Arguments = @{
                    PackageSourcePath = $Source
                    Name = $Name
                    Version = $Version
                    Force = $Force
                    Architecture = "arm32"
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
                    Architecture = "alpine-x64"
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
                }
                foreach ($Distro in $Script:DebianDistributions) {
                    $Arguments["Distribution"] = $Distro
                    if ($PSCmdlet.ShouldProcess("Create DEB Package for $Distro")) {
                        New-UnixPackage @Arguments
                    }
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

        [switch] $Force
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

    $packagePath = Join-Path -Path $PWD -ChildPath $packageName
    Write-Verbose "Create package $packageName"
    Write-Verbose "Package destination path: $packagePath"

    if (Test-Path -Path $packagePath) {
        if ($Force -or $PSCmdlet.ShouldProcess("Overwrite existing package file")) {
            Write-Verbose "Overwrite existing package file at $packagePath" -Verbose
            Remove-Item -Path $packagePath -Force -ErrorAction Stop -Confirm:$false
        }
    }

    if (Get-Command -Name tar -CommandType Application -ErrorAction Ignore) {
        if ($Force -or $PSCmdlet.ShouldProcess("Create tarball package")) {
            $options = "-czf"
            if ($PSBoundParameters.ContainsKey('Verbose') -and $PSBoundParameters['Verbose'].IsPresent) {
                # Use the verbose mode '-v' if '-Verbose' is specified
                $options = "-czvf"
            }

            try {
                Push-Location -Path $PackageSourcePath
                tar $options $packagePath .
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

    # Replace unsigned binaries with signed
    $signedFilesFilter = Join-Path -Path $signedFilesPath -ChildPath '*'
    Get-ChildItem -path $signedFilesFilter -Recurse -File | Select-Object -ExpandProperty FullName | Foreach-Object -Process {
        $relativePath = $_.ToLowerInvariant().Replace($signedFilesPath.ToLowerInvariant(),'')
        $destination = Join-Path -Path $buildPath -ChildPath $relativePath
        Write-Log "replacing $destination with $_"
        Copy-Item -Path $_ -Destination $destination -force
    }

    # Remove '$signedFilesPath' now that signed binaries are copied
    if (Test-Path $signedFilesPath)
    {
        Remove-Item -Recurse -Force -Path $signedFilesPath
    }

    $name = split-path -Path $BuildPath -Leaf
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

function Expand-PSSignedBuild
{
    param(
        [Parameter(Mandatory)]
        [string]$BuildZip,

        [Switch]$SkipPwshExeCheck
    )

    $psModulePath = Split-Path -path $PSScriptRoot
    # Expand signed build
    $buildPath = Join-Path -path $psModulePath -childpath 'ExpandedBuild'
    $null = New-Item -path $buildPath -itemtype Directory -force
    Expand-Archive -path $BuildZip -destinationpath $buildPath -Force
    # Remove the zip file that contains only those files from the parent folder of 'publish'.
    # That zip file is used for compliance scan.
    Remove-Item -Path (Join-Path -Path $buildPath -ChildPath '*.zip') -Recurse

    if ($SkipPwshExeCheck)
    {
        $windowsExecutablePath = (Join-Path $buildPath -ChildPath 'pwsh.dll')
    }
    else
    {
        $windowsExecutablePath = (Join-Path $buildPath -ChildPath 'pwsh.exe')
    }

    Restore-PSModuleToBuild -PublishPath $buildPath

    $psOptionsPath = Join-Path $buildPath -ChildPath 'psoptions.json'
    Restore-PSOptions -PSOptionsPath $psOptionsPath -Remove

    $options = Get-PSOptions

    $options.PSModuleRestore = $true

    if (Test-Path -Path $windowsExecutablePath)
    {
        $options.Output = $windowsExecutablePath
    }
    else
    {
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

        [Switch]
        $Force,

        [switch]
        $NoSudo
    )

    DynamicParam {
        if ($Type -eq "deb") {
            # Add a dynamic parameter '-Distribution' when the specified package type is 'deb'.
            # The '-Distribution' parameter can be used to indicate which Debian distro this pacakge is targeting.
            $ParameterAttr = New-Object "System.Management.Automation.ParameterAttribute"
            $ValidateSetAttr = New-Object "System.Management.Automation.ValidateSetAttribute" -ArgumentList $Script:DebianDistributions
            $Attributes = New-Object "System.Collections.ObjectModel.Collection``1[System.Attribute]"
            $Attributes.Add($ParameterAttr) > $null
            $Attributes.Add($ValidateSetAttr) > $null

            $Parameter = New-Object "System.Management.Automation.RuntimeDefinedParameter" -ArgumentList ("Distribution", [string], $Attributes)
            $Dict = New-Object "System.Management.Automation.RuntimeDefinedParameterDictionary"
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
                if (!$Environment.IsUbuntu -and !$Environment.IsDebian) {
                    throw ($ErrorMessage -f "Ubuntu or Debian")
                }

                if ($PSBoundParameters.ContainsKey('Distribution')) {
                    $DebDistro = $PSBoundParameters['Distribution']
                } elseif ($Environment.IsUbuntu16) {
                    $DebDistro = "ubuntu.16.04"
                } elseif ($Environment.IsUbuntu18) {
                    $DebDistro = "ubuntu.18.04"
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
                $packageVersion = Get-LinuxPackageSemanticVersion -Version $Version
                if (!$Environment.IsRedHatFamily -and !$Environment.IsSUSEFamily) {
                    throw ($ErrorMessage -f "Redhat or SUSE Family")
                }
            }
            "osxpkg" {
                $packageVersion = $Version
                if (!$Environment.IsMacOS) {
                    throw ($ErrorMessage -f "macOS")
                }
            }
        }

        # Determine if the version is a preview version
        $IsPreview = Test-IsPreview -Version $Version

        # Preview versions have preview in the name
        $Name = if ($IsPreview) { "powershell-preview" } else { "powershell" }

        # Verify dependencies are installed and in the path
        Test-Dependencies

        $Description = $packagingStrings.Description

        # Break the version down into its components, we are interested in the major version
        $VersionMatch = [regex]::Match($Version, '(\d+)(?:.(\d+)(?:.(\d+)(?:-preview(?:.(\d+))?)?)?)?')
        $MajorVersion = $VersionMatch.Groups[1].Value

        # Suffix is used for side-by-side preview/release package installation
        $Suffix = if ($IsPreview) { $MajorVersion + "-preview" } else { $MajorVersion }

        # Setup staging directory so we don't change the original source directory
        $Staging = "$PSScriptRoot/staging"
        if ($pscmdlet.ShouldProcess("Create staging folder")) {
            New-StagingFolder -StagingPath $Staging
        }

        # Follow the Filesystem Hierarchy Standard for Linux and macOS
        $Destination = if ($Environment.IsLinux) {
            "/opt/microsoft/powershell/$Suffix"
        } elseif ($Environment.IsMacOS) {
            "/usr/local/microsoft/powershell/$Suffix"
        }

        # Destination for symlink to powershell executable
        $Link = if ($Environment.IsLinux) {
            if ($IsPreview) { "/usr/bin/pwsh-preview" } else { "/usr/bin/pwsh" }
        } elseif ($Environment.IsMacOS) {
            if ($IsPreview) { "/usr/local/bin/pwsh-preview" } else { "/usr/local/bin/pwsh" }
        }
        $linkSource = "/tmp/pwsh"

        if ($pscmdlet.ShouldProcess("Create package file system"))
        {
            # refers to executable, does not vary by channel
            New-Item -Force -ItemType SymbolicLink -Path $linkSource -Target "$Destination/pwsh" > $null

            # Generate After Install and After Remove scripts
            $AfterScriptInfo = New-AfterScripts -Link $Link

            # there is a weird bug in fpm
            # if the target of the powershell symlink exists, `fpm` aborts
            # with a `utime` error on macOS.
            # so we move it to make symlink broken
            # refers to executable, does not vary by channel
            $symlink_dest = "$Destination/pwsh"
            $hack_dest = "./_fpm_symlink_hack_powershell"
            if ($Environment.IsMacOS) {
                if (Test-Path $symlink_dest) {
                    Write-Warning "Move $symlink_dest to $hack_dest (fpm utime bug)"
                    Start-NativeExecution ([ScriptBlock]::Create("$sudo mv $symlink_dest $hack_dest"))
                }
            }

            # Generate gzip of man file
            $ManGzipInfo = New-ManGzip -IsPreview:$IsPreview

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
            if ($pscmdlet.ShouldProcess("Add macOS launch application"))
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

        $Arguments = Get-FpmArguments `
            -Name $Name `
            -Version $packageVersion `
            -Iteration $Iteration `
            -Description $Description `
            -Type $Type `
            -Dependencies $Dependencies `
            -AfterInstallScript $AfterScriptInfo.AfterInstallScript `
            -AfterRemoveScript $AfterScriptInfo.AfterRemoveScript `
            -Staging $Staging `
            -Destination $Destination `
            -ManGzipFile $ManGzipInfo.GzipFile `
            -ManDestination $ManGzipInfo.ManFile `
            -LinkSource $LinkSource `
            -LinkDestination $Link `
            -AppsFolder $AppsFolder `
            -ErrorAction Stop

        # Build package
        try {
            if ($pscmdlet.ShouldProcess("Create $type package")) {
                Write-Log "Creating package with fpm..."
                $Output = Start-NativeExecution { fpm $Arguments }
            }
        } finally {
            if ($Environment.IsMacOS) {
                Write-Log "Starting Cleanup for mac packaging..."
                if ($pscmdlet.ShouldProcess("Cleanup macOS launcher"))
                {
                    Clear-MacOSLauncher
                }

                # this is continuation of a fpm hack for a weird bug
                if (Test-Path $hack_dest) {
                    Write-Warning "Move $hack_dest to $symlink_dest (fpm utime bug)"
                    Start-NativeExecution ([ScriptBlock]::Create("$sudo mv $hack_dest $symlink_dest"))
                }
            }
            if ($AfterScriptInfo.AfterInstallScript) {
                Remove-Item -erroraction 'silentlycontinue' $AfterScriptInfo.AfterInstallScript -Force
            }
            if ($AfterScriptInfo.AfterRemoveScript) {
                Remove-Item -erroraction 'silentlycontinue' $AfterScriptInfo.AfterRemoveScript -Force
            }
            Remove-Item -Path $ManGzipInfo.GzipFile -Force -ErrorAction SilentlyContinue
        }

        # Magic to get path output
        $createdPackage = Get-Item (Join-Path $PWD (($Output[-1] -split ":path=>")[-1] -replace '["{}]'))

        if ($Environment.IsMacOS) {
            if ($pscmdlet.ShouldProcess("Add distribution information and Fix PackageName"))
            {
                $createdPackage = New-MacOsDistributionPackage -FpmPackage $createdPackage -IsPreview:$IsPreview
            }
        }

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

function New-MacOsDistributionPackage
{
    param(
        [Parameter(Mandatory,HelpMessage='The FileInfo of the file created by FPM')]
        [System.IO.FileInfo]$FpmPackage,
        [Switch] $IsPreview
    )

    if (!$Environment.IsMacOS)
    {
        throw 'New-MacOsDistributionPackage is only supported on macOS!'
    }

    $packageName = Split-Path -leaf -Path $FpmPackage

    # Create a temp directory to store the needed files
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir -Force > $null

    $resourcesDir = Join-Path -path $tempDir -childPath 'resources'
    New-Item -ItemType Directory -Path $resourcesDir -Force > $null
    #Copy background file to temp directory
    $backgroundFile = "$RepoRoot/assets/macDialog.png"
    Copy-Item -Path $backgroundFile -Destination $resourcesDir
    # Move the current package to the temp directory
    $tempPackagePath = Join-Path -path $tempDir -ChildPath $packageName
    Move-Item -Path $FpmPackage -Destination $tempPackagePath -Force

    # Add the OS information to the macOS package file name.
    $packageExt = [System.IO.Path]::GetExtension($FpmPackage.Name)

    # get the package name from fpm without the extension, but replace powershell-preview at the beginning of the name with powershell.
    $packageNameWithoutExt = [System.IO.Path]::GetFileNameWithoutExtension($FpmPackage.Name) -replace '^powershell\-preview' , 'powershell'

    $newPackageName = "{0}-{1}{2}" -f $packageNameWithoutExt, $script:Options.Runtime, $packageExt
    $newPackagePath = Join-Path $FpmPackage.DirectoryName $newPackageName

    # -Force is not deleting the NewName if it exists, so delete it if it does
    if ($Force -and (Test-Path -Path $newPackagePath))
    {
        Remove-Item -Force $newPackagePath
    }

    # Create the distribution xml
    $distributionXmlPath = Join-Path -Path $tempDir -ChildPath 'powershellDistribution.xml'

    $packageId = Get-MacOSPackageId -IsPreview:$IsPreview.IsPresent

    # format distribution template with:
    # 0 - title
    # 1 - version
    # 2 - package path
    # 3 - minimum os version
    # 4 - Package Identifier
    $PackagingStrings.OsxDistributionTemplate -f "PowerShell - $packageVersion", $packageVersion, $packageName, '10.12', $packageId | Out-File -Encoding ascii -FilePath $distributionXmlPath -Force

    Write-Log "Applying distribution.xml to package..."
    Push-Location $tempDir
    try
    {
        # productbuild is an xcode command line tool, and those tools are installed when you install brew
        Start-NativeExecution -sb {productbuild --distribution $distributionXmlPath --resources $resourcesDir $newPackagePath} -VerboseOutputOnError
    }
    finally
    {
        Pop-Location
        Remove-item -Path $tempDir -Recurse -Force
    }

    return (Get-Item $newPackagePath)
}
function Get-FpmArguments
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

        # From start-PSPackage without modification, already validated
        # Values: deb, rpm, osxpkg
        [Parameter(Mandatory,HelpMessage='Installer Type')]
        [String]$Type,

        [Parameter(Mandatory,HelpMessage='Staging folder for installation files')]
        [String]$Staging,

        [Parameter(Mandatory,HelpMessage='Install path on target machine')]
        [String]$Destination,

        [Parameter(Mandatory,HelpMessage='The built and gzipped man file.')]
        [String]$ManGzipFile,

        [Parameter(Mandatory,HelpMessage='The destination of the man file')]
        [String]$ManDestination,

        [Parameter(Mandatory,HelpMessage='Symlink to powershell executable')]
        [String]$LinkSource,

        [Parameter(Mandatory,HelpMessage='Destination for symlink to powershell executable')]
        [String]$LinkDestination,

        [Parameter(HelpMessage='Packages required to install this package.  Not applicable for MacOS.')]
        [ValidateScript({
            if (!$Environment.IsMacOS -and $_.Count -eq 0)
            {
                throw "Must not be null or empty on this environment."
            }
            return $true
        })]
        [String[]]$Dependencies,

        [Parameter(HelpMessage='Script to run after the package installation.')]
        [AllowNull()]
        [ValidateScript({
            if (!$Environment.IsMacOS -and !$_)
            {
                throw "Must not be null on this environment."
            }
            return $true
        })]
        [String]$AfterInstallScript,

        [Parameter(HelpMessage='Script to run after the package removal.')]
        [AllowNull()]
        [ValidateScript({
            if (!$Environment.IsMacOS -and !$_)
            {
                throw "Must not be null on this environment."
            }
            return $true
        })]
        [String]$AfterRemoveScript,

        [Parameter(HelpMessage='AppsFolder used to add macOS launcher')]
        [AllowNull()]
        [ValidateScript({
            if ($Environment.IsMacOS -and !$_)
            {
                throw "Must not be null on this environment."
            }
            return $true
        })]
        [String]$AppsFolder
    )

    $Arguments = @(
        "--force", "--verbose",
        "--name", $Name,
        "--version", $Version,
        "--iteration", $Iteration,
        "--maintainer", "PowerShell Team <PowerShellTeam@hotmail.com>",
        "--vendor", "Microsoft Corporation",
        "--url", "https://microsoft.com/powershell",
        "--license", "MIT License",
        "--description", $Description,
        "--category", "shells",
        "-t", $Type,
        "-s", "dir"
    )
    if ($Environment.IsRedHatFamily) {
        $Arguments += @("--rpm-dist", "rhel.7")
        $Arguments += @("--rpm-os", "linux")
    }

    if ($Environment.IsMacOS) {
        $Arguments += @("--osxpkg-identifier-prefix", "com.microsoft")
    }

    foreach ($Dependency in $Dependencies) {
        $Arguments += @("--depends", $Dependency)
    }

    if ($AfterInstallScript) {
        $Arguments += @("--after-install", $AfterInstallScript)
    }

    if ($AfterRemoveScript) {
        $Arguments += @("--after-remove", $AfterRemoveScript)
    }

    $Arguments += @(
        "$Staging/=$Destination/",
        "$ManGzipFile=$ManDestination",
        "$LinkSource=$LinkDestination"
    )

    if ($AppsFolder)
    {
        $Arguments += "$AppsFolder=/"
    }

    return $Arguments
}

function Test-Distribution
{
    param(
        [String]
        $Distribution
    )

    if ( ($Environment.IsUbuntu -or $Environment.IsDebian) -and !$Distribution )
    {
        throw "$Distribution is required for a Debian based distribution."
    }

    if ($Script:DebianDistributions -notcontains $Distribution)
    {
        throw "$Distribution should be one of the following: $Script:DebianDistributions"
    }
    return $true
}
function Get-PackageDependencies
{
    param(
        [String]
        [ValidateScript({Test-Distribution -Distribution $_})]
        $Distribution
    )

    End {
        # These should match those in the Dockerfiles, but exclude tools like Git, which, and curl
        $Dependencies = @()
        if ($Environment.IsUbuntu -or $Environment.IsDebian) {
            $Dependencies = @(
                "libc6",
                "libgcc1",
                "libgssapi-krb5-2",
                "liblttng-ust0",
                "libstdc++6",
                "zlib1g"
            )

            switch ($Distribution) {
                "ubuntu.16.04" { $Dependencies += @("libssl1.0.0", "libicu55") }
                "ubuntu.17.10" { $Dependencies += @("libssl1.0.0", "libicu57") }
                "ubuntu.18.04" { $Dependencies += @("libssl1.0.0", "libicu60") }
                "debian.9" { $Dependencies += @("libssl1.0.2", "libicu57") }
                default { throw "Debian distro '$Distribution' is not supported." }
            }
        } elseif ($Environment.IsRedHatFamily) {
            $Dependencies = @(
                "openssl-libs",
                "libicu"
            )
        } elseif ($Environment.IsSUSEFamily) {
            $Dependencies = @(
                "libopenssl1_0_0",
                "libicu"
            )
        }

        return $Dependencies
    }
}

function Test-Dependencies
{
    foreach ($Dependency in "fpm", "ronn") {
        if (!(precheck $Dependency "Package dependency '$Dependency' not found. Run Start-PSBootstrap -Package")) {
            # These tools are not added to the path automatically on OpenSUSE 13.2
            # try adding them to the path and re-tesing first
            [string] $gemsPath = $null
            [string] $depenencyPath = $null
            $gemsPath = Get-ChildItem -Path /usr/lib64/ruby/gems | Sort-Object -Property LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
            if ($gemsPath) {
                $depenencyPath  = Get-ChildItem -Path (Join-Path -Path $gemsPath -ChildPath "gems" -AdditionalChildPath $Dependency) -Recurse | Sort-Object -Property LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty DirectoryName
                $originalPath = $env:PATH
                $env:PATH = $ENV:PATH +":" + $depenencyPath
                if ((precheck $Dependency "Package dependency '$Dependency' not found. Run Start-PSBootstrap -Package")) {
                    continue
                }
                else {
                    $env:PATH = $originalPath
                }
            }

            throw "Dependency precheck failed!"
        }
    }
}

function New-AfterScripts
{
    param(
        [Parameter(Mandatory)]
        [string]
        $Link
    )

    if ($Environment.IsRedHatFamily) {
        # add two symbolic links to system shared libraries that libmi.so is dependent on to handle
        # platform specific changes. This is the only set of platforms needed for this currently
        # as Ubuntu has these specific library files in the platform and macOS builds for itself
        # against the correct versions.
        New-Item -Force -ItemType SymbolicLink -Target "/lib64/libssl.so.10" -Path "$Staging/libssl.so.1.0.0" > $null
        New-Item -Force -ItemType SymbolicLink -Target "/lib64/libcrypto.so.10" -Path "$Staging/libcrypto.so.1.0.0" > $null

        $AfterInstallScript = [io.path]::GetTempFileName()
        $AfterRemoveScript = [io.path]::GetTempFileName()
        $packagingStrings.RedHatAfterInstallScript -f "$Link" | Out-File -FilePath $AfterInstallScript -Encoding ascii
        $packagingStrings.RedHatAfterRemoveScript -f "$Link" | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }
    elseif ($Environment.IsUbuntu -or $Environment.IsDebian -or $Environment.IsSUSEFamily) {
        $AfterInstallScript = [io.path]::GetTempFileName()
        $AfterRemoveScript = [io.path]::GetTempFileName()
        $packagingStrings.UbuntuAfterInstallScript -f "$Link" | Out-File -FilePath $AfterInstallScript -Encoding ascii
        $packagingStrings.UbuntuAfterRemoveScript -f "$Link" | Out-File -FilePath $AfterRemoveScript -Encoding ascii

        if ($Environment.IsDebian9) {
            # add two symbolic links to system shared libraries that libmi.so is dependent on to handle
            # platform specific changes. This appears to be a change in Debian 9; Debian 8 did not need these
            # symlinks.
            New-Item -Force -ItemType SymbolicLink -Target "/usr/lib/x86_64-linux-gnu/libssl.so.1.0.2" -Path "$Staging/libssl.so.1.0.0" > $null
            New-Item -Force -ItemType SymbolicLink -Target "/usr/lib/x86_64-linux-gnu/libcrypto.so.1.0.2" -Path "$Staging/libcrypto.so.1.0.0" > $null
        }
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
        $IsPreview
    )

    Write-Log "Creating man gz..."
    # run ronn to convert man page to roff
    $RonnFile = "$RepoRoot/assets/pwsh.1.ronn"
    if ($IsPreview.IsPresent)
    {
        $newRonnFile = $RonnFile -replace 'pwsh', 'pwsh-preview'
        Copy-Item -Path $RonnFile -Destination $newRonnFile -force
        $RonnFile = $newRonnFile
    }

    $RoffFile = $RonnFile -replace "\.ronn$"

    # Run ronn on assets file
    Write-Log "Creating man gz - running ronn..."
    Start-NativeExecution { ronn --roff $RonnFile }

    if ($IsPreview.IsPresent)
    {
        Remove-item $RonnFile
    }

    # gzip in assets directory
    $GzipFile = "$RoffFile.gz"
    Write-Log "Creating man gz - running gzip..."
    Start-NativeExecution { gzip -f $RoffFile } -VerboseOutputOnError

    $ManFile = Join-Path "/usr/local/share/man/man1" (Split-Path -Leaf $GzipFile)

    return [PSCustomObject ] @{
        GZipFile = $GzipFile
        ManFile = $ManFile
    }
}

# Returns the macOS Package Identifier
function Get-MacOSPackageId
{
    param(
        [switch]
        $IsPreview
    )
    if ($IsPreview.IsPresent)
    {
        return 'com.microsoft.powershell-preview'
    }
    else
    {
        return 'com.microsoft.powershell'
    }
}

# Dynamically build macOS launcher application.
function New-MacOSLauncher
{
    param(
        [Parameter(Mandatory)]
        [String]$Version
    )

    $IsPreview = Test-IsPreview -Version $Version
    $packageId = Get-MacOSPackageId -IsPreview:$IsPreview

    # Define folder for launcher application.
    $suffix = if ($IsPreview) { "-preview" }
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
    $executablepath = if ($IsPreview) { "/usr/local/bin/pwsh-preview" } else { "/usr/local/bin/pwsh" }
    $shellscript = "$macosapp/Contents/MacOS/PowerShell.sh"
    $shellscriptcontent = $packagingStrings.MacOSLauncherScript -f $executablepath
    $shellscriptcontent | Out-File -Force -Path $shellscript -Encoding utf8

    # Set permissions for plist and shell script.
    Start-NativeExecution {
        chmod 644 $plist
        chmod 755 $shellscript
    }

    # Add app folder to fpm paths.
    $appsfolder = (Resolve-Path -Path "$macosapp/..").Path

    return $appsfolder
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
        $StagingPath
    )

    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $StagingPath
    Copy-Item -Recurse $PackageSourcePath $StagingPath
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

        [switch] $Force
    )

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $PackageVersion

    $zipPackageName = $PackageName + "-" + $ProductSemanticVersion
    if ($PackageNameSuffix) {
        $zipPackageName = $zipPackageName, $PackageNameSuffix -join "-"
    }

    Write-Verbose "Create Zip for Product $zipPackageName"

    $zipLocationPath = Join-Path $PWD "$zipPackageName.zip"

    if ($Force.IsPresent)
    {
        if (Test-Path $zipLocationPath)
        {
            Remove-Item $zipLocationPath
        }
    }

    if (Get-Command Compress-Archive -ErrorAction Ignore)
    {
        if ($pscmdlet.ShouldProcess("Create zip package"))
        {
            Compress-Archive -Path $PackageSourcePath\* -DestinationPath $zipLocationPath
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
    #TODO: Use .NET Api to do compresss-archive equivalent if the pscmdlet is not present
    else
    {
        Write-Error -Message "Compress-Archive cmdlet is missing in this PowerShell version"
    }
}

function CreateNugetPlatformFolder
{
    param(
        [Parameter(Mandatory = $true)]
        [string] $Platform,

        [Parameter(Mandatory = $true)]
        [string] $PackageRuntimesFolder,

        [Parameter(Mandatory = $true)]
        [string] $PlatformBinPath
    )

    $destPath = New-Item -ItemType Directory -Path (Join-Path $PackageRuntimesFolder "$Platform/lib/netcoreapp2.1")
    $fullPath = Join-Path $PlatformBinPath $file

    if (-not(Test-Path $fullPath)) {
        throw "File not found: $fullPath"
    }

    Copy-Item -Path $fullPath -Destination $destPath
    Write-Log "Copied $file to $Platform"
}

<#
.SYNOPSIS
Creates NuGet packages containing linux, osx and Windows runtime assemblies.

.DESCRIPTION
Creates a NuGet package of IL assemblies for unix and windows.
The packages for Microsoft.PowerShell.Commands.Diagnostics, Microsoft.PowerShell.Commands.Management,
Microsoft.PowerShell.Commands.Utility, Microsoft.PowerShell.ConsoleHost, Microsoft.PowerShell.CoreCLR.Eventing,
Microsoft.PowerShell.SDK, Microsoft.PowerShell.Security, Microsoft.WSMan.Management, Microsoft.WSMan.Runtime,
System.Management.Automation are created.

.PARAMETER PackagePath
Path where the package will be created.

.PARAMETER PackageVersion
Version of the created package.

.PARAMETER WinFxdBinPath
Path to folder containing Windows framework dependent assemblies.

.PARAMETER LinuxFxdBinPath
Path to folder containing Linux framework dependent assemblies.

.PARAMETER GenAPIToolPath
Path to the GenAPI.exe tool.
#>
function New-ILNugetPackage
{
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(

        [Parameter(Mandatory = $true)]
        [string] $PackagePath,

        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        [Parameter(Mandatory = $true)]
        [string] $WinFxdBinPath,

        [Parameter(Mandatory = $true)]
        [string] $LinuxFxdBinPath,

        [Parameter(Mandatory = $true)]
        [string] $GenAPIToolPath
    )

    if (-not $Environment.IsWindows)
    {
        throw "New-ILNugetPackage can be only executed on Windows platform."
    }

    $fileList = @(
        "Microsoft.PowerShell.Commands.Diagnostics.dll",
        "Microsoft.PowerShell.Commands.Management.dll",
        "Microsoft.PowerShell.Commands.Utility.dll",
        "Microsoft.PowerShell.ConsoleHost.dll",
        "Microsoft.PowerShell.CoreCLR.Eventing.dll",
        "Microsoft.PowerShell.Security.dll",
        "Microsoft.PowerShell.SDK.dll",
        "Microsoft.WSMan.Management.dll",
        "Microsoft.WSMan.Runtime.dll",
        "System.Management.Automation.dll",
        "Microsoft.PowerShell.MarkdownRender.dll")

    $linuxExceptionList = @(
        "Microsoft.PowerShell.Commands.Diagnostics.dll",
        "Microsoft.WSMan.Management.dll",
        "Microsoft.WSMan.Runtime.dll")

    if ($PSCmdlet.ShouldProcess("Create nuget packages at: $PackagePath"))
    {

        $refBinPath = New-TempFolder
        $SnkFilePath = "$RepoRoot\src\signing\visualstudiopublic.snk"

        New-ReferenceAssembly -linux64BinPath $LinuxFxdBinPath -RefAssemblyDestinationPath $refBinPath -RefAssemblyVersion $PackageVersion -SnkFilePath $SnkFilePath -GenAPIToolPath $GenAPIToolPath

        foreach ($file in $fileList)
        {
            $tmpPackageRoot = New-TempFolder
            # Remove '.dll' at the end
            $fileBaseName = [System.IO.Path]::GetFileNameWithoutExtension($file)
            $filePackageFolder = New-Item (Join-Path $tmpPackageRoot $fileBaseName) -ItemType Directory -Force
            $packageRuntimesFolder = New-Item (Join-Path $filePackageFolder.FullName 'runtimes') -ItemType Directory

            #region ref
            $refFolder = New-Item (Join-Path $filePackageFolder.FullName 'ref/netcoreapp2.1') -ItemType Directory -Force
            CopyReferenceAssemblies -assemblyName $fileBaseName -refBinPath $refBinPath -refNugetPath $refFolder -assemblyFileList $fileList
            #endregion ref

            $packageRuntimesFolderPath = $packageRuntimesFolder.FullName

            CreateNugetPlatformFolder -Platform 'win' -PackageRuntimesFolder $packageRuntimesFolderPath -PlatformBinPath $WinFxdBinPath

            if ($linuxExceptionList -notcontains $file )
            {
                CreateNugetPlatformFolder -Platform 'unix' -PackageRuntimesFolder $packageRuntimesFolderPath -PlatformBinPath $LinuxFxdBinPath
            }

            #region nuspec
            # filed a tracking bug for automating generation of dependecy list: https://github.com/PowerShell/PowerShell/issues/6247
            $deps = [System.Collections.ArrayList]::new()

            switch ($fileBaseName) {
                'Microsoft.PowerShell.Commands.Diagnostics' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                }

                'Microsoft.PowerShell.Commands.Management' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Security'), [tuple]::Create('version', $PackageVersion))) > $null
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                }

                'Microsoft.PowerShell.Commands.Utility' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.MarkdownRender'), [tuple]::Create('version', $PackageVersion))) > $null

                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                }

                'Microsoft.PowerShell.ConsoleHost' {
                    $deps.Add([tuple]::Create( [tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                }

                'Microsoft.PowerShell.CoreCLR.Eventing' {
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
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
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.WSMan.Management'), [tuple]::Create('version', $PackageVersion))) > $null
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.Commands.Diagnostics'), [tuple]::Create('version', $PackageVersion))) > $null
                }

                'Microsoft.PowerShell.Security' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                }

                'Microsoft.WSMan.Management' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.WSMan.Runtime'), [tuple]::Create('version', $PackageVersion))) > $null
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                }

                'Microsoft.WSMan.Runtime' {
                    ## No dependencies
                }

                'System.Management.Automation' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.PowerShell.CoreCLR.Eventing'), [tuple]::Create('version', $PackageVersion))) > $null
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                }

                'Microsoft.PowerShell.MarkdownRender' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                    foreach($packageInfo in (Get-ProjectPackageInformation -ProjectName $fileBaseName))
                    {
                        $deps.Add([tuple]::Create([tuple]::Create('id', $packageInfo.Name), [tuple]::Create('version', $packageInfo.Version))) > $null
                    }
                }
            }

            New-NuSpec -PackageId $fileBaseName -PackageVersion $PackageVersion -Dependency $deps -FilePath (Join-Path $filePackageFolder.FullName "$fileBaseName.nuspec")
            New-NugetPackage -NuSpecPath $filePackageFolder.FullName -PackageDestinationPath $PackagePath
        }

        if (Test-Path $refBinPath)
        {
            Remove-Item $refBinPath -Recurse -Force -ErrorAction SilentlyContinue
        }

        if (Test-Path $tmpPackageRoot)
        {
            Remove-Item $tmpPackageRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

<#
  Copy the generated reference assemblies to the 'ref/netcoreapp2.1' folder properly.
  This is a helper function used by 'New-ILNugetPackage'
#>
function CopyReferenceAssemblies
{
    param(
        [string] $assemblyName,
        [string] $refBinPath,
        [string] $refNugetPath,
        [string[]] $assemblyFileList
    )

    switch ($assemblyName) {
        "Microsoft.PowerShell.Commands.Utility" {
            $ref_Utility = Join-Path -Path $refBinPath -ChildPath Microsoft.PowerShell.Commands.Utility.dll
            Copy-Item $ref_Utility -Destination $refNugetPath -Force
            Write-Log "Copied file $ref_Utility to $refNugetPath"
        }

        "Microsoft.PowerShell.SDK" {
            foreach ($asmFileName in $assemblyFileList) {
                $refFile = Join-Path -Path $refBinPath -ChildPath $asmFileName
                if (Test-Path -Path $refFile) {
                    Copy-Item $refFile -Destination $refNugetPath -Force
                    Write-Log "Copied file $refFile to $refNugetPath"
                }
            }
        }

        default {
            $ref_SMA = Join-Path -Path $refBinPath -ChildPath System.Management.Automation.dll
            Copy-Item $ref_SMA -Destination $refNugetPath -Force
            Write-Log "Copied file $ref_SMA to $refNugetPath"
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
    [xml] $csprojXml = (Get-content -Raw -Path $csproj)

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
        # Second tuple defines 'version' and vale eg: ["version", "4.4.2"]
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

    $nuspecTemplate = $packagingStrings.NuspecTemplate -f $PackageId,$PackageVersion
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
    $packagingStrings.NugetConfigFile | Out-File -FilePath "$genAPIFolder/Nuget.config" -Force
    Write-Log "Working directory: $genAPIFolder."

    $SMAReferenceAssembly = $null
    $assemblyNames = @(
        "System.Management.Automation",
        "Microsoft.PowerShell.Commands.Utility"
    )

    foreach ($assemblyName in $assemblyNames) {

        Write-Log "Building reference assembly for '$assemblyName'"
        $projectFolder = New-Item -Path "$genAPIFolder/$assemblyName" -ItemType Directory -Force
        $generatedSource = Join-Path $projectFolder "$assemblyName.cs"
        $filteredSource = Join-Path $projectFolder "${assemblyName}_Filtered.cs"

        $linuxDllPath = Join-Path $Linux64BinPath "$assemblyName.dll"
        if (-not (Test-Path $linuxDllPath)) {
            throw "$assemblyName.dll was not found at: $Linux64BinPath"
        }

        $genAPIArgs = "$linuxDllPath","-libPath:$Linux64BinPath,$Linux64BinPath\ref"
        Write-Log "GenAPI cmd: $genAPIExe $genAPIArgsString"

        Start-NativeExecution { & $genAPIExe $genAPIArgs } | Out-File $generatedSource -Force
        Write-Log "Reference assembly file generated at: $generatedSource"

        CleanupGeneratedSourceCode -assemblyName $assemblyName -generatedSource $generatedSource -filteredSource $filteredSource

        try
        {
            Push-Location $projectFolder

            $sourceProjectRoot = Join-Path $PSScriptRoot "projects/reference/$assemblyName"
            $sourceProjectFile = Join-Path $sourceProjectRoot "$assemblyName.csproj"
            Copy-Item -Path $sourceProjectFile -Destination "$projectFolder/$assemblyName.csproj" -Force

            Write-Host "##vso[artifact.upload containerfolder=artifact;artifactname=artifact]$projectFolder/$assemblyName.csproj"
            Write-Host "##vso[artifact.upload containerfolder=artifact;artifactname=artifact]$generatedSource"

            $arguments = GenerateBuildArguments -AssemblyName $assemblyName -RefAssemblyVersion $RefAssemblyVersion -SnkFilePath $SnkFilePath -SMAReferencePath $SMAReferenceAssembly

            Write-Log "Running: dotnet $arguments"
            Start-NativeExecution -sb {dotnet $arguments}

            $refBinPath = Join-Path $projectFolder "bin/Release/netcoreapp2.1/$assemblyName.dll"
            if ($null -eq $refBinPath) {
                throw "Reference assembly was not built."
            }

            Copy-Item $refBinPath $RefAssemblyDestinationPath -Force
            Write-Log "Reference assembly '$assemblyName.dll' built and copied to $RefAssemblyDestinationPath"

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
        '[System.Management.Automation.OutputTypeAttribute(typeof(Microsoft.PowerShell.MarkdownRender'
        '[Microsoft.PowerShell.Commands.ArgumentToTypeNameTransformationAttribute]'
        '[System.Management.Automation.Internal.ArchitectureSensitiveAttribute]'
        '[Microsoft.PowerShell.Commands.SelectStringCommand.FileinfoToStringAttribute]'
        '[System.Runtime.CompilerServices.IsReadOnlyAttribute]'
        )

    $patternsToReplace = @(
        @{
            ApplyTo = "Microsoft.PowerShell.Commands.Utility"
            Pattern = "[System.Runtime.CompilerServices.IsReadOnlyAttribute]ref Microsoft.PowerShell.Commands.JsonObject.ConvertToJsonContext"
            Replacement = "in Microsoft.PowerShell.Commands.JsonObject.ConvertToJsonContext"
        },
        @{
            ApplyTo = "Microsoft.PowerShell.Commands.Utility"
            Pattern = "public partial struct ConvertToJsonContext"
            Replacement = "public readonly struct ConvertToJsonContext"
        },
        @{
            ApplyTo = "Microsoft.PowerShell.Commands.Utility"
            Pattern = "Unable to resolve assembly 'Assembly(Name=Newtonsoft.Json"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=Newtonsoft.Json"
        },
        @{
            ApplyTo = "System.Management.Automation"
            Pattern = "Unable to resolve assembly 'Assembly(Name=System.Security.Principal.Windows"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=System.Security.Principal.Windows"
        },
        @{
            ApplyTo = "System.Management.Automation"
            Pattern = "Unable to resolve assembly 'Assembly(Name=Microsoft.Management.Infrastructure"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=Microsoft.Management.Infrastructure"
        },
        @{
            ApplyTo = "System.Management.Automation"
            Pattern = "Unable to resolve assembly 'Assembly(Name=System.Security.AccessControl"
            Replacement = "// Unable to resolve assembly 'Assembly(Name=System.Security.AccessControl"
        }
    )

    $reader = [System.IO.File]::OpenText($generatedSource)
    $writer = [System.IO.File]::CreateText($filteredSource)

    while($null -ne ($line = $reader.ReadLine()))
    {
        $lineWasProcessed = $false
        foreach ($patternToReplace in $patternsToReplace)
        {
            if ($assemblyName -eq $patternToReplace.ApplyTo -and $line.Contains($patternToReplace.Pattern)) {
                $line = $line.Replace($patternToReplace.Pattern, $patternToReplace.Replacement)
                $lineWasProcessed = $true
                break
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

    switch ($AssemblyName) {
        "Microsoft.PowerShell.Commands.Utility" {
            $arguments += "/p:SmaRefFile=$SMAReferencePath"
        }
    }

    return $arguments
}

<#
.SYNOPSIS
Create a NuGet package from a nuspec.

.DESCRIPTION
Creates a NuGet using the nuspec using at the specified folder.
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

<#
.SYNOPSIS
The function creates a nuget package for daily feed.

.DESCRIPTION
The nuget package created is a content package and has all the binaries laid out in a flat structure.
This package is used by install-powershell.ps1
#>
function New-NugetContentPackage
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (

        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $PackageName = 'powershell',

        # Suffix of the Name
        [string] $PackageNameSuffix,

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageVersion,

        # Runtime of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageRuntime,

        # Configuration of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageConfiguration,

        # Source Path to the Product Files - required to package the contents into an Zip
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageSourcePath,

        [Switch]
        $Force
    )

    Write-Log "PackageVersion: $PackageVersion"
    $nugetSemanticVersion = Get-NugetSemanticVersion -Version $PackageVersion
    Write-Log "nugetSemanticVersion: $nugetSemanticVersion"

    $nugetFolder = New-SubFolder -Path $PSScriptRoot -ChildPath 'nugetOutput' -Clean

    $nuspecPackageName = $PackageName
    if ($PackageNameSuffix)
    {
        $nuspecPackageName += '-' + $PackageNameSuffix
    }

    # Setup staging directory so we don't change the original source directory
    $stagingRoot = New-SubFolder -Path $PSScriptRoot -ChildPath 'nugetStaging' -Clean
    $contentFolder = Join-Path -path $stagingRoot -ChildPath 'content'
    if ($pscmdlet.ShouldProcess("Create staging folder")) {
        New-StagingFolder -StagingPath $contentFolder
    }

    $projectFolder = Join-Path $PSScriptRoot 'projects/nuget'

    $arguments = @('pack')
    $arguments += @('--output',$nugetFolder)
    $arguments += @('--configuration',$PackageConfiguration)
    $arguments += @('--runtime',$PackageRuntime)
    $arguments += "/p:StagingPath=$stagingRoot"
    $arguments += "/p:RID=$PackageRuntime"
    $arguments += "/p:SemVer=$nugetSemanticVersion"
    $arguments += "/p:PackageName=$nuspecPackageName"
    $arguments += $projectFolder

    Write-Log "Running dotnet $arguments"
    Write-Log "Use -verbose to see output..."
    Start-NativeExecution -sb {dotnet $arguments} | Foreach-Object {Write-Verbose $_}

    $nupkgFile = "${nugetFolder}\${nuspecPackageName}-${packageRuntime}.${nugetSemanticVersion}.nupkg"
    if (Test-Path $nupkgFile)
    {
        Get-Item $nupkgFile
    }
    else
    {
        throw "Failed to create $nupkgFile"
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
    $wixToolsetBinPath = "${env:ProgramFiles(x86)}\WiX Toolset *\bin"

    Write-Verbose "Ensure Wix Toolset is present on the machine @ $wixToolsetBinPath"
    if (-not (Test-Path $wixToolsetBinPath))
    {
        throw "The latest version of Wix Toolset 3.11 is required to create MSI package. Please install it from https://github.com/wixtoolset/wix3/releases"
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

    return [PSCustomObject] @{
        WixHeatExePath = $wixHeatExePath
        WixMeltExePath = $wixMeltExePath
        WixTorchExePath = $wixTorchExePath
        WixPyroExePath = $wixPyroExePath
        WixCandleExePath = $wixCandleExePath
        WixLightExePath = $wixLightExePath
    }

}

<#
    .Synopsis
        Creates a Windows installer MSP package from two MSIs and WIXPDB files
        This only works on a Windows machine due to the usage of WiX.
    .EXAMPLE
        # This example shows how to produce a x64 patch from 6.0.2 to a theoretical 6.0.3
        cd $RootPathOfPowerShellRepo
        Import-Module .\build.psm1; Import-Module .\tools\packaging\packaging.psm1
        New-MSIPatch -NewVersion 6.0.1 -BaselineMsiPath .\PowerShell-6.0.2-win-x64.msi -BaselineWixPdbPath .\PowerShell-6.0.2-win-x64.wixpdb -PatchMsiPath .\PowerShell-6.0.3-win-x64.msi -PatchWixPdbPath .\PowerShell-6.0.3-win-x64.wixpdb
#>
function New-MSIPatch
{
    param(
        [Parameter(Mandatory, HelpMessage='The version of the fixed or patch MSI.')]
        [ValidatePattern("^\d+\.\d+\.\d+$")]
        [string] $NewVersion,

        [Parameter(Mandatory, HelpMessage='The path to the original or baseline MSI.')]
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {(Test-Path $_) -and $_ -like '*.msi'})]
        [string] $BaselineMsiPath,

        [Parameter(Mandatory, HelpMessage='The path to the WIXPDB for the original or baseline MSI.')]
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {(Test-Path $_) -and $_ -like '*.wixpdb'})]
        [string] $BaselineWixPdbPath,

        [Parameter(Mandatory, HelpMessage='The path to the fixed or patch MSI.')]
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {(Test-Path $_) -and $_ -like '*.msi'})]
        [string] $PatchMsiPath,

        [Parameter(Mandatory, HelpMessage='The path to the WIXPDB for the fixed or patch MSI.')]
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {(Test-Path $_) -and $_ -like '*.wixpdb'})]
        [string] $PatchWixPdbPath,

        [Parameter(HelpMessage='Path to the patch template WXS.  Usually you do not need to specify this')]
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $PatchWxsPath = "$RepoRoot\assets\patch-template.wxs",

        [Parameter(HelpMessage='Produce a delta patch instead of a full patch.  Usually not worth it.')]
        [switch] $Delta
    )

    $mspName = (Split-Path -Path $PatchMsiPath -Leaf).Replace('.msi','.fullpath.msp')
    $mspDeltaName = (Split-Path -Path $PatchMsiPath -Leaf).Replace('.msi','.deltapatch.msp')

    $wixPatchXmlPath = Join-Path $env:Temp "patch.wxs"
    $wixBaselineOriginalPdbPath = Join-Path $env:Temp "baseline.original.wixpdb"
    $wixBaselinePdbPath = Join-Path $env:Temp "baseline.wixpdb"
    $wixBaselineBinariesPath = Join-Path $env:Temp "baseline.binaries"
    $wixPatchOriginalPdbPath = Join-Path $env:Temp "patch.original.wixpdb"
    $wixPatchPdbPath = Join-Path $env:Temp "patch.wixpdb"
    $wixPatchBinariesPath = Join-Path $env:Temp "patch.binaries"
    $wixPatchMstPath = Join-Path $env:Temp "patch.wixmst"
    $wixPatchObjPath = Join-Path $env:Temp "patch.wixobj"
    $wixPatchWixMspPath = Join-Path $env:Temp "patch.wixmsp"

    $filesToCleanup = @(
        $wixPatchXmlPath
        $wixBaselinePdbPath
        $wixBaselineBinariesPath
        $wixPatchPdbPath
        $wixPatchBinariesPath
        $wixPatchMstPath
        $wixPatchObjPath
        $wixPatchWixMspPath
        $wixPatchOriginalPdbPath
        $wixBaselineOriginalPdbPath
    )

    # cleanup from previous builds
    Remove-Item -Path $filesToCleanup -Force -Recurse -ErrorAction SilentlyContinue

    # Melt changes the original, so copy before running melt
    Copy-Item -Path $BaselineWixPdbPath -Destination $wixBaselineOriginalPdbPath -Force
    Copy-Item -Path $PatchWixPdbPath -Destination $wixPatchOriginalPdbPath -Force

    [xml] $filesAssetXml = Get-Content -Raw -Path "$RepoRoot\assets\files.wxs"
    [xml] $patchTemplateXml = Get-Content -Raw -Path $PatchWxsPath

    # Update the patch version
    $patchFamilyNode = $patchTemplateXml.Wix.Fragment.PatchFamily
    $patchFamilyNode.SetAttribute('Version', $NewVersion)

    # get all the file components from the files.wxs
    $components = $filesAssetXml.GetElementsByTagName('Component')

    # add all the file components to the patch
    foreach($component in $components)
    {
        $id = $component.Id
        $componentRef = $patchTemplateXml.CreateElement('ComponentRef','http://schemas.microsoft.com/wix/2006/wi')
        $idAttribute = $patchTemplateXml.CreateAttribute('Id')
        $idAttribute.Value = $id
        $null = $componentRef.Attributes.Append($idAttribute)
        $null = $patchFamilyNode.AppendChild($componentRef)
    }

    # save the updated patch xml
    $patchTemplateXml.Save($wixPatchXmlPath)

    $wixPaths = Get-WixPath

    Write-Log "Processing baseline msi..."
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixMeltExePath -nologo $BaselineMsiPath $wixBaselinePdbPath -pdb $wixBaselineOriginalPdbPath -x $wixBaselineBinariesPath}

    Write-Log "Processing patch msi..."
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixMeltExePath -nologo $PatchMsiPath $wixPatchPdbPath -pdb $wixPatchOriginalPdbPath -x $wixPatchBinariesPath}

    Write-Log  "generate diff..."
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixTorchExePath -nologo -p -xi $wixBaselinePdbPath $wixPatchPdbPath -out $wixPatchMstPath}

    Write-Log  "Compiling patch..."
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixCandleExePath -nologo $wixPatchXmlPath -out $wixPatchObjPath}

    Write-Log  "Linking patch..."
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixLightExePath -nologo $wixPatchObjPath -out $wixPatchWixMspPath}

    if ($Delta.IsPresent)
    {
        Write-Log  "Generating delta msp..."
        Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixPyroExePath -nologo $wixPatchWixMspPath -out $mspDeltaName -t RTM $wixPatchMstPath }
    }
    else
    {
        Write-Log  "Generating full msp..."
        Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixPyroExePath -nologo $wixPatchWixMspPath -out $mspName -t RTM $wixPatchMstPath }
    }

    # cleanup temporary files
    Remove-Item -Path $filesToCleanup -Force -Recurse -ErrorAction SilentlyContinue
}

<#
    .Synopsis
        Creates a Windows installer MSI package and assumes that the binaries are already built using 'Start-PSBuild'.
        This only works on a Windows machine due to the usage of WiX.
    .EXAMPLE
        # This example shows how to produce a Debug-x64 installer for development purposes.
        cd $RootPathOfPowerShellRepo
        Import-Module .\build.psm1; Import-Module .\tools\packaging\packaging.psm1
        New-MSIPackage -Verbose -ProductCode (New-Guid) -ProductSourcePath '.\src\powershell-win-core\bin\Debug\netcoreapp2.1\win7-x64\publish' -ProductTargetArchitecture x64 -ProductVersion '1.2.3'
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

        # The ProductCode property is a unique identifier for the particular product release
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductCode,

        # Source Path to the Product Files - required to package the contents into an MSI
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductSourcePath,

        # File describing the MSI Package creation semantics
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $ProductWxsPath = "$RepoRoot\assets\Product.wxs",

        # File describing the MSI file components
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $FilesWxsPath = "$RepoRoot\assets\Files.wxs",

        # Path to Assets folder containing artifacts such as icons, images
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $AssetsPath = "$RepoRoot\assets",

        # Path to license.rtf file - for the EULA
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $LicenseFilePath = "$RepoRoot\assets\license.rtf",

        # Architecture to use when creating the MSI
        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture,

        # Force overwrite of package
        [Switch] $Force
    )

    $wixPaths = Get-WixPath

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $ProductVersion
    $simpleProductVersion = '6'
    $isPreview = Test-IsPreview -Version $ProductSemanticVersion
    if ($isPreview)
    {
        $simpleProductVersion += '-preview'
    }

    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion

    $assetsInSourcePath = Join-Path $ProductSourcePath 'assets'
    New-Item $assetsInSourcePath -type directory -Force | Write-Verbose

    Write-Verbose "Place dependencies such as icons to $assetsInSourcePath"
    Copy-Item "$AssetsPath\*.ico" $assetsInSourcePath -Force

    $productVersionWithName = $ProductName + '_' + $ProductVersion
    $productSemanticVersionWithName = $ProductName + '-' + $ProductSemanticVersion
    $productDirectoryName = 'PowerShell_6'

    Write-Verbose "Create MSI for Product $productSemanticVersionWithName"

    [Environment]::SetEnvironmentVariable("ProductSourcePath", $ProductSourcePath, "Process")
    # These variables are used by Product.wxs in assets directory
    [Environment]::SetEnvironmentVariable("ProductDirectoryName", $productDirectoryName, "Process")
    [Environment]::SetEnvironmentVariable("ProductName", $ProductName, "Process")
    [Environment]::SetEnvironmentVariable("ProductCode", $ProductCode, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersion", $ProductVersion, "Process")
    [Environment]::SetEnvironmentVariable("SimpleProductVersion", $simpleProductVersion, "Process")
    [Environment]::SetEnvironmentVariable("ProductSemanticVersion", $ProductSemanticVersion, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersionWithName", $productVersionWithName, "Process")
    if (!$isPreview)
    {
        [Environment]::SetEnvironmentVariable("PwshPath", "[$productDirectoryName]", "Process")
        [Environment]::SetEnvironmentVariable("UpgradeCodeX64", '31ab5147-9a97-4452-8443-d9709f0516e1', "Process")
        [Environment]::SetEnvironmentVariable("UpgradeCodeX86", '1d00683b-0f84-4db8-a64f-2f98ad42fe06', "Process")
        [Environment]::SetEnvironmentVariable("IconPath", 'assets\Powershell_black.ico', "Process")
        # The ApplicationProgramsMenuShortcut GUID should be changed when bumping the major version because the installation directory changes
        [Environment]::SetEnvironmentVariable("ApplicationProgramsMenuShortcut", '6a69de6c-183d-4bf4-a40e-83007d6293bf', "Process")
    }
    else
    {
        [Environment]::SetEnvironmentVariable("PwshPath", "[$productDirectoryName]preview", "Process")
        [Environment]::SetEnvironmentVariable("UpgradeCodeX64", '39243d76-adaf-42b1-94fb-16ecf83237c8', "Process")
        [Environment]::SetEnvironmentVariable("UpgradeCodeX86", '86abcfbd-1ccc-4a88-b8b2-0facfde29094', "Process")
        [Environment]::SetEnvironmentVariable("IconPath", 'assets\Powershell_av_colors.ico', "Process")
        [Environment]::SetEnvironmentVariable("ApplicationProgramsMenuShortcut", 'ab727c4f-2311-474c-9ade-f2c6fd7f7322', "Process")
    }
    $fileArchitecture = 'amd64'
    $ProductProgFilesDir = "ProgramFiles64Folder"
    if ($ProductTargetArchitecture -eq "x86")
    {
        $fileArchitecture = 'x86'
        $ProductProgFilesDir = "ProgramFilesFolder"
    }
    [Environment]::SetEnvironmentVariable("ProductProgFilesDir", $ProductProgFilesDir, "Process")
    [Environment]::SetEnvironmentVariable("FileArchitecture", $fileArchitecture, "Process")

    $wixFragmentPath = Join-Path $env:Temp "Fragment.wxs"
    $wixObjProductPath = Join-Path $env:Temp "Product.wixobj"
    $wixObjFragmentPath = Join-Path $env:Temp "files.wixobj"

    # cleanup any garbage on the system
    Remove-Item -ErrorAction SilentlyContinue $wixFragmentPath -Force
    Remove-Item -ErrorAction SilentlyContinue $wixObjProductPath -Force
    Remove-Item -ErrorAction SilentlyContinue $wixObjFragmentPath -Force

    $packageName = $productSemanticVersionWithName
    if ($ProductNameSuffix) {
        $packageName += "-$ProductNameSuffix"
    }
    $msiLocationPath = Join-Path $pwd "$packageName.msi"
    $msiPdbLocationPath = Join-Path $pwd "$packageName.wixpdb"

    if (!$Force.IsPresent -and (Test-Path -Path $msiLocationPath))
    {
        Write-Error -Message "Package already exists, use -Force to overwrite, path:  $msiLocationPath" -ErrorAction Stop
    }

    Write-Log "verifying no new files have been added or removed..."
    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixHeatExePath dir  $ProductSourcePath -dr  $productDirectoryName -cg $productDirectoryName -gg -sfrag -srd -scom -sreg -out $wixFragmentPath -var env.ProductSourcePath -v}

    # We are verifying that the generated $wixFragmentPath and $FilesWxsPath are functionally the same
    Test-FileWxs -FilesWxsPath $FilesWxsPath -HeatFilesWxsPath $wixFragmentPath

    if ($isPreview)
    {
        # Now that we know that the two are functionally the same,
        # We only need to use $FilesWxsPath for release we want to be able to Path
        # and two releases shouldn't have the same identifiers,
        # so we use the generated one for preview
        $FilesWxsPath = $wixFragmentPath

        $wixObjFragmentPath = Join-Path $env:Temp "Fragment.wixobj"

        # cleanup any garbage on the system
        Remove-Item -ErrorAction SilentlyContinue $wixObjFragmentPath -Force
    }

    Write-Log "running candle..."
    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixCandleExePath  "$ProductWxsPath"  "$FilesWxsPath" -out (Join-Path "$env:Temp" "\\") -ext WixUIExtension -ext WixUtilExtension -arch $ProductTargetArchitecture -v}

    Write-Log "running light..."
    # suppress ICE61, because we allow same version upgrades
    # suppress ICE57, this suppresses an error caused by our shortcut not being installed per user
    Start-NativeExecution -VerboseOutputOnError {& $wixPaths.wixLightExePath -sice:ICE61 -sice:ICE57 -out $msiLocationPath -pdbout $msiPdbLocationPath $wixObjProductPath $wixObjFragmentPath -ext WixUIExtension -ext WixUtilExtension -dWixUILicenseRtf="$LicenseFilePath"}

    Remove-Item -ErrorAction SilentlyContinue $wixFragmentPath -Force
    Remove-Item -ErrorAction SilentlyContinue $wixObjProductPath -Force
    Remove-Item -ErrorAction SilentlyContinue $wixObjFragmentPath -Force

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
        if ($null -ne $env:CI)
        {
           Add-AppveyorCompilationMessage $errorMessage -Category Error -FileName $MyInvocation.ScriptName -Line $MyInvocation.ScriptLineNumber
        }
        throw $errorMessage
    }
}

# verify no files have been added or removed
# if so, write an error with details
function Test-FileWxs
{
    param
    (
        # File describing the MSI file components from the asset folder
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $FilesWxsPath = "$RepoRoot\assets\Files.wxs",

        # File describing the MSI file components generated by heat
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $HeatFilesWxsPath
    )

    # Update the fileArchitecture in our file to the actual value.  Since, the heat file will have the actual value.
    # Wix will update this automaticaly, but the output is not the same xml
    $filesAssetString = (Get-Content -Raw -Path $FilesWxsPath).Replace('$(var.FileArchitecture)',$env:FileArchitecture)

    [xml] $filesAssetXml = $filesAssetString
    [xml] $newFilesAssetXml = $filesAssetString
    $xmlns=[System.Xml.XmlNamespaceManager]::new($newFilesAssetXml.NameTable)
    $xmlns.AddNamespace('Wix','http://schemas.microsoft.com/wix/2006/wi')

    [xml] $heatFilesXml = Get-Content -Raw -Path $HeatFilesWxsPath
    $assetFiles = $filesAssetXml.GetElementsByTagName('File')
    $heatFiles = $heatFilesXml.GetElementsByTagName('File')
    $heatNodesByFile = @{}

    # Index the list of files generated by heat
    foreach($file in $heatFiles)
    {
        $heatNodesByFile.Add($file.Source, $file)
    }

    # Index the files from the asset wxs
    # and verify that no files have been removed.
    $passed = $true
    $indexedAssetFiles = @()
    foreach($file in $assetFiles)
    {
        $name = $file.Source
        if ($heatNodesByFile.Keys -inotcontains $name)
        {
            $passed = $false
            Write-Warning "{$name} is no longer in product and should be removed from {$FilesWxsPath}"
            $componentId = $file.ParentNode.Id
            $componentXPath = '//Wix:Component[@Id="{0}"]' -f $componentId
            $componentNode = Get-XmlNodeByXPath -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns -XPath $componentXPath
            if ($componentNode)
            {
                # Remove the Component
                Remove-XmlElement -Element $componentNode -RemoveEmptyParents
                # Remove teh ComponentRef
                Remove-ComponentRefNode -Id $componentId -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns
            }
            else
            {
                Write-Warning "Could not remove this node!"
            }
        }
        $indexedAssetFiles += $name
    }

    # verify that no files have been added.
    foreach($file in $heatNodesByFile.Keys)
    {
        if ($indexedAssetFiles -inotcontains $file)
        {
            $passed = $false
            $folder = Split-Path -Path $file
            $name = Split-Path -Path $file -Leaf
            $heatNode = $heatNodesByFile[$file]
            $compGroupNode = Get-ComponentGroupNode -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns
            $filesNode = Get-DirectoryNode -Node $heatNode -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns
            # Create new Component
            $newComponent = New-XmlElement -XmlDoc $newFilesAssetXml -LocalName 'Component' -Node $filesNode -PassThru -NamespaceUri 'http://schemas.microsoft.com/wix/2006/wi'
            $componentId = New-WixId -Prefix 'cmp'
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newComponent -Name 'Id' -Value $componentId
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newComponent -Name 'Guid' -Value "{$(New-Guid)}"
            # Crete new File in Component
            $newFile = New-XmlElement -XmlDoc $newFilesAssetXml -LocalName 'File' -Node $newComponent -PassThru -NamespaceUri 'http://schemas.microsoft.com/wix/2006/wi'
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newFile -Name 'Id' -Value (New-WixId -Prefix 'fil')
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newFile -Name 'KeyPath' -Value "yes"
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newFile -Name 'Source' -Value $file
            # Create new ComponentRef
            $newComponentRef = New-XmlElement -XmlDoc $newFilesAssetXml -LocalName 'ComponentRef' -Node $compGroupNode -PassThru -NamespaceUri 'http://schemas.microsoft.com/wix/2006/wi'
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newComponentRef -Name 'Id' -Value $componentId

            Write-Warning "new file in {$folder} with name {$name} in a {$($filesNode.LocalName)} need to be added to {$FilesWxsPath}"
        }
    }

    if (!$passed)
    {
        $newXmlFileName = Join-Path -Path $env:TEMP -ChildPath ([System.io.path]::GetRandomFileName() + '.wxs')
        $newFilesAssetXml.Save($newXmlFileName)
        $newXml = Get-Content -raw $newXmlFileName
        $newXml = $newXml -replace 'amd64', '$(var.FileArchitecture)'
        $newXml = $newXml -replace 'x86', '$(var.FileArchitecture)'
        $newXml | Out-File -FilePath $newXmlFileName -Encoding ascii
        Write-Log -message "Updated xml saved to $newXmlFileName."
        Write-Log -message "If component files were intentionally changed, such as due to moving to a newer .NET Core runtime, update '$FilesWxsPath' with the content from '$newXmlFileName'."
        if ($env:TF_BUILD)
        {
            Write-Host "##vso[artifact.upload containerfolder=wix;artifactname=wix]$newXmlFileName"
        }

        throw "Current files to not match  {$FilesWxsPath}"
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

# Builds coming out of this project can have version number as 'a.b.c' OR 'a.b.c-d-f'
# This function converts the above version into major.minor[.build[.revision]] format
function Get-PackageVersionAsMajorMinorBuildRevision
{
    [CmdletBinding()]
    param (
        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Version
        )

    Write-Verbose "Extract the version in the form of major.minor[.build[.revision]] for $Version"
    $packageVersionTokens = $Version.Split('-')
    $packageVersion = ([regex]::matches($Version, "\d+(\.\d+)+"))[0].value

    if (1 -eq $packageVersionTokens.Count) {
        # In case the input is of the form a.b.c, add a '0' at the end for revision field
        $packageVersion = $packageVersion + '.0'
    } elseif (1 -lt $packageVersionTokens.Count) {
        # We have all the four fields
        $packageBuildTokens = ([regex]::Matches($packageVersionTokens[1], "\d+"))[0].value

        if ($packageBuildTokens)
        {
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
Create a Global tool nuget package for PowerShell.

.DESCRIPTION
If the UnifiedPackage switch is present, then create a packag with both Windows and Unix runtimes.
Else create two packages, one for Windows and other for Linux.

.PARAMETER LinuxBinPath
Path to the folder containing the fxdependent package for Linux.

.PARAMETER WindowsBinPath
Path to the folder containing the fxdependent package for Windows.

.PARAMETER PackageVersion
Version for the NuGet package that will be generated.

.PARAMETER DestinationPath
Path to the folder where the generated packages will be copied to.

.PARAMETER UnifiedPackage
Create package with both Windows and Unix runtimes.
#>
function New-GlobalToolNupkg
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $LinuxBinPath,
        [Parameter(Mandatory)] [string] $WindowsBinPath,
        [Parameter(Mandatory)] [string] $PackageVersion,
        [Parameter(Mandatory)] [string] $DestinationPath,
        [Parameter(ParameterSetName="UnifiedPackage")] [switch] $UnifiedPackage
    )

    $packageInfo = @()

    Remove-Item -Path (Join-Path $LinuxBinPath 'libcrypto.so.1.0.0') -Verbose -Force -Recurse
    Remove-Item -Path (Join-Path $LinuxBinPath 'libssl.so.1.0.0') -Verbose -Force -Recurse

    ## Remove unnecessary xml files
    Get-ChildItem -Path $LinuxBinPath, $WindowsBinPath -Filter *.xml | Remove-Item -Verbose

    if ($UnifiedPackage)
    {
        Write-Log "Creating a unified package"
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell"; Type = "Unified"}
        $ShimDllPath = Join-Path $WindowsBinPath "Microsoft.PowerShell.GlobalTool.Shim.dll"
    }
    else
    {
        Write-Log "Reducing size of Linux package"
        ReduceFxDependentPackage -Path $LinuxBinPath

        Write-Log "Reducing size of Windows package"
        ReduceFxDependentPackage -Path $WindowsBinPath -KeepWindowsRuntimes

        Write-Log "Creating a Linux and Windows packages"
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.Alpine"; Type = "PowerShell.Linux.Alpine"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.x64"; Type = "PowerShell.Linux.x64"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.arm32"; Type = "PowerShell.Linux.arm32"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.arm64"; Type = "PowerShell.Linux.arm64"}

        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Windows.x64"; Type = "PowerShell.Windows.x64"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Windows.arm32"; Type = "PowerShell.Windows.arm32"}
    }

    $packageInfo | ForEach-Object {
        $ridFolder = New-Item -Path (Join-Path $_.RootFolder "tools/netcoreapp2.1/any") -ItemType Directory

        $packageType = $_.Type

        switch ($packageType)
        {
            "Unified"
            {
                $winFolder = New-Item (Join-Path $ridFolder "win") -ItemType Directory
                $unixFolder = New-Item (Join-Path $ridFolder "unix") -ItemType Directory

                Write-Log "Copying runtime assemblies from $WindowsBinPath"
                Copy-Item "$WindowsBinPath\*" -Destination $winFolder -Recurse

                Write-Log "Copying runtime assemblies from $LinuxBinPath"
                Copy-Item "$LinuxBinPath\*" -Destination $unixFolder -Recurse

                Write-Log "Copying shim dll from $ShimDllPath"
                Copy-Item $ShimDllPath -Destination $ridFolder

                $shimConfigFile = Join-Path (Split-Path $ShimDllPath -Parent) 'Microsoft.PowerShell.GlobalTool.Shim.runtimeconfig.json'
                Write-Log "Copying shim config file from $shimConfigFile"
                Copy-Item $shimConfigFile -Destination $ridFolder -ErrorAction Stop

                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f (Split-Path $ShimDllPath -Leaf)
            }

            "PowerShell.Linux.Alpine"
            {
                Write-Log "Copying runtime assemblies from $LinuxBinPath for $packageType"
                Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
                Remove-Item -Path $ridFolder/runtimes/linux-arm -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-arm64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-x64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
            }

            "PowerShell.Linux.x64"
            {
                Write-Log "Copying runtime assemblies from $LinuxBinPath for $packageType"
                Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
                Remove-Item -Path $ridFolder/runtimes/linux-arm -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-arm64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-musl-x64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
            }

            "PowerShell.Linux.arm32"
            {
                Write-Log "Copying runtime assemblies from $LinuxBinPath for $packageType"
                Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
                Remove-Item -Path $ridFolder/runtimes/linux-arm64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-musl-x64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-x64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
            }

            "PowerShell.Linux.arm64"
            {
                Write-Log "Copying runtime assemblies from $LinuxBinPath for $packageType"
                Copy-Item "$LinuxBinPath/*" -Destination $ridFolder -Recurse
                Remove-Item -Path $ridFolder/runtimes/linux-arm -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-musl-x64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/linux-x64 -Recurse -Force
                Remove-Item -Path $ridFolder/runtimes/osx -Recurse -Force
                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
            }

            "PowerShell.Windows.x64"
            {
                Write-Log "Copying runtime assemblies from $WindowsBinPath for $packageType"
                Copy-Item "$WindowsBinPath/*" -Destination $ridFolder -Recurse
                Remove-Item -Path $ridFolder/runtimes/win-arm -Recurse -Force
                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
            }

            "PowerShell.Windows.arm32"
            {
                Write-Log "Copying runtime assemblies from $WindowsBinPath for $packageType"
                Copy-Item "$WindowsBinPath/*" -Destination $ridFolder -Recurse
                Remove-Item -Path $ridFolder/runtimes/win-x64 -Recurse -Force
                $toolSettings = $packagingStrings.GlobalToolSettingsFile -f "pwsh.dll"
            }
        }

        $packageName = $_.PackageName
        $nuSpec = $packagingStrings.GlobalToolNuSpec -f $packageName, $PackageVersion
        $nuSpec | Out-File -FilePath (Join-Path $_.RootFolder "$packageName.nuspec") -Encoding ascii
        $toolSettings | Out-File -FilePath (Join-Path $ridFolder "DotnetToolSettings.xml") -Encoding ascii

        Write-Log "Creating a package: $packageName"
        New-NugetPackage -NuSpecPath $_.RootFolder -PackageDestinationPath $DestinationPath
    }
}
