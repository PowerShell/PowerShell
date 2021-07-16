# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    # Skips a check that prevents building PowerShell on unsupported Linux distributions
    [parameter(Mandatory = $false)][switch]$SkipLinuxDistroCheck = $false
)

Set-StrictMode -Version 3.0

# On Unix paths is separated by colon
# On Windows paths is separated by semicolon
$script:TestModulePathSeparator = [System.IO.Path]::PathSeparator
$script:Options = $null

$dotnetMetadata = Get-Content $PSScriptRoot/DotnetRuntimeMetadata.json | ConvertFrom-Json
$dotnetCLIChannel = $dotnetMetadata.Sdk.Channel
$dotnetCLIQuality = $dotnetMetadata.Sdk.Quality
$dotnetCLIRequiredVersion = $(Get-Content $PSScriptRoot/global.json | ConvertFrom-Json).Sdk.Version

# Track if tags have been sync'ed
$tagsUpToDate = $false

# Sync Tags
# When not using a branch in PowerShell/PowerShell, tags will not be fetched automatically
# Since code that uses Get-PSCommitID and Get-PSLatestTag assume that tags are fetched,
# This function can ensure that tags have been fetched.
# This function is used during the setup phase in tools/ci.psm1
function Sync-PSTags
{
    param(
        [Switch]
        $AddRemoteIfMissing
    )

    $powerShellRemoteUrls = @(
        'https://github.com/PowerShell/PowerShell'
        'git@github.com:PowerShell/PowerShell'
    )
    $defaultRemoteUrl = "$($powerShellRemoteUrls[0]).git"

    $upstreamRemoteDefaultName = 'upstream'
    $remotes = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" remote}
    $upstreamRemote = $null
    foreach($remote in $remotes)
    {
        $url = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" remote get-url $remote}
        if ($url.EndsWith('.git')) { $url = $url.Substring(0, $url.Length - 4) }

        if($url -in $powerShellRemoteUrls)
        {
            $upstreamRemote = $remote
            break
        }
    }

    if(!$upstreamRemote -and $AddRemoteIfMissing.IsPresent -and $remotes -notcontains $upstreamRemoteDefaultName)
    {
        $null = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" remote add $upstreamRemoteDefaultName $defaultRemoteUrl}
        $upstreamRemote = $upstreamRemoteDefaultName
    }
    elseif(!$upstreamRemote)
    {
        Write-Error "Please add a remote to PowerShell\PowerShell.  Example:  git remote add $upstreamRemoteDefaultName $defaultRemoteUrl" -ErrorAction Stop
    }

    $null = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" fetch --tags --quiet $upstreamRemote}
    $script:tagsUpToDate=$true
}

# Gets the latest tag for the current branch
function Get-PSLatestTag
{
    [CmdletBinding()]
    param()
    # This function won't always return the correct value unless tags have been sync'ed
    # So, Write a warning to run Sync-PSTags
    if(!$tagsUpToDate)
    {
        Write-Warning "Run Sync-PSTags to update tags"
    }

    return (Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" describe --abbrev=0})
}

function Get-PSVersion
{
    [CmdletBinding()]
    param(
        [switch]
        $OmitCommitId
    )
    if($OmitCommitId.IsPresent)
    {
        return (Get-PSLatestTag) -replace '^v'
    }
    else
    {
        return (Get-PSCommitId) -replace '^v'
    }
}

function Get-PSCommitId
{
    [CmdletBinding()]
    param()
    # This function won't always return the correct value unless tags have been sync'ed
    # So, Write a warning to run Sync-PSTags
    if(!$tagsUpToDate)
    {
        Write-Warning "Run Sync-PSTags to update tags"
    }

    return (Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" describe --dirty --abbrev=60})
}

function Get-EnvironmentInformation
{
    $environment = @{'IsWindows' = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT}
    # PowerShell will likely not be built on pre-1709 nanoserver
    if ('System.Management.Automation.Platform' -as [type]) {
        $environment += @{'IsCoreCLR' = [System.Management.Automation.Platform]::IsCoreCLR}
        $environment += @{'IsLinux' = [System.Management.Automation.Platform]::IsLinux}
        $environment += @{'IsMacOS' = [System.Management.Automation.Platform]::IsMacOS}
    } else {
        $environment += @{'IsCoreCLR' = $false}
        $environment += @{'IsLinux' = $false}
        $environment += @{'IsMacOS' = $false}
    }

    if ($environment.IsWindows)
    {
        $environment += @{'IsAdmin' = (New-Object Security.Principal.WindowsPrincipal ([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)}
        $environment += @{'nugetPackagesRoot' = "${env:USERPROFILE}\.nuget\packages"}
    }
    else
    {
        $environment += @{'nugetPackagesRoot' = "${env:HOME}/.nuget/packages"}
    }

    if ($environment.IsMacOS) {
        $environment += @{'UsingHomebrew' = [bool](Get-Command brew -ErrorAction ignore)}
        $environment += @{'UsingMacports' = [bool](Get-Command port -ErrorAction ignore)}

        $environment += @{
            'OSArchitecture' = if ((uname -v) -match 'ARM64') { 'arm64' } else { 'x64' }
        }

        if (-not($environment.UsingHomebrew -or $environment.UsingMacports)) {
            throw "Neither Homebrew nor MacPorts is installed on this system, visit https://brew.sh/ or https://www.macports.org/ to continue"
        }
    }

    if ($environment.IsLinux) {
        $LinuxInfo = Get-Content /etc/os-release -Raw | ConvertFrom-StringData
        $lsb_release = Get-Command lsb_release -Type Application -ErrorAction Ignore | Select-Object -First 1
        if ($lsb_release) {
            $LinuxID = & $lsb_release -is
        }
        else {
            $LinuxID = ""
        }

        $environment += @{'LinuxInfo' = $LinuxInfo}
        $environment += @{'IsDebian' = $LinuxInfo.ID -match 'debian' -or $LinuxInfo.ID -match 'kali'}
        $environment += @{'IsDebian9' = $environment.IsDebian -and $LinuxInfo.VERSION_ID -match '9'}
        $environment += @{'IsDebian10' = $environment.IsDebian -and $LinuxInfo.VERSION_ID -match '10'}
        $environment += @{'IsDebian11' = $environment.IsDebian -and $LinuxInfo.PRETTY_NAME -match 'bullseye'}
        $environment += @{'IsUbuntu' = $LinuxInfo.ID -match 'ubuntu' -or $LinuxID -match 'Ubuntu'}
        $environment += @{'IsUbuntu16' = $environment.IsUbuntu -and $LinuxInfo.VERSION_ID -match '16.04'}
        $environment += @{'IsUbuntu18' = $environment.IsUbuntu -and $LinuxInfo.VERSION_ID -match '18.04'}
        $environment += @{'IsUbuntu20' = $environment.IsUbuntu -and $LinuxInfo.VERSION_ID -match '20.04'}
        $environment += @{'IsCentOS' = $LinuxInfo.ID -match 'centos' -and $LinuxInfo.VERSION_ID -match '7'}
        $environment += @{'IsFedora' = $LinuxInfo.ID -match 'fedora' -and $LinuxInfo.VERSION_ID -ge 24}
        $environment += @{'IsOpenSUSE' = $LinuxInfo.ID -match 'opensuse'}
        $environment += @{'IsSLES' = $LinuxInfo.ID -match 'sles'}
        $environment += @{'IsRedHat' = $LinuxInfo.ID -match 'rhel'}
        $environment += @{'IsRedHat7' = $environment.IsRedHat -and $LinuxInfo.VERSION_ID -match '7' }
        $environment += @{'IsOpenSUSE13' = $environment.IsOpenSUSE -and $LinuxInfo.VERSION_ID  -match '13'}
        $environment += @{'IsOpenSUSE42.1' = $environment.IsOpenSUSE -and $LinuxInfo.VERSION_ID  -match '42.1'}
        $environment += @{'IsDebianFamily' = $environment.IsDebian -or $environment.IsUbuntu}
        $environment += @{'IsRedHatFamily' = $environment.IsCentOS -or $environment.IsFedora -or $environment.IsRedHat}
        $environment += @{'IsSUSEFamily' = $environment.IsSLES -or $environment.IsOpenSUSE}
        $environment += @{'IsAlpine' = $LinuxInfo.ID -match 'alpine'}

        # Workaround for temporary LD_LIBRARY_PATH hack for Fedora 24
        # https://github.com/PowerShell/PowerShell/issues/2511
        if ($environment.IsFedora -and (Test-Path ENV:\LD_LIBRARY_PATH)) {
            Remove-Item -Force ENV:\LD_LIBRARY_PATH
            Get-ChildItem ENV:
        }

        if( -not(
            $environment.IsDebian -or
            $environment.IsUbuntu -or
            $environment.IsRedHatFamily -or
            $environment.IsSUSEFamily -or
            $environment.IsAlpine)
        ) {
            if ($SkipLinuxDistroCheck) {
                Write-Warning "The current OS : $($LinuxInfo.ID) is not supported for building PowerShell."
            } else {
                throw "The current OS : $($LinuxInfo.ID) is not supported for building PowerShell. Import this module with '-ArgumentList `$true' to bypass this check."
            }
        }
    }

    return [PSCustomObject] $environment
}

$environment = Get-EnvironmentInformation

# Autoload (in current session) temporary modules used in our tests
$TestModulePath = Join-Path $PSScriptRoot "test/tools/Modules"
if ( -not $env:PSModulePath.Contains($TestModulePath) ) {
    $env:PSModulePath = $TestModulePath+$TestModulePathSeparator+$($env:PSModulePath)
}

<#
    .Synopsis
        Tests if a version is preview
    .EXAMPLE
        Test-IsPreview -version '6.1.0-sometthing' # returns true
        Test-IsPreview -version '6.1.0' # returns false
#>
function Test-IsPreview
{
    param(
        [parameter(Mandatory)]
        [string]
        $Version,

        [switch]$IsLTS
    )

    if ($IsLTS.IsPresent) {
        ## If we are building a LTS package, then never consider it preview.
        return $false
    }

    return $Version -like '*-*'
}

<#
    .Synopsis
        Tests if a version is a Release Candidate
    .EXAMPLE
        Test-IsReleaseCandidate -version '6.1.0-sometthing' # returns false
        Test-IsReleaseCandidate -version '6.1.0-rc.1' # returns true
        Test-IsReleaseCandidate -version '6.1.0' # returns false
#>
function Test-IsReleaseCandidate
{
    param(
        [parameter(Mandatory)]
        [string]
        $Version
    )

    if ($Version -like '*-rc.*')
    {
        return $true
    }

    return $false
}

function Start-PSBuild {
    [CmdletBinding(DefaultParameterSetName="Default")]
    param(
        # When specified this switch will stops running dev powershell
        # to help avoid compilation error, because file are in use.
        [switch]$StopDevPowerShell,

        [switch]$Restore,
        # Accept a path to the output directory
        # When specified, --output <path> will be passed to dotnet
        [string]$Output,
        [switch]$ResGen,
        [switch]$TypeGen,
        [switch]$Clean,
        [Parameter(ParameterSetName="Legacy")]
        [switch]$PSModuleRestore,
        [Parameter(ParameterSetName="Default")]
        [switch]$NoPSModuleRestore,
        [switch]$CI,
        [switch]$ForMinimalSize,

        # Skips the step where the pwsh that's been built is used to create a configuration
        # Useful when changing parsing/compilation, since bugs there can mean we can't get past this step
        [switch]$SkipExperimentalFeatureGeneration,

        # this switch will re-build only System.Management.Automation.dll
        # it's useful for development, to do a quick changes in the engine
        [switch]$SMAOnly,

        # These runtimes must match those in project.json
        # We do not use ValidateScript since we want tab completion
        # If this parameter is not provided it will get determined automatically.
        [ValidateSet("alpine-x64",
                     "fxdependent",
                     "fxdependent-win-desktop",
                     "linux-arm",
                     "linux-arm64",
                     "linux-x64",
                     "osx-arm64",
                     "osx-x64",
                     "win-arm",
                     "win-arm64",
                     "win7-x64",
                     "win7-x86")]
        [string]$Runtime,

        [ValidateSet('Debug', 'Release', 'CodeCoverage', '')] # We might need "Checked" as well
        [string]$Configuration,

        [switch]$CrossGen,

        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag,
        [switch]$Detailed,
        [switch]$InteractiveAuth,
        [switch]$SkipRoslynAnalyzers
    )

    if ($ReleaseTag -and $ReleaseTag -notmatch "^v\d+\.\d+\.\d+(-(preview|rc)(\.\d{1,2})?)?$") {
        Write-Warning "Only preview or rc are supported for releasing pre-release version of PowerShell"
    }

    if ($PSCmdlet.ParameterSetName -eq "Default" -and !$NoPSModuleRestore)
    {
        $PSModuleRestore = $true
    }

    if ($Runtime -eq "linux-arm" -and $environment.IsLinux -and -not $environment.IsUbuntu) {
        throw "Cross compiling for linux-arm is only supported on Ubuntu environment"
    }

    if ("win-arm","win-arm64" -contains $Runtime -and -not $environment.IsWindows) {
        throw "Cross compiling for win-arm or win-arm64 is only supported on Windows environment"
    }

    if ($ForMinimalSize) {
        if ($CrossGen) {
            throw "Build for the minimal size requires the minimal disk footprint, so `CrossGen` is not allowed"
        }

        if ($Runtime -and "linux-x64", "win7-x64", "osx-x64" -notcontains $Runtime) {
            throw "Build for the minimal size is enabled only for following runtimes: 'linux-x64', 'win7-x64', 'osx-x64'"
        }
    }

    function Stop-DevPowerShell {
        Get-Process pwsh* |
            Where-Object {
                $_.Modules |
                Where-Object {
                    $_.FileName -eq (Resolve-Path $script:Options.Output).Path
                }
            } |
        Stop-Process -Verbose
    }

    if ($Clean) {
        Write-Log -message "Cleaning your working directory. You can also do it with 'git clean -fdX --exclude .vs/PowerShell/v16/Server/sqlite3'"
        Push-Location $PSScriptRoot
        try {
            # Excluded sqlite3 folder is due to this Roslyn issue: https://github.com/dotnet/roslyn/issues/23060
            # Excluded src/Modules/nuget.config as this is required for release build.
            # Excluded nuget.config as this is required for release build.
            git clean -fdX --exclude .vs/PowerShell/v16/Server/sqlite3 --exclude src/Modules/nuget.config  --exclude nuget.config
        } finally {
            Pop-Location
        }
    }

    # Add .NET CLI tools to PATH
    Find-Dotnet

    # Verify we have git in place to do the build, and abort if the precheck failed
    $precheck = precheck 'git' "Build dependency 'git' not found in PATH. See <URL: https://docs.github.com/en/github/getting-started-with-github/set-up-git#setting-up-git >"
    if (-not $precheck) {
        return
    }

    # Verify we have .NET SDK in place to do the build, and abort if the precheck failed
    $precheck = precheck 'dotnet' "Build dependency 'dotnet' not found in PATH. Run Start-PSBootstrap. Also see <URL: https://dotnet.github.io/getting-started/ >"
    if (-not $precheck) {
        return
    }

    # Verify if the dotnet in-use is the required version
    $dotnetCLIInstalledVersion = Start-NativeExecution -sb { dotnet --version } -IgnoreExitcode
    If ($dotnetCLIInstalledVersion -ne $dotnetCLIRequiredVersion) {
        Write-Warning @"
The currently installed .NET Command Line Tools is not the required version.

Installed version: $dotnetCLIInstalledVersion
Required version: $dotnetCLIRequiredVersion

Fix steps:

1. Remove the installed version from:
    - on windows '`$env:LOCALAPPDATA\Microsoft\dotnet'
    - on macOS and linux '`$env:HOME/.dotnet'
2. Run Start-PSBootstrap or Install-Dotnet
3. Start-PSBuild -Clean
`n
"@
        return
    }

    # set output options
    $OptionsArguments = @{
        CrossGen=$CrossGen
        Output=$Output
        Runtime=$Runtime
        Configuration=$Configuration
        Verbose=$true
        SMAOnly=[bool]$SMAOnly
        PSModuleRestore=$PSModuleRestore
        ForMinimalSize=$ForMinimalSize
    }
    $script:Options = New-PSOptions @OptionsArguments

    if ($StopDevPowerShell) {
        Stop-DevPowerShell
    }

    # setup arguments
    # adding ErrorOnDuplicatePublishOutputFiles=false due to .NET SDk issue: https://github.com/dotnet/sdk/issues/15748
    $Arguments = @("publish","--no-restore","/property:GenerateFullPaths=true", "/property:ErrorOnDuplicatePublishOutputFiles=false")
    if ($Output -or $SMAOnly) {
        $Arguments += "--output", (Split-Path $Options.Output)
    }

    if ($Options.Runtime -like 'win*' -or ($Options.Runtime -like 'fxdependent*' -and $environment.IsWindows)) {
        $Arguments += "/property:IsWindows=true"
    }
    else {
        $Arguments += "/property:IsWindows=false"
    }

    # Framework Dependent builds do not support ReadyToRun as it needs a specific runtime to optimize for.
    # The property is set in Powershell.Common.props file.
    # We override the property through the build command line.
    if($Options.Runtime -like 'fxdependent*' -or $ForMinimalSize) {
        $Arguments += "/property:PublishReadyToRun=false"
    }

    $Arguments += "--configuration", $Options.Configuration
    $Arguments += "--framework", $Options.Framework

    if ($Detailed.IsPresent)
    {
        $Arguments += '--verbosity', 'd'
    }

    if (-not $SMAOnly -and $Options.Runtime -notlike 'fxdependent*') {
        # libraries should not have runtime
        $Arguments += "--runtime", $Options.Runtime
    }

    if ($ReleaseTag) {
        $ReleaseTagToUse = $ReleaseTag -Replace '^v'
        $Arguments += "/property:ReleaseTag=$ReleaseTagToUse"
    }

    if ($SkipRoslynAnalyzers) {
        $Arguments += "/property:RunAnalyzersDuringBuild=false"
    }

    # handle Restore
    Restore-PSPackage -Options $Options -Force:$Restore -InteractiveAuth:$InteractiveAuth

    # handle ResGen
    # Heuristic to run ResGen on the fresh machine
    if ($ResGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.ConsoleHost/gen")) {
        Write-Log -message "Run ResGen (generating C# bindings for resx files)"
        Start-ResGen
    }

    # Handle TypeGen
    # .inc file name must be different for Windows and Linux to allow build on Windows and WSL.
    $incFileName = "powershell_$($Options.Runtime).inc"
    if ($TypeGen -or -not (Test-Path "$PSScriptRoot/src/TypeCatalogGen/$incFileName")) {
        Write-Log -message "Run TypeGen (generating CorePsTypeCatalog.cs)"
        Start-TypeGen -IncFileName $incFileName
    }

    # Get the folder path where pwsh.exe is located.
    if ((Split-Path $Options.Output -Leaf) -like "pwsh*") {
        $publishPath = Split-Path $Options.Output -Parent
    }
    else {
        $publishPath = $Options.Output
    }

    try {
        # Relative paths do not work well if cwd is not changed to project
        Push-Location $Options.Top

        if ($Options.Runtime -notlike 'fxdependent*') {
            $sdkToUse = 'Microsoft.NET.Sdk'
            if ($Options.Runtime -like 'win7-*' -and !$ForMinimalSize) {
                ## WPF/WinForm and the PowerShell GraphicalHost assemblies are included
                ## when 'Microsoft.NET.Sdk.WindowsDesktop' is used.
                $sdkToUse = 'Microsoft.NET.Sdk.WindowsDesktop'
            }

            $Arguments += "/property:SDKToUse=$sdkToUse"

            Write-Log -message "Run dotnet $Arguments from $PWD"
            Start-NativeExecution { dotnet $Arguments }
            Write-Log -message "PowerShell output: $($Options.Output)"

            if ($CrossGen) {
                # fxdependent package cannot be CrossGen'ed
                Start-CrossGen -PublishPath $publishPath -Runtime $script:Options.Runtime
                Write-Log -message "pwsh.exe with ngen binaries is available at: $($Options.Output)"
            }
        } else {
            $globalToolSrcFolder = Resolve-Path (Join-Path $Options.Top "../Microsoft.PowerShell.GlobalTool.Shim") | Select-Object -ExpandProperty Path

            if ($Options.Runtime -eq 'fxdependent') {
                $Arguments += "/property:SDKToUse=Microsoft.NET.Sdk"
            } elseif ($Options.Runtime -eq 'fxdependent-win-desktop') {
                $Arguments += "/property:SDKToUse=Microsoft.NET.Sdk.WindowsDesktop"
            }

            Write-Log -message "Run dotnet $Arguments from $PWD"
            Start-NativeExecution { dotnet $Arguments }
            Write-Log -message "PowerShell output: $($Options.Output)"

            try {
                Push-Location $globalToolSrcFolder
                $Arguments += "--output", $publishPath
                Write-Log -message "Run dotnet $Arguments from $PWD to build global tool entry point"
                Start-NativeExecution { dotnet $Arguments }
            }
            finally {
                Pop-Location
            }
        }
    } finally {
        Pop-Location
    }

    # No extra post-building task will run if '-SMAOnly' is specified, because its purpose is for a quick update of S.M.A.dll after full build.
    if ($SMAOnly) {
        return
    }

    # publish reference assemblies
    try {
        Push-Location "$PSScriptRoot/src/TypeCatalogGen"
        $refAssemblies = Get-Content -Path $incFileName | Where-Object { $_ -like "*microsoft.netcore.app*" } | ForEach-Object { $_.TrimEnd(';') }
        $refDestFolder = Join-Path -Path $publishPath -ChildPath "ref"

        if (Test-Path $refDestFolder -PathType Container) {
            Remove-Item $refDestFolder -Force -Recurse -ErrorAction Stop
        }
        New-Item -Path $refDestFolder -ItemType Directory -Force -ErrorAction Stop > $null
        Copy-Item -Path $refAssemblies -Destination $refDestFolder -Force -ErrorAction Stop
    } finally {
        Pop-Location
    }

    if ($ReleaseTag) {
        $psVersion = $ReleaseTag
    }
    else {
        $psVersion = git --git-dir="$PSScriptRoot/.git" describe
    }

    if ($environment.IsLinux) {
        if ($environment.IsRedHatFamily -or $environment.IsDebian) {
            # Symbolic links added here do NOT affect packaging as we do not build on Debian.
            # add two symbolic links to system shared libraries that libmi.so is dependent on to handle
            # platform specific changes. This is the only set of platforms needed for this currently
            # as Ubuntu has these specific library files in the platform and macOS builds for itself
            # against the correct versions.

            if ($environment.IsDebian10 -or $environment.IsDebian11){
                $sslTarget = "/usr/lib/x86_64-linux-gnu/libssl.so.1.1"
                $cryptoTarget = "/usr/lib/x86_64-linux-gnu/libcrypto.so.1.1"
            }
            elseif ($environment.IsDebian9){
                # NOTE: Debian 8 doesn't need these symlinks
                $sslTarget = "/usr/lib/x86_64-linux-gnu/libssl.so.1.0.2"
                $cryptoTarget = "/usr/lib/x86_64-linux-gnu/libcrypto.so.1.0.2"
            }
            else { #IsRedHatFamily
                $sslTarget = "/lib64/libssl.so.10"
                $cryptoTarget = "/lib64/libcrypto.so.10"
            }

            if ( ! (Test-Path "$publishPath/libssl.so.1.0.0")) {
                $null = New-Item -Force -ItemType SymbolicLink -Target $sslTarget -Path "$publishPath/libssl.so.1.0.0" -ErrorAction Stop
            }
            if ( ! (Test-Path "$publishPath/libcrypto.so.1.0.0")) {
                $null = New-Item -Force -ItemType SymbolicLink -Target $cryptoTarget -Path "$publishPath/libcrypto.so.1.0.0" -ErrorAction Stop
            }
        }
    }

    # download modules from powershell gallery.
    #   - PowerShellGet, PackageManagement, Microsoft.PowerShell.Archive
    if ($PSModuleRestore) {
        Restore-PSModuleToBuild -PublishPath $publishPath
    }

    # publish powershell.config.json
    $config = @{}
    if ($environment.IsWindows) {
        $config = @{ "Microsoft.PowerShell:ExecutionPolicy" = "RemoteSigned";
                     "WindowsPowerShellCompatibilityModuleDenyList" = @("PSScheduledJob","BestPractices","UpdateServices") }
    }

    # When building preview, we want the configuration to enable all experiemental features by default
    # ARM is cross compiled, so we can't run pwsh to enumerate Experimental Features
    if (-not $SkipExperimentalFeatureGeneration -and
        (Test-IsPreview $psVersion) -and
        -not (Test-IsReleaseCandidate $psVersion) -and
        -not $Runtime.Contains("arm") -and
        -not ($Runtime -like 'fxdependent*')) {

        $json = & $publishPath\pwsh -noprofile -command {
            # Special case for DSC code in PS;
            # this experimental feature requires new DSC module that is not inbox,
            # so we don't want default DSC use case be broken
            [System.Collections.ArrayList] $expFeatures = Get-ExperimentalFeature | Where-Object Name -NE PS7DscSupport | ForEach-Object -MemberName Name

            $expFeatures | Out-String | Write-Verbose -Verbose

            # Make sure ExperimentalFeatures from modules in PSHome are added
            # https://github.com/PowerShell/PowerShell/issues/10550
            $ExperimentalFeaturesFromGalleryModulesInPSHome = @()
            $ExperimentalFeaturesFromGalleryModulesInPSHome | ForEach-Object {
                if (!$expFeatures.Contains($_)) {
                    $null = $expFeatures.Add($_)
                }
            }

            ConvertTo-Json $expFeatures
        }

        $config += @{ ExperimentalFeatures = ([string[]] ($json | ConvertFrom-Json)) }
    }

    if ($config.Count -gt 0) {
        $configPublishPath = Join-Path -Path $publishPath -ChildPath "powershell.config.json"
        Set-Content -Path $configPublishPath -Value ($config | ConvertTo-Json) -Force -ErrorAction Stop
    }

    # Restore the Pester module
    if ($CI) {
        Restore-PSPester -Destination (Join-Path $publishPath "Modules")
    }
}

function Restore-PSPackage
{
    [CmdletBinding()]
    param(
        [ValidateNotNullOrEmpty()]
        [Parameter()]
        [string[]] $ProjectDirs,

        [ValidateNotNullOrEmpty()]
        [Parameter()]
        $Options = (Get-PSOptions -DefaultToNew),

        [switch] $Force,

        [switch] $InteractiveAuth,

        [switch] $PSModule
    )

    if (-not $ProjectDirs)
    {
        $ProjectDirs = @($Options.Top, "$PSScriptRoot/src/TypeCatalogGen", "$PSScriptRoot/src/ResGen", "$PSScriptRoot/src/Modules")

        if ($Options.Runtime -like 'fxdependent*') {
            $ProjectDirs += "$PSScriptRoot/src/Microsoft.PowerShell.GlobalTool.Shim"
        }
    }

    if ($Force -or (-not (Test-Path "$($Options.Top)/obj/project.assets.json"))) {

        if ($Options.Runtime -eq 'fxdependent-win-desktop') {
            $sdkToUse = 'Microsoft.NET.Sdk.WindowsDesktop'
        }
        else {
            $sdkToUse = 'Microsoft.NET.Sdk'
            if ($Options.Runtime -like 'win7-*' -and !$Options.ForMinimalSize) {
                $sdkToUse = 'Microsoft.NET.Sdk.WindowsDesktop'
            }
        }

        if ($PSModule.IsPresent) {
            $RestoreArguments = @("--verbosity")
        }
        elseif ($Options.Runtime -notlike 'fxdependent*') {
            $RestoreArguments = @("--runtime", $Options.Runtime, "/property:SDKToUse=$sdkToUse", "--verbosity")
        } else {
            $RestoreArguments = @("/property:SDKToUse=$sdkToUse", "--verbosity")
        }

        if ($VerbosePreference -eq 'Continue') {
            $RestoreArguments += "detailed"
        } else {
            $RestoreArguments += "quiet"
        }

        if ($InteractiveAuth) {
            $RestoreArguments += "--interactive"
        }

        $ProjectDirs | ForEach-Object {
            $project = $_
            Write-Log -message "Run dotnet restore $project $RestoreArguments"
            $retryCount = 0
            $maxTries = 5
            while($retryCount -lt $maxTries)
            {
                try
                {
                    Start-NativeExecution { dotnet restore $project $RestoreArguments }
                }
                catch
                {
                    Write-Log -message "Failed to restore $project, retrying..."
                    $retryCount++
                    if($retryCount -ge $maxTries)
                    {
                        throw
                    }
                    continue
                }

                Write-Log -message "Done restoring $project"
                break
            }
        }
    }
}

function Restore-PSModuleToBuild
{
    param(
        [Parameter(Mandatory)]
        [string]
        $PublishPath
    )

    Write-Log -message "Restore PowerShell modules to $publishPath"
    $modulesDir = Join-Path -Path $publishPath -ChildPath "Modules"
    Copy-PSGalleryModules -Destination $modulesDir -CsProjPath "$PSScriptRoot\src\Modules\PSGalleryModules.csproj"

    # Remove .nupkg.metadata files
    Get-ChildItem $PublishPath -Filter '.nupkg.metadata' -Recurse | ForEach-Object { Remove-Item $_.FullName -ErrorAction SilentlyContinue -Force }
}

function Restore-PSPester
{
    param(
        [ValidateNotNullOrEmpty()]
        [string] $Destination = ([IO.Path]::Combine((Split-Path (Get-PSOptions -DefaultToNew).Output), "Modules"))
    )
    Save-Module -Name Pester -Path $Destination -Repository PSGallery -MaximumVersion 4.99
}

function Compress-TestContent {
    [CmdletBinding()]
    param(
        $Destination
    )

    $null = Publish-PSTestTools
    $powerShellTestRoot =  Join-Path $PSScriptRoot 'test'
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
    [System.IO.Compression.ZipFile]::CreateFromDirectory($powerShellTestRoot, $resolvedPath)
}

function New-PSOptions {
    [CmdletBinding()]
    param(
        [ValidateSet("Debug", "Release", "CodeCoverage", '')]
        [string]$Configuration,

        [ValidateSet("net6.0")]
        [string]$Framework = "net6.0",

        # These are duplicated from Start-PSBuild
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("",
                     "alpine-x64",
                     "fxdependent",
                     "fxdependent-win-desktop",
                     "linux-arm",
                     "linux-arm64",
                     "linux-x64",
                     "osx-arm64",
                     "osx-x64",
                     "win-arm",
                     "win-arm64",
                     "win7-x64",
                     "win7-x86")]
        [string]$Runtime,

        [switch]$CrossGen,

        # Accept a path to the output directory
        # If not null or empty, name of the executable will be appended to
        # this path, otherwise, to the default path, and then the full path
        # of the output executable will be assigned to the Output property
        [string]$Output,

        [switch]$SMAOnly,

        [switch]$PSModuleRestore,

        [switch]$ForMinimalSize
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    if (-not $Configuration) {
        $Configuration = 'Debug'
    }

    Write-Verbose "Using configuration '$Configuration'"
    Write-Verbose "Using framework '$Framework'"

    if (-not $Runtime) {
        if ($environment.IsLinux) {
            $Runtime = "linux-x64"
        } elseif ($environment.IsMacOS) {
            if ($PSVersionTable.OS.Contains('ARM64')) {
                $Runtime = "osx-arm64"
            }
            else {
                $Runtime = "osx-x64"
            }
        } else {
            $RID = dotnet --info | ForEach-Object {
                if ($_ -match "RID") {
                    $_ -split "\s+" | Select-Object -Last 1
                }
            }

            # We plan to release packages targetting win7-x64 and win7-x86 RIDs,
            # which supports all supported windows platforms.
            # So we, will change the RID to win7-<arch>
            $Runtime = $RID -replace "win\d+", "win7"
        }

        if (-not $Runtime) {
            Throw "Could not determine Runtime Identifier, please update dotnet"
        } else {
            Write-Verbose "Using runtime '$Runtime'"
        }
    }

    $PowerShellDir = if ($Runtime -like 'win*' -or ($Runtime -like 'fxdependent*' -and $environment.IsWindows)) {
        "powershell-win-core"
    } else {
        "powershell-unix"
    }

    $Top = [IO.Path]::Combine($PSScriptRoot, "src", $PowerShellDir)
    Write-Verbose "Top project directory is $Top"

    $Executable = if ($Runtime -like 'fxdependent*') {
        "pwsh.dll"
    } elseif ($environment.IsLinux -or $environment.IsMacOS) {
        "pwsh"
    } elseif ($environment.IsWindows) {
        "pwsh.exe"
    }

    # Build the Output path
    if (!$Output) {
        if ($Runtime -like 'fxdependent*') {
            $Output = [IO.Path]::Combine($Top, "bin", $Configuration, $Framework, "publish", $Executable)
        } else {
            $Output = [IO.Path]::Combine($Top, "bin", $Configuration, $Framework, $Runtime, "publish", $Executable)
        }
    } else {
        $Output = [IO.Path]::Combine($Output, $Executable)
    }

    if ($SMAOnly)
    {
        $Top = [IO.Path]::Combine($PSScriptRoot, "src", "System.Management.Automation")
    }

    $RootInfo = @{RepoPath = $PSScriptRoot}

    # the valid root is the root of the filesystem and the folder PowerShell
    $RootInfo['ValidPath'] = Join-Path -Path ([system.io.path]::GetPathRoot($RootInfo.RepoPath)) -ChildPath 'PowerShell'

    if($RootInfo.RepoPath -ne $RootInfo.ValidPath)
    {
        $RootInfo['Warning'] = "Please ensure your repo is at the root of the file system and named 'PowerShell' (example: '$($RootInfo.ValidPath)'), when building and packaging for release!"
        $RootInfo['IsValid'] = $false
    }
    else
    {
        $RootInfo['IsValid'] = $true
    }

    return New-PSOptionsObject `
                -RootInfo ([PSCustomObject]$RootInfo) `
                -Top $Top `
                -Runtime $Runtime `
                -Crossgen $Crossgen.IsPresent `
                -Configuration $Configuration `
                -PSModuleRestore $PSModuleRestore.IsPresent `
                -Framework $Framework `
                -Output $Output `
                -ForMinimalSize $ForMinimalSize
}

# Get the Options of the last build
function Get-PSOptions {
    param(
        [Parameter(HelpMessage='Defaults to New-PSOption if a build has not occurred.')]
        [switch]
        $DefaultToNew
    )

    if (!$script:Options -and $DefaultToNew.IsPresent)
    {
        return New-PSOptions
    }

    return $script:Options
}

function Set-PSOptions {
    param(
        [PSObject]
        $Options
    )

    $script:Options = $Options
}

function Get-PSOutput {
    [CmdletBinding()]param(
        [hashtable]$Options
    )
    if ($Options) {
        return $Options.Output
    } elseif ($script:Options) {
        return $script:Options.Output
    } else {
        return (New-PSOptions).Output
    }
}

function Get-PesterTag {
    param ( [Parameter(Position=0)][string]$testbase = "$PSScriptRoot/test/powershell" )
    $alltags = @{}
    $warnings = @()

    Get-ChildItem -Recurse $testbase -File | Where-Object {$_.name -match "tests.ps1"}| ForEach-Object {
        $fullname = $_.fullname
        $tok = $err = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($FullName, [ref]$tok,[ref]$err)
        $des = $ast.FindAll({
            $args[0] -is [System.Management.Automation.Language.CommandAst] `
                -and $args[0].CommandElements.GetType() -in @(
                    [System.Management.Automation.Language.StringConstantExpressionAst],
                    [System.Management.Automation.Language.ExpandableStringExpressionAst]
                ) `
                -and $args[0].CommandElements[0].Value -eq "Describe"
        }, $true)
        foreach( $describe in $des) {
            $elements = $describe.CommandElements
            $lineno = $elements[0].Extent.StartLineNumber
            $foundPriorityTags = @()
            for ( $i = 0; $i -lt $elements.Count; $i++) {
                if ( $elements[$i].extent.text -match "^-t" ) {
                    $vAst = $elements[$i+1]
                    if ( $vAst.FindAll({$args[0] -is "System.Management.Automation.Language.VariableExpressionAst"},$true) ) {
                        $warnings += "TAGS must be static strings, error in ${fullname}, line $lineno"
                    }
                    $values = $vAst.FindAll({$args[0] -is "System.Management.Automation.Language.StringConstantExpressionAst"},$true).Value
                    $values | ForEach-Object {
                        if (@('REQUIREADMINONWINDOWS', 'REQUIRESUDOONUNIX', 'SLOW') -contains $_) {
                            # These are valid tags also, but they are not the priority tags
                        }
                        elseif (@('CI', 'FEATURE', 'SCENARIO') -contains $_) {
                            $foundPriorityTags += $_
                        }
                        else {
                            $warnings += "${fullname} includes improper tag '$_', line '$lineno'"
                        }

                        $alltags[$_]++
                    }
                }
            }
            if ( $foundPriorityTags.Count -eq 0 ) {
                $warnings += "${fullname}:$lineno does not include -Tag in Describe"
            }
            elseif ( $foundPriorityTags.Count -gt 1 ) {
                $warnings += "${fullname}:$lineno includes more then one scope -Tag: $foundPriorityTags"
            }
        }
    }
    if ( $Warnings.Count -gt 0 ) {
        $alltags['Result'] = "Fail"
    }
    else {
        $alltags['Result'] = "Pass"
    }
    $alltags['Warnings'] = $warnings
    $o = [pscustomobject]$alltags
    $o.psobject.TypeNames.Add("DescribeTagsInUse")
    $o
}

function Publish-PSTestTools {
    [CmdletBinding()]
    param(
        [string]
        $runtime
    )

    Find-Dotnet

    $tools = @(
        @{Path="${PSScriptRoot}/test/tools/TestExe";Output="testexe"}
        @{Path="${PSScriptRoot}/test/tools/WebListener";Output="WebListener"}
        @{Path="${PSScriptRoot}/test/tools/TestService";Output="TestService"}
    )

    $Options = Get-PSOptions -DefaultToNew

    # Publish tools so it can be run by tests
    foreach ($tool in $tools)
    {
        Push-Location $tool.Path
        try {
            $toolPath = Join-Path -Path $tool.Path -ChildPath "bin"
            $objPath = Join-Path -Path $tool.Path -ChildPath "obj"

            if (Test-Path $toolPath) {
                Remove-Item -Path $toolPath -Recurse -Force
            }

            if (Test-Path $objPath) {
                Remove-Item -Path $objPath -Recurse -Force
            }

            if (-not $runtime) {
                dotnet publish --output bin --configuration $Options.Configuration --framework $Options.Framework --runtime $Options.Runtime
            } else {
                dotnet publish --output bin --configuration $Options.Configuration --framework $Options.Framework --runtime $runtime
            }

            if ( -not $env:PATH.Contains($toolPath) ) {
                $env:PATH = $toolPath+$TestModulePathSeparator+$($env:PATH)
            }
        } finally {
            Pop-Location
        }
    }

    # `dotnet restore` on test project is not called if product projects have been restored unless -Force is specified.
    Copy-PSGalleryModules -Destination "${PSScriptRoot}/test/tools/Modules" -CsProjPath "$PSScriptRoot/test/tools/Modules/PSGalleryTestModules.csproj" -Force
}

function Get-ExperimentalFeatureTests {
    $testMetadataFile = Join-Path $PSScriptRoot "test/tools/TestMetadata.json"
    $metadata = Get-Content -Path $testMetadataFile -Raw | ConvertFrom-Json | ForEach-Object -MemberName ExperimentalFeatures
    $features = $metadata | Get-Member -MemberType NoteProperty | ForEach-Object -MemberName Name

    $featureTests = @{}
    foreach ($featureName in $features) {
        $featureTests[$featureName] = $metadata.$featureName
    }
    $featureTests
}

function Start-PSPester {
    [CmdletBinding(DefaultParameterSetName='default')]
    param(
        [Parameter(Position=0)]
        [string[]]$Path = @("$PSScriptRoot/test/powershell"),
        [string]$OutputFormat = "NUnitXml",
        [string]$OutputFile = "pester-tests.xml",
        [string[]]$ExcludeTag = 'Slow',
        [string[]]$Tag = @("CI","Feature"),
        [switch]$ThrowOnFailure,
        [string]$BinDir = (Split-Path (Get-PSOptions -DefaultToNew).Output),
        [string]$powershell = (Join-Path $BinDir 'pwsh'),
        [string]$Pester = ([IO.Path]::Combine($BinDir, "Modules", "Pester")),
        [Parameter(ParameterSetName='Unelevate',Mandatory=$true)]
        [switch]$Unelevate,
        [switch]$Quiet,
        [switch]$Terse,
        [Parameter(ParameterSetName='PassThru',Mandatory=$true)]
        [switch]$PassThru,
        [Parameter(ParameterSetName='PassThru',HelpMessage='Run commands on Linux with sudo.')]
        [switch]$Sudo,
        [switch]$IncludeFailingTest,
        [switch]$IncludeCommonTests,
        [string]$ExperimentalFeatureName,
        [Parameter(HelpMessage='Title to publish the results as.')]
        [string]$Title = 'PowerShell 7 Tests',
        [Parameter(ParameterSetName='Wait', Mandatory=$true,
            HelpMessage='Wait for the debugger to attach to PowerShell before Pester starts.  Debug builds only!')]
        [switch]$Wait,
        [switch]$SkipTestToolBuild
    )

    if (-not (Get-Module -ListAvailable -Name $Pester -ErrorAction SilentlyContinue | Where-Object { $_.Version -ge "4.2" } ))
    {
        Restore-PSPester
    }

    if ($IncludeFailingTest.IsPresent)
    {
        $Path += "$PSScriptRoot/tools/failingTests"
    }

    if($IncludeCommonTests.IsPresent)
    {
        $path = += "$PSScriptRoot/test/common"
    }

    # we need to do few checks and if user didn't provide $ExcludeTag explicitly, we should alternate the default
    if ($Unelevate)
    {
        if (-not $environment.IsWindows)
        {
            throw '-Unelevate is currently not supported on non-Windows platforms'
        }

        if (-not $environment.IsAdmin)
        {
            throw '-Unelevate cannot be applied because the current user is not Administrator'
        }

        if (-not $PSBoundParameters.ContainsKey('ExcludeTag'))
        {
            $ExcludeTag += 'RequireAdminOnWindows'
        }
    }
    elseif ($environment.IsWindows -and (-not $environment.IsAdmin))
    {
        if (-not $PSBoundParameters.ContainsKey('ExcludeTag'))
        {
            $ExcludeTag += 'RequireAdminOnWindows'
        }
    }
    elseif (-not $environment.IsWindows -and (-not $Sudo.IsPresent))
    {
        if (-not $PSBoundParameters.ContainsKey('ExcludeTag'))
        {
            $ExcludeTag += 'RequireSudoOnUnix'
        }
    }
    elseif (-not $environment.IsWindows -and $Sudo.IsPresent)
    {
        if (-not $PSBoundParameters.ContainsKey('Tag'))
        {
            $Tag = 'RequireSudoOnUnix'
        }
    }

    Write-Verbose "Running pester tests at '$path' with tag '$($Tag -join ''', ''')' and ExcludeTag '$($ExcludeTag -join ''', ''')'" -Verbose
    if(!$SkipTestToolBuild.IsPresent)
    {
        $publishArgs = @{ }
        # if we are building for Alpine, we must include the runtime as linux-x64
        # will not build runnable test tools
        if ( $environment.IsLinux -and $environment.IsAlpine ) {
            $publishArgs['runtime'] = 'alpine-x64'
        }
        Publish-PSTestTools @publishArgs | ForEach-Object {Write-Host $_}
    }

    # All concatenated commands/arguments are suffixed with the delimiter (space)

    # Disable telemetry for all startups of pwsh in tests
    $command = "`$env:POWERSHELL_TELEMETRY_OPTOUT = 'yes';"
    if ($Terse)
    {
        $command += "`$ProgressPreference = 'silentlyContinue'; "
    }

    # Autoload (in subprocess) temporary modules used in our tests
    $newPathFragment = $TestModulePath + $TestModulePathSeparator
    $command += '$env:PSModulePath = '+"'$newPathFragment'" + '+$env:PSModulePath;'

    # Windows needs the execution policy adjusted
    if ($environment.IsWindows) {
        $command += "Set-ExecutionPolicy -Scope Process Unrestricted; "
    }

    $command += "Import-Module '$Pester'; "

    if ($Unelevate)
    {
        $outputBufferFilePath = [System.IO.Path]::GetTempFileName()
    }

    $command += "Invoke-Pester "

    $command += "-OutputFormat ${OutputFormat} -OutputFile ${OutputFile} "
    if ($ExcludeTag -and ($ExcludeTag -ne "")) {
        $command += "-ExcludeTag @('" + (${ExcludeTag} -join "','") + "') "
    }
    if ($Tag) {
        $command += "-Tag @('" + (${Tag} -join "','") + "') "
    }
    # sometimes we need to eliminate Pester output, especially when we're
    # doing a daily build as the log file is too large
    if ( $Quiet ) {
        $command += "-Quiet "
    }
    if ( $PassThru ) {
        $command += "-PassThru "
    }

    $command += "'" + ($Path -join "','") + "'"
    if ($Unelevate)
    {
        $command += " *> $outputBufferFilePath; '__UNELEVATED_TESTS_THE_END__' >> $outputBufferFilePath"
    }

    Write-Verbose $command

    $script:nonewline = $true
    $script:inerror = $false
    function Write-Terse([string] $line)
    {
        $trimmedline = $line.Trim()
        if ($trimmedline.StartsWith("[+]")) {
            Write-Host "+" -NoNewline -ForegroundColor Green
            $script:nonewline = $true
            $script:inerror = $false
        }
        elseif ($trimmedline.StartsWith("[?]")) {
            Write-Host "?" -NoNewline -ForegroundColor Cyan
            $script:nonewline = $true
            $script:inerror = $false
        }
        elseif ($trimmedline.StartsWith("[!]")) {
            Write-Host "!" -NoNewline -ForegroundColor Gray
            $script:nonewline = $true
            $script:inerror = $false
        }
        elseif ($trimmedline.StartsWith("Executing script ")) {
            # Skip lines where Pester reports that is executing a test script
            return
        }
        elseif ($trimmedline -match "^\d+(\.\d+)?m?s$") {
            # Skip the time elapse like '12ms', '1ms', '1.2s' and '12.53s'
            return
        }
        else {
            if ($script:nonewline) {
                Write-Host "`n" -NoNewline
            }
            if ($trimmedline.StartsWith("[-]") -or $script:inerror) {
                Write-Host $line -ForegroundColor Red
                $script:inerror = $true
            }
            elseif ($trimmedline.StartsWith("VERBOSE:")) {
                Write-Host $line -ForegroundColor Yellow
                $script:inerror = $false
            }
            elseif ($trimmedline.StartsWith("Describing") -or $trimmedline.StartsWith("Context")) {
                Write-Host $line -ForegroundColor Magenta
                $script:inerror = $false
            }
            else {
                Write-Host $line -ForegroundColor Gray
            }
            $script:nonewline = $false
        }
    }

    $PSFlags = @("-noprofile")
    if (-not [string]::IsNullOrEmpty($ExperimentalFeatureName)) {
        $configFile = [System.IO.Path]::GetTempFileName()
        $configFile = [System.IO.Path]::ChangeExtension($configFile, ".json")

        ## Create the config.json file to enable the given experimental feature.
        ## On Windows, we need to have 'RemoteSigned' declared for ExecutionPolicy because the ExecutionPolicy is 'Restricted' by default.
        ## On Unix, ExecutionPolicy is not supported, so we don't need to declare it.
        if ($environment.IsWindows) {
            $content = @"
{
    "Microsoft.PowerShell:ExecutionPolicy":"RemoteSigned",
    "ExperimentalFeatures": [
        "$ExperimentalFeatureName"
    ]
}
"@
        } else {
            $content = @"
{
    "ExperimentalFeatures": [
        "$ExperimentalFeatureName"
    ]
}
"@
        }

        Set-Content -Path $configFile -Value $content -Encoding Ascii -Force
        $PSFlags = @("-settings", $configFile, "-noprofile")
    }

	# -Wait is only available on Debug builds
	# It is used to allow the debugger to attach before PowerShell
	# runs pester in this case
    if($Wait.IsPresent){
        $PSFlags += '-wait'
    }

    # To ensure proper testing, the module path must not be inherited by the spawned process
    try {
        $originalModulePath = $env:PSModulePath
        $originalTelemetry = $env:POWERSHELL_TELEMETRY_OPTOUT
        $env:POWERSHELL_TELEMETRY_OPTOUT = 'yes'
        if ($Unelevate)
        {
            Start-UnelevatedProcess -process $powershell -arguments ($PSFlags + "-c $Command")
            $currentLines = 0
            while ($true)
            {
                $lines = Get-Content $outputBufferFilePath | Select-Object -Skip $currentLines
                if ($Terse)
                {
                    foreach ($line in $lines)
                    {
                        Write-Terse -line $line
                    }
                }
                else
                {
                    $lines | Write-Host
                }
                if ($lines | Where-Object { $_ -eq '__UNELEVATED_TESTS_THE_END__'})
                {
                    break
                }

                $count = ($lines | Measure-Object).Count
                if ($count -eq 0)
                {
                    Start-Sleep -Seconds 1
                }
                else
                {
                    $currentLines += $count
                }
            }
        }
        else
        {
            if ($PassThru.IsPresent)
            {
                $passThruFile = [System.IO.Path]::GetTempFileName()
                try
                {
                    $command += "| Export-Clixml -Path '$passThruFile' -Force"

                    $passThruCommand = { & $powershell $PSFlags -c $command }
                    if ($Sudo.IsPresent) {
                        # -E says to preserve the environment
                        $passThruCommand =  { & sudo -E $powershell $PSFlags -c $command }
                    }

                    $writeCommand = { Write-Host $_ }
                    if ($Terse)
                    {
                        $writeCommand = { Write-Terse $_ }
                    }

                    Start-NativeExecution -sb $passThruCommand | ForEach-Object $writeCommand
                    Import-Clixml -Path $passThruFile | Where-Object {$_.TotalCount -is [Int32]}
                }
                finally
                {
                    Remove-Item $passThruFile -ErrorAction SilentlyContinue -Force
                }
            }
            else
            {
                if ($Terse)
                {
                    Start-NativeExecution -sb {& $powershell $PSFlags -c $command} | ForEach-Object { Write-Terse -line $_ }
                }
                else
                {
                    Start-NativeExecution -sb {& $powershell $PSFlags -c $command}
                }
            }
        }
    } finally {
        $env:PSModulePath = $originalModulePath
        $env:POWERSHELL_TELEMETRY_OPTOUT = $originalTelemetry
        if ($Unelevate)
        {
            Remove-Item $outputBufferFilePath
        }
    }

    Publish-TestResults -Path $OutputFile -Title $Title

    if($ThrowOnFailure)
    {
        Test-PSPesterResults -TestResultsFile $OutputFile
    }
}

function Publish-TestResults
{
    param(
        [Parameter(Mandatory)]
        [string]
        $Title,

        [Parameter(Mandatory)]
        [ValidateScript({Test-Path -Path $_})]
        [string]
        $Path,

        [ValidateSet('NUnit','XUnit')]
        [string]
        $Type='NUnit'
    )

    # In VSTS publish Test Results
    if($env:TF_BUILD)
    {
        $fileName = Split-Path -Leaf -Path $Path
        $tempPath = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
        if (! $tempPath)
        {
            $tempPath = [system.io.path]::GetTempPath()
        }
        $tempFilePath = Join-Path -Path $tempPath -ChildPath $fileName

        # NUnit allowed values are: Passed, Failed, Inconclusive or Ignored (the spec says Skipped but it doesn' work with Azure DevOps)
        # https://github.com/nunit/docs/wiki/Test-Result-XML-Format
        # Azure DevOps Reporting is so messed up for NUnit V2 and doesn't follow their own spec
        # https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/test/publish-test-results?view=azure-devops&tabs=yaml
        # So, we will map skipped to the actual value in the NUnit spec and they will ignore all results for tests which were not executed
        Get-Content $Path | ForEach-Object {
            $_ -replace 'result="Ignored"', 'result="Skipped"'
        } | Out-File -FilePath $tempFilePath -Encoding ascii -Force

        # If we attempt to upload a result file which has no test cases in it, then vsts will produce a warning
        # so check to be sure we actually have a result file that contains test cases to upload.
        # If the the "test-case" count is greater than 0, then we have results.
        # Regardless, we want to upload this as an artifact, so this logic doesn't pertain to that.
        if ( @(([xml](Get-Content $Path)).SelectNodes(".//test-case")).Count -gt 0 -or $Type -eq 'XUnit' ) {
            Write-Host "##vso[results.publish type=$Type;mergeResults=true;runTitle=$Title;publishRunAttachments=true;resultFiles=$tempFilePath;failTaskOnFailedTests=true]"
        }

        $resolvedPath = (Resolve-Path -Path $Path).ProviderPath
        Write-Host "##vso[artifact.upload containerfolder=testResults;artifactname=testResults]$resolvedPath"
    }
}

function script:Start-UnelevatedProcess
{
    param(
        [string]$process,
        [string[]]$arguments
    )
    if (-not $environment.IsWindows)
    {
        throw "Start-UnelevatedProcess is currently not supported on non-Windows platforms"
    }

    runas.exe /trustlevel:0x20000 "$process $arguments"
}

function Show-PSPesterError
{
    [CmdletBinding(DefaultParameterSetName='xml')]
    param (
        [Parameter(ParameterSetName='xml',Mandatory)]
        [Xml.XmlElement]$testFailure,
        [Parameter(ParameterSetName='object',Mandatory)]
        [PSCustomObject]$testFailureObject
        )

    if ($PSCmdlet.ParameterSetName -eq 'xml')
    {
        $description = $testFailure.description
        $name = $testFailure.name
        $message = $testFailure.failure.message
        $StackTrace = $testFailure.failure."stack-trace"
    }
    elseif ($PSCmdlet.ParameterSetName -eq 'object')
    {
        $description = $testFailureObject.Describe + '/' + $testFailureObject.Context
        $name = $testFailureObject.Name
        $message = $testFailureObject.FailureMessage
        $StackTrace = $testFailureObject.StackTrace
    }
    else
    {
        throw 'Unknown Show-PSPester parameter set'
    }

    Write-Log -isError -message ("Description: " + $description)
    Write-Log -isError -message ("Name:        " + $name)
    Write-Log -isError -message "message:"
    Write-Log -isError -message $message
    Write-Log -isError -message "stack-trace:"
    Write-Log -isError -message $StackTrace

}

function Test-XUnitTestResults
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $TestResultsFile
    )

    if(-not (Test-Path $TestResultsFile))
    {
        throw "File not found $TestResultsFile"
    }

    try
    {
        $results = [xml] (Get-Content $TestResultsFile)
    }
    catch
    {
        throw "Cannot convert $TestResultsFile to xml : $($_.message)"
    }

    $failedTests = $results.assemblies.assembly.collection.test | Where-Object result -eq "fail"

    if(-not $failedTests)
    {
        return $true
    }

    foreach($failure in $failedTests)
    {
        $description = $failure.type
        $name = $failure.method
        $message = $failure.failure.message
        $StackTrace = $failure.failure.'stack-trace'

        Write-Log -isError -message ("Description: " + $description)
        Write-Log -isError -message ("Name:        " + $name)
        Write-Log -isError -message "message:"
        Write-Log -isError -message $message
        Write-Log -isError -message "stack-trace:"
        Write-Log -isError -message $StackTrace
        Write-Log -isError -message " "
    }

    throw "$($results.assemblies.assembly.failed) tests failed"
}

#
# Read the test result file and
# Throw if a test failed
function Test-PSPesterResults
{
    [CmdletBinding(DefaultParameterSetName='file')]
    param(
        [Parameter(ParameterSetName='file')]
        [string] $TestResultsFile = "pester-tests.xml",

        [Parameter(ParameterSetName='file')]
        [string] $TestArea = 'test/powershell',

        [Parameter(ParameterSetName='PesterPassThruObject', Mandatory)]
        [pscustomobject] $ResultObject,

        [Parameter(ParameterSetName='PesterPassThruObject')]
        [switch] $CanHaveNoResult
    )

    if($PSCmdlet.ParameterSetName -eq 'file')
    {
        if(!(Test-Path $TestResultsFile))
        {
            throw "Test result file '$testResultsFile' not found for $TestArea."
        }

        $x = [xml](Get-Content -Raw $testResultsFile)
        if ([int]$x.'test-results'.failures -gt 0)
        {
            Write-Log -isError -message "TEST FAILURES"
            # switch between methods, SelectNode is not available on dotnet core
            if ( "System.Xml.XmlDocumentXPathExtensions" -as [Type] )
            {
                $failures = [System.Xml.XmlDocumentXPathExtensions]::SelectNodes($x."test-results",'.//test-case[@result = "Failure"]')
            }
            else
            {
                $failures = $x.SelectNodes('.//test-case[@result = "Failure"]')
            }
            foreach ( $testfail in $failures )
            {
                Show-PSPesterError -testFailure $testfail
            }
            throw "$($x.'test-results'.failures) tests in $TestArea failed"
        }
    }
    elseif ($PSCmdlet.ParameterSetName -eq 'PesterPassThruObject')
    {
        if ($ResultObject.TotalCount -le 0 -and -not $CanHaveNoResult)
        {
            throw 'NO TESTS RUN'
        }
        elseif ($ResultObject.FailedCount -gt 0)
        {
            Write-Log -isError -message 'TEST FAILURES'

            $ResultObject.TestResult | Where-Object {$_.Passed -eq $false} | ForEach-Object {
                Show-PSPesterError -testFailureObject $_
            }

            throw "$($ResultObject.FailedCount) tests in $TestArea failed"
        }
    }
}

function Start-PSxUnit {
    [CmdletBinding()]param(
        [string] $xUnitTestResultsFile = "xUnitResults.xml"
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    $Content = Split-Path -Parent (Get-PSOutput)
    if (-not (Test-Path $Content)) {
        throw "PowerShell must be built before running tests!"
    }

    try {
        Push-Location $PSScriptRoot/test/xUnit

        # Path manipulation to obtain test project output directory

        if(-not $environment.IsWindows)
        {
            if($environment.IsMacOS)
            {
                $nativeLib = "$Content/libpsl-native.dylib"
            }
            else
            {
                $nativeLib = "$Content/libpsl-native.so"
            }

            $requiredDependencies = @(
                $nativeLib,
                "$Content/Microsoft.Management.Infrastructure.dll",
                "$Content/System.Text.Encoding.CodePages.dll"
            )

            if((Test-Path $requiredDependencies) -notcontains $false)
            {
                $options = Get-PSOptions -DefaultToNew
                $Destination = "bin/$($options.configuration)/$($options.framework)"
                New-Item $Destination -ItemType Directory -Force > $null
                Copy-Item -Path $requiredDependencies -Destination $Destination -Force
            }
            else
            {
                throw "Dependencies $requiredDependencies not met."
            }
        }

        if (Test-Path $xUnitTestResultsFile) {
            Remove-Item $xUnitTestResultsFile -Force -ErrorAction SilentlyContinue
        }

        # We run the xUnit tests sequentially to avoid race conditions caused by manipulating the config.json file.
        # xUnit tests run in parallel by default. To make them run sequentially, we need to define the 'xunit.runner.json' file.
        dotnet test --configuration $Options.configuration --test-adapter-path:. "--logger:xunit;LogFilePath=$xUnitTestResultsFile"

        Publish-TestResults -Path $xUnitTestResultsFile -Type 'XUnit' -Title 'Xunit Sequential'
    }
    finally {
        Pop-Location
    }
}

function Install-Dotnet {
    [CmdletBinding()]
    param(
        [string]$Channel = $dotnetCLIChannel,
        [string]$Version = $dotnetCLIRequiredVersion,
        [string]$Quality = $dotnetCLIQuality,
        [switch]$NoSudo,
        [string]$InstallDir,
        [string]$AzureFeed,
        [string]$FeedCredential
    )

    # This allows sudo install to be optional; needed when running in containers / as root
    # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
    $sudo = if (!$NoSudo) { "sudo" }

    $installObtainUrl = "https://dotnet.microsoft.com/download/dotnet-core/scripts/v1"
    $uninstallObtainUrl = "https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain"

    # Install for Linux and OS X
    if ($environment.IsLinux -or $environment.IsMacOS) {
        $wget = Get-Command -Name wget -CommandType Application -TotalCount 1 -ErrorAction Stop

        # Uninstall all previous dotnet packages
        $uninstallScript = if ($environment.IsLinux -and $environment.IsUbuntu) {
            "dotnet-uninstall-debian-packages.sh"
        } elseif ($environment.IsMacOS) {
            "dotnet-uninstall-pkgs.sh"
        }

        if ($uninstallScript) {
            Start-NativeExecution {
                & $wget $uninstallObtainUrl/uninstall/$uninstallScript
                Invoke-Expression "$sudo bash ./$uninstallScript"
            }
        } else {
            Write-Warning "This script only removes prior versions of dotnet for Ubuntu and OS X"
        }

        # Install new dotnet 1.1.0 preview packages
        $installScript = "dotnet-install.sh"
        Start-NativeExecution {
            Write-Verbose -Message "downloading install script from $installObtainUrl/$installScript ..." -Verbose
            & $wget $installObtainUrl/$installScript

            if ((Get-ChildItem "./$installScript").Length -eq 0) {
                throw "./$installScript was 0 length"
            }

            if ($Version) {
                $bashArgs = @("./$installScript", '-v', $Version, '-q', $Quality)
            }
            elseif ($Channel) {
                $bashArgs = @("./$installScript", '-c', $Channel, '-q', $Quality)
            }

            if ($InstallDir) {
                $bashArgs += @('-i', $InstallDir)
            }

            if ($AzureFeed) {
                $bashArgs += @('-AzureFeed', $AzureFeed, '-FeedCredential', $FeedCredential)
            }

            bash @bashArgs
        }
    } elseif ($environment.IsWindows) {
        Remove-Item -ErrorAction SilentlyContinue -Recurse -Force ~\AppData\Local\Microsoft\dotnet
        $installScript = "dotnet-install.ps1"
        Invoke-WebRequest -Uri $installObtainUrl/$installScript -OutFile $installScript
        if (-not $environment.IsCoreCLR) {
            $installArgs = @{
                Channel = $Channel
                Quality = $Quality
            }

            if ($InstallDir) {
                $installArgs += @{ InstallDir = $InstallDir }
            }

            if ($AzureFeed) {
                $installArgs += @{
                    AzureFeed       = $AzureFeed
                    $FeedCredential = $FeedCredential
                }
            }

            & ./$installScript @installArgs
        }
        else {
            # dotnet-install.ps1 uses APIs that are not supported in .NET Core, so we run it with Windows PowerShell
            $fullPSPath = Join-Path -Path $env:windir -ChildPath "System32\WindowsPowerShell\v1.0\powershell.exe"
            $fullDotnetInstallPath = Join-Path -Path $PWD.Path -ChildPath $installScript
            Start-NativeExecution {

                if ($Version) {
                    $psArgs = @('-NoLogo', '-NoProfile', '-File', $fullDotnetInstallPath, '-Version', $Version, '-Quality', $Quality)
                }
                elseif ($Channel) {
                    $psArgs = @('-NoLogo', '-NoProfile', '-File', $fullDotnetInstallPath, '-Channel', $Channel, '-Quality', $Quality)
                }

                if ($InstallDir) {
                    $psArgs += @('-InstallDir', $InstallDir)
                }

                if ($AzureFeed) {
                    $psArgs += @('-AzureFeed', $AzureFeed, '-FeedCredential', $FeedCredential)
                }

                & $fullPSPath @psArgs
            }
        }
    }
}

function Get-RedHatPackageManager {
    if ($environment.IsCentOS) {
        "yum install -y -q"
    } elseif ($environment.IsFedora) {
        "dnf install -y -q"
    } else {
        throw "Error determining package manager for this distribution."
    }
}

function Start-PSBootstrap {
    [CmdletBinding()]
    param(
        [string]$Channel = $dotnetCLIChannel,
        # we currently pin dotnet-cli version, and will
        # update it when more stable version comes out.
        [string]$Version = $dotnetCLIRequiredVersion,
        [switch]$Package,
        [switch]$NoSudo,
        [switch]$BuildLinuxArm,
        [switch]$Force
    )

    Write-Log -message "Installing PowerShell build dependencies"

    Push-Location $PSScriptRoot/tools

    try {
        if ($environment.IsLinux -or $environment.IsMacOS) {
            # This allows sudo install to be optional; needed when running in containers / as root
            # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
            $sudo = if (!$NoSudo) { "sudo" }

            if ($BuildLinuxArm -and $environment.IsLinux -and -not $environment.IsUbuntu) {
                Write-Error "Cross compiling for linux-arm is only supported on Ubuntu environment"
                return
            }

            # Install ours and .NET's dependencies
            $Deps = @()
            if ($environment.IsLinux -and $environment.IsUbuntu) {
                # Build tools
                $Deps += "curl", "g++", "cmake", "make"

                if ($BuildLinuxArm) {
                    $Deps += "gcc-arm-linux-gnueabihf", "g++-arm-linux-gnueabihf"
                }

                # .NET Core required runtime libraries
                $Deps += "libunwind8"
                if ($environment.IsUbuntu16) { $Deps += "libicu55" }
                elseif ($environment.IsUbuntu18) { $Deps += "libicu60"}

                # Packaging tools
                if ($Package) { $Deps += "ruby-dev", "groff", "libffi-dev" }

                # Install dependencies
                # change the fontend from apt-get to noninteractive
                $originalDebianFrontEnd=$env:DEBIAN_FRONTEND
                $env:DEBIAN_FRONTEND='noninteractive'
                try {
                    Start-NativeExecution {
                        Invoke-Expression "$sudo apt-get update -qq"
                        Invoke-Expression "$sudo apt-get install -y -qq $Deps"
                    }
                }
                finally {
                    # change the apt frontend back to the original
                    $env:DEBIAN_FRONTEND=$originalDebianFrontEnd
                }
            } elseif ($environment.IsLinux -and $environment.IsRedHatFamily) {
                # Build tools
                $Deps += "which", "curl", "gcc-c++", "cmake", "make"

                # .NET Core required runtime libraries
                $Deps += "libicu", "libunwind"

                # Packaging tools
                if ($Package) { $Deps += "ruby-devel", "rpm-build", "groff", 'libffi-devel' }

                $PackageManager = Get-RedHatPackageManager

                $baseCommand = "$sudo $PackageManager"

                # On OpenSUSE 13.2 container, sudo does not exist, so don't use it if not needed
                if($NoSudo)
                {
                    $baseCommand = $PackageManager
                }

                # Install dependencies
                Start-NativeExecution {
                    Invoke-Expression "$baseCommand $Deps"
                }
            } elseif ($environment.IsLinux -and $environment.IsSUSEFamily) {
                # Build tools
                $Deps += "gcc", "cmake", "make"

                # Packaging tools
                if ($Package) { $Deps += "ruby-devel", "rpmbuild", "groff", 'libffi-devel' }

                $PackageManager = "zypper --non-interactive install"
                $baseCommand = "$sudo $PackageManager"

                # On OpenSUSE 13.2 container, sudo does not exist, so don't use it if not needed
                if($NoSudo)
                {
                    $baseCommand = $PackageManager
                }

                # Install dependencies
                Start-NativeExecution {
                    Invoke-Expression "$baseCommand $Deps"
                }
            } elseif ($environment.IsMacOS) {
                if ($environment.UsingHomebrew) {
                    $PackageManager = "brew"
                } elseif ($environment.UsingMacports) {
                    $PackageManager = "$sudo port"
                }

                # Build tools
                $Deps += "cmake"

                # .NET Core required runtime libraries
                $Deps += "openssl"

                # Install dependencies
                # ignore exitcode, because they may be already installed
                Start-NativeExecution ([ScriptBlock]::Create("$PackageManager install $Deps")) -IgnoreExitcode
            } elseif ($environment.IsLinux -and $environment.IsAlpine) {
                $Deps += 'libunwind', 'libcurl', 'bash', 'cmake', 'clang', 'build-base', 'git', 'curl'

                Start-NativeExecution {
                    Invoke-Expression "apk add $Deps"
                }
            }

            # Install [fpm](https://github.com/jordansissel/fpm) and [ronn](https://github.com/rtomayko/ronn)
            if ($Package) {
                try {
                    # We cannot guess if the user wants to run gem install as root on linux and windows,
                    # but macOs usually requires sudo
                    $gemsudo = ''
                    if($environment.IsMacOS -or $env:TF_BUILD) {
                        $gemsudo = $sudo
                    }
                    Start-NativeExecution ([ScriptBlock]::Create("$gemsudo gem install ffi -v 1.12.0 --no-document"))
                    Start-NativeExecution ([ScriptBlock]::Create("$gemsudo gem install fpm -v 1.11.0 --no-document"))
                    Start-NativeExecution ([ScriptBlock]::Create("$gemsudo gem install ronn -v 0.7.3 --no-document"))
                } catch {
                    Write-Warning "Installation of fpm and ronn gems failed! Must resolve manually."
                }
            }
        }

        # Try to locate dotnet-SDK before installing it
        Find-Dotnet

        # Install dotnet-SDK
        $dotNetExists = precheck 'dotnet' $null
        $dotNetVersion = [string]::Empty
        if($dotNetExists) {
            $dotNetVersion = Start-NativeExecution -sb { dotnet --version } -IgnoreExitcode
        }

        if(!$dotNetExists -or $dotNetVersion -ne $dotnetCLIRequiredVersion -or $Force.IsPresent) {
            if($Force.IsPresent) {
                Write-Log -message "Installing dotnet due to -Force."
            }
            elseif(!$dotNetExists) {
                Write-Log -message "dotnet not present.  Installing dotnet."
            }
            else {
                Write-Log -message "dotnet out of date ($dotNetVersion).  Updating dotnet."
            }

            $DotnetArguments = @{ Channel=$Channel; Version=$Version; NoSudo=$NoSudo }
            Install-Dotnet @DotnetArguments
        }
        else {
            Write-Log -message "dotnet is already installed.  Skipping installation."
        }

        # Install Windows dependencies if `-Package` or `-BuildWindowsNative` is specified
        if ($environment.IsWindows) {
            ## The VSCode build task requires 'pwsh.exe' to be found in Path
            if (-not (Get-Command -Name pwsh.exe -CommandType Application -ErrorAction Ignore))
            {
                Write-Log -message "pwsh.exe not found. Install latest PowerShell release and add it to Path"
                $psInstallFile = [System.IO.Path]::Combine($PSScriptRoot, "tools", "install-powershell.ps1")
                & $psInstallFile -AddToPath
            }
        }
    } finally {
        Pop-Location
    }
}

function Start-DevPowerShell {
    [CmdletBinding(DefaultParameterSetName='ConfigurationParamSet')]
    param(
        [string[]]$ArgumentList = @(),
        [switch]$LoadProfile,
        [Parameter(ParameterSetName='ConfigurationParamSet')]
        [ValidateSet("Debug", "Release", "CodeCoverage", '')] # should match New-PSOptions -Configuration values
        [string]$Configuration,
        [Parameter(ParameterSetName='BinDirParamSet')]
        [string]$BinDir,
        [switch]$NoNewWindow,
        [string]$Command,
        [switch]$KeepPSModulePath
    )

    try {
        if (-not $BinDir) {
            $BinDir = Split-Path (New-PSOptions -Configuration $Configuration).Output
        }

        if ((-not $NoNewWindow) -and ($environment.IsCoreCLR)) {
            Write-Warning "Start-DevPowerShell -NoNewWindow is currently implied in PowerShellCore edition https://github.com/PowerShell/PowerShell/issues/1543"
            $NoNewWindow = $true
        }

        if (-not $LoadProfile) {
            $ArgumentList = @('-noprofile') + $ArgumentList
        }

        if (-not $KeepPSModulePath) {
            if (-not $Command) {
                $ArgumentList = @('-NoExit') + $ArgumentList
            }
            $Command = '$env:PSModulePath = Join-Path $env:DEVPATH Modules; ' + $Command
        }

        if ($Command) {
            $ArgumentList = $ArgumentList + @("-command $Command")
        }

        $env:DEVPATH = $BinDir


        # splatting for the win
        $startProcessArgs = @{
            FilePath = "$BinDir\pwsh"
        }

        if ($ArgumentList) {
            $startProcessArgs.ArgumentList = $ArgumentList
        }

        if ($NoNewWindow) {
            $startProcessArgs.NoNewWindow = $true
            $startProcessArgs.Wait = $true
        }

        Start-Process @startProcessArgs
    } finally {
        if($env:DevPath)
        {
            Remove-Item env:DEVPATH
        }
    }
}

function Start-TypeGen
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()]
        $IncFileName = 'powershell.inc'
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    # This custom target depends on 'ResolveAssemblyReferencesDesignTime', whose definition can be found in the sdk folder.
    # To find the available properties of '_ReferencesFromRAR' when switching to a new dotnet sdk, follow the steps below:
    #   1. create a dummy project using the new dotnet sdk.
    #   2. build the dummy project with this command:
    #      dotnet msbuild .\dummy.csproj /t:ResolveAssemblyReferencesDesignTime /fileLogger /noconsolelogger /v:diag
    #   3. search '_ReferencesFromRAR' in the produced 'msbuild.log' file. You will find the properties there.
    $GetDependenciesTargetPath = "$PSScriptRoot/src/Microsoft.PowerShell.SDK/obj/Microsoft.PowerShell.SDK.csproj.TypeCatalog.targets"
    $GetDependenciesTargetValue = @'
<Project>
    <Target Name="_GetDependencies"
            DependsOnTargets="ResolveAssemblyReferencesDesignTime">
        <ItemGroup>
            <_RefAssemblyPath Include="%(_ReferencesFromRAR.OriginalItemSpec)%3B" Condition=" '%(_ReferencesFromRAR.NuGetPackageId)' != 'Microsoft.Management.Infrastructure' "/>
        </ItemGroup>
        <WriteLinesToFile File="$(_DependencyFile)" Lines="@(_RefAssemblyPath)" Overwrite="true" />
    </Target>
</Project>
'@
    New-Item -ItemType Directory -Path (Split-Path -Path $GetDependenciesTargetPath -Parent) -Force > $null
    Set-Content -Path $GetDependenciesTargetPath -Value $GetDependenciesTargetValue -Force -Encoding Ascii

    Push-Location "$PSScriptRoot/src/Microsoft.PowerShell.SDK"
    try {
        $ps_inc_file = "$PSScriptRoot/src/TypeCatalogGen/$IncFileName"
        dotnet msbuild .\Microsoft.PowerShell.SDK.csproj /t:_GetDependencies "/property:DesignTimeBuild=true;_DependencyFile=$ps_inc_file" /nologo
    } finally {
        Pop-Location
    }

    Push-Location "$PSScriptRoot/src/TypeCatalogGen"
    try {
        dotnet run ../System.Management.Automation/CoreCLR/CorePsTypeCatalog.cs $IncFileName
    } finally {
        Pop-Location
    }
}

function Start-ResGen
{
    [CmdletBinding()]
    param()

    # Add .NET CLI tools to PATH
    Find-Dotnet

    Push-Location "$PSScriptRoot/src/ResGen"
    try {
        Start-NativeExecution { dotnet run } | Write-Verbose
    } finally {
        Pop-Location
    }
}

function Find-Dotnet() {
    $originalPath = $env:PATH
    $dotnetPath = if ($environment.IsWindows) { "$env:LocalAppData\Microsoft\dotnet" } else { "$env:HOME/.dotnet" }

    # If there dotnet is already in the PATH, check to see if that version of dotnet can find the required SDK
    # This is "typically" the globally installed dotnet
    if (precheck dotnet) {
        # Must run from within repo to ensure global.json can specify the required SDK version
        Push-Location $PSScriptRoot
        $dotnetCLIInstalledVersion = Start-NativeExecution -sb { dotnet --version } -IgnoreExitcode 2> $null
        Pop-Location
        if ($dotnetCLIInstalledVersion -ne $dotnetCLIRequiredVersion) {
            Write-Warning "The 'dotnet' in the current path can't find SDK version ${dotnetCLIRequiredVersion}, prepending $dotnetPath to PATH."
            # Globally installed dotnet doesn't have the required SDK version, prepend the user local dotnet location
            $env:PATH = $dotnetPath + [IO.Path]::PathSeparator + $env:PATH
        }
    }
    else {
        Write-Warning "Could not find 'dotnet', appending $dotnetPath to PATH."
        $env:PATH += [IO.Path]::PathSeparator + $dotnetPath
    }

    if (-not (precheck 'dotnet' "Still could not find 'dotnet', restoring PATH.")) {
        $env:PATH = $originalPath
    }
}

<#
    This is one-time conversion. We use it for to turn GetEventResources.txt into GetEventResources.resx

    .EXAMPLE Convert-TxtResourceToXml -Path Microsoft.PowerShell.Commands.Diagnostics\resources
#>
function Convert-TxtResourceToXml
{
    param(
        [string[]]$Path
    )

    process {
        $Path | ForEach-Object {
            Get-ChildItem $_ -Filter "*.txt" | ForEach-Object {
                $txtFile = $_.FullName
                $resxFile = Join-Path (Split-Path $txtFile) "$($_.BaseName).resx"
                $resourceHashtable = ConvertFrom-StringData (Get-Content -Raw $txtFile)
                $resxContent = $resourceHashtable.GetEnumerator() | ForEach-Object {
@'
  <data name="{0}" xml:space="preserve">
    <value>{1}</value>
  </data>
'@ -f $_.Key, $_.Value
                } | Out-String
                Set-Content -Path $resxFile -Value ($script:RESX_TEMPLATE -f $resxContent)
            }
        }
    }
}

function script:Use-MSBuild {
    # TODO: we probably should require a particular version of msbuild, if we are taking this dependency
    # msbuild v14 and msbuild v4 behaviors are different for XAML generation
    $frameworkMsBuildLocation = "${env:SystemRoot}\Microsoft.Net\Framework\v4.0.30319\msbuild"

    $msbuild = Get-Command msbuild -ErrorAction Ignore
    if ($msbuild) {
        # all good, nothing to do
        return
    }

    if (-not (Test-Path $frameworkMsBuildLocation)) {
        throw "msbuild not found in '$frameworkMsBuildLocation'. Install Visual Studio 2015."
    }

    Set-Alias msbuild $frameworkMsBuildLocation -Scope Script
}

function script:Write-Log
{
    param
    (
        [Parameter(Position=0, Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $message,

        [switch] $isError
    )
    if ($isError)
    {
        Write-Host -Foreground Red $message
    }
    else
    {
        Write-Host -Foreground Green $message
    }
    #reset colors for older package to at return to default after error message on a compilation error
    [console]::ResetColor()
}
function script:precheck([string]$command, [string]$missedMessage) {
    $c = Get-Command $command -ErrorAction Ignore
    if (-not $c) {
        if (-not [string]::IsNullOrEmpty($missedMessage))
        {
            Write-Warning $missedMessage
        }
        return $false
    } else {
        return $true
    }
}

# this function wraps native command Execution
# for more information, read https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
function script:Start-NativeExecution
{
    param(
        [scriptblock]$sb,
        [switch]$IgnoreExitcode,
        [switch]$VerboseOutputOnError
    )
    $backupEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if($VerboseOutputOnError.IsPresent)
        {
            $output = & $sb 2>&1
        }
        else
        {
            & $sb
        }

        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode) {
            if($VerboseOutputOnError.IsPresent -and $output)
            {
                $output | Out-String | Write-Verbose -Verbose
            }

            # Get caller location for easier debugging
            $caller = Get-PSCallStack -ErrorAction SilentlyContinue
            if($caller)
            {
                $callerLocationParts = $caller[1].Location -split ":\s*line\s*"
                $callerFile = $callerLocationParts[0]
                $callerLine = $callerLocationParts[1]

                $errorMessage = "Execution of {$sb} by ${callerFile}: line $callerLine failed with exit code $LASTEXITCODE"
                throw $errorMessage
            }
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    } finally {
        $ErrorActionPreference = $backupEAP
    }
}

function Start-CrossGen {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory= $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $PublishPath,

        [Parameter(Mandatory=$true)]
        [ValidateSet("alpine-x64",
                     "linux-arm",
                     "linux-arm64",
                     "linux-x64",
                     "osx-arm64",
                     "osx-x64",
                     "win-arm",
                     "win-arm64",
                     "win7-x64",
                     "win7-x86")]
        [string]
        $Runtime
    )

    function New-CrossGenAssembly {
        param (
            [Parameter(Mandatory= $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $AssemblyPath,
            [Parameter(Mandatory= $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $CrossgenPath
        )

        $outputAssembly = $AssemblyPath.Replace(".dll", ".ni.dll")
        $platformAssembliesPath = Split-Path $AssemblyPath -Parent
        $crossgenFolder = Split-Path $CrossgenPath
        $niAssemblyName = Split-Path $outputAssembly -Leaf

        try {
            Push-Location $crossgenFolder

            # Generate the ngen assembly
            Write-Verbose "Generating assembly $niAssemblyName"
            Start-NativeExecution {
                & $CrossgenPath /ReadyToRun /MissingDependenciesOK /in $AssemblyPath /out $outputAssembly /Platform_Assemblies_Paths $platformAssembliesPath
            } | Write-Verbose
        } finally {
            Pop-Location
        }
    }

    function New-CrossGenSymbol {
        param (
            [Parameter(Mandatory= $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $AssemblyPath,
            [Parameter(Mandatory= $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $CrossgenPath
        )


        $platformAssembliesPath = Split-Path $AssemblyPath -Parent
        $crossgenFolder = Split-Path $CrossgenPath

        try {
            Push-Location $crossgenFolder

            $symbolsPath = [System.IO.Path]::ChangeExtension($assemblyPath, ".pdb")

            $createSymbolOptionName = $null
            if($Environment.IsWindows)
            {
                $createSymbolOptionName = '-CreatePDB'

            }
            elseif ($Environment.IsLinux)
            {
                $createSymbolOptionName = '-CreatePerfMap'
            }

            if($createSymbolOptionName)
            {
                Start-NativeExecution {
                    & $CrossgenPath -readytorun -platform_assemblies_paths $platformAssembliesPath $createSymbolOptionName $platformAssembliesPath $AssemblyPath
                } | Write-Verbose
            }

            # Rename the corresponding ni.dll assembly to be the same as the IL assembly
            $niSymbolsPath = [System.IO.Path]::ChangeExtension($symbolsPath, "ni.pdb")
            Rename-Item $niSymbolsPath $symbolsPath -Force -ErrorAction Stop
        } finally {
            Pop-Location
        }
    }

    if (-not (Test-Path $PublishPath)) {
        throw "Path '$PublishPath' does not exist."
    }

    # Get the path to crossgen
    $crossGenExe = if ($environment.IsWindows) { "crossgen.exe" } else { "crossgen" }
    $generateSymbols = $false

    # The crossgen tool is only published for these particular runtimes
    $crossGenRuntime = if ($environment.IsWindows) {
        if ($Runtime -match "-x86") {
            "win-x86"
        } elseif ($Runtime -match "-x64") {
            "win-x64"
            $generateSymbols = $true
        } elseif (!($env:PROCESSOR_ARCHITECTURE -match "arm")) {
            throw "crossgen for 'win-arm' and 'win-arm64' must be run on that platform"
        }
    } elseif ($Runtime -eq "linux-arm") {
        throw "crossgen is not available for 'linux-arm'"
    } elseif ($Runtime -eq "linux-x64") {
        $Runtime
        # We should set $generateSymbols = $true, but the code needs to be adjusted for different extension on Linux
    } else {
        $Runtime
    }

    if (-not $crossGenRuntime) {
        throw "crossgen is not available for this platform"
    }

    $dotnetRuntimeVersion = $script:Options.Framework -replace 'net'

    # Get the CrossGen.exe for the correct runtime with the latest version
    $crossGenPath = Get-ChildItem $script:Environment.nugetPackagesRoot $crossGenExe -Recurse | `
                        Where-Object { $_.FullName -match $crossGenRuntime } | `
                        Where-Object { $_.FullName -match $dotnetRuntimeVersion } | `
                        Where-Object { (Split-Path $_.FullName -Parent).EndsWith('tools') } | `
                        Sort-Object -Property FullName -Descending | `
                        Select-Object -First 1 | `
                        ForEach-Object { $_.FullName }
    if (-not $crossGenPath) {
        throw "Unable to find latest version of crossgen.exe. 'Please run Start-PSBuild -Clean' first, and then try again."
    }
    Write-Verbose "Matched CrossGen.exe: $crossGenPath" -Verbose

    # Crossgen.exe requires the following assemblies:
    # mscorlib.dll
    # System.Private.CoreLib.dll
    # clrjit.dll on Windows or libclrjit.so/dylib on Linux/OS X
    $crossGenRequiredAssemblies = @("mscorlib.dll", "System.Private.CoreLib.dll")

    $crossGenRequiredAssemblies += if ($environment.IsWindows) {
        "clrjit.dll"
    } elseif ($environment.IsLinux) {
        "libclrjit.so"
    } elseif ($environment.IsMacOS) {
        "libclrjit.dylib"
    }

    # Make sure that all dependencies required by crossgen are at the directory.
    $crossGenFolder = Split-Path $crossGenPath
    foreach ($assemblyName in $crossGenRequiredAssemblies) {
        if (-not (Test-Path "$crossGenFolder\$assemblyName")) {
            Copy-Item -Path "$PublishPath\$assemblyName" -Destination $crossGenFolder -Force -ErrorAction Stop
        }
    }

    # Common assemblies used by Add-Type or assemblies with high JIT and no pdbs to crossgen
    $commonAssembliesForAddType = @(
        "Microsoft.CodeAnalysis.CSharp.dll"
        "Microsoft.CodeAnalysis.dll"
        "System.Linq.Expressions.dll"
        "Microsoft.CSharp.dll"
        "System.Runtime.Extensions.dll"
        "System.Linq.dll"
        "System.Collections.Concurrent.dll"
        "System.Collections.dll"
        "Newtonsoft.Json.dll"
        "System.IO.FileSystem.dll"
        "System.Diagnostics.Process.dll"
        "System.Threading.Tasks.Parallel.dll"
        "System.Security.AccessControl.dll"
        "System.Text.Encoding.CodePages.dll"
        "System.Private.Uri.dll"
        "System.Threading.dll"
        "System.Security.Principal.Windows.dll"
        "System.Console.dll"
        "Microsoft.Win32.Registry.dll"
        "System.IO.Pipes.dll"
        "System.Diagnostics.FileVersionInfo.dll"
        "System.Collections.Specialized.dll"
        "Microsoft.ApplicationInsights.dll"
    )

    $fullAssemblyList = $commonAssembliesForAddType

    foreach ($assemblyName in $fullAssemblyList) {
        $assemblyPath = Join-Path $PublishPath $assemblyName
        New-CrossGenAssembly -CrossgenPath $crossGenPath -AssemblyPath $assemblyPath
    }

    #
    # With the latest dotnet.exe, the default load context is only able to load TPAs, and TPA
    # only contains IL assembly names. In order to make the default load context able to load
    # the NI PS assemblies, we need to replace the IL PS assemblies with the corresponding NI
    # PS assemblies, but with the same IL assembly names.
    #
    Write-Verbose "PowerShell Ngen assemblies have been generated. Deploying ..." -Verbose
    foreach ($assemblyName in $fullAssemblyList) {

        # Remove the IL assembly and its symbols.
        $assemblyPath = Join-Path $PublishPath $assemblyName
        $symbolsPath = [System.IO.Path]::ChangeExtension($assemblyPath, ".pdb")

        Remove-Item $assemblyPath -Force -ErrorAction Stop

        # Rename the corresponding ni.dll assembly to be the same as the IL assembly
        $niAssemblyPath = [System.IO.Path]::ChangeExtension($assemblyPath, "ni.dll")
        Rename-Item $niAssemblyPath $assemblyPath -Force -ErrorAction Stop

        # No symbols are available for Microsoft.CodeAnalysis.CSharp.dll, Microsoft.CodeAnalysis.dll,
        # Microsoft.CodeAnalysis.VisualBasic.dll, and Microsoft.CSharp.dll.
        if ($commonAssembliesForAddType -notcontains $assemblyName) {
            Remove-Item $symbolsPath -Force -ErrorAction Stop

            if($generateSymbols)
            {
                Write-Verbose "Generating Symbols for $assemblyPath"
                New-CrossGenSymbol -CrossgenPath $crossGenPath -AssemblyPath $assemblyPath
            }
        }
    }
}

# Cleans the PowerShell repo - everything but the root folder
function Clear-PSRepo
{
    [CmdletBinding()]
    param()

    Get-ChildItem $PSScriptRoot\* -Directory | ForEach-Object {
        Write-Verbose "Cleaning $_ ..."
        git clean -fdX $_
    }
}

# Install PowerShell modules such as PackageManagement, PowerShellGet
function Copy-PSGalleryModules
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$CsProjPath,

        [Parameter(Mandatory=$true)]
        [string]$Destination,

        [Parameter()]
        [switch]$Force
    )

    if (!$Destination.EndsWith("Modules")) {
        throw "Installing to an unexpected location"
    }

    Find-DotNet

    Restore-PSPackage -ProjectDirs (Split-Path $CsProjPath) -Force:$Force.IsPresent -PSModule

    $cache = dotnet nuget locals global-packages -l
    if ($cache -match "global-packages: (.*)") {
        $nugetCache = $Matches[1]
    }
    else {
        throw "Can't find nuget global cache"
    }

    $psGalleryProj = [xml](Get-Content -Raw $CsProjPath)

    foreach ($m in $psGalleryProj.Project.ItemGroup.PackageReference) {
        $name = $m.Include
        $version = $m.Version
        Write-Log -message "Name='$Name', Version='$version', Destination='$Destination'"

        # Remove the build revision from the src (nuget drops it).
        $srcVer = if ($version -match "(\d+.\d+.\d+).0") {
            $Matches[1]
        } elseif ($version -match "^\d+.\d+$") {
            # Two digit versions are stored as three digit versions
            "$version.0"
        } else {
            $version
        }

        # Nuget seems to always use lowercase in the cache
        $src = "$nugetCache/$($name.ToLower())/$srcVer"
        $dest = "$Destination/$name"

        Remove-Item -Force -ErrorAction Ignore -Recurse "$Destination/$name"
        New-Item -Path $dest -ItemType Directory -Force -ErrorAction Stop > $null
        # Exclude files/folders that are not needed. The fullclr folder is coming from the PackageManagement module
        $dontCopy = '*.nupkg', '*.nupkg.metadata', '*.nupkg.sha512', '*.nuspec', 'System.Runtime.InteropServices.RuntimeInformation.dll', 'fullclr'
        Copy-Item -Exclude $dontCopy -Recurse $src/* $dest -ErrorAction Stop
    }
}

function Merge-TestLogs
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({Test-Path $_})]
        [string]$XUnitLogPath,

        [Parameter(Mandatory = $true)]
        [ValidateScript({Test-Path $_})]
        [string[]]$NUnitLogPath,

        [Parameter()]
        [ValidateScript({Test-Path $_})]
        [string[]]$AdditionalXUnitLogPath,

        [Parameter()]
        [string]$OutputLogPath
    )

    # Convert all the NUnit logs into single object
    $convertedNUnit = ConvertFrom-PesterLog -logFile $NUnitLogPath

    $xunit = [xml] (Get-Content $XUnitLogPath -ReadCount 0 -Raw)

    $strBld = [System.Text.StringBuilder]::new($xunit.assemblies.InnerXml)

    foreach($assembly in $convertedNUnit.assembly)
    {
        $strBld.Append($assembly.ToString()) | Out-Null
    }

    foreach($path in $AdditionalXUnitLogPath)
    {
        $addXunit = [xml] (Get-Content $path -ReadCount 0 -Raw)
        $strBld.Append($addXunit.assemblies.InnerXml) | Out-Null
    }

    $xunit.assemblies.InnerXml = $strBld.ToString()
    $xunit.Save($OutputLogPath)
}

function ConvertFrom-PesterLog {
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline = $true, Mandatory = $true, Position = 0)]
        [string[]]$Logfile,
        [Parameter()][switch]$IncludeEmpty,
        [Parameter()][switch]$MultipleLog
    )
    <#
Convert our test logs to
xunit schema - top level assemblies
Pester conversion
foreach $r in "test-results"."test-suite".results."test-suite"
assembly
    name = $r.Description
    config-file = log file (this is the only way we can determine between admin/nonadmin log)
    test-framework = Pester
    environment = top-level "test-results.environment.platform
    run-date = date (doesn't exist in pester except for beginning)
    run-time = time
    time =
#>

    BEGIN {
        # CLASSES
        class assemblies {
            # attributes
            [datetime]$timestamp
            # child elements
            [System.Collections.Generic.List[testAssembly]]$assembly
            assemblies() {
                $this.timestamp = [datetime]::now
                $this.assembly = [System.Collections.Generic.List[testAssembly]]::new()
            }
            static [assemblies] op_Addition([assemblies]$ls, [assemblies]$rs) {
                $newAssembly = [assemblies]::new()
                $newAssembly.assembly.AddRange($ls.assembly)
                $newAssembly.assembly.AddRange($rs.assembly)
                return $newAssembly
            }
            [string]ToString() {
                $sb = [text.stringbuilder]::new()
                $sb.AppendLine('<assemblies timestamp="{0:MM}/{0:dd}/{0:yyyy} {0:HH}:{0:mm}:{0:ss}">' -f $this.timestamp)
                foreach ( $a in $this.assembly  ) {
                    $sb.Append("$a")
                }
                $sb.AppendLine("</assemblies>");
                return $sb.ToString()
            }
            # use Write-Output to emit these into the pipeline
            [array]GetTests() {
                return $this.Assembly.collection.test
            }
        }

        class testAssembly {
            # attributes
            [string]$name # path to pester file
            [string]${config-file}
            [string]${test-framework} # Pester
            [string]$environment
            [string]${run-date}
            [string]${run-time}
            [decimal]$time
            [int]$total
            [int]$passed
            [int]$failed
            [int]$skipped
            [int]$errors
            testAssembly ( ) {
                $this."config-file" = "no config"
                $this."test-framework" = "Pester"
                $this.environment = $script:environment
                $this."run-date" = $script:rundate
                $this."run-time" = $script:runtime
                $this.collection = [System.Collections.Generic.List[collection]]::new()
            }
            # child elements
            [error[]]$error
            [System.Collections.Generic.List[collection]]$collection
            [string]ToString() {
                $sb = [System.Text.StringBuilder]::new()
                $sb.AppendFormat('  <assembly name="{0}" ', $this.name)
                $sb.AppendFormat('environment="{0}" ', [security.securityelement]::escape($this.environment))
                $sb.AppendFormat('test-framework="{0}" ', $this."test-framework")
                $sb.AppendFormat('run-date="{0}" ', $this."run-date")
                $sb.AppendFormat('run-time="{0}" ', $this."run-time")
                $sb.AppendFormat('total="{0}" ', $this.total)
                $sb.AppendFormat('passed="{0}" ', $this.passed)
                $sb.AppendFormat('failed="{0}" ', $this.failed)
                $sb.AppendFormat('skipped="{0}" ', $this.skipped)
                $sb.AppendFormat('time="{0}" ', $this.time)
                $sb.AppendFormat('errors="{0}" ', $this.errors)
                $sb.AppendLine(">")
                if ( $this.error ) {
                    $sb.AppendLine("    <errors>")
                    foreach ( $e in $this.error ) {
                        $sb.AppendLine($e.ToString())
                    }
                    $sb.AppendLine("    </errors>")
                } else {
                    $sb.AppendLine("    <errors />")
                }
                foreach ( $col in $this.collection ) {
                    $sb.AppendLine($col.ToString())
                }
                $sb.AppendLine("  </assembly>")
                return $sb.ToString()
            }
        }

        class collection {
            # attributes
            [string]$name
            [decimal]$time
            [int]$total
            [int]$passed
            [int]$failed
            [int]$skipped
            # child element
            [System.Collections.Generic.List[test]]$test
            # constructor
            collection () {
                $this.test = [System.Collections.Generic.List[test]]::new()
            }
            [string]ToString() {
                $sb = [Text.StringBuilder]::new()
                if ( $this.test.count -eq 0 ) {
                    $sb.AppendLine("    <collection />")
                } else {
                    $sb.AppendFormat('    <collection total="{0}" passed="{1}" failed="{2}" skipped="{3}" name="{4}" time="{5}">' + "`n",
                        $this.total, $this.passed, $this.failed, $this.skipped, [security.securityelement]::escape($this.name), $this.time)
                    foreach ( $t in $this.test ) {
                        $sb.AppendLine("    " + $t.ToString());
                    }
                    $sb.Append("    </collection>")
                }
                return $sb.ToString()
            }
        }

        class errors {
            [error[]]$error
        }
        class error {
            # attributes
            [string]$type
            [string]$name
            # child elements
            [failure]$failure
            [string]ToString() {
                $sb = [system.text.stringbuilder]::new()
                $sb.AppendLine('<error type="{0}" name="{1}" >' -f $this.type, [security.securityelement]::escape($this.Name))
                $sb.AppendLine($this.failure -as [string])
                $sb.AppendLine("</error>")
                return $sb.ToString()
            }
        }

        class cdata {
            [string]$text
            cdata ( [string]$s ) { $this.text = $s }
            [string]ToString() {
                return '<![CDATA[' + [security.securityelement]::escape($this.text) + ']]>'
            }
        }

        class failure {
            [string]${exception-type}
            [cdata]$message
            [cdata]${stack-trace}
            failure ( [string]$message, [string]$stack ) {
                $this."exception-type" = "Pester"
                $this.Message = [cdata]::new($message)
                $this."stack-trace" = [cdata]::new($stack)
            }
            [string]ToString() {
                $sb = [text.stringbuilder]::new()
                $sb.AppendLine("        <failure>")
                $sb.AppendLine("          <message>" + ($this.message -as [string]) + "</message>")
                $sb.AppendLine("          <stack-trace>" + ($this."stack-trace" -as [string]) + "</stack-trace>")
                $sb.Append("        </failure>")
                return $sb.ToString()
            }
        }

        enum resultenum {
            Pass
            Fail
            Skip
        }

        class trait {
            # attributes
            [string]$name
            [string]$value
        }
        class traits {
            [trait[]]$trait
        }
        class test {
            # attributes
            [string]$name
            [string]$type
            [string]$method
            [decimal]$time
            [resultenum]$result
            # child elements
            [trait[]]$traits
            [failure]$failure
            [cdata]$reason # skip reason
            [string]ToString() {
                $sb = [text.stringbuilder]::new()
                $sb.appendformat('  <test name="{0}" type="{1}" method="{2}" time="{3}" result="{4}"',
                    [security.securityelement]::escape($this.name), [security.securityelement]::escape($this.type),
                    [security.securityelement]::escape($this.method), $this.time, $this.result)
                if ( $this.failure ) {
                    $sb.AppendLine(">")
                    $sb.AppendLine($this.failure -as [string])
                    $sb.append('      </test>')
                } else {
                    $sb.Append("/>")
                }
                return $sb.ToString()
            }
        }

        function convert-pesterlog ( [xml]$x, $logpath, [switch]$includeEmpty ) {
            <#$resultMap = @{
                Success = "Pass"
                Ignored = "Skip"
                Failure = "Fail"
            }#>

            $resultMap = @{
                Success = "Pass"
                Ignored = "Skip"
                Failure = "Fail"
                Inconclusive = "Skip"
            }

            $configfile = $logpath
            $runtime = $x."test-results".time
            $environment = $x."test-results".environment.platform + "-" + $x."test-results".environment."os-version"
            $rundate = $x."test-results".date
            $suites = $x."test-results"."test-suite".results."test-suite"
            $assemblies = [assemblies]::new()
            foreach ( $suite in $suites ) {
                $tCases = $suite.SelectNodes(".//test-case")
                # only create an assembly group if we have tests
                if ( $tCases.count -eq 0 -and ! $includeEmpty ) { continue }
                $tGroup = $tCases | Group-Object result
                $total = $tCases.Count
                $asm = [testassembly]::new()
                $asm.environment = $environment
                $asm."run-date" = $rundate
                $asm."run-time" = $runtime
                $asm.Name = $suite.name
                $asm."config-file" = $configfile
                $asm.time = $suite.time
                $asm.total = $suite.SelectNodes(".//test-case").Count
                $asm.Passed = $tGroup| Where-Object -FilterScript {$_.Name -eq "Success"} | ForEach-Object -Process {$_.Count}
                $asm.Failed = $tGroup| Where-Object -FilterScript {$_.Name -eq "Failure"} | ForEach-Object -Process {$_.Count}
                $asm.Skipped = $tGroup| Where-Object -FilterScript { $_.Name -eq "Ignored" } | ForEach-Object -Process {$_.Count}
                $asm.Skipped += $tGroup| Where-Object -FilterScript { $_.Name -eq "Inconclusive" } | ForEach-Object -Process {$_.Count}
                $c = [collection]::new()
                $c.passed = $asm.Passed
                $c.failed = $asm.failed
                $c.skipped = $asm.skipped
                $c.total = $asm.total
                $c.time = $asm.time
                $c.name = $asm.name
                foreach ( $tc in $suite.SelectNodes(".//test-case")) {
                    if ( $tc.result -match "Success|Ignored|Failure" ) {
                        $t = [test]::new()
                        $t.name = $tc.Name
                        $t.time = $tc.time
                        $t.method = $tc.description # the pester actually puts the name of the "it" as description
                        $t.type = $suite.results."test-suite".description | Select-Object -First 1
                        $t.result = $resultMap[$tc.result]
                        if ( $tc.failure ) {
                            $t.failure = [failure]::new($tc.failure.message, $tc.failure."stack-trace")
                        }
                        $null = $c.test.Add($t)
                    }
                }
                $null = $asm.collection.add($c)
                $assemblies.assembly.Add($asm)
            }
            $assemblies
        }

        # convert it to our object model
        # a simple conversion
        function convert-xunitlog {
            param ( $x, $logpath )
            $asms = [assemblies]::new()
            $asms.timestamp = $x.assemblies.timestamp
            foreach ( $assembly in $x.assemblies.assembly ) {
                $asm = [testAssembly]::new()
                $asm.environment = $assembly.environment
                $asm."test-framework" = $assembly."test-framework"
                $asm."run-date" = $assembly."run-date"
                $asm."run-time" = $assembly."run-time"
                $asm.total = $assembly.total
                $asm.passed = $assembly.passed
                $asm.failed = $assembly.failed
                $asm.skipped = $assembly.skipped
                $asm.time = $assembly.time
                $asm.name = $assembly.name
                foreach ( $coll in $assembly.collection ) {
                    $c = [collection]::new()
                    $c.name = $coll.name
                    $c.total = $coll.total
                    $c.passed = $coll.passed
                    $c.failed = $coll.failed
                    $c.skipped = $coll.skipped
                    $c.time = $coll.time
                    foreach ( $t in $coll.test ) {
                        $test = [test]::new()
                        $test.name = $t.name
                        $test.type = $t.type
                        $test.method = $t.method
                        $test.time = $t.time
                        $test.result = $t.result
                        $c.test.Add($test)
                    }
                    $null = $asm.collection.add($c)
                }
                $null = $asms.assembly.add($asm)
            }
            $asms
        }
        $Logs = @()
    }

    PROCESS {
        #### MAIN ####
        foreach ( $log in $Logfile ) {
            foreach ( $logpath in (Resolve-Path $log).path ) {
                Write-Progress "converting file $logpath"
                if ( ! $logpath) { throw "Cannot resolve $Logfile" }
                $x = [xml](Get-Content -Raw -ReadCount 0 $logpath)

                if ( $x.psobject.properties['test-results'] ) {
                    $Logs += convert-pesterlog $x $logpath -includeempty:$includeempty
                } elseif ( $x.psobject.properties['assemblies'] ) {
                    $Logs += convert-xunitlog $x $logpath -includeEmpty:$includeEmpty
                } else {
                    Write-Error "Cannot determine log type"
                }
            }
        }
    }

    END {
        if ( $MultipleLog ) {
            $Logs
        } else {
            $combinedLog = $Logs[0]
            for ( $i = 1; $i -lt $logs.count; $i++ ) {
                $combinedLog += $Logs[$i]
            }
            $combinedLog
        }
    }
}

# Save PSOptions to be restored by Restore-PSOptions
function Save-PSOptions {
    param(
        [ValidateScript({$parent = Split-Path $_;if($parent){Test-Path $parent}else{return $true}})]
        [ValidateNotNullOrEmpty()]
        [string]
        $PSOptionsPath = (Join-Path -Path $PSScriptRoot -ChildPath 'psoptions.json'),

        [ValidateNotNullOrEmpty()]
        [object]
        $Options = (Get-PSOptions -DefaultToNew)
    )

    $Options | ConvertTo-Json -Depth 3 | Out-File -Encoding utf8 -FilePath $PSOptionsPath
}

# Restore PSOptions
# Optionally remove the PSOptions file
function Restore-PSOptions {
    param(
        [ValidateScript({Test-Path $_})]
        [string]
        $PSOptionsPath = (Join-Path -Path $PSScriptRoot -ChildPath 'psoptions.json'),
        [switch]
        $Remove
    )

    $options = Get-Content -Path $PSOptionsPath | ConvertFrom-Json

    if($Remove)
    {
        # Remove PSOptions.
        # The file is only used to set the PSOptions.
        Remove-Item -Path $psOptionsPath -Force
    }

    $newOptions = New-PSOptionsObject `
                    -RootInfo $options.RootInfo `
                    -Top $options.Top `
                    -Runtime $options.Runtime `
                    -Crossgen $options.Crossgen `
                    -Configuration $options.Configuration `
                    -PSModuleRestore $options.PSModuleRestore `
                    -Framework $options.Framework `
                    -Output $options.Output `
                    -ForMinimalSize $options.ForMinimalSize

    Set-PSOptions -Options $newOptions
}

function New-PSOptionsObject
{
    param(
        [PSCustomObject]
        $RootInfo,

        [Parameter(Mandatory)]
        [String]
        $Top,

        [Parameter(Mandatory)]
        [String]
        $Runtime,

        [Parameter(Mandatory)]
        [Bool]
        $CrossGen,

        [Parameter(Mandatory)]
        [String]
        $Configuration,

        [Parameter(Mandatory)]
        [Bool]
        $PSModuleRestore,

        [Parameter(Mandatory)]
        [String]
        $Framework,

        [Parameter(Mandatory)]
        [String]
        $Output,

        [Parameter(Mandatory)]
        [Bool]
        $ForMinimalSize
    )

    return @{
        RootInfo = $RootInfo
        Top = $Top
        Configuration = $Configuration
        Framework = $Framework
        Runtime = $Runtime
        Output = $Output
        CrossGen = $CrossGen
        PSModuleRestore = $PSModuleRestore
        ForMinimalSize = $ForMinimalSize
    }
}

$script:RESX_TEMPLATE = @'
<?xml version="1.0" encoding="utf-8"?>
<root>
  <!--
    Microsoft ResX Schema

    Version 2.0

    The primary goals of this format is to allow a simple XML format
    that is mostly human readable. The generation and parsing of the
    various data types are done through the TypeConverter classes
    associated with the data types.

    Example:

    ... ado.net/XML headers & schema ...
    <resheader name="resmimetype">text/microsoft-resx</resheader>
    <resheader name="version">2.0</resheader>
    <resheader name="reader">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name="writer">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name="Name1"><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name="Color1" type="System.Drawing.Color, System.Drawing">Blue</data>
    <data name="Bitmap1" mimetype="application/x-microsoft.net.object.binary.base64">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name="Icon1" type="System.Drawing.Icon, System.Drawing" mimetype="application/x-microsoft.net.object.bytearray.base64">
        <value>[base64 mime encoded string representing a byte array form of the .NET Framework object]</value>
        <comment>This is a comment</comment>
    </data>

    There are any number of "resheader" rows that contain simple
    name/value pairs.

    Each data row contains a name, and value. The row also contains a
    type or mimetype. Type corresponds to a .NET class that support
    text/value conversion through the TypeConverter architecture.
    Classes that don't support this are serialized and stored with the
    mimetype set.

    The mimetype is used for serialized objects, and tells the
    ResXResourceReader how to depersist the object. This is currently not
    extensible. For a given mimetype the value must be set accordingly:

    Note - application/x-microsoft.net.object.binary.base64 is the format
    that the ResXResourceWriter will generate, however the reader can
    read any of the formats listed below.

    mimetype: application/x-microsoft.net.object.binary.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.soap.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Soap.SoapFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.bytearray.base64
    value   : The object must be serialized into a byte array
            : using a System.ComponentModel.TypeConverter
            : and then encoded with base64 encoding.
    -->
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
{0}
</root>
'@

function Get-UniquePackageFolderName {
    param(
        [Parameter(Mandatory)] $Root
    )

    $packagePath = Join-Path $Root 'TestPackage'

    $triesLeft = 10

    while(Test-Path $packagePath) {
        $suffix = Get-Random

        # Not using Guid to avoid maxpath problems as in example below.
        # Example: 'TestPackage-ba0ae1db-8512-46c5-8b6c-1862d33a2d63\test\powershell\Modules\Microsoft.PowerShell.Security\TestData\CatalogTestData\UserConfigProv\DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.schema.mof'
        $packagePath = Join-Path $Root "TestPackage_$suffix"
        $triesLeft--

        if ($triesLeft -le 0) {
            throw "Could find unique folder name for package path"
        }
    }

    $packagePath
}

function New-TestPackage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Destination,
        [string] $Runtime
    )

    if (Test-Path $Destination -PathType Leaf)
    {
        throw "Destination: '$Destination' is not a directory or does not exist."
    }
    else
    {
        $null = New-Item -Path $Destination -ItemType Directory -Force
        Write-Verbose -Message "Creating destination folder: $Destination"
    }

    $rootFolder = $env:TEMP

    # In some build agents, typically macOS on AzDevOps, $env:TEMP might not be set.
    if (-not $rootFolder -and $env:TF_BUILD) {
        $rootFolder = $env:AGENT_WORKFOLDER
    }

    Write-Verbose -Message "RootFolder: $rootFolder" -Verbose
    $packageRoot = Get-UniquePackageFolderName -Root $rootFolder

    $null = New-Item -ItemType Directory -Path $packageRoot -Force
    $packagePath = Join-Path $Destination "TestPackage.zip"
    Write-Verbose -Message "PackagePath: $packagePath" -Verbose

    # Build test tools so they are placed in appropriate folders under 'test' then copy to package root.
    $null = Publish-PSTestTools -runtime $Runtime
    $powerShellTestRoot =  Join-Path $PSScriptRoot 'test'
    Copy-Item $powerShellTestRoot -Recurse -Destination $packageRoot -Force
    Write-Verbose -Message "Copied test directory"

    # Copy assests folder to package root for wix related tests.
    $assetsPath = Join-Path $PSScriptRoot 'assets'
    Copy-Item $assetsPath -Recurse -Destination $packageRoot -Force
    Write-Verbose -Message "Copied assests directory"

    # Create expected folder structure for resx files in package root.
    $srcRootForResx = New-Item -Path "$packageRoot/src" -Force -ItemType Directory

    $resourceDirectories = Get-ChildItem -Recurse "$PSScriptRoot/src" -Directory -Filter 'resources'

    $resourceDirectories | ForEach-Object {
        $directoryFullName = $_.FullName

        $partToRemove = Join-Path $PSScriptRoot "src"

        $assemblyPart = $directoryFullName.Replace($partToRemove, '')
        $assemblyPart = $assemblyPart.TrimStart([io.path]::DirectorySeparatorChar)
        $resxDestPath = Join-Path $srcRootForResx $assemblyPart
        $null = New-Item -Path $resxDestPath -Force -ItemType Directory
        Write-Verbose -Message "Created resx directory : $resxDestPath"
        Copy-Item -Path "$directoryFullName\*" -Recurse $resxDestPath -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if(Test-Path $packagePath)
    {
        Remove-Item -Path $packagePath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot, $packagePath)
}

function New-NugetConfigFile
{
    param(
        [Parameter(Mandatory=$true)] [string] $NugetFeedUrl,
        [Parameter(Mandatory=$true)] [string] $FeedName,
        [Parameter(Mandatory=$true)] [string] $UserName,
        [Parameter(Mandatory=$true)] [string] $ClearTextPAT,
        [Parameter(Mandatory=$true)] [string] $Destination
    )

    $nugetConfigTemplate = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="[FEEDNAME]" value="[FEED]" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
  <packageSourceCredentials>
    <[FEEDNAME]>
      <add key="Username" value="[USERNAME]" />
      <add key="ClearTextPassword" value="[PASSWORD]" />
    </[FEEDNAME]>
  </packageSourceCredentials>
</configuration>
'@

    $content = $nugetConfigTemplate.Replace('[FEED]', $NugetFeedUrl).Replace('[FEEDNAME]', $FeedName).Replace('[USERNAME]', $UserName).Replace('[PASSWORD]', $ClearTextPAT)

    Set-Content -Path (Join-Path $Destination 'nuget.config') -Value $content -Force
}
