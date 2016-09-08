# This is intended to be used inside Docker containers

git clone --recursive https://github.com/$env:fork/PowerShell.git -b $env:branch
Set-Location PowerShell
Import-Module ./build.psm1
Start-PSBootstrap -Package -NoSudo
Start-PSBuild -Crossgen
Start-PSPackage
