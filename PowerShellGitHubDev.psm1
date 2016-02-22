# TODO: use PowerShell to check OS when available
try {
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]
    $Linux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $OSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $Windows = $Runtime::IsOSPlatform($OSPlatform::Windows)
} catch [System.Management.Automation.RuntimeException] {
    $Linux = $false
    $OSX = $false
    $Windows = $true
}

function Start-PSBuild
{
    param(
            [switch]$Restore,
            [string]$Output = "$PSScriptRoot/bin"
         )

    if (-Not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        throw "Build dependency 'dotnet' not found in PATH! See: https://dotnet.github.io/getting-started/"
    }

    New-Item -Force -Type Directory $Output | Out-Null

    $Top = "$PSScriptRoot/src/Microsoft.PowerShell.Linux.Host"
    if ($Restore -Or -Not (Test-Path "$Top/project.lock.json")) {
        dotnet restore
    }

    if ($Linux -Or $OSX) {
        $InstallCommand = if ($Linux) { "apt-get" } elseif ($OSX) { "brew" }
        foreach ($Dependency in "cmake", "g++") {
            if (-Not (Get-Command $Dependency -ErrorAction SilentlyContinue)) {
                throw "Build dependency '$Dependency' not found in PATH! Run '$InstallCommand install $Dependency'"
            }
        }

        $Ext = if ($Linux) { "so" } elseif ($OSX) { "dylib" }
        $Lib = "src/libpsl-native.$Ext"

        Write-Host "Building $Lib"

        pushd "src/libpsl-native"
        cmake -DCMAKE_BUILD_TYPE=Debug .
        make -j
        ctest -V
        if (-Not (Test-Path $Lib)) { throw "Compilation of $Lib failed" }
        cp $Lib $Output
        popd
    }

    Write-Host "Building PowerShell"

    $Configuration =
        if ($Linux -Or $OSX) { "Linux" }
        elseif ($Windows) { "Debug" }

    dotnet publish -o $Output -c $Configuration -f "netstandardapp1.5" $Top
}

function Start-PSPackage
{
    param(
            [version]$Version = "0.1.0",
            [int]$Iteration = 1
         )

    if ($Windows) { throw "Building Windows packages is not yet supported!" }

    if (-Not (Get-Command "fpm" -ErrorAction SilentlyContinue)) {
        throw "Build dependency 'fpm' not found in PATH! See: https://github.com/jordansissel/fpm"
    }

    Start-PSBuild

    Write-Host "Change permissions for packaging"
    chmod -R go=u bin

    $Output = if ($Linux) { "deb" } elseif ($OSX) { "osxpkg" }

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
        --depends "libunwind8" `
        --depends "libicu52" `
        --deb-build-depends "dotnet" `
        --deb-build-depends "cmake" `
        --deb-build-depends "g++" `
        -t $Output `
        -s dir `
        -- "bin/=/usr/local/share/powershell/" "package/powershell=/usr/local/bin"
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
