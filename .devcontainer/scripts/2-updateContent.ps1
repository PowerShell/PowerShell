#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
. $PSScriptRoot/shared.ps1

#If doing a clone into devcontainer, VSCode defaults to a shallow clone. The build process does not support shallow
#clones due to the use of git tag describe, so we must "unshallow" it if this occurs
if ((git rev-parse --is-shallow-repository) -ne 'false') {
    log 'Shallow Clone detected, this is not supported by the PowerShell build process. Unshallowing...'
    git fetch --unshallow
}

# Fetch upstream tags. The PowerShell dotnet restore requires this (GetPSCoreVersionFromGit)
Import-Module -Force ./build.psm1
Sync-PSTags -AddRemoteIfMissing

log 'Bootstrap PowerShell Build Prerequisites'
Import-Module -Force ./build.psm1
Start-PSBootstrap -Scenario DotNet

#Ping is needed for tests but is not included in the .NET SDK devcontainer and PSBootstrap doesn't cover it
log 'Installing iputils (for the ping utility)'
sudo apt install iputils-ping -y

# Perform a build if Github Codespaces prebuild, otherwise optimize to start quickly
if ($ENV:CODESPACES) {
    log "Prebuilding PowerShell for Codespaces to $outputPath"
    Start-PSBuild -UseNugetOrg -Clean
    log 'Prebuilding Tests'
    dotnet build test/xUnit test/Modules
}
