#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

. $PSScriptRoot/shared.ps1
Import-Module -Force ./build.psm1

#Create a symbolic link from "debug" in the root folder to the publish folder for convenience.
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
    New-Item -ItemType SymbolicLink -Value $publishPath -Path $debugPath
}
