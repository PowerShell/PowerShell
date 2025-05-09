#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
. $PSScriptRoot/shared.ps1

#If doing a clone into devcontainer, VSCode defaults to a shallow clone. The build process does not support shallow
#clones due to the use of git tag describe, so we must "unshallow" it if this occurs
if ([bool](git rev-parse --is-shallow-repository)) {
    log 'Shallow Clone detected, this is not supported by the PowerShell build process. Unshallowing...'
    git fetch --unshallow
}

# Fetch upstream tags. The PowerShell dotnet restore requires this (GetPSCoreVersionFromGit)
Import-Module -Force ./build.psm1
Sync-PSTags -AddRemoteIfMissing

log 'Bootstrap PowerShell Prerequisites'
& sudo pwsh -c {
    Import-Module -Force ./build.psm1
    Start-PSBootstrap -Scenario DotNet
}

# Perform a build if Github Codespaces prebuild, otherwise optimize to start quickly
if ($ENV:CODESPACES) {
    $outputPath = Join-Path $SCRIPT:WorkspaceFolder 'debug'
    log "Prebuilding PowerShell for Codespaces to $outputPath"
    Start-PSBuild -UseNugetOrg -Clean -Output $outputPath

    log 'Prebuilding Tests'
    dotnet build test/xUnit test/Modules
}
