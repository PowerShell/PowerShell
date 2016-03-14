# Use the .NET Core APIs to determine the current platform; if a runtime
# exception is thrown, we are on FullCLR, not .NET Core.
#
# TODO: import Microsoft.PowerShell.Platform instead
try {
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

    $IsCore = $true
    $IsLinux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $IsOSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $IsWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
} catch [System.Management.Automation.RuntimeException] {
    $IsCore = $false
    $IsLinux = $false
    $IsOSX = $false
    $IsWindows = $true
}

function Start-PSBuild
{
    [CmdletBinding(DefaultParameterSetName='CoreCLR')]
    param(
        [switch]$Restore,
        [switch]$Clean,
        [string]$Output,

        # These runtimes must match those in project.json
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("ubuntu.14.04-x64",
                     "centos.7.1-x64",
                     "win7-x64",
                     "win10-x64",
                     "osx.10.10-x64",
                     "osx.10.11-x64")]
        [Parameter(ParameterSetName='CoreCLR')]
        [string]$Runtime,

        [Parameter(ParameterSetName='FullCLR')]
        [switch]$FullCLR,

        [Parameter(ParameterSetName='FullCLR')]
        [string]$cmakeGenerator = "Visual Studio 12 2013",

        [Parameter(ParameterSetName='FullCLR')]
        [ValidateSet("Debug",
                     "Release")] 
        [string]$msbuildConfiguration = "Release"   
    )

    function precheck([string]$command, [string]$missedMessage)
    {
        $c = Get-Command $command -ErrorAction SilentlyContinue
        if (-not $c)
        {
            Write-Warning $missedMessage
            return $false
        }
        else 
        {
            return $true    
        }
    }

    function log([string]$message)
    {
        Write-Host -Foreground Green $message
    }

    # simplify ParameterSetNames, set output
    if ($PSCmdlet.ParameterSetName -eq 'FullCLR')
    {
        $FullCLR = $true
    }

    if (-not $Output)
    {
        if ($FullCLR) { $Output = "$PSScriptRoot/binFull" } else { $Output = "$PSScriptRoot/bin" }
    }    

    # verify we have all tools in place to do the build
    $precheck = precheck 'dotnet' "Build dependency 'dotnet' not found in PATH! See: https://dotnet.github.io/getting-started/"
    if ($FullCLR)
    {
        # cmake is needed to build powershell.exe
        $precheck = $precheck -and (precheck 'cmake' 'cmake not found. You can install it from https://chocolatey.org/packages/cmake.portable')
        # msbuild is needed to build powershell.exe
        $precheck = $precheck -and (precheck 'msbuild' 'msbuild not found. Install Visual Studio and add msbuild to $env:PATH')
    }
    
    if (-not $precheck) { return }

    # handle clean
    if ($Clean) {
        Remove-Item -Force -Recurse $Output -ErrorAction SilentlyContinue
    }

    New-Item -Force -Type Directory $Output | Out-Null

    # define key build variables
    if ($FullCLR) 
    {
        $Top = "$PSScriptRoot\src\Microsoft.PowerShell.ConsoleHost"
        $framework = 'net451'
    }
    else 
    {
        $Top = "$PSScriptRoot/src/Microsoft.PowerShell.Linux.Host"   
        $framework = 'netstandardapp1.5'
    }

    # handle Restore
    if ($Restore -Or -Not (Test-Path "$Top/project.lock.json")) {
        log "Run dotnet restore"
        # restore is genuinely verbose.
        # we don't show it by default to keep CI build log size small
        if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent)
        {
            dotnet restore $PSScriptRoot
        }
        else 
        {
            dotnet restore $PSScriptRoot > $null    
        }
    }

    # Build native components
    if (-not $FullCLR)
    {
        if ($IsLinux -Or $IsOSX) {
            log "Start building native components"
            $InstallCommand = if ($IsLinux) { "apt-get" } elseif ($IsOSX) { "brew" }
            foreach ($Dependency in "cmake", "g++") {
                if (-Not (Get-Command $Dependency -ErrorAction SilentlyContinue)) {
                    throw "Build dependency '$Dependency' not found in PATH! Run '$InstallCommand install $Dependency'"
                }
            }

            $Ext = if ($IsLinux) { "so" } elseif ($IsOSX) { "dylib" }
            $Native = "$PSScriptRoot/src/libpsl-native"
            $Lib = "$Native/src/libpsl-native.$Ext"
            Write-Verbose "Building $Lib"

            try {
                Push-Location $Native
                cmake -DCMAKE_BUILD_TYPE=Debug .
                make -j
                make test
            } finally {
                Pop-Location
            }

            if (-Not (Test-Path $Lib)) { throw "Compilation of $Lib failed" }
            Copy-Item $Lib $Output
        }
    }
    else
    {
        log "Start building native powershell.exe"
        $build = "$PSScriptRoot/build"
        if ($Clean) {
            Remove-Item -Force -Recurse $build -ErrorAction SilentlyContinue
        }

        mkdir $build -ErrorAction SilentlyContinue
        try
        {
            Push-Location $build

            if ($cmakeGenerator)
            {
                cmake -G $cmakeGenerator ..\src\powershell-native
            }
            else
            {
                cmake ..\src\powershell-native
            }
            msbuild powershell.vcxproj /p:Configuration=$msbuildConfiguration
            cp -rec $msbuildConfiguration\* $Output
        }
        finally { Pop-Location }
    }

    log "Building PowerShell"
    $Arguments = "--framework", $framework, "--output", $Output
    if ($IsLinux -Or $IsOSX) { $Arguments += "--configuration", "Linux" }
    if ($Runtime) { $Arguments += "--runtime", $Runtime }
    
    if ($FullCLR) 
    {
        # there is a problem with code signing: 
        # AssemblyKeyFileAttribute file path cannot be correctly located, if `dotnet publish $TOP` syntax is used
        # we workaround it with calling `dotnet publish` from $TOP directory instead.
        Push-Location $Top
    }
    else 
    {
        $Arguments += $Top
    }

    Write-Verbose "Run dotnet publish $Arguments from $pwd"

    # this try-finally is part of workaround about AssemblyKeyFileAttribute issue
    try 
    {
        dotnet publish $Arguments
    }
    finally
    {
        if ($FullCLR)  { Pop-Location }
    }
}

function Start-PSPackage
{
    # PowerShell packages use Semantic Versioning http://semver.org/
    #
    # Ubuntu and OS X packages are supported.
    param(
        [string]$Version,
        [int]$Iteration = 1,
        [ValidateSet("deb", "osxpkg", "rpm")]
        [string]$Type
    )

    if ($IsWindows) { throw "Building Windows packages is not yet supported!" }

    if (-Not (Get-Command "fpm" -ErrorAction SilentlyContinue)) {
        throw "Build dependency 'fpm' not found in PATH! See: https://github.com/jordansissel/fpm"
    }

    if (-Not(Test-Path "$PSScriptRoot/bin/powershell")) {
        throw "Please Start-PSBuild with the corresponding runtime for the package"
    }

    # Change permissions for packaging
    chmod -R go=u "$PSScriptRoot/bin"

    # Decide package output type
    if (-Not($Type)) {
        $Type = if ($IsLinux) { "deb" } elseif ($IsOSX) { "osxpkg" }
        Write-Warning "-Type was not specified, continuing with $Type"
    }

    # Use Git tag if not given a version
    if (-Not($Version)) {
        $Version = (git --git-dir="$PSScriptRoot/.git" describe) -Replace '^v'
    }

    $libunwind = switch ($Type) {
        "deb" { "libunwind8" }
        "rpm" { "libunwind" }
    }

    $libicu = switch ($Type) {
        "deb" { "libicu52" }
        "rpm" { "libicu" }
    }

    # Build package
    fpm --force --verbose `
        --name "powershell" `
        --version $Version `
        --iteration $Iteration `
        --maintainer "Andrew Schwartzmeyer <andschwa@microsoft.com>" `
        --vendor "Microsoft <mageng@microsoft.com>" `
        --url "https://github.com/PowerShell/PowerShell" `
        --license "Unlicensed" `
        --description "Open PowerShell on .NET Core\nPowerShell is an open-source, cross-platform, scripting language and rich object shell. Built upon .NET Core, it is also a C# REPL.\n" `
        --category "shells" `
        --depends $libunwind `
        --depends $libicu `
        --deb-build-depends "dotnet" `
        --deb-build-depends "cmake" `
        --deb-build-depends "g++" `
        -t $Type `
        -s dir `
        "$PSScriptRoot/bin/=/usr/local/share/powershell/" `
        "$PSScriptRoot/package/powershell=/usr/local/bin"
}

function Start-DevPSGitHub
{
    param(
        [switch]$ZapDisable,
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = "$PSScriptRoot\binFull",
        [switch]$NoNewWindow
    )

    try
    {
        if ($LoadProfile -eq $false)
        {
            $ArgumentList = @('-noprofile') + $ArgumentList
        }

        $env:DEVPATH = $binDir
        if ($ZapDisable)
        {
            $env:COMPLUS_ZapDisable = 1
        }

        if (-Not (Test-Path $binDir\powershell.exe.config))
        {
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
    }
    finally
    {
        ri env:DEVPATH
        if ($ZapDisable)
        {
            ri env:COMPLUS_ZapDisable
        }
    }
}
