# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$Environment = Get-EnvironmentInformation
$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path

$packagingStrings = Import-PowerShellDataFile "$PSScriptRoot\packaging.strings.psd1"
Import-Module "$PSScriptRoot\..\Xml" -ErrorAction Stop -Force
$DebianDistributions = @("deb")
$RedhatDistributions = @("rh")
$script:netCoreRuntime = 'net6.0'
$script:iconFileName = "Powershell_black_64.png"
$script:iconPath = Join-Path -path $PSScriptRoot -ChildPath "../../assets/$iconFileName" -Resolve

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
        [ValidateSet("msix", "deb", "osxpkg", "rpm", "msi", "zip", "zip-pdb", "nupkg", "tar", "tar-arm", "tar-arm64", "tar-alpine", "fxdependent", "fxdependent-win-desktop", "min-size")]
        [string[]]$Type,

        # Generate windows downlevel package
        [ValidateSet("win7-x86", "win7-x64", "win-arm", "win-arm64")]
        [ValidateScript({$Environment.IsWindows})]
        [string] $WindowsRuntime,

        [ValidateSet('osx-x64', 'osx-arm64')]
        [ValidateScript({$Environment.IsMacOS})]
        [string] $MacOSRuntime,

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
        } elseif ($Type -eq "tar-alpine") {
            New-PSOptions -Configuration "Release" -Runtime "alpine-x64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type -eq "tar-arm") {
            New-PSOptions -Configuration "Release" -Runtime "Linux-ARM" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
        } elseif ($Type -eq "tar-arm64") {
            if ($IsMacOS) {
                New-PSOptions -Configuration "Release" -Runtime "osx-arm64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
            } else {
                New-PSOptions -Configuration "Release" -Runtime "Linux-ARM64" -WarningAction SilentlyContinue | ForEach-Object { $_.Runtime, $_.Configuration }
            }
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
        } elseif ($Type -eq 'fxdependent-win-desktop') {
            $NameSuffix = "win-fxdependentWinDesktop"
            Write-Log "Packaging : '$Type'; Packaging Configuration: '$Configuration'"
        } elseif ($MacOSRuntime) {
            $NameSuffix = $MacOSRuntime
        } else {
            Write-Log "Packaging RID: '$Runtime'; Packaging Configuration: '$Configuration'"
        }

        $Script:Options = Get-PSOptions
        $actualParams = @()

        $crossGenCorrect = $false
        if ($Runtime -match "arm" -or $Type -eq 'min-size') {
            ## crossgen doesn't support arm32/64;
            ## For the min-size package, we intentionally avoid crossgen.
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

        $precheckFailed = if ($Type -like 'fxdependent*' -or $Type -eq 'tar-alpine') {
            ## We do not check for runtime and crossgen for framework dependent package.
            -not $Script:Options -or                                ## Start-PSBuild hasn't been executed yet
            -not $PSModuleRestoreCorrect -or                        ## Last build didn't specify '-PSModuleRestore' correctly
            $Script:Options.Configuration -ne $Configuration -or    ## Last build was with configuration other than 'Release'
            $Script:Options.Framework -ne $script:netCoreRuntime    ## Last build wasn't for CoreCLR
        } else {
            -not $Script:Options -or                                ## Start-PSBuild hasn't been executed yet
            -not $crossGenCorrect -or                               ## Last build didn't specify '-CrossGen' correctly
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


            $params = @('-Clean')

            # CrossGen cannot be done for framework dependent package as it is runtime agnostic.
            if ($Type -notlike 'fxdependent*') {
                $params += '-CrossGen'
            }

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

        $Source = Split-Path -Path $Script:Options.Output -Parent

        # Copy the ThirdPartyNotices.txt so it's part of the package
        Copy-Item "$RepoRoot/ThirdPartyNotices.txt" -Destination $Source -Force

        # Copy the default.help.txt so it's part of the package
        Copy-Item "$RepoRoot/assets/default.help.txt" -Destination "$Source/en-US" -Force

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
                    "deb", "nupkg", "tar"
                } elseif ($Environment.IsRedHatFamily) {
                    "rpm", "nupkg"
                } elseif ($Environment.IsSUSEFamily) {
                    "rpm", "nupkg"
                } else {
                    throw "Building packages for $($Environment.LinuxInfo.PRETTY_NAME) is unsupported!"
                }
            } elseif ($Environment.IsMacOS) {
                "osxpkg", "nupkg", "tar"
            } elseif ($Environment.IsWindows) {
                "msi", "nupkg", "msix"
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
                # Remove symbol files, xml document files.
                Remove-Item "$Source\*.pdb", "$Source\*.xml" -Force

                # Add suffix '-gc' because this package is for the Guest Config team.
                if ($Environment.IsWindows) {
                    $Arguments = @{
                        PackageNameSuffix = "$NameSuffix-gc"
                        PackageSourcePath = $Source
                        PackageVersion = $Version
                        Force = $Force
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
                    }

                    if ($PSCmdlet.ShouldProcess("Create tar.gz Package")) {
                        New-TarballPackage @Arguments
                    }
                }
            }
            { $_ -like "fxdependent*" } {
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
                } elseif ($Environment.IsLinux) {
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
                }

                if ($PSCmdlet.ShouldProcess("Create MSIX Package")) {
                    New-MSIXPackage @Arguments
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

                if ($MacOSRuntime) {
                    $Arguments['Architecture'] = $MacOSRuntime.Split('-')[1]
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
                    ExcludeSymbolicLinks = $true
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
                    ExcludeSymbolicLinks = $true
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
                }
                foreach ($Distro in $Script:RedhatDistributions) {
                    $Arguments["Distribution"] = $Distro
                    if ($PSCmdlet.ShouldProcess("Create RPM Package for $Distro")) {
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
                    LTS = $LTS
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

        [string] $CurrentLocation = (Get-Location)
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
    New-StagingFolder -StagingPath $Staging -PackageSourcePath $PackageSourcePath

    if (Get-Command -Name tar -CommandType Application -ErrorAction Ignore) {
        if ($Force -or $PSCmdlet.ShouldProcess("Create tarball package")) {
            $options = "-czf"
            if ($PSBoundParameters.ContainsKey('Verbose') -and $PSBoundParameters['Verbose'].IsPresent) {
                # Use the verbose mode '-v' if '-Verbose' is specified
                $options = "-czvf"
            }

            try {
                Push-Location -Path $Staging
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
        [string]$SignedFilesPath
    )

    # Replace unsigned binaries with signed
    $signedFilesFilter = Join-Path -Path $SignedFilesPath -ChildPath '*'
    Get-ChildItem -Path $signedFilesFilter -Recurse -File | Select-Object -ExpandProperty FullName | ForEach-Object -Process {
        $relativePath = $_.ToLowerInvariant().Replace($SignedFilesPath.ToLowerInvariant(),'')
        $destination = Join-Path -Path $BuildPath -ChildPath $relativePath
        Write-Log "replacing $destination with $_"
        Copy-Item -Path $_ -Destination $destination -Force
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
    Restore-PSOptions -PSOptionsPath $psOptionsPath -Remove

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
        if ($Type -eq "deb" -or $Type -eq 'rpm') {
            # Add a dynamic parameter '-Distribution' when the specified package type is 'deb'.
            # The '-Distribution' parameter can be used to indicate which Debian distro this pacakge is targeting.
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
        $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS

        # Preview versions have preview in the name
        $Name = if($LTS) {
            "powershell-lts"
        }
        elseif ($IsPreview) {
            "powershell-preview"
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
            -LinkInfo $Links `
            -AppsFolder $AppsFolder `
            -Distribution $DebDistro `
            -ErrorAction Stop

        # Build package
        try {
            if ($PSCmdlet.ShouldProcess("Create $type package")) {
                Write-Log "Creating package with fpm..."
                $Output = Start-NativeExecution { fpm $Arguments }
            }
        } finally {
            if ($Environment.IsMacOS) {
                Write-Log "Starting Cleanup for mac packaging..."
                if ($PSCmdlet.ShouldProcess("Cleanup macOS launcher"))
                {
                    Clear-MacOSLauncher
                }

                # this is continuation of a fpm hack for a weird bug
                if (Test-Path $hack_dest) {
                    Write-Warning "Move $hack_dest to $symlink_dest (fpm utime bug)"
                    Start-NativeExecution -sb ([ScriptBlock]::Create("$sudo mv $hack_dest $symlink_dest")) -VerboseOutputOnError
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

        if ($Environment.IsMacOS) {
            if ($PSCmdlet.ShouldProcess("Add distribution information and Fix PackageName"))
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
    param(
        [Parameter(Mandatory,HelpMessage='The FileInfo of the file created by FPM')]
        [System.IO.FileInfo]$FpmPackage,
        [Switch] $IsPreview
    )

    if (!$Environment.IsMacOS)
    {
        throw 'New-MacOsDistributionPackage is only supported on macOS!'
    }

    $packageName = Split-Path -Leaf -Path $FpmPackage

    # Create a temp directory to store the needed files
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir -Force > $null

    $resourcesDir = Join-Path -Path $tempDir -ChildPath 'resources'
    New-Item -ItemType Directory -Path $resourcesDir -Force > $null
    #Copy background file to temp directory
    $backgroundFile = "$RepoRoot/assets/macDialog.png"
    Copy-Item -Path $backgroundFile -Destination $resourcesDir
    # Move the current package to the temp directory
    $tempPackagePath = Join-Path -Path $tempDir -ChildPath $packageName
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
    $PackagingStrings.OsxDistributionTemplate -f "PowerShell - $packageVersion", $packageVersion, $packageName, '10.13', $packageId | Out-File -Encoding ascii -FilePath $distributionXmlPath -Force

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
        Remove-Item -Path $tempDir -Recurse -Force
    }

    return (Get-Item $newPackagePath)
}

Class LinkInfo
{
    [string] $Source
    [string] $Destination
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
        [LinkInfo[]]$LinkInfo,

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
        [String]$AppsFolder,
        [String]$Distribution = 'rhel.7'
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
        $Arguments += @("--rpm-dist", $Distribution)
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
        "$ManGzipFile=$ManDestination"
    )

    foreach($link in $LinkInfo)
    {
        $linkArgument = "$($link.Source)=$($link.Destination)"
        $Arguments += $linkArgument
    }

    if ($AppsFolder)
    {
        $Arguments += "$AppsFolder=/"
    }

    return $Arguments
}

function Get-PackageDependencies
{
    param(
        [String]
        [ValidateSet('rh','deb','macOS')]
        $Distribution
    )

    End {
        # These should match those in the Dockerfiles, but exclude tools like Git, which, and curl
        $Dependencies = @()
        if ($Distribution -eq 'deb') {
            $Dependencies = @(
                "libc6",
                "libgcc1",
                "libgssapi-krb5-2",
                "libstdc++6",
                "zlib1g",
                "libicu72|libicu71|libicu70|libicu69|libicu68|libicu67|libicu66|libicu65|libicu63|libicu60|libicu57|libicu55|libicu52",
                "libssl1.1|libssl1.0.2|libssl1.0.0"
            )

        } elseif ($Distribution -eq 'rh') {
            $Dependencies = @(
                "openssl-libs",
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
        $Link,

        [Parameter(Mandatory)]
        [string]
        $Distribution,

        [Parameter(Mandatory)]
        [string]
        $Destination
    )

    Write-Verbose -Message "AfterScript Distribution: $Distribution" -Verbose

    if ($Environment.IsRedHatFamily) {
        $AfterInstallScript = [io.path]::GetTempFileName()
        $AfterRemoveScript = [io.path]::GetTempFileName()
        $packagingStrings.RedHatAfterInstallScript -f "$Link", $Destination  | Out-File -FilePath $AfterInstallScript -Encoding ascii
        $packagingStrings.RedHatAfterRemoveScript -f "$Link", $Destination | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }
    elseif ($Environment.IsDebianFamily -or $Environment.IsSUSEFamily) {
        $AfterInstallScript = [io.path]::GetTempFileName()
        $AfterRemoveScript = [io.path]::GetTempFileName()
        $packagingStrings.UbuntuAfterInstallScript -f "$Link", $Destination | Out-File -FilePath $AfterInstallScript -Encoding ascii
        $packagingStrings.UbuntuAfterRemoveScript -f "$Link", $Destination | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }
    elseif ($Environment.IsMacOS) {
        # NOTE: The macos pkgutil doesn't support uninstall actions so we did not implement it.
        # Handling uninstall can be done in Homebrew so we'll take advantage of that in the brew formula.
        $AfterInstallScript = [io.path]::GetTempFileName()
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
    # run ronn to convert man page to roff
    $RonnFile = "$RepoRoot/assets/pwsh.1.ronn"

    if ($IsPreview.IsPresent -or $IsLTS.IsPresent)
    {
        $prodName = if ($IsLTS) { 'pwsh-lts' } else { 'pwsh-preview' }
        $newRonnFile = $RonnFile -replace 'pwsh', $prodName
        Copy-Item -Path $RonnFile -Destination $newRonnFile -Force
        $RonnFile = $newRonnFile
    }

    $RoffFile = $RonnFile -replace "\.ronn$"

    # Run ronn on assets file
    Write-Log "Creating man gz - running ronn..."
    Start-NativeExecution { ronn --roff $RonnFile }

    if ($IsPreview.IsPresent)
    {
        Remove-Item $RonnFile
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
        [String]$Version,

        [switch]$LTS
    )

    $IsPreview = Test-IsPreview -Version $Version -IsLTS:$LTS
    $packageId = Get-MacOSPackageId -IsPreview:$IsPreview

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
        chmod 755 $shellscript
    }

    # Add app folder to fpm paths.
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
        $Filter = '*'
    )

    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $StagingPath
    Copy-Item -Recurse $PackageSourcePath $StagingPath -Filter $Filter
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

        [string] $CurrentLocation = (Get-Location)
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
            New-StagingFolder -StagingPath $staging -PackageSourcePath $PackageSourcePath

            Get-ChildItem $staging -Filter *.pdb -Recurse | Remove-Item -Force

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
    #TODO: Use .NET Api to do compresss-archive equivalent if the pscmdlet is not present
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

    $destPath = New-Item -ItemType Directory -Path (Join-Path $PackageRuntimesFolder "$Platform/lib/$script:netCoreRuntime")
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
            $refFolder = New-Item (Join-Path $filePackageFolder.FullName "ref/$script:netCoreRuntime") -ItemType Directory -Force
            CopyReferenceAssemblies -assemblyName $fileBaseName -refBinPath $refBinPath -refNugetPath $refFolder -assemblyFileList $fileList
            #endregion ref

            $packageRuntimesFolderPath = $packageRuntimesFolder.FullName

            CreateNugetPlatformFolder -Platform 'win' -PackageRuntimesFolder $packageRuntimesFolderPath -PlatformBinPath $WinFxdBinPath

            if ($linuxExceptionList -notcontains $file )
            {
                CreateNugetPlatformFolder -Platform 'unix' -PackageRuntimesFolder $packageRuntimesFolderPath -PlatformBinPath $LinuxFxdBinPath
            }

            if ($file -eq "Microsoft.PowerShell.SDK.dll")
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

            #region nuspec
            # filed a tracking bug for automating generation of dependecy list: https://github.com/PowerShell/PowerShell/issues/6247
            $deps = [System.Collections.ArrayList]::new()

            switch ($fileBaseName) {
                'Microsoft.Management.Infrastructure.CimCmdlets' {
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'System.Management.Automation'), [tuple]::Create('version', $PackageVersion))) > $null
                }

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
                    $deps.Add([tuple]::Create([tuple]::Create('id', 'Microsoft.Management.Infrastructure.CimCmdlets'), [tuple]::Create('version', $PackageVersion))) > $null
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
            }

            New-NuSpec -PackageId $fileBaseName -PackageVersion $PackageVersion -Dependency $deps -FilePath (Join-Path $filePackageFolder.FullName "$fileBaseName.nuspec")

            # Copy icon file to package
            Copy-Item -Path $iconPath -Destination "$($filePackageFolder.Fullname)/$iconFileName" -Verbose

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
  Copy the generated reference assemblies to the 'ref/net6.0' folder properly.
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

    $supportedRefList = @(
        "Microsoft.PowerShell.Commands.Utility",
        "Microsoft.PowerShell.ConsoleHost")

    switch ($assemblyName) {
        { $_ -in $supportedRefList } {
            $refDll = Join-Path -Path $refBinPath -ChildPath "$assemblyName.dll"
            $refDoc = Join-Path -Path $refBinPath -ChildPath "$assemblyName.xml"
            Copy-Item $refDll, $refDoc -Destination $refNugetPath -Force
            Write-Log "Copied file '$refDll' and '$refDoc' to '$refNugetPath'"
        }

        "Microsoft.PowerShell.SDK" {
            foreach ($asmFileName in $assemblyFileList) {
                $refFile = Join-Path -Path $refBinPath -ChildPath $asmFileName
                if (Test-Path -Path $refFile) {
                    $refDoc = Join-Path -Path $refBinPath -ChildPath ([System.IO.Path]::ChangeExtension($asmFileName, "xml"))
                    Copy-Item $refFile, $refDoc -Destination $refNugetPath -Force
                    Write-Log "Copied file '$refFile' and '$refDoc' to '$refNugetPath'"
                }
            }
        }

        default {
            $ref_SMA = Join-Path -Path $refBinPath -ChildPath System.Management.Automation.dll
            $ref_doc = Join-Path -Path $refBinPath -ChildPath System.Management.Automation.xml
            Copy-Item $ref_SMA, $ref_doc -Destination $refNugetPath -Force
            Write-Log "Copied file '$ref_SMA' and '$ref_doc' to '$refNugetPath'"
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
    $packagingStrings.NugetConfigFile | Out-File -FilePath "$genAPIFolder/Nuget.config" -Force
    Write-Log "Working directory: $genAPIFolder."

    $SMAReferenceAssembly = $null
    $assemblyNames = @(
        "System.Management.Automation",
        "Microsoft.PowerShell.Commands.Utility",
        "Microsoft.PowerShell.ConsoleHost"
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

            Write-Host "##vso[artifact.upload containerfolder=artifact;artifactname=artifact]$destProjectFile"
            Write-Host "##vso[artifact.upload containerfolder=artifact;artifactname=artifact]$generatedSource"

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
    $contentFolder = Join-Path -Path $stagingRoot -ChildPath 'content'
    if ($PSCmdlet.ShouldProcess("Create staging folder")) {
        New-StagingFolder -StagingPath $contentFolder -PackageSourcePath $PackageSourcePath
    }

    $projectFolder = Join-Path $PSScriptRoot 'projects/nuget'

    $arguments = @('pack')
    $arguments += @('--output',$nugetFolder)
    $arguments += @('--configuration',$PackageConfiguration)
    $arguments += "/p:StagingPath=$stagingRoot"
    $arguments += "/p:RID=$PackageRuntime"
    $arguments += "/p:SemVer=$nugetSemanticVersion"
    $arguments += "/p:PackageName=$nuspecPackageName"
    $arguments += $projectFolder

    Write-Log "Running dotnet $arguments"
    Write-Log "Use -verbose to see output..."
    Start-NativeExecution -sb {dotnet $arguments} | ForEach-Object {Write-Verbose $_}

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
        [string] $PatchWxsPath = "$RepoRoot\assets\wix\patch-template.wxs",

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

    [xml] $filesAssetXml = Get-Content -Raw -Path "$RepoRoot\assets\wix\files.wxs"
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
        New-MSIPackage -Verbose -ProductSourcePath '.\src\powershell-win-core\bin\Debug\net6.0\win7-x64\publish' -ProductTargetArchitecture x64 -ProductVersion '1.2.3'
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

        # File describing the MSI file components
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $FilesWxsPath = "$RepoRoot\assets\wix\Files.wxs",

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
        [ValidateSet("x86", "x64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture,

        # Force overwrite of package
        [Switch] $Force,

        [string] $CurrentLocation = (Get-Location)
    )

    $wixPaths = Get-WixPath

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

    Get-ChildItem $staging -Filter *.pdb -Recurse | Remove-Item -Force

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

    $wixFragmentPath = Join-Path $env:Temp "Fragment.wxs"

    # cleanup any garbage on the system
    Remove-Item -ErrorAction SilentlyContinue $wixFragmentPath -Force

    $msiLocationPath = Join-Path $CurrentLocation "$packageName.msi"
    $msiPdbLocationPath = Join-Path $CurrentLocation "$packageName.wixpdb"

    if (!$Force.IsPresent -and (Test-Path -Path $msiLocationPath)) {
        Write-Error -Message "Package already exists, use -Force to overwrite, path:  $msiLocationPath" -ErrorAction Stop
    }

    Write-Log "verifying no new files have been added or removed..."
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

    Start-NativeExecution -VerboseOutputOnError { & $wixPaths.wixHeatExePath dir $staging -dr  VersionFolder -cg ApplicationFiles -ag -sfrag -srd -scom -sreg -out $wixFragmentPath -var var.ProductSourcePath $buildArguments -v}

    # We are verifying that the generated $wixFragmentPath and $FilesWxsPath are functionally the same
    Test-FileWxs -FilesWxsPath $FilesWxsPath -HeatFilesWxsPath $wixFragmentPath -FileArchitecture $fileArchitecture

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

    Start-MsiBuild -WxsFile $ProductWxsPath, $FilesWxsPath -ProductTargetArchitecture $ProductTargetArchitecture -Argument $arguments -MsiLocationPath $msiLocationPath -MsiPdbLocationPath $msiPdbLocationPath

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
    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion

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
        [ValidateSet("x86", "x64")]
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
        $EnginePath
    )

    <#
    2. detach the engine from TestInstaller.exe:
    insignia -ib TestInstaller.exe -o engine.exe
    #>

    $wixPaths = Get-WixPath

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
        $EnginePath
    )


    <#
    4. re-attach the signed engine.exe to the bundle:
    insignia -ab engine.exe TestInstaller.exe -o TestInstaller.exe
    #>

    $wixPaths = Get-WixPath

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
        $buildArguments += "-d$key=`"$($Argument.$key)`""
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

    $wixPaths = Get-WixPath

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
        New-MSIXPackage -Verbose -ProductSourcePath '.\src\powershell-win-core\bin\Debug\net6.0\win7-x64\publish' -ProductTargetArchitecture x64 -ProductVersion '1.2.3'
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
    if ($ProductNameSuffix) {
        $packageName += "-$ProductNameSuffix"
    }

    $displayName = $productName

    if ($ProductSemanticVersion.Contains('-')) {
        $ProductName += 'Preview'
        $displayName += ' Preview'
    }

    Write-Verbose -Verbose "ProductName: $productName"
    Write-Verbose -Verbose "DisplayName: $displayName"

    $ProductVersion = Get-WindowsVersion -PackageName $packageName

    $isPreview = Test-IsPreview -Version $ProductSemanticVersion
    if ($isPreview) {
        Write-Verbose "Using Preview assets" -Verbose
    }

    # Appx manifest needs to be in root of source path, but the embedded version needs to be updated
    # cp-459155 is 'CN=Microsoft Windows Store Publisher (Store EKU), O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
    # authenticodeFormer is 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'
    $releasePublisher = 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US'

    $appxManifest = Get-Content "$RepoRoot\assets\AppxManifest.xml" -Raw
    $appxManifest = $appxManifest.Replace('$VERSION$', $ProductVersion).Replace('$ARCH$', $Architecture).Replace('$PRODUCTNAME$', $productName).Replace('$DISPLAYNAME$', $displayName).Replace('$PUBLISHER$', $releasePublisher)
    Set-Content -Path "$ProductSourcePath\AppxManifest.xml" -Value $appxManifest -Force
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

# verify no files have been added or removed
# if so, write an error with details
function Test-FileWxs
{
    param
    (
        # File describing the MSI file components from the asset folder
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $FilesWxsPath = "$RepoRoot\assets\wix\Files.wxs",

        # File describing the MSI file components generated by heat
        [ValidateNotNullOrEmpty()]
        [ValidateScript( {Test-Path $_})]
        [string] $HeatFilesWxsPath,

        [string] $FileArchitecture
    )

    # Update the fileArchitecture in our file to the actual value.  Since, the heat file will have the actual value.
    # Wix will update this automaticaly, but the output is not the same xml
    $filesAssetString = (Get-Content -Raw -Path $FilesWxsPath).Replace('$(var.FileArchitecture)', $FileArchitecture)

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
            $heatNode = $heatNodesByFile[$file]
            $compGroupNode = Get-ComponentGroupNode -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns
            $filesNode = Get-DirectoryNode -Node $heatNode -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns
            # Create new Component
            $newComponent = New-XmlElement -XmlDoc $newFilesAssetXml -LocalName 'Component' -Node $filesNode -PassThru -NamespaceUri 'http://schemas.microsoft.com/wix/2006/wi'
            $componentId = New-WixId -Prefix 'cmp'
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newComponent -Name 'Id' -Value $componentId
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

    # get all the file components from the files.wxs
    $components = $filesAssetXml.GetElementsByTagName('Component')
    $componentRefs = $filesAssetXml.GetElementsByTagName('ComponentRef')

    $componentComparison = Compare-Object -ReferenceObject $components.id -DifferenceObject $componentRefs.id
    if ( $componentComparison.Count -gt 0){
        $passed = $false
        Write-Verbose "Rebuilding componentRefs" -Verbose

        # add all the file components to the patch
        foreach($component in $componentRefs)
        {
            $componentId = $component.Id
            Write-Verbose "Removing $componentId" -Verbose
            Remove-ComponentRefNode -Id $componentId -XmlDoc $newFilesAssetXml -XmlNsManager $xmlns
        }

        # There is only one ComponentGroup.
        # So we get all of them and select the first one.
        $componentGroups = @($newFilesAssetXml.GetElementsByTagName('ComponentGroup'))
        $componentGroup = $componentGroups[0]

        # add all the file components to the patch
        foreach($component in $components)
        {
            $id = $component.Id
            Write-Verbose "Adding $id" -Verbose
            $newComponentRef = New-XmlElement -XmlDoc $newFilesAssetXml -LocalName 'ComponentRef' -Node $componentGroup -PassThru -NamespaceUri 'http://schemas.microsoft.com/wix/2006/wi'
            New-XmlAttribute -XmlDoc $newFilesAssetXml -Element $newComponentRef -Name 'Id' -Value $id
        }
    }

    if (!$passed)
    {
        $newXmlFileName = Join-Path -Path $env:TEMP -ChildPath ([System.io.path]::GetRandomFileName() + '.wxs')
        $newFilesAssetXml.Save($newXmlFileName)
        $newXml = Get-Content -Raw $newXmlFileName
        $newXml = $newXml -replace 'amd64', '$(var.FileArchitecture)'
        $newXml = $newXml -replace 'x86', '$(var.FileArchitecture)'
        $newXml | Out-File -FilePath $newXmlFileName -Encoding ascii
        Write-Log -message "Updated xml saved to $newXmlFileName."
        Write-Log -message "If component files were intentionally changed, such as due to moving to a newer .NET Core runtime, update '$FilesWxsPath' with the content from '$newXmlFileName'."
        Write-Information -MessageData @{FilesWxsPath = $FilesWxsPath; NewFile = $newXmlFileName} -Tags 'PackagingWxs'
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
        [string] $Version
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
        [Parameter(Mandatory)] [string] $WindowsDesktopBinPath,
        [Parameter(Mandatory)] [string] $PackageVersion,
        [Parameter(Mandatory)] [string] $DestinationPath,
        [Parameter(ParameterSetName="UnifiedPackage")] [switch] $UnifiedPackage
    )

    $packageInfo = @()

    Remove-Item -Path (Join-Path $LinuxBinPath 'libcrypto.so.1.0.0') -Verbose -Force -Recurse
    Remove-Item -Path (Join-Path $LinuxBinPath 'libssl.so.1.0.0') -Verbose -Force -Recurse

    ## Remove unnecessary xml files
    Get-ChildItem -Path $LinuxBinPath, $WindowsBinPath, $WindowsDesktopBinPath -Filter *.xml | Remove-Item -Verbose

    if ($UnifiedPackage)
    {
        Write-Log "Creating a unified package"
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell"; Type = "Unified"}
        $ShimDllPath = Join-Path $WindowsDesktopBinPath "Microsoft.PowerShell.GlobalTool.Shim.dll"
    }
    else
    {
        Write-Log "Reducing size of Linux package"
        ReduceFxDependentPackage -Path $LinuxBinPath

        Write-Log "Reducing size of Windows package"
        ReduceFxDependentPackage -Path $WindowsBinPath -KeepWindowsRuntimes

        Write-Log "Reducing size of WindowsDesktop package"
        ReduceFxDependentPackage -Path $WindowsDesktopBinPath -KeepWindowsRuntimes

        Write-Log "Creating a Linux and Windows packages"
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.Alpine"; Type = "PowerShell.Linux.Alpine"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.x64"; Type = "PowerShell.Linux.x64"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.arm32"; Type = "PowerShell.Linux.arm32"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Linux.arm64"; Type = "PowerShell.Linux.arm64"}

        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Windows.x64"; Type = "PowerShell.Windows.x64"}
        $packageInfo += @{ RootFolder = (New-TempFolder); PackageName = "PowerShell.Windows.arm32"; Type = "PowerShell.Windows.arm32"}
    }

    $packageInfo | ForEach-Object {
        $ridFolder = New-Item -Path (Join-Path $_.RootFolder "tools/$script:netCoreRuntime/any") -ItemType Directory

        # Add the icon file to the package
        Copy-Item -Path $iconPath -Destination "$($_.RootFolder)/$iconFileName" -Verbose

        $packageType = $_.Type

        switch ($packageType)
        {
            "Unified"
            {
                $winFolder = New-Item (Join-Path $ridFolder "win") -ItemType Directory
                $unixFolder = New-Item (Join-Path $ridFolder "unix") -ItemType Directory

                Write-Log "Copying runtime assemblies from $WindowsDesktopBinPath"
                Copy-Item "$WindowsDesktopBinPath\*" -Destination $winFolder -Recurse

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
                Write-Log "Copying runtime assemblies from $WindowsDesktopBinPath for $packageType"
                Copy-Item "$WindowsDesktopBinPath/*" -Destination $ridFolder -Recurse
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
        $nuSpec = $packagingStrings.GlobalToolNuSpec -f $packageName, $PackageVersion, $iconFileName
        $nuSpec | Out-File -FilePath (Join-Path $_.RootFolder "$packageName.nuspec") -Encoding ascii
        $toolSettings | Out-File -FilePath (Join-Path $ridFolder "DotnetToolSettings.xml") -Encoding ascii

        Write-Log "Creating a package: $packageName"
        New-NugetPackage -NuSpecPath $_.RootFolder -PackageDestinationPath $DestinationPath
    }
}
