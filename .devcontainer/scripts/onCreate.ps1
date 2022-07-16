#Reference: https://code.visualstudio.com/docs/remote/devcontainerjson-reference#_lifecycle-scripts
Import-Module ./build.psm1
Start-PSBootstrap

# Link the bootstrapped dotnet to /usr/share/dotnet
# There are some build steps that still hardcode to this path improperly
& sudo ln -s $HOME/.dotnet /usr/share/dotnet

Restore-PSPackage
