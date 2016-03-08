param(
    # "" is for default generator on the platform
    [ValidateSet(
        "",
        "Visual Studio 12 2013", 
        "Visual Studio 12 2013 Win64",
        "Visual Studio 14 2015", 
        "Visual Studio 14 2015 Win64")]
    [string]$cmakeGenerator = "Visual Studio 12 2013",

    [ValidateSet(
        "Debug",
        "Release")] 
    [string]$msbuildConfiguration = "Release"   
)

$origPWD = $pwd
try
{
    $prechecks = $true    
    # check per-requests
    if (-not (get-command cmake -ErrorAction SilentlyContinue))
    {
        Write-Warning 'cmake not found. You can install it from https://chocolatey.org/packages/cmake.portable'
        $prechecks = $false
    }

    if (-not (get-command msbuild -ErrorAction SilentlyContinue))
    {
        Write-Warning 'msbuild not found. Install Visual Studio and add msbuild to $env:PATH'
        $prechecks = $false
    }

    if (-not (get-command dotnet -ErrorAction SilentlyContinue))
    {
        Write-Warning 'dotnet not found. Install it from http://dotnet.github.io/getting-started/'
        $prechecks = $false
    }

    if (-not $prechecks)
    {
        return
    }
    # end per-requests

    $BINFULL = "$pwd/binFull"
    $BUILD = "$pwd/build"

    mkdir $BINFULL -ErrorAction SilentlyContinue

    # Publish PowerShell
    cd src\Microsoft.PowerShell.ConsoleHost
    dotnet publish --framework net451 --output $BINFULL

    mkdir $build -ErrorAction SilentlyContinue
    cd $build

    if ($cmakeGenerator)
    {
        cmake -G "$cmakeGenerator" ..\src\powershell-native
    }
    else
    {
        cmake ..\src\powershell-native
    }
    
    msbuild powershell.vcxproj /p:Configuration=$msbuildConfiguration

    cp -rec $msbuildConfiguration\* $BINFULL
}
finally
{
    cd $origPWD
}
