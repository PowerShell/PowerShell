function log ([string[]]$message) {
    $message = $message -join ' '
    Write-Host -ForegroundColor Cyan $message
}

#If WorkspaceMount is ever changed in the devcontainer, this requires an update
[string]$SCRIPT:WorkspaceFolder = Get-Content -Raw $PSScriptRoot/../devcontainer.json
| ConvertFrom-Json
| Select-Object -ExpandProperty workspacefolder

#Github Codespaces default
$WorkspaceFolder ??= '/workspaces/PowerShell'

# Suppresses ANSI Output in Codespaces Build Output
# $env:CODESPACES is a "magic variable" in the codespaces CI that can be used to detect it
if ($env:CODESPACES) {
    [Environment]::SetEnvironmentVariable('TERM', 'dumb', [EnvironmentVariableTarget]::User)
    [Environment]::SetEnvironmentVariable('TERM', 'dumb', [EnvironmentVariableTarget]::Process)
    $ENV:TERM = 'dumb'
    $ENV:NO_COLOR = $true
    $ENV:DOTNET_CLI_CONTEXT_ANSI_PASS_THRU = $false
    $ENV:DOTNET_CLI_CONTEXT_VERBOSE = $false
    $PSStyle.OutputRendering = 'PlainText'
}
