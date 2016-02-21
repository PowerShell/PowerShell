function Start-PSBuild
{
    param(
            [switch]$Restore,
            [string]$Output = "$PSScriptRoot/bin"
         )

    if (-Not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        throw "Build dependency 'dotnet' not found in PATH! See: https://dotnet.github.io/getting-started/"
    }

    $Top = "$PSScriptRoot/src/Microsoft.PowerShell.Linux.Host"
    if ($Restore -Or -Not (Test-Path "$Top/project.lock.json")) {
        dotnet restore
    }

    # TODO: use PowerShell to check OS when available
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]
    $Linux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $OSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $Windows = $Runtime::IsOSPlatform($OSPlatform::Windows)

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
