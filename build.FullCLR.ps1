$origPWD = $pwd
try
{
    $BINFULL = "$pwd/binFull"
    $BUILD = "$pwd/build"

    mkdir $BINFULL -ErrorAction SilentlyContinue

    # Publish PowerShell
    cd src\Microsoft.PowerShell.ConsoleHost
    dotnet publish --framework dnx451 --output $BINFULL

    cd ..\..\src\Microsoft.PowerShell.Commands.Utility
    dotnet publish --framework dnx451 --output $BINFULL

    cd ..\..\src\Microsoft.PowerShell.Commands.Management
    dotnet publish --framework dnx451 --output $BINFULL

    # build native host
    mkdir $build -ErrorAction SilentlyContinue
    cd $build

    cmake ..\src\powershell-native
    msbuild powershell.sln

    cp -rec Debug\* $BINFULL
}
finally
{
    cd $origPWD
}
