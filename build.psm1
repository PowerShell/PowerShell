# On Unix paths is separated by colon
# On Windows paths is separated by semicolon
$script:TestModulePathSeparator = [System.IO.Path]::PathSeparator

$dotnetCLIChannel = "release"
$dotnetCLIRequiredVersion = "2.0.2"

# Track if tags have been sync'ed
$tagsUpToDate = $false

# Sync Tags
# When not using a branch in PowerShell/PowerShell, tags will not be fetched automatically
# Since code that uses Get-PSCommitID and Get-PSLatestTag assume that tags are fetched,
# This function can ensure that tags have been fetched.
# This function is used during the setup phase in tools/appveyor.psm1 and tools/travis.ps1
function Sync-PSTags
{
    param(
        [Switch]
        $AddRemoteIfMissing
    )

    $PowerShellRemoteUrl = "https://github.com/powershell/powershell.git"
    $upstreamRemoteDefaultName = 'upstream'
    $remotes = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" remote}
    $upstreamRemote = $null
    foreach($remote in $remotes)
    {
        $url = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" remote get-url $remote}
        if($url -eq $PowerShellRemoteUrl)
        {
            $upstreamRemote = $remote
            break
        }
    }

    if(!$upstreamRemote -and $AddRemoteIfMissing.IsPresent -and $remotes -notcontains $upstreamRemoteDefaultName)
    {
        $null = Start-NativeExecution {git --git-dir="$PSScriptRoot/.git" remote add $upstreamRemoteDefaultName $PowerShellRemoteUrl}
        $upstreamRemote = $upstreamRemoteDefaultName
    }
    elseif(!$upstreamRemote)
    {
        Write-Error "Please add a remote to PowerShell\PowerShell.  Example:  git remote add $upstreamRemoteDefaultName $PowerShellRemoteUrl" -ErrorAction Stop
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
    $environment = @{}
    # Use the .NET Core APIs to determine the current platform.
    # If a runtime exception is thrown, we are on Windows PowerShell, not PowerShell Core,
    # because System.Runtime.InteropServices.RuntimeInformation
    # and System.Runtime.InteropServices.OSPlatform do not exist in Windows PowerShell.
    try {
        $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
        $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

        $environment += @{'IsCoreCLR' = $true}
        $environment += @{'IsLinux' = $Runtime::IsOSPlatform($OSPlatform::Linux)}
        $environment += @{'IsMacOS' = $Runtime::IsOSPlatform($OSPlatform::OSX)}
        $environment += @{'IsWindows' = $Runtime::IsOSPlatform($OSPlatform::Windows)}
    } catch {
        $environment += @{'IsCoreCLR' = $false}
        $environment += @{'IsLinux' = $false}
        $environment += @{'IsMacOS' = $false}
        $environment += @{'IsWindows' = $true}
    }

    if ($Environment.IsWindows)
    {
        $environment += @{'IsAdmin' = (New-Object Security.Principal.WindowsPrincipal ([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)}
        # Can't use $env:HOME - not available on older systems (e.g. in AppVeyor)
        $environment += @{'nugetPackagesRoot' = "${env:HOMEDRIVE}${env:HOMEPATH}\.nuget\packages"}
    }
    else
    {
        $environment += @{'nugetPackagesRoot' = "${env:HOME}/.nuget/packages"}
    }

    if ($Environment.IsLinux) {
        $LinuxInfo = Get-Content /etc/os-release -Raw | ConvertFrom-StringData

        $environment += @{'LinuxInfo' = $LinuxInfo}
        $environment += @{'IsDebian' = $LinuxInfo.ID -match 'debian'}
        $environment += @{'IsDebian8' = $Environment.IsDebian -and $LinuxInfo.VERSION_ID -match '8'}
        $environment += @{'IsDebian9' = $Environment.IsDebian -and $LinuxInfo.VERSION_ID -match '9'}
        $environment += @{'IsUbuntu' = $LinuxInfo.ID -match 'ubuntu'}
        $environment += @{'IsUbuntu14' = $Environment.IsUbuntu -and $LinuxInfo.VERSION_ID -match '14.04'}
        $environment += @{'IsUbuntu16' = $Environment.IsUbuntu -and $LinuxInfo.VERSION_ID -match '16.04'}
        $environment += @{'IsUbuntu17' = $Environment.IsUbuntu -and $LinuxInfo.VERSION_ID -match '17.04'}
        $environment += @{'IsCentOS' = $LinuxInfo.ID -match 'centos' -and $LinuxInfo.VERSION_ID -match '7'}
        $environment += @{'IsFedora' = $LinuxInfo.ID -match 'fedora' -and $LinuxInfo.VERSION_ID -ge 24}
        $environment += @{'IsOpenSUSE' = $LinuxInfo.ID -match 'opensuse'}
        $environment += @{'IsOpenSUSE13' = $Environment.IsOpenSUSE -and $LinuxInfo.VERSION_ID  -match '13'}
        $environment += @{'IsOpenSUSE42.1' = $Environment.IsOpenSUSE -and $LinuxInfo.VERSION_ID  -match '42.1'}
        $environment += @{'IsRedHatFamily' = $Environment.IsCentOS -or $Environment.IsFedora -or $Environment.IsOpenSUSE}

        # Workaround for temporary LD_LIBRARY_PATH hack for Fedora 24
        # https://github.com/PowerShell/PowerShell/issues/2511
        if ($environment.IsFedora -and (Test-Path ENV:\LD_LIBRARY_PATH)) {
            Remove-Item -Force ENV:\LD_LIBRARY_PATH
            Get-ChildItem ENV:
        }
    }

    return [PSCustomObject] $environment
}

$Environment = Get-EnvironmentInformation

# Autoload (in current session) temporary modules used in our tests
$TestModulePath = Join-Path $PSScriptRoot "test/tools/Modules"
if ( $env:PSModulePath -notcontains $TestModulePath ) {
    $env:PSModulePath = $TestModulePath+$TestModulePathSeparator+$($env:PSModulePath)
}

function Test-Win10SDK {
    # The Windows 10 SDK is installed to "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64",
    # but the directory may exist even if the SDK has not been installed.
    #
    # A slightly more robust check is for the mc.exe binary within that directory.
    # It is only present if the SDK is installed.
    return (Test-Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\mc.exe")
}

function Start-BuildNativeWindowsBinaries {
    param(
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Release',

        [ValidateSet('x64', 'x86')]
        [string]$Arch = 'x64'
    )

    if (-not $Environment.IsWindows) {
        Write-Warning -Message "'Start-BuildNativeWindowsBinaries' is only supported on Windows platforms"
        return
    }

    # cmake is needed to build pwsh.exe
    if (-not (precheck 'cmake' $null)) {
        throw 'cmake not found. Run "Start-PSBootstrap -BuildWindowsNative". You can also install it from https://chocolatey.org/packages/cmake'
    }

    Use-MSBuild

    # mc.exe is Message Compiler for native resources
    if (-Not (Test-Win10SDK)) {
        throw 'Win 10 SDK not found. Run "Start-PSBootstrap -BuildWindowsNative" or install Microsoft Windows 10 SDK from https://developer.microsoft.com/en-US/windows/downloads/windows-10-sdk'
    }

    $vcPath = (Get-Item(Join-Path -Path "$env:VS140COMNTOOLS" -ChildPath '../../vc')).FullName
    $atlMfcIncludePath = Join-Path -Path $vcPath -ChildPath 'atlmfc/include'

    # atlbase.h is included in the pwrshplugin project
    if ((Test-Path -Path $atlMfcIncludePath\atlbase.h) -eq $false) {
        throw "Could not find Visual Studio include file atlbase.h at $atlMfcIncludePath. Please ensure the optional feature 'Microsoft Foundation Classes for C++' is installed."
    }

    # vcvarsall.bat is used to setup environment variables
    if ((Test-Path -Path $vcPath\vcvarsall.bat) -eq $false) {
        throw "Could not find Visual Studio vcvarsall.bat at $vcPath. Please ensure the optional feature 'Common Tools for Visual C++' is installed."
    }

    log "Start building native Windows binaries"

    try {
        Push-Location "$PSScriptRoot\src\powershell-native"

        # setup cmakeGenerator
        if ($Arch -eq 'x86') {
            $cmakeGenerator = 'Visual Studio 14 2015'
        } else {
            $cmakeGenerator = 'Visual Studio 14 2015 Win64'
        }

        # Compile native resources
        $currentLocation = Get-Location
        @("nativemsh/pwrshplugin") | ForEach-Object {
            $nativeResourcesFolder = $_
            Get-ChildItem $nativeResourcesFolder -Filter "*.mc" | ForEach-Object {
                $command = @"
cmd.exe /C cd /d "$currentLocation" "&" "$($vcPath)\vcvarsall.bat" "$Arch" "&" mc.exe -o -d -c -U "$($_.FullName)" -h "$nativeResourcesFolder" -r "$nativeResourcesFolder"
"@
                log "  Executing mc.exe Command: $command"
                Start-NativeExecution { Invoke-Expression -Command:$command 2>&1 }
            }
        }

        # Disabling until I figure out if it is necessary
        # $overrideFlags = "-DCMAKE_USER_MAKE_RULES_OVERRIDE=$PSScriptRoot\src\powershell-native\windows-compiler-override.txt"
        $overrideFlags = ""
        $location = Get-Location

        $command = @"
cmd.exe /C cd /d "$location" "&" "$($vcPath)\vcvarsall.bat" "$Arch" "&" cmake "$overrideFlags" -DBUILD_ONECORE=ON -DBUILD_TARGET_ARCH=$Arch -G "$cmakeGenerator" . "&" msbuild ALL_BUILD.vcxproj "/p:Configuration=$Configuration"
"@
        log "  Executing Build Command: $command"
        Start-NativeExecution { Invoke-Expression -Command:$command }

        # Copy the binaries from the local build directory to the packaging directory
        $FilesToCopy = @('pwrshplugin.dll', 'pwrshplugin.pdb')
        $dstPath = "$PSScriptRoot\src\powershell-win-core"
        $FilesToCopy | ForEach-Object {
            $srcPath = [IO.Path]::Combine((Get-Location), "bin", $Configuration, "CoreClr/$_")

            log "  Copying $srcPath to $dstPath"
            Copy-Item $srcPath $dstPath
        }
    } finally {
        Pop-Location
    }
}

function Start-BuildNativeUnixBinaries {
    param (
        [switch] $BuildLinuxArm
    )

    if (-not $Environment.IsLinux -and -not $Environment.IsMacOS) {
        Write-Warning -Message "'Start-BuildNativeUnixBinaries' is only supported on Linux/macOS platforms"
        return
    }

    if ($BuildLinuxArm -and -not $Environment.IsUbuntu) {
        throw "Cross compiling for linux-arm is only supported on Ubuntu environment"
    }

    # Verify we have all tools in place to do the build
    $precheck = $true
    foreach ($Dependency in 'cmake', 'make', 'g++') {
        $precheck = $precheck -and (precheck $Dependency "Build dependency '$Dependency' not found. Run 'Start-PSBootstrap'.")
    }

    if ($BuildLinuxArm) {
        foreach ($Dependency in 'arm-linux-gnueabihf-gcc', 'arm-linux-gnueabihf-g++') {
            $precheck = $precheck -and (precheck $Dependency "Build dependency '$Dependency' not found. Run 'Start-PSBootstrap'.")
        }
    }

    # Abort if any precheck failed
    if (-not $precheck) {
        return
    }

    # Build native components
    $Ext = if ($Environment.IsLinux) {
        "so"
    } elseif ($Environment.IsMacOS) {
        "dylib"
    }

    $Native = "$PSScriptRoot/src/libpsl-native"
    $Lib = "$PSScriptRoot/src/powershell-unix/libpsl-native.$Ext"
    log "Start building $Lib"

    git clean -qfdX $Native

    try {
        Push-Location $Native
        if ($BuildLinuxArm) {
            Start-NativeExecution { cmake -DCMAKE_TOOLCHAIN_FILE="./arm.toolchain.cmake" . }
            Start-NativeExecution { make -j }
        }
        else {
            Start-NativeExecution { cmake -DCMAKE_BUILD_TYPE=Debug . }
            Start-NativeExecution { make -j }
            Start-NativeExecution { ctest --verbose }
        }
    } finally {
        Pop-Location
    }

    if (-not (Test-Path $Lib)) {
        throw "Compilation of $Lib failed"
    }
}

function Start-PSBuild {
    [CmdletBinding()]
    param(
        # When specified this switch will stops running dev powershell
        # to help avoid compilation error, because file are in use.
        [switch]$StopDevPowerShell,

        [switch]$Restore,
        [string]$Output,
        [switch]$ResGen,
        [switch]$TypeGen,
        [switch]$Clean,
        [switch]$PSModuleRestore,

        # this switch will re-build only System.Management.Automation.dll
        # it's useful for development, to do a quick changes in the engine
        [switch]$SMAOnly,

        # These runtimes must match those in project.json
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("win7-x64",
                     "win7-x86",
                     "osx.10.12-x64",
                     "linux-x64",
                     "linux-arm")]
        [string]$Runtime,

        [ValidateSet('Linux', 'Debug', 'Release', 'CodeCoverage', '')] # We might need "Checked" as well
        [string]$Configuration,

        [switch]$CrossGen,

        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+\.\d+)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag
    )

    if ($Runtime -eq "linux-arm" -and -not $Environment.IsUbuntu) {
        throw "Cross compiling for linux-arm is only supported on Ubuntu environment"
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
        log "Cleaning your working directory. You can also do it with 'git clean -fdX'"
        Push-Location $PSScriptRoot
        try {
            git clean -fdX
            # Extra cleaning is required to delete the CMake temporary files.
            # These are not cleaned when using "X" and cause CMake to retain state, leading to
            # mis-configured environment issues when switching between x86 and x64 compilation
            # environments.
            git clean -fdx .\src\powershell-native
        } finally {
            Pop-Location
        }
    }

    # create the telemetry flag file
    $null = new-item -force -type file "$psscriptroot/DELETE_ME_TO_DISABLE_CONSOLEHOST_TELEMETRY"

    # Add .NET CLI tools to PATH
    Find-Dotnet

    # Verify we have .NET SDK in place to do the build, and abort if the precheck failed
    $precheck = precheck 'dotnet' "Build dependency 'dotnet' not found in PATH. Run Start-PSBootstrap. Also see: https://dotnet.github.io/getting-started/"
    if (-not $precheck) {
        return
    }

    # Verify if the dotnet in-use is the required version
    $dotnetCLIInstalledVersion = (dotnet --version)
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
    }
    $script:Options = New-PSOptions @OptionsArguments

    if ($StopDevPowerShell) {
        Stop-DevPowerShell
    }

    # setup arguments
    $Arguments = @("publish")
    if ($Output) {
        $Arguments += "--output", $Output
    }
    elseif ($SMAOnly) {
        $Arguments += "--output", (Split-Path $script:Options.Output)
    }

    $Arguments += "--configuration", $Options.Configuration
    $Arguments += "--framework", $Options.Framework

    if (-not $SMAOnly) {
        # libraries should not have runtime
        $Arguments += "--runtime", $Options.Runtime
    }

    if ($ReleaseTag) {
        $ReleaseTagToUse = $ReleaseTag -Replace '^v'
        $Arguments += "/property:ReleaseTag=$ReleaseTagToUse"
    }

    # handle Restore
    if ($Restore -or -not (Test-Path "$($Options.Top)/obj/project.assets.json")) {
        log "Run dotnet restore"

        $srcProjectDirs = @($Options.Top, "$PSScriptRoot/src/TypeCatalogGen", "$PSScriptRoot/src/ResGen")
        $testProjectDirs = Get-ChildItem "$PSScriptRoot/test/*.csproj" -Recurse | ForEach-Object { [System.IO.Path]::GetDirectoryName($_) }

        $RestoreArguments = @("--verbosity")
        if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
            $RestoreArguments += "detailed"
        } else {
            $RestoreArguments += "quiet"
        }

        ($srcProjectDirs + $testProjectDirs) | ForEach-Object { Start-NativeExecution { dotnet restore $_ $RestoreArguments } }
    }

    # handle ResGen
    # Heuristic to run ResGen on the fresh machine
    if ($ResGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.ConsoleHost/gen")) {
        log "Run ResGen (generating C# bindings for resx files)"
        Start-ResGen
    }

    # handle TypeGen
    if ($TypeGen -or -not (Test-Path "$PSScriptRoot/src/System.Management.Automation/CoreCLR/CorePsTypeCatalog.cs")) {
        log "Run TypeGen (generating CorePsTypeCatalog.cs)"
        Start-TypeGen
    }

    # Get the folder path where pwsh.exe is located.
    $publishPath = Split-Path $Options.Output -Parent
    try {
        # Relative paths do not work well if cwd is not changed to project
        Push-Location $Options.Top
        log "Run dotnet $Arguments from $pwd"
        Start-NativeExecution { dotnet $Arguments }

        if ($CrossGen) {
            Start-CrossGen -PublishPath $publishPath -Runtime $script:Options.Runtime
            log "pwsh.exe with ngen binaries is available at: $($Options.Output)"
        } else {
            log "PowerShell output: $($Options.Output)"
        }
    } finally {
        Pop-Location
    }

    # publish netcoreapp2.0 reference assemblies
    try {
        Push-Location "$PSScriptRoot/src/TypeCatalogGen"
        $refAssemblies = Get-Content -Path "powershell.inc" | Where-Object { $_ -like "*microsoft.netcore.app*" } | ForEach-Object { $_.TrimEnd(';') }
        $refDestFolder = Join-Path -Path $publishPath -ChildPath "ref"

        if (Test-Path $refDestFolder -PathType Container) {
            Remove-Item $refDestFolder -Force -Recurse -ErrorAction Stop
        }
        New-Item -Path $refDestFolder -ItemType Directory -Force -ErrorAction Stop > $null
        Copy-Item -Path $refAssemblies -Destination $refDestFolder -Force -ErrorAction Stop
    } finally {
        Pop-Location
    }

    if ($Environment.IsRedHatFamily) {
        # add two symbolic links to system shared libraries that libmi.so is dependent on to handle
        # platform specific changes. This is the only set of platforms needed for this currently
        # as Ubuntu has these specific library files in the platform and macOS builds for itself
        # against the correct versions.
        if ( ! (test-path "$publishPath/libssl.so.1.0.0")) {
            $null = New-Item -Force -ItemType SymbolicLink -Target "/lib64/libssl.so.10" -Path "$publishPath/libssl.so.1.0.0" -ErrorAction Stop
        }
        if ( ! (test-path "$publishPath/libcrypto.so.1.0.0")) {
            $null = New-Item -Force -ItemType SymbolicLink -Target "/lib64/libcrypto.so.10" -Path "$publishPath/libcrypto.so.1.0.0" -ErrorAction Stop
        }
    }

    # download modules from powershell gallery.
    #   - PowerShellGet, PackageManagement, Microsoft.PowerShell.Archive
    if($PSModuleRestore)
    {
        $ProgressPreference = "SilentlyContinue"
        log "Restore PowerShell modules to $publishPath"

        $modulesDir = Join-Path -Path $publishPath -ChildPath "Modules"

        # Restore modules from myget feed
        Restore-PSModule -Destination $modulesDir -Name @(
            # PowerShellGet depends on PackageManagement module, so PackageManagement module will be installed with the PowerShellGet module.
            'PowerShellGet'
        )

        # Restore modules from powershellgallery feed
        Restore-PSModule -Destination $modulesDir -Name @(
            'Microsoft.PowerShell.Archive'
        ) -SourceLocation "https://www.powershellgallery.com/api/v2/"
    }
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
        [ValidateSet("Linux", "Debug", "Release", "CodeCoverage", "")]
        [string]$Configuration,

        [ValidateSet("netcoreapp2.0")]
        [string]$Framework,

        # These are duplicated from Start-PSBuild
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("",
                     "win7-x86",
                     "win7-x64",
                     "osx.10.12-x64",
                     "linux-x64",
                     "linux-arm")]
        [string]$Runtime,

        [switch]$CrossGen,

        [string]$Output,

        [switch]$SMAOnly,
        
        [switch]$PSModuleRestore
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    $ConfigWarningMsg = "The passed-in Configuration value '{0}' is not supported on '{1}'. Use '{2}' instead."
    if (-not $Configuration) {
        $Configuration = if ($Environment.IsLinux -or $Environment.IsMacOS) {
            "Linux"
        } elseif ($Environment.IsWindows) {
            "Debug"
        }
    } else {
        switch ($Configuration) {
            "Linux" {
                if ($Environment.IsWindows) {
                    $Configuration = "Debug"
                    Write-Warning ($ConfigWarningMsg -f $switch.Current, "Windows", $Configuration)
                }
            }
            "CodeCoverage" {
                if(-not $Environment.IsWindows) {
                    $Configuration = "Linux"
                    Write-Warning ($ConfigWarningMsg -f $switch.Current, $Environment.LinuxInfo.PRETTY_NAME, $Configuration)
                }
            }
            Default {
                if ($Environment.IsLinux -or $Environment.IsMacOS) {
                    $Configuration = "Linux"
                    Write-Warning ($ConfigWarningMsg -f $switch.Current, $Environment.LinuxInfo.PRETTY_NAME, $Configuration)
                }
            }
        }
    }
    Write-Verbose "Using configuration '$Configuration'"

    $PowerShellDir = if ($Configuration -eq 'Linux') {
        "powershell-unix"
    } else {
        "powershell-win-core"
    }
    $Top = [IO.Path]::Combine($PSScriptRoot, "src", $PowerShellDir)
    Write-Verbose "Top project directory is $Top"

    if (-not $Framework) {
        $Framework = "netcoreapp2.0"
        Write-Verbose "Using framework '$Framework'"
    }

    if (-not $Runtime) {
        if ($Environment.IsLinux) {
            $Runtime = "linux-x64"
        } else {
            $RID = dotnet --info | ForEach-Object {
                if ($_ -match "RID") {
                    $_ -split "\s+" | Select-Object -Last 1
                }
            }

            if ($Environment.IsWindows) {
                # We plan to release packages targetting win7-x64 and win7-x86 RIDs,
                # which supports all supported windows platforms.
                # So we, will change the RID to win7-<arch>
                $Runtime = $RID -replace "win\d+", "win7"
            } else {
                $Runtime = $RID
            }
        }

        if (-not $Runtime) {
            Throw "Could not determine Runtime Identifier, please update dotnet"
        } else {
            Write-Verbose "Using runtime '$Runtime'"
        }
    }

    $Executable = if ($Environment.IsLinux -or $Environment.IsMacOS) {
        "pwsh"
    } elseif ($Environment.IsWindows) {
        "pwsh.exe"
    }

    # Build the Output path
    if (!$Output) {
        $Output = [IO.Path]::Combine($Top, "bin", $Configuration, $Framework, $Runtime, "publish", $Executable)
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

    return @{ RootInfo = [PSCustomObject]$RootInfo
              Top = $Top;
              Configuration = $Configuration;
              Framework = $Framework;
              Runtime = $Runtime;
              Output = $Output;
              CrossGen = $CrossGen
              PSModuleRestore = $PSModuleRestore }
}

# Get the Options of the last build
function Get-PSOptions {
    return $script:Options
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

    get-childitem -Recurse $testbase -File | Where-Object {$_.name -match "tests.ps1"}| ForEach-Object {
        $fullname = $_.fullname
        $tok = $err = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($FullName, [ref]$tok,[ref]$err)
        $des = $ast.FindAll({$args[0] -is "System.Management.Automation.Language.CommandAst" -and $args[0].CommandElements[0].Value -eq "Describe"},$true)
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
                        if (@('REQUIREADMINONWINDOWS', 'SLOW') -contains $_) {
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
    param()

    Find-Dotnet

    $tools = @(
        @{Path="${PSScriptRoot}/test/tools/TestExe";Output="testexe"}
        @{Path="${PSScriptRoot}/test/tools/WebListener";Output="WebListener"}
    )
    if ($null -eq $Options)
    {
        $Options = New-PSOptions
    }

    # Publish tools so it can be run by tests
    foreach ($tool in $tools)
    {
        Push-Location $tool.Path
        try {
            dotnet publish --output bin --configuration $Options.Configuration --framework $Options.Framework --runtime $Options.Runtime
            $toolPath = Join-Path -Path $tool.Path -ChildPath "bin"

            if ( $env:PATH -notcontains $toolPath ) {
                $env:PATH = $toolPath+$TestModulePathSeparator+$($env:PATH)
            }
        } finally {
            Pop-Location
        }
    }
}

function Start-PSPester {
    [CmdletBinding(DefaultParameterSetName='default')]
    param(
        [string]$OutputFormat = "NUnitXml",
        [string]$OutputFile = "pester-tests.xml",
        [string[]]$ExcludeTag = 'Slow',
        [string[]]$Tag = @("CI","Feature"),
        [string[]]$Path = @("$PSScriptRoot/test/common","$PSScriptRoot/test/powershell"),
        [switch]$ThrowOnFailure,
        [string]$binDir = (Split-Path (New-PSOptions).Output),
        [string]$powershell = (Join-Path $binDir 'pwsh'),
        [string]$Pester = ([IO.Path]::Combine($binDir, "Modules", "Pester")),
        [Parameter(ParameterSetName='Unelevate',Mandatory=$true)]
        [switch]$Unelevate,
        [switch]$Quiet,
        [switch]$Terse,
        [Parameter(ParameterSetName='PassThru',Mandatory=$true)]
        [switch]$PassThru,
        [switch]$IncludeFailingTest
    )

    if (-not (Get-Module -ListAvailable -Name $Pester -ErrorAction SilentlyContinue))
    {
        Write-Warning @"
Pester module not found.
Make sure that the proper git submodules are installed by running:
    git submodule update --init
"@
        return;
    }

    if ($IncludeFailingTest.IsPresent)
    {
        $Path += "$PSScriptRoot/tools/failingTests"
    }

    # we need to do few checks and if user didn't provide $ExcludeTag explicitly, we should alternate the default
    if ($Unelevate)
    {
        if (-not $Environment.IsWindows)
        {
            throw '-Unelevate is currently not supported on non-Windows platforms'
        }

        if (-not $Environment.IsAdmin)
        {
            throw '-Unelevate cannot be applied because the current user is not Administrator'
        }

        if (-not $PSBoundParameters.ContainsKey('ExcludeTag'))
        {
            $ExcludeTag += 'RequireAdminOnWindows'
        }
    }
    elseif ($Environment.IsWindows -and (-not $Environment.IsAdmin))
    {
        if (-not $PSBoundParameters.ContainsKey('ExcludeTag'))
        {
            $ExcludeTag += 'RequireAdminOnWindows'
        }
    }

    Write-Verbose "Running pester tests at '$path' with tag '$($Tag -join ''', ''')' and ExcludeTag '$($ExcludeTag -join ''', ''')'" -Verbose
    Publish-PSTestTools | ForEach-Object {Write-Host $_}

    # All concatenated commands/arguments are suffixed with the delimiter (space)
    $Command = ""
    if ($Terse)
    {
        $Command += "`$ProgressPreference = 'silentlyContinue'; "
    }

    # Autoload (in subprocess) temporary modules used in our tests
    $Command += '$env:PSModulePath = '+"'$TestModulePath$TestModulePathSeparator'" + '+$($env:PSModulePath);'

    # Windows needs the execution policy adjusted
    if ($Environment.IsWindows) {
        $Command += "Set-ExecutionPolicy -Scope Process Unrestricted; "
    }

    $Command += "Import-Module '$Pester'; "

    if ($Unelevate)
    {
        $outputBufferFilePath = [System.IO.Path]::GetTempFileName()
    }

    $Command += "Invoke-Pester "

    $Command += "-OutputFormat ${OutputFormat} -OutputFile ${OutputFile} "
    if ($ExcludeTag -and ($ExcludeTag -ne "")) {
        $Command += "-ExcludeTag @('" + (${ExcludeTag} -join "','") + "') "
    }
    if ($Tag) {
        $Command += "-Tag @('" + (${Tag} -join "','") + "') "
    }
    # sometimes we need to eliminate Pester output, especially when we're
    # doing a daily build as the log file is too large
    if ( $Quiet ) {
        $Command += "-Quiet "
    }
    if ( $PassThru ) {
        $Command += "-PassThru "
    }

    $Command += "'" + ($Path -join "','") + "'"
    if ($Unelevate)
    {
        $Command += " *> $outputBufferFilePath; '__UNELEVATED_TESTS_THE_END__' >> $outputBufferFilePath"
    }

    Write-Verbose $Command

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

    # To ensure proper testing, the module path must not be inherited by the spawned process
    try {
        $originalModulePath = $env:PSModulePath
        if ($Unelevate)
        {
            Start-UnelevatedProcess -process $powershell -arguments @('-noprofile', '-c', $Command)
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

                $count = ($lines | measure-object).Count
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
                    $Command += "|Export-Clixml -Path '$passThruFile' -Force"
                    if ($Terse)
                    {
                        Start-NativeExecution -sb {& $powershell -noprofile -c $Command} | ForEach-Object { Write-Terse $_}
                    }
                    else
                    {
                        Start-NativeExecution -sb {& $powershell -noprofile -c $Command} | ForEach-Object { Write-Host $_}
                    }
                    Import-Clixml -Path $passThruFile | Where-Object {$_.TotalCount -is [Int32]}
                }
                finally
                {
                    Remove-Item $passThruFile -ErrorAction SilentlyContinue
                }
            }
            else
            {
                if ($Terse)
                {
                    Start-NativeExecution -sb {& $powershell -noprofile -c $Command} | ForEach-Object { Write-Terse -line $_ }
                }
                else
                {
                    Start-NativeExecution -sb {& $powershell -noprofile -c $Command}
                }
            }
        }
    } finally {
        $env:PSModulePath = $originalModulePath
        if ($Unelevate)
        {
            Remove-Item $outputBufferFilePath
        }
    }

    if($ThrowOnFailure)
    {
        Test-PSPesterResults -TestResultsFile $OutputFile
    }
}

function script:Start-UnelevatedProcess
{
    param(
        [string]$process,
        [string[]]$arguments
    )
    if (-not $Environment.IsWindows)
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

    if ($PSCmdLet.ParameterSetName -eq 'xml')
    {
        $description = $testFailure.description
        $name = $testFailure.name
        $message = $testFailure.failure.message
        $stackTrace = $testFailure.failure."stack-trace"
    }
    elseif ($PSCmdLet.ParameterSetName -eq 'object')
    {
        $description = $testFailureObject.Describe + '/' + $testFailureObject.Context
        $name = $testFailureObject.Name
        $message = $testFailureObject.FailureMessage
        $stackTrace = $testFailureObject.StackTrace
    }
    else
    {
        throw 'Unknown Show-PSPester parameter set'
    }

    logerror ("Description: " + $description)
    logerror ("Name:        " + $name)
    logerror "message:"
    logerror $message
    logerror "stack-trace:"
    logerror $stackTrace

}

#
# Read the test result file and
# Throw if a test failed
function Test-PSPesterResults
{
    [CmdletBinding(DefaultParameterSetName='file')]
    param(
        [Parameter(ParameterSetName='file')]
        [string]$TestResultsFile = "pester-tests.xml",
        [Parameter(ParameterSetName='file')]
        [string]$TestArea = 'test/powershell',
        [Parameter(ParameterSetName='PesterPassThruObject',Mandatory)]
        [pscustomobject] $ResultObject
        )

    if($PSCmdLet.ParameterSetName -eq 'file')
    {
        if(!(Test-Path $TestResultsFile))
        {
            throw "Test result file '$testResultsFile' not found for $TestArea."
        }

        $x = [xml](Get-Content -raw $testResultsFile)
        if ([int]$x.'test-results'.failures -gt 0)
        {
            logerror "TEST FAILURES"
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
    elseif ($PSCmdLet.ParameterSetName -eq 'PesterPassThruObject')
    {
        if ($ResultObject.TotalCount -le 0)
        {
            throw 'NO TESTS RUN'
        }
        elseif ($ResultObject.FailedCount -gt 0)
        {
            logerror 'TEST FAILURES'

            $ResultObject.TestResult | Where-Object {$_.Passed -eq $false} | ForEach-Object {
                Show-PSPesterError -testFailureObject $_
            }

            throw "$($ResultObject.FailedCount) tests in $TestArea failed"
        }
    }
}


function Start-PSxUnit {
    [CmdletBinding()]param()

    log "xUnit tests are currently disabled pending fixes due to API and AssemblyLoadContext changes - @andschwa"
    return

    if ($Environment.IsWindows) {
        throw "xUnit tests are only currently supported on Linux / OS X"
    }

    if ($Environment.IsMacOS) {
        log "Not yet supported on OS X, pretending they passed..."
        return
    }

    # Add .NET CLI tools to PATH
    Find-Dotnet

    $Arguments = "--configuration", "Linux", "-parallel", "none"
    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        $Arguments += "-verbose"
    }

    $Content = Split-Path -Parent (Get-PSOutput)
    if (-not (Test-Path $Content)) {
        throw "PowerShell must be built before running tests!"
    }

    try {
        Push-Location $PSScriptRoot/test/csharp
        # Path manipulation to obtain test project output directory
        $Output = Join-Path $pwd ((Split-Path -Parent (Get-PSOutput)) -replace (New-PSOptions).Top)
        Write-Verbose "Output is $Output"

        Copy-Item -ErrorAction SilentlyContinue -Recurse -Path $Content/* -Include Modules,libpsl-native* -Destination $Output
        Start-NativeExecution { dotnet test $Arguments }

        if ($LASTEXITCODE -ne 0) {
            throw "$LASTEXITCODE xUnit tests failed"
        }
    } finally {
        Pop-Location
    }
}


function Install-Dotnet {
    [CmdletBinding()]
    param(
        [string]$Channel = $dotnetCLIChannel,
        [string]$Version = $dotnetCLIRequiredVersion,
        [switch]$NoSudo
    )

    # This allows sudo install to be optional; needed when running in containers / as root
    # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
    $sudo = if (!$NoSudo) { "sudo" }

    $obtainUrl = "https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain"

    # Install for Linux and OS X
    if ($Environment.IsLinux -or $Environment.IsMacOS) {
        # Uninstall all previous dotnet packages
        $uninstallScript = if ($Environment.IsUbuntu) {
            "dotnet-uninstall-debian-packages.sh"
        } elseif ($Environment.IsMacOS) {
            "dotnet-uninstall-pkgs.sh"
        }

        if ($uninstallScript) {
            Start-NativeExecution {
                curl -sO $obtainUrl/uninstall/$uninstallScript
                Invoke-Expression "$sudo bash ./$uninstallScript"
            }
        } else {
            Write-Warning "This script only removes prior versions of dotnet for Ubuntu 14.04 and OS X"
        }

        # Install new dotnet 1.1.0 preview packages
        $installScript = "dotnet-install.sh"
        Start-NativeExecution {
            curl -sO $obtainUrl/$installScript
            bash ./$installScript -c $Channel -v $Version
        }
    } elseif ($Environment.IsWindows) {
        Remove-Item -ErrorAction SilentlyContinue -Recurse -Force ~\AppData\Local\Microsoft\dotnet
        $installScript = "dotnet-install.ps1"
        Invoke-WebRequest -Uri $obtainUrl/$installScript -OutFile $installScript

        if (-not $Environment.IsCoreCLR) {
            & ./$installScript -Channel $Channel -Version $Version
        } else {
            # dotnet-install.ps1 uses APIs that are not supported in .NET Core, so we run it with Windows PowerShell
            $fullPSPath = Join-Path -Path $env:windir -ChildPath "System32\WindowsPowerShell\v1.0\powershell.exe"
            $fullDotnetInstallPath = Join-Path -Path $pwd.Path -ChildPath $installScript
            Start-NativeExecution { & $fullPSPath -NoLogo -NoProfile -File $fullDotnetInstallPath -Channel $Channel -Version $Version }
        }
    }
}

function Get-RedHatPackageManager {
    if ($Environment.IsCentOS) {
        "yum install -y -q"
    } elseif ($Environment.IsFedora) {
        "dnf install -y -q"
    } elseif ($Environment.IsOpenSUSE) {
        "zypper --non-interactive install"
    } else {
        throw "Error determining package manager for this distribution."
    }
}

function Start-PSBootstrap {
    [CmdletBinding(
        SupportsShouldProcess=$true,
        ConfirmImpact="High")]
    param(
        [string]$Channel = $dotnetCLIChannel,
        # we currently pin dotnet-cli version, and will
        # update it when more stable version comes out.
        [string]$Version = $dotnetCLIRequiredVersion,
        [switch]$Package,
        [switch]$NoSudo,
        [switch]$BuildWindowsNative,
        [switch]$BuildLinuxArm,
        [switch]$Force
    )

    log "Installing PowerShell build dependencies"

    Push-Location $PSScriptRoot/tools

    try {
        if ($Environment.IsLinux -or $Environment.IsMacOS) {
            # This allows sudo install to be optional; needed when running in containers / as root
            # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
            $sudo = if (!$NoSudo) { "sudo" }

            try {
                # Update googletest submodule for linux native cmake
                Push-Location $PSScriptRoot
                $Submodule = "$PSScriptRoot/src/libpsl-native/test/googletest"
                Remove-Item -Path $Submodule -Recurse -Force -ErrorAction SilentlyContinue
                git submodule --quiet update --init -- $submodule
            } finally {
                Pop-Location
            }

            if ($BuildLinuxArm -and -not $Environment.IsUbuntu) {
                Write-Error "Cross compiling for linux-arm is only supported on Ubuntu environment"
                return
            }

            # Install ours and .NET's dependencies
            $Deps = @()
            if ($Environment.IsUbuntu) {
                # Build tools
                $Deps += "curl", "g++", "cmake", "make"

                if ($BuildLinuxArm) {
                    $Deps += "gcc-arm-linux-gnueabihf", "g++-arm-linux-gnueabihf"
                }

                # .NET Core required runtime libraries
                $Deps += "libunwind8"
                if ($Environment.IsUbuntu14) { $Deps += "libicu52" }
                elseif ($Environment.IsUbuntu16) { $Deps += "libicu55" }

                # Packaging tools
                if ($Package) { $Deps += "ruby-dev", "groff" }

                # Install dependencies
                Start-NativeExecution {
                    Invoke-Expression "$sudo apt-get update -qq"
                    Invoke-Expression "$sudo apt-get install -y -qq $Deps"
                }
            } elseif ($Environment.IsRedHatFamily) {
                # Build tools
                $Deps += "which", "curl", "gcc-c++", "cmake", "make"

                # .NET Core required runtime libraries
                $Deps += "libicu", "libunwind"

                # Packaging tools
                if ($Package) { $Deps += "ruby-devel", "rpm-build", "groff" }

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
            } elseif ($Environment.IsMacOS) {
                precheck 'brew' "Bootstrap dependency 'brew' not found, must install Homebrew! See http://brew.sh/"

                # Build tools
                $Deps += "cmake"

                # .NET Core required runtime libraries
                $Deps += "openssl"

                # Install dependencies
                # ignore exitcode, because they may be already installed
                Start-NativeExecution { brew install $Deps } -IgnoreExitcode

                # Install patched version of curl
                Start-NativeExecution { brew install curl --with-openssl } -IgnoreExitcode
            }

            # Install [fpm](https://github.com/jordansissel/fpm) and [ronn](https://github.com/rtomayko/ronn)
            if ($Package) {
                try {
                    # We cannot guess if the user wants to run gem install as root
                    Start-NativeExecution { gem install fpm -v 1.8.1 }
                    Start-NativeExecution { gem install ronn }
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
            $dotNetVersion = (dotnet --version)
        }

        if(!$dotNetExists -or $dotNetVersion -ne $dotnetCLIRequiredVersion -or $Force.IsPresent) {
            if($Force.IsPresent) {
                log "Installing dotnet due to -Force."
            }
            elseif(!$dotNetExistis) {
                log "dotnet not present.  Installing dotnet."
            }
            else {
                log "dotnet out of date ($dotNetVersion).  Updating dotnet."
            }

            $DotnetArguments = @{ Channel=$Channel; Version=$Version; NoSudo=$NoSudo }
            Install-Dotnet @DotnetArguments
        }
        else {
            log "dotnet is already installed.  Skipping installation."
        }

        # Install Windows dependencies if `-Package` or `-BuildWindowsNative` is specified
        if ($Environment.IsWindows) {
            ## need RCEdit to modify the binaries embedded resources
            if (-not (Test-Path "~/.rcedit/rcedit-x64.exe"))
            {
                log "Install RCEdit for modifying exe resources"
                $rceditUrl = "https://github.com/electron/rcedit/releases/download/v1.0.0/rcedit-x64.exe"
                New-Item -Path "~/.rcedit" -Type Directory -Force > $null
                Invoke-WebRequest -OutFile "~/.rcedit/rcedit-x64.exe" -Uri $rceditUrl
            }

            if ($BuildWindowsNative) {
                log "Install Windows dependencies for building PSRP plugin"

                $machinePath = [Environment]::GetEnvironmentVariable('Path', 'MACHINE')
                $newMachineEnvironmentPath = $machinePath

                $cmakePresent = precheck 'cmake' $null
                $sdkPresent = Test-Win10SDK

                # Install chocolatey
                $chocolateyPath = "$env:AllUsersProfile\chocolatey\bin"

                if(precheck 'choco' $null) {
                    log "Chocolatey is already installed. Skipping installation."
                }
                elseif(($cmakePresent -eq $false) -or ($sdkPresent -eq $false)) {
                    log "Chocolatey not present. Installing chocolatey."
                    if ($Force -or $PSCmdlet.ShouldProcess("Install chocolatey via https://chocolatey.org/install.ps1")) {
                        Invoke-Expression ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))
                        if (-not ($machinePath.ToLower().Contains($chocolateyPath.ToLower()))) {
                            log "Adding $chocolateyPath to Path environment variable"
                            $env:Path += ";$chocolateyPath"
                            $newMachineEnvironmentPath += ";$chocolateyPath"
                        } else {
                            log "$chocolateyPath already present in Path environment variable"
                        }
                    } else {
                        Write-Error "Chocolatey is required to install missing dependencies. Please install it from https://chocolatey.org/ manually. Alternatively, install cmake and Windows 10 SDK."
                        return
                    }
                } else {
                    log "Skipping installation of chocolatey, cause both cmake and Win 10 SDK are present."
                }

                # Install cmake
                $cmakePath = "${env:ProgramFiles}\CMake\bin"
                if($cmakePresent) {
                    log "Cmake is already installed. Skipping installation."
                } else {
                    log "Cmake not present. Installing cmake."
                    Start-NativeExecution { choco install cmake -y --version 3.6.0 }
                    if (-not ($machinePath.ToLower().Contains($cmakePath.ToLower()))) {
                        log "Adding $cmakePath to Path environment variable"
                        $env:Path += ";$cmakePath"
                        $newMachineEnvironmentPath = "$cmakePath;$newMachineEnvironmentPath"
                    } else {
                        log "$cmakePath already present in Path environment variable"
                    }
                }

                # Install Windows 10 SDK
                $packageName = "windows-sdk-10.0"

                if (-not $sdkPresent) {
                    log "Windows 10 SDK not present. Installing $packageName."
                    Start-NativeExecution { choco install windows-sdk-10.0 -y }
                } else {
                    log "Windows 10 SDK present. Skipping installation."
                }

                # Update path machine environment variable
                if ($newMachineEnvironmentPath -ne $machinePath) {
                    log "Updating Path machine environment variable"
                    if ($Force -or $PSCmdlet.ShouldProcess("Update Path machine environment variable to $newMachineEnvironmentPath")) {
                        [Environment]::SetEnvironmentVariable('Path', $newMachineEnvironmentPath, 'MACHINE')
                    }
                }
            }
        }
    } finally {
        Pop-Location
    }
}

function Publish-NuGetFeed
{
    param(
        [string]$OutputPath = "$PSScriptRoot/nuget-artifacts",
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+\.\d+)?$")]
        [ValidateNotNullOrEmpty()]
        [string]$ReleaseTag
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    ## We update 'project.assets.json' files with new version tag value by 'GetPSCoreVersionFromGit' target.
    $TopProject = (New-PSOptions).Top
    if ($ReleaseTag) {
        $ReleaseTagToUse = $ReleaseTag -Replace '^v'
        dotnet restore $TopProject "/property:ReleaseTag=$ReleaseTagToUse"
    } else {
        dotnet restore $TopProject
    }

    try {
        Push-Location $PSScriptRoot
        @(
'Microsoft.PowerShell.Commands.Management',
'Microsoft.PowerShell.Commands.Utility',
'Microsoft.PowerShell.Commands.Diagnostics',
'Microsoft.PowerShell.ConsoleHost',
'Microsoft.PowerShell.Security',
'System.Management.Automation',
'Microsoft.PowerShell.CoreCLR.Eventing',
'Microsoft.WSMan.Management',
'Microsoft.WSMan.Runtime',
'Microsoft.PowerShell.SDK'
        ) | ForEach-Object {
            if ($ReleaseTag) {
                dotnet pack "src/$_" --output $OutputPath "/property:IncludeSymbols=true;ReleaseTag=$ReleaseTagToUse"
            } else {
                dotnet pack "src/$_" --output $OutputPath
            }
        }
    } finally {
        Pop-Location
    }
}

function Start-DevPowerShell {
    param(
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = (Split-Path (New-PSOptions).Output),
        [switch]$NoNewWindow,
        [string]$Command,
        [switch]$KeepPSModulePath
    )

    try {
        if ((-not $NoNewWindow) -and ($Environment.IsCoreCLR)) {
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

        $env:DEVPATH = $binDir

        # splatting for the win
        $startProcessArgs = @{
            FilePath = "$binDir\pwsh"
            ArgumentList = "$ArgumentList"
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

        if ($ZapDisable) {
            Remove-Item env:COMPLUS_ZapDisable
        }
    }
}

function Start-TypeGen
{
    [CmdletBinding()]
    param()

    # Add .NET CLI tools to PATH
    Find-Dotnet

    $GetDependenciesTargetPath = "$PSScriptRoot/src/Microsoft.PowerShell.SDK/obj/Microsoft.PowerShell.SDK.csproj.TypeCatalog.targets"
    $GetDependenciesTargetValue = @'
<Project>
    <Target Name="_GetDependencies"
            DependsOnTargets="ResolveAssemblyReferencesDesignTime">
        <ItemGroup>
            <_RefAssemblyPath Include="%(_ReferencesFromRAR.ResolvedPath)%3B" Condition=" '%(_ReferencesFromRAR.Type)' == 'assembly' And '%(_ReferencesFromRAR.PackageName)' != 'Microsoft.Management.Infrastructure' " />
        </ItemGroup>
        <WriteLinesToFile File="$(_DependencyFile)" Lines="@(_RefAssemblyPath)" Overwrite="true" />
    </Target>
</Project>
'@
    Set-Content -Path $GetDependenciesTargetPath -Value $GetDependenciesTargetValue -Force -Encoding Ascii

    Push-Location "$PSScriptRoot/src/Microsoft.PowerShell.SDK"
    try {
        $ps_inc_file = "$PSScriptRoot/src/TypeCatalogGen/powershell.inc"
        dotnet msbuild .\Microsoft.PowerShell.SDK.csproj /t:_GetDependencies "/property:DesignTimeBuild=true;_DependencyFile=$ps_inc_file" /nologo
    } finally {
        Pop-Location
    }

    Push-Location "$PSScriptRoot/src/TypeCatalogGen"
    try {
        dotnet run ../System.Management.Automation/CoreCLR/CorePsTypeCatalog.cs powershell.inc
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
    $dotnetPath = if ($Environment.IsWindows) {
        "$env:LocalAppData\Microsoft\dotnet"
    } else {
        "$env:HOME/.dotnet"
    }

    if (-not (precheck 'dotnet' "Could not find 'dotnet', appending $dotnetPath to PATH.")) {
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

    $msbuild = get-command msbuild -ErrorAction SilentlyContinue
    if ($msbuild) {
        # all good, nothing to do
        return
    }

    if (-not (Test-Path $frameworkMsBuildLocation)) {
        throw "msbuild not found in '$frameworkMsBuildLocation'. Install Visual Studio 2015."
    }

    Set-Alias msbuild $frameworkMsBuildLocation -Scope Script
}


function script:log([string]$message) {
    Write-Host -Foreground Green $message
    #reset colors for older package to at return to default after error message on a compilation error
    [console]::ResetColor()
}

function script:logerror([string]$message) {
    Write-Host -Foreground Red $message
    #reset colors for older package to at return to default after error message on a compilation error
    [console]::ResetColor()
}

function script:precheck([string]$command, [string]$missedMessage) {
    $c = Get-Command $command -ErrorAction SilentlyContinue
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
function script:Start-NativeExecution([scriptblock]$sb, [switch]$IgnoreExitcode)
{
    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Continue"
    try {
        & $sb
        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode) {
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    } finally {
        $script:ErrorActionPreference = $backupEAP
    }
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
        $packageVersion = $packageVersion + '.' + $packageBuildTokens
    }

    $packageVersion
}

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

        # Product Guid needs to change for every version to support SxS install
        [ValidateNotNullOrEmpty()]
        [string] $ProductGuid = 'a5249933-73a1-4b10-8a4c-13c98bdc16fe',

        # Source Path to the Product Files - required to package the contents into an MSI
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductSourcePath,

        # File describing the MSI Package creation semantics
        [ValidateNotNullOrEmpty()]
        [string] $ProductWxsPath = "$PSScriptRoot\assets\Product.wxs",

        # Path to Assets folder containing artifacts such as icons, images
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $AssetsPath,

        # Path to license.rtf file - for the EULA
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $LicenseFilePath,

        # Architecture to use when creating the MSI
        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64")]
        [ValidateNotNullOrEmpty()]
        [string] $ProductTargetArchitecture,

        # Force overwrite of package
        [Switch] $Force
    )

    ## need RCEdit to modify the binaries embedded resources
    if (-not (Test-Path "~/.rcedit/rcedit-x64.exe"))
    {
        throw "RCEdit is required to modify pwsh.exe resources, please run 'Start-PSBootStrap' to install"
    }

    Start-NativeExecution { & "~/.rcedit/rcedit-x64.exe" (Get-PSOutput) --set-icon "$AssetsPath\Powershell_black.ico" `
        --set-file-version $ProductVersion --set-product-version $ProductVersion --set-version-string "ProductName" "PowerShell Core 6" `
        --set-requested-execution-level "asInvoker" --set-version-string "LegalCopyright" "(C) Microsoft Corporation.  All Rights Reserved." } | Write-Verbose

    ## AppVeyor base image might update the version for Wix. Hence, we should
    ## not hard code version numbers.
    $wixToolsetBinPath = "${env:ProgramFiles(x86)}\WiX Toolset *\bin"

    Write-Verbose "Ensure Wix Toolset is present on the machine @ $wixToolsetBinPath"
    if (-not (Test-Path $wixToolsetBinPath))
    {
        throw "Wix Toolset is required to create MSI package. Please install Wix from https://wix.codeplex.com/downloads/get/1540240"
    }

    ## Get the latest if multiple versions exist.
    $wixToolsetBinPath = (Get-ChildItem $wixToolsetBinPath).FullName | Sort-Object -Descending | Select-Object -First 1

    Write-Verbose "Initialize Wix executables - Heat.exe, Candle.exe, Light.exe"
    $wixHeatExePath = Join-Path $wixToolsetBinPath "Heat.exe"
    $wixCandleExePath = Join-Path $wixToolsetBinPath "Candle.exe"
    $wixLightExePath = Join-Path $wixToolsetBinPath "Light.exe"

    $ProductSemanticVersion = Get-PackageSemanticVersion -Version $ProductVersion
    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion

    $assetsInSourcePath = Join-Path $ProductSourcePath 'assets'
    New-Item $assetsInSourcePath -type directory -Force | Write-Verbose

    Write-Verbose "Place dependencies such as icons to $assetsInSourcePath"
    Copy-Item "$AssetsPath\*.ico" $assetsInSourcePath -Force

    $productVersionWithName = $ProductName + '_' + $ProductVersion
    $productSemanticVersionWithName = $ProductName + '-' + $ProductSemanticVersion

    Write-Verbose "Create MSI for Product $productSemanticVersionWithName"

    [Environment]::SetEnvironmentVariable("ProductSourcePath", $ProductSourcePath, "Process")
    # These variables are used by Product.wxs in assets directory
    [Environment]::SetEnvironmentVariable("ProductName", $ProductName, "Process")
    [Environment]::SetEnvironmentVariable("ProductGuid", $ProductGuid, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersion", $ProductVersion, "Process")
    [Environment]::SetEnvironmentVariable("ProductSemanticVersion", $ProductSemanticVersion, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersionWithName", $productVersionWithName, "Process")
    [Environment]::SetEnvironmentVariable("ProductTargetArchitecture", $ProductTargetArchitecture, "Process")
    $ProductProgFilesDir = "ProgramFiles64Folder"
    if ($ProductTargetArchitecture -eq "x86")
    {
        $ProductProgFilesDir = "ProgramFilesFolder"
    }
    [Environment]::SetEnvironmentVariable("ProductProgFilesDir", $ProductProgFilesDir, "Process")

    $wixFragmentPath = (Join-path $env:Temp "Fragment.wxs")
    $wixObjProductPath = (Join-path $env:Temp "Product.wixobj")
    $wixObjFragmentPath = (Join-path $env:Temp "Fragment.wixobj")

    $packageName = $productSemanticVersionWithName
    if ($ProductNameSuffix) {
        $packageName += "-$ProductNameSuffix"
    }
    $msiLocationPath = Join-Path $pwd "$packageName.msi"

    if(!$Force.IsPresent -and (Test-Path -Path $msiLocationPath))
    {
        Write-Error -Message "Package already exists, use -Force to overwrite, path:  $msiLocationPath" -ErrorAction Stop
    }

    $WiXHeatLog = & $wixHeatExePath dir  $ProductSourcePath -dr  $productVersionWithName -cg $productVersionWithName -gg -sfrag -srd -scom -sreg -out $wixFragmentPath -var env.ProductSourcePath -v
    $WiXCandleLog = & $wixCandleExePath  "$ProductWxsPath"  "$wixFragmentPath" -out (Join-Path "$env:Temp" "\\") -ext WixUIExtension -ext WixUtilExtension -arch x64 -v
    $WiXLightLog = & $wixLightExePath -out $msiLocationPath $wixObjProductPath $wixObjFragmentPath -ext WixUIExtension -ext WixUtilExtension -dWixUILicenseRtf="$LicenseFilePath" -v

    Remove-Item -ErrorAction SilentlyContinue *.wixpdb -Force

    if (Test-Path $msiLocationPath)
    {
        Write-Verbose "You can find the MSI @ $msiLocationPath" -Verbose
        $msiLocationPath
    }
    else
    {
        $WiXHeatLog   | Out-String | Write-Verbose -Verbose
        $WiXCandleLog | Out-String | Write-Verbose -Verbose
        $WiXLightLog  | Out-String | Write-Verbose -Verbose
        throw "Failed to create $msiLocationPath"
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
        [ValidateSet("win7-x86",
                     "win7-x64",
                     "osx.10.12-x64",
                     "linux-x64",
                     "linux-arm")]
        [string]
        $Runtime
    )

    function Generate-CrossGenAssembly {
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
                & $CrossgenPath /MissingDependenciesOK /in $AssemblyPath /out $outputAssembly /Platform_Assemblies_Paths $platformAssembliesPath
            } | Write-Verbose

            <#
            # TODO: Generate the pdb for the ngen binary - currently, there is a hard dependency on diasymreader.dll, which is available at %windir%\Microsoft.NET\Framework\v4.0.30319.
            # However, we still need to figure out the prerequisites on Linux.
            Start-NativeExecution {
                & $CrossgenPath /Platform_Assemblies_Paths $platformAssembliesPath  /CreatePDB $platformAssembliesPath /lines $platformAssembliesPath $niAssemblyName
            } | Write-Verbose
            #>
        } finally {
            Pop-Location
        }
    }

    if (-not (Test-Path $PublishPath)) {
        throw "Path '$PublishPath' does not exist."
    }

    # Get the path to crossgen
    $crossGenExe = if ($Environment.IsWindows) { "crossgen.exe" } else { "crossgen" }

    # The crossgen tool is only published for these particular runtimes
    $crossGenRuntime = if ($Environment.IsWindows) {
        if ($Runtime -match "-x86") {
            "win-x86"
        } else {
            "win-x64"
        }
    } elseif ($Runtime -eq "linux-arm") {
        throw "crossgen is not available for 'linux-arm'"
    } elseif ($Environment.IsLinux) {
        "linux-x64"
    } elseif ($Environment.IsMacOS) {
        "osx-x64"
    }

    if (-not $crossGenRuntime) {
        throw "crossgen is not available for this platform"
    }

    # Get the CrossGen.exe for the correct runtime with the latest version
    $crossGenPath = Get-ChildItem $script:Environment.nugetPackagesRoot $crossGenExe -Recurse | `
                        Where-Object { $_.FullName -match $crossGenRuntime } | `
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

    $crossGenRequiredAssemblies += if ($Environment.IsWindows) {
         "clrjit.dll"
    } elseif ($Environment.IsLinux) {
        "libclrjit.so"
    } elseif ($Environment.IsMacOS) {
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
    )

    # Common PowerShell libraries to crossgen
    $psCoreAssemblyList = @(
        "Microsoft.PowerShell.Commands.Utility.dll",
        "Microsoft.PowerShell.Commands.Management.dll",
        "Microsoft.PowerShell.Security.dll",
        "Microsoft.PowerShell.CoreCLR.Eventing.dll",
        "Microsoft.PowerShell.ConsoleHost.dll",
        "Microsoft.PowerShell.PSReadLine.dll",
        "System.Management.Automation.dll"
    )

    # Add Windows specific libraries
    if ($Environment.IsWindows) {
        $psCoreAssemblyList += @(
            "Microsoft.WSMan.Management.dll",
            "Microsoft.WSMan.Runtime.dll",
            "Microsoft.PowerShell.Commands.Diagnostics.dll",
            "Microsoft.Management.Infrastructure.CimCmdlets.dll"
        )
    }

    $fullAssemblyList = $commonAssembliesForAddType + $psCoreAssemblyList

    foreach ($assemblyName in $fullAssemblyList) {
        $assemblyPath = Join-Path $PublishPath $assemblyName
        Generate-CrossGenAssembly -CrossgenPath $crossGenPath -AssemblyPath $assemblyPath
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

        # No symbols are available for Microsoft.CodeAnalysis.CSharp.dll, Microsoft.CodeAnalysis.dll,
        # Microsoft.CodeAnalysis.VisualBasic.dll, and Microsoft.CSharp.dll.
        if ($commonAssembliesForAddType -notcontains $assemblyName) {
            Remove-Item $symbolsPath -Force -ErrorAction Stop
        }

        # Rename the corresponding ni.dll assembly to be the same as the IL assembly
        $niAssemblyPath = [System.IO.Path]::ChangeExtension($assemblyPath, "ni.dll")
        Rename-Item $niAssemblyPath $assemblyPath -Force -ErrorAction Stop
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
function Restore-PSModule
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$Name,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$Destination,

        [string]$SourceLocation="https://powershell.myget.org/F/powershellmodule/api/v2/",

        [string]$RequiredVersion
        )

    $needRegister = $true
    $RepositoryName = "mygetpsmodule"

    # Check if the PackageManagement works in the base-oS or PowerShellCore
    Get-PackageProvider -Name NuGet -ForceBootstrap -Verbose:$VerbosePreference
    Get-PackageProvider -Name PowerShellGet -Verbose:$VerbosePreference

    # Get the existing registered PowerShellGet repositories
    $psrepos = PowerShellGet\Get-PSRepository

    foreach ($repo in $psrepos)
    {
        if(($repo.SourceLocation -eq $SourceLocation) -or ($repo.SourceLocation.TrimEnd("/") -eq $SourceLocation.TrimEnd("/")))
        {
            # found a registered repository that matches the source location
            $needRegister = $false
            $RepositoryName = $repo.Name
            break
        }
    }

    if($needRegister)
    {
        $regVar = PowerShellGet\Get-PSRepository -Name $RepositoryName -ErrorAction SilentlyContinue
        if($regVar)
        {
            PowerShellGet\UnRegister-PSRepository -Name $RepositoryName
        }

        log "Registering PSRepository with name: $RepositoryName and sourcelocation: $SourceLocation"
        PowerShellGet\Register-PSRepository -Name $RepositoryName -SourceLocation $SourceLocation -ErrorVariable ev -verbose
        if($ev)
        {
            throw ("Failed to register repository '{0}'" -f $RepositoryName)
        }

        $regVar = PowerShellGet\Get-PSRepository -Name $RepositoryName
        if(-not $regVar)
        {
            throw ("'{0}' is not registered" -f $RepositoryName)
        }
    }

    log ("Name='{0}', Destination='{1}', Repository='{2}'" -f ($Name -join ','), $Destination, $RepositoryName)

    # do not output progress
    $ProgressPreference = "SilentlyContinue"
    $Name | ForEach-Object {

        $command = @{
                        Name=$_
                        Path = $Destination
                        Repository =$RepositoryName
                    }

        if($RequiredVersion)
        {
            $command.Add("RequiredVersion", $RequiredVersion)
        }

        # pull down the module
        log "running save-module $_"
        PowerShellGet\Save-Module @command -Force

        # Remove PSGetModuleInfo.xml file
        Find-Module -Name $_ -Repository $RepositoryName -IncludeDependencies | ForEach-Object {
            Remove-Item -Path $Destination\$($_.Name)\*\PSGetModuleInfo.xml -Force
        }
    }

    # Clean up
    if($needRegister)
    {
        $regVar = PowerShellGet\Get-PSRepository -Name $RepositoryName -ErrorAction SilentlyContinue
        if($regVar)
        {
            log "Unregistering PSRepository with name: $RepositoryName"
            PowerShellGet\UnRegister-PSRepository -Name $RepositoryName
        }
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
