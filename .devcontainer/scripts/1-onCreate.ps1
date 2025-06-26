#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# The workspace folder name can be variable, we need a stable target for some settings like terminal path
$absolutePath = '/powershell'
. $PSScriptRoot/shared.ps1

if ($PWD -ne $absolutePath) {
    log "Linking $SCRIPT:WorkspaceFolder to $absolutePath"
    sudo ln -s $SCRIPT:WorkspaceFolder /powershell
    log "Adding $absolutePath to git safe directories"
    git config --global --add safe.directory $absolutePath
}

log "Adding $SCRIPT:WorkspaceFolder to git safe directories"
git config --global --add safe.directory $SCRIPT:WorkspaceFolder


# NOTE: We override the Azure Devops private feed as it may not be up to date with the required packages
# This is only for development and not builds so any potential vulnerabilities will be caught at CI time
# If you want to restore private restore behavior for testing, perform the following:
# PS> dotnet nuget disable source nuget.org;dotnet nuget enable source powershell

#Because several PowerSHell build steps request you to use -UseNugetOrg, these files get changed when you do that. We ignore this in the codespaces so they do not get accidentally committed to PRs. You can always use --no-skip-worktree after build if you intentionally want to modify these files.
log 'Ignoring nuget.config changes'
git update-index --skip-worktree nuget.config src/Modules/nuget.config test/tools/Modules/nuget.config

log 'Switching to Nuget.Org Packages Only for Codespaces Development'
Import-Module ./build.psm1
Switch-PSNugetConfig -Source NuGetOnly
