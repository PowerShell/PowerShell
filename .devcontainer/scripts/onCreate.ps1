#Reference: https://code.visualstudio.com/docs/remote/devcontainerjson-reference#_lifecycle-scripts
$ErrorActionPreference = 'Stop'
Import-Module ./build.psm1

# If we are rebuilding the container, we probably want to reset our build environment too
Start-PSBuild -Clean

#Bootstrap initial requirements such as downloading the appropriate dotnet
Start-PSBootstrap

