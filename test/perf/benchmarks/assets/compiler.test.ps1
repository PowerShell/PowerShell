## Copyright (c) Microsoft Corporation.
## Licensed under the MIT License.

function Get-EnvInformation
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
        $environment += @{'nugetPackagesRoot' = "${env:USERPROFILE}\.nuget\packages", "${env:NUGET_PACKAGES}"}
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
    # removing --no-restore due to .NET SDK issue: https://github.com/dotnet/sdk/issues/18999
    # $Arguments = @("publish","--no-restore","/property:GenerateFullPaths=true", "/property:ErrorOnDuplicatePublishOutputFiles=false")
    $Arguments = @("publish","/property:GenerateFullPaths=true", "/property:ErrorOnDuplicatePublishOutputFiles=false")
    if ($Output -or $SMAOnly) {
        $Arguments += "--output", (Split-Path $Options.Output)
    }

    # Add --self-contained due to "warning NETSDK1179: One of '--self-contained' or '--no-self-contained' options are required when '--runtime' is used."
    if ($Options.Runtime -like 'fxdependent*') {
        $Arguments += "--no-self-contained"
    }
    else {
        $Arguments += "--self-contained"
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

            # We plan to release packages targeting win7-x64 and win7-x86 RIDs,
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
        if ($environment.IsWindows) {
            $outputBufferFilePath = [System.IO.Path]::GetTempFileName()
        }
        else {
            # Azure DevOps agents do not have Temp folder setup on Ubuntu 20.04, hence using HOME directory
            $outputBufferFilePath = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        }
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

        if ($environment.IsWindows) {
            $configFile = [System.IO.Path]::GetTempFileName()
        }
        else {
            $configFile = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        }

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
                if ($environment.IsWindows) {
                    $passThruFile = [System.IO.Path]::GetTempFileName()
                }
                else {
                    $passThruFile = Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName())
                }

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
                Quality = $Quality
            }

            if ($Version) {
                $installArgs += @{ Version = $Version }
            } elseif ($Channel) {
                $installArgs += @{ Channel = $Channel }
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
                $Deps += "curl", "g++", "make"

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
                $Deps += "which", "curl", "gcc-c++", "make"

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
                $Deps += "gcc", "make"

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

                # .NET Core required runtime libraries
                $Deps += "openssl"

                # Install dependencies
                # ignore exitcode, because they may be already installed
                Start-NativeExecution ([ScriptBlock]::Create("$PackageManager install $Deps")) -IgnoreExitcode
            } elseif ($environment.IsLinux -and $environment.IsAlpine) {
                $Deps += 'libunwind', 'libcurl', 'bash', 'clang', 'build-base', 'git', 'curl'

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
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [String[]]
            $AssemblyPath,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $CrossgenPath,

            [Parameter(Mandatory = $true)]
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

        $platformAssembliesPath = Split-Path $AssemblyPath[0] -Parent

        $targetOS, $targetArch = $Runtime -split '-'

        # Special cases where OS / Arch does not conform with runtime names
        switch ($Runtime) {
            'alpine-x64' {
                $targetOS = 'linux'
                $targetArch = 'x64'
             }
            'win-arm' {
                $targetOS = 'windows'
                $targetArch = 'arm'
            }
            'win-arm64' {
                $targetOS = 'windows'
                $targetArch = 'arm64'
            }
            'win7-x64' {
                $targetOS = 'windows'
                $targetArch = 'x64'
            }
            'win7-x86' {
                $targetOS = 'windows'
                $targetArch = 'x86'
            }
        }

        $generatePdb = $targetos -eq 'windows'

        # The path to folder must end with directory separator
        $dirSep = [System.IO.Path]::DirectorySeparatorChar
        $platformAssembliesPath = if (-not $platformAssembliesPath.EndsWith($dirSep)) { $platformAssembliesPath + $dirSep }

        Start-NativeExecution {
            $crossgen2Params = @(
                "-r"
                $platformAssembliesPath
                "--out-near-input"
                "--single-file-compilation"
                "-O"
                "--targetos"
                $targetOS
                "--targetarch"
                $targetArch
            )

            if ($generatePdb) {
                $crossgen2Params += "--pdb"
            }

            $crossgen2Params += $AssemblyPath

            & $CrossgenPath $crossgen2Params
        }
    }

    if (-not (Test-Path $PublishPath)) {
        throw "Path '$PublishPath' does not exist."
    }

    # Get the path to crossgen
    $crossGenExe = if ($environment.IsWindows) { "crossgen2.exe" } else { "crossgen2" }

    # The crossgen tool is only published for these particular runtimes
    $crossGenRuntime = if ($environment.IsWindows) {
        # for windows the tool architecture is the host machine architecture, so it is always x64.
        # we can cross compile for x86, arm and arm64
        "win-x64"
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
        throw "Unable to find latest version of crossgen2.exe. 'Please run Start-PSBuild -Clean' first, and then try again."
    }
    Write-Verbose "Matched CrossGen2.exe: $crossGenPath" -Verbose

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

    $assemblyFullPaths = @()
    $assemblyFullPaths += foreach ($assemblyName in $fullAssemblyList) {
        Join-Path $PublishPath $assemblyName
    }

    New-CrossGenAssembly -CrossgenPath $crossGenPath -AssemblyPath $assemblyFullPaths -Runtime $Runtime

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
        }
    }
}

function Use-PSClass {
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
