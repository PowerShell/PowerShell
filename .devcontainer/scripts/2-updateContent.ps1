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
log 'Syncing PowerShell Git Tags from Remote'
Sync-PSTags -AddRemoteIfMissing

log 'Bootstrap PowerShell Build Prerequisites'

Start-PSBootstrap -Scenario DotNet

#Ping is needed for tests but is not included in the .NET SDK devcontainer and PSBootstrap doesn't cover it
log 'Installing iputils (for the ping utility)'
sudo apt install iputils-ping -y

# Perform an initial build of PowerShell, this is needed so the "pwsh dev" terminal and debug launch tasks are available on first run
log "Building PowerShell"
Start-PSBuild -UseNugetOrg -Clean -PublishLinkPath $SCRIPT:WorkspaceFolder/debug

# Prebuild more if in a codespace, otherwise leave this to the user to optimize local startup time
if ($ENV:CODESPACES) {
    log 'Prebuilding Tests'
    dotnet build test/xUnit
    log 'Fetching Pester for Tests'
    Restore-PSPester
    log 'Build Testing Tools'
    Publish-PSTestTools
    Publish-CustomConnectionTestModule
}
