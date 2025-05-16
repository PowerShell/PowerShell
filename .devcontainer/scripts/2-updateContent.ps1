#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

. $PSScriptRoot/shared.ps1
Import-Module -Force ./build.psm1

#If doing a clone into devcontainer, VSCode defaults to a shallow clone. The build process does not support shallow
#clones due to the use of git tag describe, so we must "unshallow" it if this occurs
if ((git rev-parse --is-shallow-repository) -ne 'false') {
    log 'Shallow Clone detected, this is not supported by the PowerShell build process. Unshallowing...'
    git fetch --unshallow
}

# Fetch upstream tags. The PowerShell dotnet restore requires this (GetPSCoreVersionFromGit)
Sync-PSTags -AddRemoteIfMissing

log 'Bootstrap PowerShell Build Prerequisites'

Start-PSBootstrap -Scenario DotNet

#Ping is needed for tests but is not included in the .NET SDK devcontainer and PSBootstrap doesn't cover it
log 'Installing iputils (for the ping utility)'
sudo apt install iputils-ping -y

# Perform a build if Github Codespaces prebuild, otherwise optimize to start quickly
if ($ENV:CODESPACES) {
    log "Prebuilding PowerShell for Codespaces to $outputPath"
    Start-PSBuild -UseNugetOrg -Clean
    log 'Prebuilding Tests'
    dotnet build test/xUnit
    log 'Fetching Pester for Tests'
    Restore-PSPester
    log 'Build Testing Tools'
    Publish-PSTestTools
    Publish-CustomConnectionTestModule
}

#Create a symbolic link from "debug" in the root folder to the publish folder for convenience.
#This is required because some settings like terminal.integrated.profiles.linux cannot use variables like ${workspaceFolder} so we need an absolute path.
$publishPath = Split-Path (get-psoptions -DefaultToNew).Output
if (-not $publishPath) {
    New-Item -Path $publishPath -ItemType Directory -Force | Out-Null
}
$debugPath = Join-Path $PWD 'debug'
if (-not (
    (Test-Path $debugPath) -and
    ((Get-Item $debugPath).LinkTarget -eq $publishPath)
    )) {
    #Remove the old link if it exists
    if (Test-Path $debugPath) {
        log "Removing existing debug symbolic link at $debugPath"
        Remove-Item $debugPath -Force
    }
    log "Creating convenience link from $debugPath to $publishPath"
    ln -s $publishPath $debugPath
}
