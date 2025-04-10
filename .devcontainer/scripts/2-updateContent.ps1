#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
. $PSScriptRoot/shared.ps1

# Fetch upstream tags. The PowerShell dotnet restore requires this (GetPSCoreVersionFromGit)
foreach ($source in 'upstream', 'origin') {
    $remotes = git remote
    if ($remotes -contains $source) {
        Write-Host -Foreground Cyan "Fetching $source tags"
        git fetch $source --tags
    }
}

Import-Module ./build.psm1
log 'Bootstrap PowerShell Prerequisites'
& sudo pwsh -c {
    Import-Module ./build.psm1
    Start-PSBootstrap -Scenario DotNet
}

# Perform a build if Github Codespaces prebuild, otherwise optimize to start quickly
if ($ENV:CODESPACES) {
    $outputPath = Join-Path $SCRIPT:WorkspaceFolder 'debug'
    log "Prebuilding PowerShell for Codespaces to $outputPath"
    Start-PSBuild -UseNugetOrg -Output $outputPath

    log 'Prebuilding Tests'
    dotnet build test/xUnit
}
