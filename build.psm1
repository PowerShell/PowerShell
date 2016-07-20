# Use the .NET Core APIs to determine the current platform; if a runtime
# exception is thrown, we are on FullCLR, not .NET Core.
try {
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

    $IsCore = $true
    $IsLinux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $IsOSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $IsWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
} catch {
    # If these are already set, then they're read-only and we're done
    try {
        $IsCore = $false
        $IsLinux = $false
        $IsOSX = $false
        $IsWindows = $true
    }
    catch { }
}

if ($IsLinux) {
    $LinuxInfo = Get-Content /etc/os-release | ConvertFrom-StringData

    $IsUbuntu = $LinuxInfo.ID -match 'ubuntu' -and $LinuxInfo.VERSION_ID -match '14.04'
    $IsCentOS = $LinuxInfo.ID -match 'centos' -and $LinuxInfo.VERSION_ID -match '7'
}


function Start-PSBuild {
    [CmdletBinding(DefaultParameterSetName='CoreCLR')]
    param(
        [switch]$NoPath,
        [switch]$Restore,
        [string]$Output,
        [switch]$ResGen,
        [switch]$TypeGen,
        [switch]$Clean,

        [Parameter(ParameterSetName='CoreCLR')]
        [switch]$Publish,

        # These runtimes must match those in project.json
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("ubuntu.14.04-x64",
                     "debian.8-x64",
                     "centos.7-x64",
                     "win7-x64",
                     "win81-x64",
                     "win10-x64",
                     "osx.10.11-x64")]
        [Parameter(ParameterSetName='CoreCLR')]
        [string]$Runtime,

        [Parameter(ParameterSetName='FullCLR', Mandatory=$true)]
        [switch]$FullCLR,

        [Parameter(ParameterSetName='FullCLR')]
        [switch]$XamlGen,

        [Parameter(ParameterSetName='FullCLR')]
        [ValidateSet('x86', 'x64')] # TODO: At some point, we need to add ARM support to match CoreCLR
        [string]$NativeHostArch = "x64",

        [ValidateSet('Linux', 'Debug', 'Release', '')] # We might need "Checked" as well
        [string]$Configuration
    )

    if ($Clean)
    {
        log "Cleaning your working directory. You can also do it with 'git clean -fdX'"
        git clean -fdX
    }

    # save Git description to file for PowerShell to include in PSVersionTable
    git --git-dir="$PSScriptRoot/.git" describe --dirty --abbrev=60 > "$psscriptroot/powershell.version"

    # simplify ParameterSetNames
    if ($PSCmdlet.ParameterSetName -eq 'FullCLR') {
        $FullCLR = $true
    }

    # Add .NET CLI tools to PATH
    Find-Dotnet

    if ($IsWindows) {
        # use custom package store - this value is also defined in nuget.config under config/repositoryPath
        # dotnet restore uses this value as the target for installing the assemblies for referenced nuget packages.
        # dotnet build does not currently consume the  config value but will consume env:NUGET_PACKAGES to resolve these dependencies
        $env:NUGET_PACKAGES="$PSScriptRoot\Packages"
    }

    # verify we have all tools in place to do the build
    $precheck = precheck 'dotnet' "Build dependency 'dotnet' not found in PATH. Run Start-PSBootstrap. Also see: https://dotnet.github.io/getting-started/"
    if ($FullCLR) {
        # cmake is needed to build powershell.exe
        $precheck = $precheck -and (precheck 'cmake' 'cmake not found. You can install it from https://chocolatey.org/packages/cmake.portable')

        Use-MSBuild

        #mc.exe is Message Compiler for native resources
        $mcexe = Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\" -Recurse -Filter 'mc.exe' | ? {$_.FullName -match 'x64'} | select -First 1 | % {$_.FullName}
        if (-not $mcexe) {
            throw 'mc.exe not found. Install Microsoft Windows SDK.'
        }

        $vcVarsPath = (Get-Item(Join-Path -Path "$env:VS140COMNTOOLS" -ChildPath '../../vc')).FullName
        if ((Test-Path -Path $vcVarsPath\vcvarsall.bat) -eq $false)
        {
            throw "Could not find Visual Studio vcvarsall.bat at" + $vcVarsPath
        }

        # setup msbuild configuration
        if ($Configuration -eq 'Debug' -or $Configuration -eq 'Release')
        {
            $msbuildConfiguration = $Configuration
        }
        else 
        {
            $msbuildConfiguration = 'Release'
        }

        # setup cmakeGenerator
        if ($NativeHostArch -eq 'x86')
        {
            $cmakeGenerator = 'Visual Studio 14 2015'    
        }
        else 
        {
            $cmakeGenerator = 'Visual Studio 14 2015 Win64'    
        }

    } elseif ($IsLinux -or $IsOSX) {
        foreach ($Dependency in 'cmake', 'make', 'g++') {
            $precheck = $precheck -and (precheck $Dependency "Build dependency '$Dependency' not found. Run Start-PSBootstrap.")
        }
    }

    # Abort if any precheck failed
    if (-not $precheck) {
        return
    }

    # set output options
    $OptionsArguments = @{Publish=$Publish; Output=$Output; FullCLR=$FullCLR; Runtime=$Runtime; Configuration=$Configuration; Verbose=$true}
    $script:Options = New-PSOptions @OptionsArguments

    # setup arguments
    $Arguments = @()
    if ($Publish) {
        $Arguments += "publish"
    } else {
        $Arguments += "build"
    }
    if ($Output) {
        $Arguments += "--output", (Join-Path $PSScriptRoot $Output)
    }
    $Arguments += "--configuration", $Options.Configuration
    $Arguments += "--framework", $Options.Framework
    $Arguments += "--runtime", $Options.Runtime

    # handle Restore
    if ($Restore -or -not (Test-Path "$($Options.Top)/project.lock.json")) {
        log "Run dotnet restore"

        $RestoreArguments = @("--verbosity")
        if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
            $RestoreArguments += "Info"
        } else {
            $RestoreArguments += "Warning"
        }

        $RestoreArguments += "$PSScriptRoot"

        Start-NativeExecution { dotnet restore $RestoreArguments }
    }

    # handle ResGen
    # Heuristic to run ResGen on the fresh machine
    if ($ResGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.ConsoleHost/gen"))
    {
        log "Run ResGen (generating C# bindings for resx files)"
        Start-ResGen
    }

    # handle xaml files
    # Heuristic to resolve xaml on the fresh machine
    if ($FullCLR -and ($XamlGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.Activities/gen/*.g.cs")))
    {
        log "Run XamlGen (generating .g.cs and .resources for .xaml files)"
        Start-XamlGen -MSBuildConfiguration $msbuildConfiguration
    }

    # Build native components
    if ($IsLinux -or $IsOSX) {
        $Ext = if ($IsLinux) {
            "so"
        } elseif ($IsOSX) {
            "dylib"
        }

        $Native = "$PSScriptRoot/src/libpsl-native"
        $Lib = "$($Options.Top)/libpsl-native.$Ext"
        log "Start building $Lib"

        try {
            Push-Location $Native
            Start-NativeExecution { cmake -DCMAKE_BUILD_TYPE=Debug . }
            Start-NativeExecution { make -j }
            Start-NativeExecution { ctest --verbose }
        } finally {
            Pop-Location
        }

        if (-not (Test-Path $Lib)) {
            throw "Compilation of $Lib failed"
        }
    } elseif ($FullCLR) {
        log "Start building native powershell.exe"

        try {
            Push-Location "$PSScriptRoot\src\powershell-native"

            # Compile native resources
            @("nativemsh/pwrshplugin") | % {
                $nativeResourcesFolder = $_
                Get-ChildItem $nativeResourcesFolder -Filter "*.mc" | % {
                    Start-NativeExecution { & $mcexe -o -d -c -U $_.FullName -h "$nativeResourcesFolder" -r "$nativeResourcesFolder" }
                }
            }
 
# Disabling until I figure out if it is necessary          
#            $overrideFlags = "-DCMAKE_USER_MAKE_RULES_OVERRIDE=$PSScriptRoot\src\powershell-native\windows-compiler-override.txt" 
            $overrideFlags = ""
            $location = Get-Location
            #
            # BUILD_ONECORE
            #
            
            $BuildOneCoreValues = @("ON","OFF")
            foreach ($oneCoreValue in $BuildOneCoreValues)
            {
                $command = @"
cmd.exe /C cd /d "$location" "&" "$($vcVarsPath)\vcvarsall.bat" "$NativeHostArch" "&" cmake "$overrideFlags" -DBUILD_ONECORE=$oneCoreValue -G "$cmakeGenerator" . "&" msbuild ALL_BUILD.vcxproj "/p:Configuration=$msbuildConfiguration"
"@
                log "  Executing Build Command: $command"
                Start-NativeExecution { Invoke-Expression -Command:$command }
            }

            # Copy the executable binary from the local build directory to the expected destination to enable Start-DevPowerShell to work
            #
            # TODO: This should be updated to handle per-architecture builds gracefully.
            $srcPath = Join-Path (Join-Path (Join-Path (Get-Location) "bin") $msbuildConfiguration) "FullCLR/powershell.exe"
            $dstPath = ($script:Options).Top
            log "  Copying $srcPath to $dstPath"
            Copy-Item $srcPath $dstPath
        } finally {
            Pop-Location
        }
    }

    # handle TypeGen
    if ($TypeGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs"))
    {
        log "Run TypeGen (generating CorePsTypeCatalog.cs)"
        Start-TypeGen
    }

    try {
        # Relative paths do not work well if cwd is not changed to project
        Push-Location $Options.Top
        log "Run dotnet $Arguments from $pwd"
        Start-NativeExecution { dotnet $Arguments }
        log "PowerShell output: $($Options.Output)"
    } finally {
        Pop-Location
    }
}


function New-PSOptions {
    [CmdletBinding()]
    param(
        [ValidateSet("Linux", "Debug", "Release", "")]
        [string]$Configuration,

        [ValidateSet("netcoreapp1.0", "net451")]
        [string]$Framework,

        # These are duplicated from Start-PSBuild
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("",
                     "ubuntu.14.04-x64",
                     "debian.8-x64",
                     "centos.7-x64",
                     "win7-x64",
                     "win81-x64",
                     "win10-x64",
                     "osx.10.11-x64")]
        [string]$Runtime,

        [switch]$Publish,
        [string]$Output,

        [switch]$FullCLR
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    if ($FullCLR) {
        $Top = "$PSScriptRoot/src/Microsoft.PowerShell.ConsoleHost"
    } else {
        $Top = "$PSScriptRoot/src/powershell"
    }
    Write-Verbose "Top project directory is $Top"

    if (-not $Configuration) {
        $Configuration = if ($IsLinux -or $IsOSX) {
            "Linux"
        } elseif ($IsWindows) {
            "Debug"
        }
        Write-Verbose "Using configuration '$Configuration'"
    }

    if (-not $Framework) {
        $Framework = if ($FullCLR) {
            "net451"
        } else {
            "netcoreapp1.0"
        }
        Write-Verbose "Using framework '$Framework'"
    }

    if (-not $Runtime) {
        $Runtime = dotnet --info | % {
            if ($_ -match "RID") {
                $_ -split "\s+" | Select-Object -Last 1
            }
        }

        if (-not $Runtime) {
            Throw "Could not determine Runtime Identifier, please update dotnet"
        } else {
            Write-Verbose "Using runtime '$Runtime'"
        }
    }

    $Executable = if ($IsLinux -or $IsOSX) {
        "powershell"
    } elseif ($IsWindows) {
        "powershell.exe"
    }

    # Build the Output path
    if ($Output) {
        $Output = Join-Path $PSScriptRoot $Output
    } else {
        $Output = [IO.Path]::Combine($Top, "bin", $Configuration, $Framework)

        # FullCLR only builds a library, so there is no runtime component
        if (-not $FullCLR) {
            $Output = [IO.Path]::Combine($Output, $Runtime)
        }

        # Publish injects the publish directory
        if ($Publish) {
            $Output = [IO.Path]::Combine($Output, "publish")
        }

        $Output = [IO.Path]::Combine($Output, $Executable)
    }

    return @{ Top = $Top;
              Configuration = $Configuration;
              Framework = $Framework;
              Runtime = $Runtime;
              Output = $Output }
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


function Start-PSPester {
    [CmdletBinding()]param(
        [string]$Flags = "-ExcludeTag 'Slow' -EnableExit -OutputFile pester-tests.xml -OutputFormat NUnitXml",
        [string]$Tests = "*",
        [ValidateScript({ Test-Path -PathType Container $_})]
        [string]$Directory = "$PSScriptRoot/test/powershell"
    )

    & (Get-PSOutput) -noprofile -c "Import-Module '$PSScriptRoot/src/Modules/Shared/Pester'; Invoke-Pester $Flags $Directory/$Tests"
    if ($LASTEXITCODE -ne 0) {
        throw "$LASTEXITCODE Pester tests failed"
    }
}


function Start-PSxUnit {
    [CmdletBinding()]param()

    log "xUnit tests are currently disabled pending fixes due to API and AssemblyLoadContext changes - @andschwa"
    return

    if ($IsWindows) {
        throw "xUnit tests are only currently supported on Linux / OS X"
    }

    if ($IsOSX) {
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


function Start-PSBootstrap {
    [CmdletBinding()]param(
        [ValidateSet("dev", "beta", "preview")]
        [string]$Channel = "rel-1.0.0",
        [string]$Version = "latest",
        [switch]$Package
    )

    log "Installing Open PowerShell build dependencies"

    Push-Location $PSScriptRoot/tools

    try {
        # Install ours and .NET's dependencies
        $Deps = @()
        if ($IsUbuntu) {
            # Build tools
            $Deps += "curl", "g++", "cmake", "make"
            # .NET Core required runtime libraries
            $Deps += "libicu52", "libunwind8"
            # Packaging tools
            if ($Package) { $Deps += "ruby-dev" }
            # Install dependencies
            sudo apt-get install -y -qq $Deps
        } elseif ($IsCentos) {
            # Build tools
            $Deps += "curl", "gcc-c++", "cmake", "make"
            # .NET Core required runtime libraries
            $Deps += "libicu", "libunwind"
            # Packaging tools
            if ($Package) { $Deps += "ruby-devel", "rpmbuild" }
            # Install dependencies
            sudo yum install -y -q $Deps
        } elseif ($IsOSX) {
            precheck 'brew' "Bootstrap dependency 'brew' not found, must install Homebrew! See http://brew.sh/"
            # Build tools
            $Deps += "curl", "cmake"
            # .NET Core required runtime libraries
            $Deps += "openssl"
            # Packaging tools
            if ($Package) { $Deps += "ruby" }
            # Install dependencies
            brew install $Deps
            # OpenSSL libraries must be updated
            brew link --force openssl
        } else {
            Write-Warning "This script only supports Ubuntu 14.04, CentOS 7, and OS X, you must install dependencies manually!"
        }

        # Install [fpm](https://github.com/jordansissel/fpm)
        if ($Package) {
            gem install fpm
        }

        $obtainUrl = "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain"

        # Install for Linux and OS X
        if ($IsLinux -or $IsOSX) {
            # Uninstall all previous dotnet packages
            $uninstallScript = if ($IsUbuntu) {
                "dotnet-uninstall-debian-packages.sh"
            } elseif ($IsOSX) {
                "dotnet-uninstall-pkgs.sh"
            }

            if ($uninstallScript) {
                curl -s $obtainUrl/uninstall/$uninstallScript -o $uninstallScript
                chmod +x $uninstallScript
                sudo ./$uninstallScript
            } else {
                Write-Warning "This script only removes prior versions of dotnet for Ubuntu 14.04 and OS X"
            }

            # Install new dotnet 1.0.0 preview packages
            $installScript = "dotnet-install.sh"
            curl -s $obtainUrl/$installScript -o $installScript
            chmod +x $installScript
            bash ./$installScript -c $Channel -v $Version
        }

        # Install for Windows
        if ($IsWindows -and -not $IsCore) {
            Remove-Item -ErrorAction SilentlyContinue -Recurse -Force ~\AppData\Local\Microsoft\dotnet
            $installScript = "dotnet-install.ps1"
            Invoke-WebRequest -Uri $obtainUrl/$installScript -OutFile $installScript
            & ./$installScript -c $Channel -v $Version
        } elseif ($IsWindows) {
            Write-Warning "Start-PSBootstrap cannot be run in Core PowerShell on Windows (need Invoke-WebRequest!)"
        }
    } finally {
        Pop-Location
    }
}


function Start-PSPackage {
    [CmdletBinding()]param(
        # PowerShell packages use Semantic Versioning http://semver.org/
        [string]$Version,
        # Package iteration version (rarely changed)
        [int]$Iteration = 1,
        # Ubuntu, CentOS, and OS X packages are supported
        [ValidateSet("deb", "osxpkg", "rpm")]
        [string]$Type
    )

    $Description = @"
Open PowerShell on .NET Core
PowerShell is an open-source, cross-platform, scripting language and rich object shell.
Built upon .NET Core, it is also a C# REPL.
"@

    # Use Git tag if not given a version
    if (-not $Version) {
        $Version = (git --git-dir="$PSScriptRoot/.git" describe) -Replace '^v'
    }

    $Source = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Publish))
    Write-Verbose "Packaging $Source"

    if ($IsWindows) {
        
        # Product Guid needs to be unique for every PowerShell version to allow SxS install
        $productGuid = [guid]::NewGuid()
        $msiPackagePath = New-MSIPackage -ProductSourcePath $Source -ProductVersion $Version -AssetsPath "$PSScriptRoot\Assets" -ProductGuid $productGuid
        $appxPackagePath = New-AppxPackage -PackageVersion $Version -SourcePath $Source -AssetsPath "$PSScriptRoot\Assets"

        $packages = @($msiPackagePath, $appxPackagePath)

        return $packages
    }

    if (-not (Get-Command "fpm" -ErrorAction SilentlyContinue)) {
        throw "Build dependency 'fpm' not found in PATH! See: https://github.com/jordansissel/fpm"
    }

    # Decide package output type
    if (-not $Type) {
        $Type = if ($IsLinux) {
            if ($LinuxInfo.ID -match 'ubuntu') {
                "deb"
            } elseif ($LinuxInfo.ID -match 'centos') {
                "rpm"
            } else {
                throw "Building packages for $($LinuxInfo.PRETTY_NAME) is unsupported!"
            }
        } elseif ($IsOSX) {
            'osxpkg'
        }
        Write-Warning "-Type was not specified, continuing with $Type"
    }

    # Follow the Filesystem Hierarchy Standard for Linux and OS X
    $Destination = if ($IsLinux) {
        "/opt/microsoft/powershell"
    } elseif ($IsOSX) {
        "/usr/local/microsoft/powershell"
    }

    # Destination for symlink to powershell executable
    $Link = if ($IsLinux) {
        "/usr/bin"
    } elseif ($IsOSX) {
        "/usr/local/bin"
    }

    New-Item -Force -ItemType SymbolicLink -Path /tmp/powershell -Target $Destination/powershell >$null

    # there is a weired bug in fpm
    # if the target of the powershell symlink exists, `fpm` aborts
    # with a `utime` error on OS X.
    # so we move it to make symlink broken
    $symlink_dest = "$Destination/powershell"
    $hack_dest = "./_fpm_symlink_hack_powershell"
    if ($IsOSX)
    {
        if (Test-Path $symlink_dest)
        {
            Write-Warning "Move $symlink_dest to $hack_dest (fpm utime bug)"
            Move-Item $symlink_dest $hack_dest
        }
    }

    # Change permissions for packaging
    chmod -R go=u $Source /tmp/powershell

    $libunwind = switch ($Type) {
        "deb" { "libunwind8" }
        "rpm" { "libunwind" }
    }

    $libicu = switch ($Type) {
        "deb" { "libicu52" }
        "rpm" { "libicu" }
    }


    $Arguments = @(
        "--force", "--verbose",
        "--name", "powershell",
        "--version", $Version,
        "--iteration", $Iteration,
        "--maintainer", "Andrew Schwartzmeyer <andschwa@microsoft.com>",
        "--vendor", "Microsoft <mageng@microsoft.com>",
        "--url", "https://github.com/PowerShell/PowerShell",
        "--license", "Unlicensed",
        "--description", $Description,
        "--category", "shells",
        "--rpm-os", "linux",
        "--depends", $libunwind,
        "--depends", $libicu,
        "--deb-build-depends", "dotnet",
        "--deb-build-depends", "cmake",
        "--deb-build-depends", "g++",
        "-t", $Type,
        "-s", "dir",
        "$Source/=$Destination/",
        "/tmp/powershell=$Link"
    )

    # Build package
    fpm $Arguments

    if ($IsOSX)
    {
        # this is continuation of a fpm hack for a weired bug
        if (Test-Path $hack_dest)
        {
            Write-Warning "Move $hack_dest to $symlink_dest (fpm utime bug)"
            Move-Item $hack_dest $symlink_dest
        }
    }
}

function Publish-NuGetFeed
{
    param(
        [string]$OutputPath = "$PSScriptRoot/nuget-artifacts",
        [Parameter(Mandatory=$true)]
        [string]$VersionSuffix
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    @(
'Microsoft.PowerShell.Commands.Management',
'Microsoft.PowerShell.Commands.Utility',
'Microsoft.PowerShell.ConsoleHost',
'Microsoft.PowerShell.PSReadLine',
'Microsoft.PowerShell.Security',
'System.Management.Automation',
'Microsoft.PowerShell.CoreCLR.AssemblyLoadContext',
'Microsoft.PowerShell.CoreCLR.Eventing'
    ) | % {
        if ($VersionSuffix)
        {
            dotnet pack "src/$_" --output $OutputPath --version-suffix $VersionSuffix
        }
        else
        {
            dotnet pack "src/$_" --output $OutputPath
        }
    }
}


function Start-DevPowerShell {
    param(
        [switch]$ZapDisable,
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = (Split-Path (New-PSOptions -FullCLR).Output),
        [switch]$NoNewWindow,
        [string]$Command,
        [switch]$KeepPSModulePath
    )

    try {
        if (-not $LoadProfile) {
            $ArgumentList = @('-noprofile') + $ArgumentList
        }

        if (-not $KeepPSModulePath)
        {
            if (-not $Command)
            {
                $ArgumentList = @('-NoExit') + $ArgumentList
            }

            $Command = '$env:PSModulePath = Join-Path $env:DEVPATH Modules; ' + $Command   
        }   

        if ($Command)
        {
            $ArgumentList = $ArgumentList + @("-command $Command")
        }

        $env:DEVPATH = $binDir
        if ($ZapDisable) {
            $env:COMPLUS_ZapDisable = 1
        }

        if (-not (Test-Path $binDir\powershell.exe.config)) {
            $configContents = @"
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
<runtime>
<developmentMode developerInstallation="true"/>
</runtime>
</configuration>
"@
            $configContents | Out-File -Encoding Ascii $binDir\powershell.exe.config
        }

        # splatting for the win
        $startProcessArgs = @{
            FilePath = "$binDir\powershell.exe"
            ArgumentList = "$ArgumentList"
        }

        if ($NoNewWindow) {
            $startProcessArgs.NoNewWindow = $true
            $startProcessArgs.Wait = $true
        }

        Start-Process @startProcessArgs
    } finally {
        ri env:DEVPATH
        if ($ZapDisable) {
            ri env:COMPLUS_ZapDisable
        }
    }
}


<#
.EXAMPLE 
PS C:> Copy-MappedFiles -PslMonadRoot .\src\monad

copy files FROM .\src\monad (old location of submodule) TO src/<project> folders
#>
function Copy-MappedFiles {

    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        [string[]]$Path = "$PSScriptRoot",
        [Parameter(Mandatory=$true)]
        [string]$PslMonadRoot,
        [switch]$Force,
        [switch]$WhatIf
    )

    begin 
    {
        function MaybeTerminatingWarning
        {
            param([string]$Message)

            if ($Force)
            {
                Write-Warning "$Message : ignoring (-Force)"
            }
            elseif ($WhatIf)
            {
                Write-Warning "$Message : ignoring (-WhatIf)"   
            }
            else
            {
                throw "$Message : use -Force to ignore"
            }
        }

        if (-not (Test-Path -PathType Container $PslMonadRoot))
        {
            throw "$pslMonadRoot is not a valid folder"
        }

        # Do some intelligens to prevent shouting us in the foot with CL management

        # finding base-line CL
        $cl = git --git-dir="$PSScriptRoot/.git" tag | % {if ($_ -match 'SD.(\d+)$') {[int]$Matches[1]} } | Sort-Object -Descending | Select-Object -First 1
        if ($cl)
        {
            log "Current base-line CL is SD:$cl (based on tags)"
        }
        else 
        {
            MaybeTerminatingWarning "Could not determine base-line CL based on tags"
        }

        try
        {
            Push-Location $PslMonadRoot
            if (git status --porcelain -uno)
            {
                MaybeTerminatingWarning "$pslMonadRoot has changes"
            }

            if (git log --grep="SD:$cl" HEAD^..HEAD)
            {
                log "$pslMonadRoot HEAD matches [SD:$cl]"
            }
            else 
            {
                Write-Warning "Try to checkout this commit in $pslMonadRoot :" 
                git log --grep="SD:$cl" | Write-Warning
                MaybeTerminatingWarning "$pslMonadRoot HEAD doesn't match [SD:$cl]"
            }
        }
        finally
        {
            Pop-Location
        }

        $map = @{}
    }

    process
    {
        $map += Get-Mappings $Path -Root $PslMonadRoot
    }

    end
    {
        $map.GetEnumerator() | % {
            New-Item -ItemType Directory (Split-Path $_.Value) -ErrorAction SilentlyContinue > $null

            Copy-Item $_.Key $_.Value -Verbose:([bool]$PSBoundParameters['Verbose']) -WhatIf:$WhatIf
        }
    }
}

function Get-Mappings
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        [string[]]$Path = "$PSScriptRoot",
        [string]$Root,
        [switch]$KeepRelativePaths
    )

    begin 
    {
        $mapFiles = @()
    }

    process
    {
        Write-Verbose "Discovering map files in $Path"
        $count = $mapFiles.Count

        if (-not (Test-Path $Path)) 
        {
            throw "Mapping file not found in $mappingFilePath"
        }

        if (Test-Path -PathType Container $Path)
        {
            $mapFiles += Get-ChildItem -Recurse $Path -Filter 'map.json' -File
        }
        else 
        {
            # it exists and it's a file, don't check the name pattern
            $mapFiles += Get-ChildItem $Path
        }

        Write-Verbose "Found $($mapFiles.Count - $count) map files in $Path"
    }

    end
    {
        $map = @{}
        $mapFiles | % {
            $file = $_
            try
            {
                $rawHashtable = $_ | Get-Content -Raw | ConvertFrom-Json | Convert-PSObjectToHashtable
            }
            catch
            {
                Write-Error "Exception, when processing $($file.FullName): $_"
            }

            $mapRoot = Split-Path $_.FullName
            if ($KeepRelativePaths) 
            {
                # not very elegant way to find relative for the current directory path
                $mapRoot = $mapRoot.Substring($PSScriptRoot.Length + 1)
                # keep original unix-style paths for git
                $mapRoot = $mapRoot.Replace('\', '/')
            }

            $rawHashtable.GetEnumerator() | % {
                $newKey = if ($Root) { Join-Path $Root $_.Key } else { $_.Key }
                $newValue = if ($KeepRelativePaths) { ($mapRoot + '/' + $_.Value) } else { Join-Path $mapRoot $_.Value } 
                $map[$newKey] = $newValue
            }
        }

        return $map
    }
}


<#
.EXAMPLE Send-GitDiffToSd -diffArg1 32b90c048aa0c5bc8e67f96a98ea01c728c4a5be~1 -diffArg2 32b90c048aa0c5bc8e67f96a98ea01c728c4a5be -AdminRoot d:\e\ps_dev\admin
Apply a signle commit to admin folder
#>
function Send-GitDiffToSd {
    param(
        [Parameter(Mandatory)]
        [string]$diffArg1,
        [Parameter(Mandatory)]
        [string]$diffArg2,
        [Parameter(Mandatory)]
        [string]$AdminRoot,
        [switch]$WhatIf
    )

    # this is only for windows, because you cannot have SD enlistment on Linux
    $patchPath = (ls (Join-Path (get-command git).Source '..\..') -Recurse -Filter 'patch.exe').FullName
    $m = Get-Mappings -KeepRelativePaths -Root $AdminRoot
    $affectedFiles = git diff --name-only $diffArg1 $diffArg2
    $affectedFiles | % {
        log "Changes in file $_"
    }

    $rev = Get-InvertedOrderedMap $m
    foreach ($file in $affectedFiles) {
        if ($rev.Contains) {
            $sdFilePath = $rev[$file]
            if (-not $sdFilePath)
            {
                Write-Warning "Cannot find mapped file for $file, skipping"
                continue
            }

            $diff = git diff $diffArg1 $diffArg2 -- $file
            if ($diff) {
                log "Apply patch to $sdFilePath"
                Set-Content -Value $diff -Path $env:TEMP\diff -Encoding Ascii
                if ($WhatIf) {
                    log "Patch content"
                    Get-Content $env:TEMP\diff
                } else {
                    & $patchPath --binary -p1 $sdFilePath $env:TEMP\diff
                }
            } else {
                log "No changes in $file"
            }
        } else {
            log "Ignore changes in $file, because there is no mapping for it"
        }
    }
}

function Start-TypeGen
{
    [CmdletBinding()]
    param()

    # Add .NET CLI tools to PATH
    Find-Dotnet

    Push-Location "$PSScriptRoot/src/TypeCatalogParser"
    try
    {
        dotnet run
    }
    finally
    {
        Pop-Location
    }

    Push-Location "$PSScriptRoot/src/TypeCatalogGen"
    try
    {
        dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
    }
    finally
    {
        Pop-Location
    }
}

function Start-ResGen
{
    [CmdletBinding()]
    param()

    Get-ChildItem $PSScriptRoot/src -Directory | ? {
        Get-ChildItem (Join-Path $_.FullName 'resources') -ErrorAction SilentlyContinue} | % {
            $_. Name} | % {

                $module = $_
                Get-ChildItem "$PSScriptRoot/src/$module/resources" -Filter '*.resx' | % {
                    $className = $_.Name.Replace('.resx', '')
                    $xml = [xml](Get-Content -raw $_.FullName)

                    $fileName = $className
                    $genSource = Get-StronglyTypeCsFileForResx -xml $xml -ModuleName $module -ClassName $className
                    $outPath = "$PSScriptRoot/src/$module/gen/$fileName.cs"
                    Write-Verbose "ResGen for $outPath"
                    New-Item -Type Directory -ErrorAction SilentlyContinue (Split-Path $outPath) > $null
                    Set-Content -Encoding Ascii -Path $outPath -Value $genSource
                }
    }
}


function Find-Dotnet() {
    $originalPath = $env:PATH
    $dotnetPath = if ($IsWindows) {
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

    process
    {
        $Path | % {
            Get-ChildItem $_ -Filter "*.txt" | % {
                $txtFile = $_.FullName
                $resxFile = Join-Path (Split-Path $txtFile) "$($_.BaseName).resx"
                $resourceHashtable = ConvertFrom-StringData (Get-Content -Raw $txtFile)
                $resxContent = $resourceHashtable.GetEnumerator() | % {
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

function Start-XamlGen
{
    [CmdletBinding()]
    param(
        [Parameter()]
        [ValidateSet("Debug", "Release")]
        [string]
        $MSBuildConfiguration = "Release"
    )

    Use-MSBuild
    Get-ChildItem -Path "$PSScriptRoot/src" -Directory | % {
        
        $XamlDir = Join-Path -Path $_.FullName -ChildPath Xamls
        if ((Test-Path -Path $XamlDir -PathType Container) -and
            (@(Get-ChildItem -Path "$XamlDir\*.xaml").Count -gt 0))
        {
            $OutputDir = Join-Path -Path $env:TEMP -ChildPath "_Resolve_Xaml_"
            Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
            mkdir -Path $OutputDir -Force > $null

            # we will get failures, but it's ok: we only need to copy *.g.cs files in the dotnet cli project.
            $SourceDir = ConvertFrom-Xaml -Configuration $MSBuildConfiguration -OutputDir $OutputDir -XamlDir $XamlDir -IgnoreMsbuildFailure:$true
            $DestinationDir = Join-Path -Path $_.FullName -ChildPath gen
            
            New-Item -ItemType Directory $DestinationDir -ErrorAction SilentlyContinue > $null
            $filesToCopy = Get-Item "$SourceDir\*.cs", "$SourceDir\*.g.resources"
            if (-not $filesToCopy)
            {
                throw "No .cs or .g.resources files are generated for $XamlDir, something went wrong. Run 'Start-XamlGen -Verbose' for details."
            }

            $filesToCopy | % {
                $sourcePath = $_.FullName
                Write-Verbose "Copy generated xaml artifact: $sourcePath -> $DestinationDir"
                Copy-Item -Path $sourcePath -Destination $DestinationDir
            }
        }
    }
}

$Script:XamlProj = @"
<Project DefaultTargets="ResolveAssemblyReferences;MarkupCompilePass1;PrepareResources" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <Language>C#</Language>
        <AssemblyName>Microsoft.PowerShell.Activities</AssemblyName>
        <OutputType>library</OutputType>
        <Configuration>{0}</Configuration>
        <Platform>Any CPU</Platform>
        <OutputPath>{1}</OutputPath>
        <Do_CodeGenFromXaml>true</Do_CodeGenFromXaml>
    </PropertyGroup>

    <Import Project="`$(MSBuildBinPath)\Microsoft.CSharp.targets" />
    <Import Project="`$(MSBuildBinPath)\Microsoft.WinFX.targets" Condition="'`$(TargetFrameworkVersion)' == 'v2.0' OR '`$(TargetFrameworkVersion)' == 'v3.0' OR '`$(TargetFrameworkVersion)' == 'v3.5'" />

    <ItemGroup>
{2}
        <Reference Include="WindowsBase.dll">
            <Private>False</Private>
        </Reference>
        <Reference Include="PresentationCore.dll">
            <Private>False</Private>
        </Reference>
        <Reference Include="PresentationFramework.dll">
            <Private>False</Private>
        </Reference>
    </ItemGroup>
</Project>
"@

$Script:XamlProjPage = @'
        <Page Include="{0}" />

'@

function script:ConvertFrom-Xaml {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string] $Configuration,

        [Parameter(Mandatory=$true)]
        [string] $OutputDir,

        [Parameter(Mandatory=$true)]
        [string] $XamlDir,

        [switch] $IgnoreMsbuildFailure
    )

    log "ConvertFrom-Xaml for $XamlDir"

    $Pages = ""
    Get-ChildItem -Path "$XamlDir\*.xaml" | % {
        $Page = $Script:XamlProjPage -f $_.FullName
        $Pages += $Page
    }

    $XamlProjContent = $Script:XamlProj -f $Configuration, $OutputDir, $Pages
    $XamlProjPath = Join-Path -Path $OutputDir -ChildPath xaml.proj
    Set-Content -Path $XamlProjPath -Value $XamlProjContent -Encoding Ascii -NoNewline -Force

    msbuild $XamlProjPath | Write-Verbose

    if ($LASTEXITCODE -ne 0)
    {
        $message = "When processing $XamlDir 'msbuild $XamlProjPath > `$null' failed with exit code $LASTEXITCODE"
        if ($IgnoreMsbuildFailure)
        {
            Write-Verbose $message
        }
        else
        {
            throw $message
        }
    }

    return (Join-Path -Path $OutputDir -ChildPath "obj\Any CPU\$Configuration")
}


function script:Use-MSBuild {
    # TODO: we probably should require a particular version of msbuild, if we are taking this dependency
    # msbuild v14 and msbuild v4 behaviors are different for XAML generation
    $frameworkMsBuildLocation = "${env:SystemRoot}\Microsoft.Net\Framework\v4.0.30319\msbuild"

    $msbuild = get-command msbuild -ErrorAction SilentlyContinue
    if ($msbuild)
    {
        # all good, nothing to do
        return
    }

    if (-not (Test-Path $frameworkMsBuildLocation))
    {
        throw "msbuild not found in '$frameworkMsBuildLocation'. Install Visual Studio 2015."
    }

    Set-Alias msbuild $frameworkMsBuildLocation -Scope Script
}


function script:log([string]$message) {
    Write-Host -Foreground Green $message
}


function script:precheck([string]$command, [string]$missedMessage) {
    $c = Get-Command $command -ErrorAction SilentlyContinue
    if (-not $c) {
        Write-Warning $missedMessage
        return $false
    } else {
        return $true
    }
}


function script:Get-InvertedOrderedMap {
    param(
        $h
    )
    $res = [ordered]@{}
    foreach ($q in $h.GetEnumerator()) {
        if ($res.Contains($q.Value)) {
            throw "Cannot invert hashtable: duplicated key $($q.Value)"
        }

        $res[$q.Value] = $q.Key
    }
    return $res
}


## this function is from Dave Wyatt's answer on
## http://stackoverflow.com/questions/22002748/hashtables-from-convertfrom-json-have-different-type-from-powershells-built-in-h
function script:Convert-PSObjectToHashtable {
    param (
        [Parameter(ValueFromPipeline)]
        $InputObject
    )

    process {
        if ($null -eq $InputObject) { return $null }

        if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
            $collection = @(
                foreach ($object in $InputObject) { Convert-PSObjectToHashtable $object }
            )

            Write-Output -NoEnumerate $collection
        } elseif ($InputObject -is [psobject]) {
            $hash = @{}

            foreach ($property in $InputObject.PSObject.Properties)
            {
                $hash[$property.Name] = Convert-PSObjectToHashtable $property.Value
            }

            $hash
        } else {
            $InputObject
        }
    }
}

# this function wraps native command Execution
# for more information, read https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
function script:Start-NativeExecution([scriptblock]$sb)
{
    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Continue"
    try
    {
        & $sb
        # note, if $sb doens't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0)
        {
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    }
    finally
    {
        $script:ErrorActionPreference = $backupEAP
    }
}

function script:Get-StronglyTypeCsFileForResx
{
    param($xml, $ModuleName, $ClassName)

    # Example
    #
    # $ClassName = Full.Name.Of.The.ClassFoo
    # $shortClassName = ClassFoo
    # $namespaceName = Full.Name.Of.The

    $shortClassName = $ClassName
    $namespaceName = $null

    $lastIndexOfDot = $className.LastIndexOf(".")
    if ($lastIndexOfDot -ne -1)
    {
        $namespaceName = $className.Substring(0, $lastIndexOfDot)
        $shortClassName = $className.Substring($lastIndexOfDot + 1)
    }

$banner = @'
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a Start-ResGen funciton from build.psm1.
//     To add or remove a member, edit your .ResX file then rerun Start-ResGen.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

{0}
'@

$namespace = @'
namespace {0} {{
{1}
}}
'@

$body = @'
using System;
using System.Reflection;

/// <summary>
///   A strongly-typed resource class, for looking up localized strings, etc.
/// </summary>
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]

internal class {0} {{

    private static global::System.Resources.ResourceManager resourceMan;

    private static global::System.Globalization.CultureInfo resourceCulture;

    [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    internal {0}() {{
    }}

    /// <summary>
    ///   Returns the cached ResourceManager instance used by this class.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Resources.ResourceManager ResourceManager {{
        get {{
            if (object.ReferenceEquals(resourceMan, null)) {{
                global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("{1}.resources.{3}", typeof({0}).GetTypeInfo().Assembly);
                resourceMan = temp;
            }}
            return resourceMan;
        }}
    }}

    /// <summary>
    ///   Overrides the current thread's CurrentUICulture property for all
    ///   resource lookups using this strongly typed resource class.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Globalization.CultureInfo Culture {{
        get {{
            return resourceCulture;
        }}
        set {{
            resourceCulture = value;
        }}
    }}
    {2}
}}
'@

    $entry = @'

    /// <summary>
    ///   Looks up a localized string similar to {1}
    /// </summary>
    internal static string {0} {{
        get {{
            return ResourceManager.GetString("{0}", resourceCulture);
        }}
    }}
'@
    $entries = $xml.root.data | % {
        if ($_) {
            $val = $_.value.Replace("`n", "`n    ///")
            $name = $_.name.Replace(' ', '_')
            $entry -f $name,$val
        }
    } | Out-String
    
    $bodyCode = $body -f $shortClassName,$ModuleName,$entries,$ClassName

    if ($NamespaceName)
    {
        $bodyCode = $namespace -f $NamespaceName, $bodyCode
    }

    $resultCode = $banner -f $bodyCode

    return $resultCode -replace "`r`n?|`n","`r`n"
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

    if (1 -eq $packageVersionTokens.Count)
    {           
        # In case the input is of the form a.b.c, add a '0' at the end for revision field
       $packageVersion = $packageVersion + '.0'
    }
    elseif (1 -lt $packageVersionTokens.Count)
    {
        # We have all the four fields
       $packageVersion = $packageVersion + '.' + $packageVersionTokens[1]
    }

    return $packageVersion
}

function New-MSIPackage
{
    [CmdletBinding()]
    param (
    
        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $ProductName = 'PowerShell', 

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
        [string] $ProductWxsPath = (Join-Path $pwd '\assets\Product.wxs'),

        # Path to Assets folder containing artifacts such as icons, images
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $AssetsPath  

    )    

    $wixToolsetBinPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.10\bin"

    Write-Verbose "Ensure Wix Toolset is present on the machine @ $wixToolsetBinPath"
    if (-not (Test-Path $wixToolsetBinPath))
    {
        throw "Install Wix Toolset prior to running this script - https://wix.codeplex.com/downloads/get/1540240"
    }

    Write-Verbose "Initialize Wix executables - Heat.exe, Candle.exe, Light.exe"
    $wixHeatExePath = Join-Path $wixToolsetBinPath "Heat.exe"
    $wixCandleExePath = Join-Path $wixToolsetBinPath "Candle.exe"
    $wixLightExePath = Join-Path $wixToolsetBinPath "Light.exe"

    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion -Verbose
    
    $assetsInSourcePath = "$ProductSourcePath" + '\assets'
    New-Item $assetsInSourcePath -type directory -Force | Write-Verbose

    $assetsInSourcePath = Join-Path $ProductSourcePath 'assets'

    Write-Verbose "Place dependencies such as icons to $assetsInSourcePath" 
    Copy-Item "$AssetsPath\*.ico" $assetsInSourcePath -Force

    $productVersionWithName = $ProductName + "_" + $ProductVersion
    Write-Verbose "Create MSI for Product $productVersionWithName"

    [Environment]::SetEnvironmentVariable("ProductSourcePath", $ProductSourcePath, "Process")
    [Environment]::SetEnvironmentVariable("ProductName", $ProductName, "Process")
    [Environment]::SetEnvironmentVariable("ProductGuid", $ProductGuid, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersion", $ProductVersion, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersionWithName", $productVersionWithName, "Process")

    $wixFragmentPath = (Join-path $env:Temp "Fragment.wxs")
    $wixObjProductPath = (Join-path $env:Temp "Product.wixobj")
    $wixObjFragmentPath = (Join-path $env:Temp "Fragment.wixobj")
    
    $msiLocationPath = Join-Path $pwd "$productVersionWithName.msi"    
    Remove-Item -ErrorAction SilentlyContinue $msiLocationPath -Force

    & $wixHeatExePath dir  $ProductSourcePath -dr  $productVersionWithName -cg $productVersionWithName -gg -sfrag -srd -scom -sreg -out $wixFragmentPath -var env.ProductSourcePath -v | Write-Verbose
    & $wixCandleExePath  "$ProductWxsPath"  "$wixFragmentPath" -out (Join-Path "$env:Temp" "\\") -arch x64 -v | Write-Verbose
    & $wixLightExePath -out "$productVersionWithName.msi" $wixObjProductPath $wixObjFragmentPath -ext WixUIExtension -v | Write-Verbose
    
    Remove-Item -ErrorAction SilentlyContinue *.wixpdb -Force

    Write-Verbose "You can find the MSI @ $msiLocationPath"
    return $msiLocationPath
}

# Function to create an Appx package compatible with Windows 8.1 and above
function New-AppxPackage
{
    [CmdletBinding()]
    param (
    
        # Name of the Package
        [ValidateNotNullOrEmpty()]
        [string] $PackageName = 'PowerShell', 

        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageVersion,        

        # Source Path to the Binplaced Files
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $SourcePath,

        # Path to Assets folder containing Appx specific artifacts
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $AssetsPath        
    )
        
    $PackageVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $PackageVersion -Verbose
    Write-Verbose "Package Version is $PackageVersion"

    $win10sdkBinPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64"

    Write-Verbose "Ensure Win10 SDK is present on the machine @ $win10sdkBinPath"
    if (-not (Test-Path $win10sdkBinPath))
    {
        throw "Install Win10 SDK prior to running this script - https://go.microsoft.com/fwlink/p/?LinkID=698771"
    }

    Write-Verbose "Ensure Source Path is valid - $SourcePath"
    if (-not (Test-Path $SourcePath))
    {
        throw "Invalid SourcePath - $SourcePath"
    }

    Write-Verbose "Ensure Assets Path is valid - $AssetsPath"
    if (-not (Test-Path $AssetsPath))
    {
        throw "Invalid AssetsPath - $AssetsPath"
    }
    
    Write-Verbose "Initialize MakeAppx executable path"
    $makeappxExePath = Join-Path $win10sdkBinPath "MakeAppx.exe"

    $appxManifest = @"
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Identity Name="Microsoft.PowerShell" ProcessorArchitecture="x64" Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" Version="#VERSION#" />
  <Properties>
    <DisplayName>PowerShell</DisplayName>
    <PublisherDisplayName>Microsoft Corporation</PublisherDisplayName>
    <Logo>#LOGO#</Logo>
  </Properties>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.14257.0" MaxVersionTested="12.0.0.0" />
    <TargetDeviceFamily Name="Windows.Server" MinVersion="10.0.14257.0" MaxVersionTested="12.0.0.0" />
  </Dependencies>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
  <Applications>
    <Application Id="PowerShell" Executable="powershell.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="PowerShell" Description="PowerShell for every system" BackgroundColor="transparent" Square150x150Logo="#SQUARE150x150LOGO#" Square44x44Logo="#SQUARE44x44LOGO#">
      </uap:VisualElements>
    </Application>
  </Applications>
</Package>
"@

    $appxManifest = $appxManifest.Replace('#VERSION#', $PackageVersion)
    $appxManifest = $appxManifest.Replace('#LOGO#', 'Assets\Powershell_256.png')
    $appxManifest = $appxManifest.Replace('#SQUARE150x150LOGO#', 'Assets\Powershell_256.png')
    $appxManifest = $appxManifest.Replace('#SQUARE44x44LOGO#', 'Assets\Powershell_48.png')

    Write-Verbose "Place Appx Manifest in $SourcePath"
    $appxManifest | Out-File "$SourcePath\AppxManifest.xml" -Force
    
    $assetsInSourcePath = "$SourcePath" + '\Assets'
    New-Item $assetsInSourcePath -type directory -Force | Out-Null

    $assetsInSourcePath = Join-Path $SourcePath 'Assets'

    Write-Verbose "Place AppxManifest dependencies such as images to $assetsInSourcePath" 
    Copy-Item "$AssetsPath\*.png" $assetsInSourcePath -Force
    
    $appxPackageName = $PackageName + "_" + $PackageVersion
    $appxPackagePath = "$pwd\$appxPackageName.appx"
    Write-Verbose "Calling MakeAppx from $makeappxExePath to create the package @ $appxPackagePath"
    & $makeappxExePath pack /o /v /d $SourcePath  /p $appxPackagePath | Write-Verbose

    Write-Verbose "Clean-up Appx artifacts and Assets from $SourcePath"    
    Remove-Item $assetsInSourcePath -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$SourcePath\AppxManifest.xml" -Force -ErrorAction SilentlyContinue

    return $appxPackagePath
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
