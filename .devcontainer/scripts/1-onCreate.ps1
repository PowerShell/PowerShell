#Docs: https://containers.dev/implementors/json_reference/#lifecycle-scripts
. $PSScriptRoot/shared.ps1

log "Adding $SCRIPT:WorkspaceFolder to git safe directories"
git config --global --add safe.directory $SCRIPT:WorkspaceFolder

# WorkspaceFolder might end up as a different path, so link it if it is not
# This is so any scripts that require a hardcoded path to the workspace folder can use it
$defaultWorkspaceFolder = '/powershell'
if (-not (Test-Path $defaultWorkspaceFolder)) {
    log "Linking $defaultWorkspaceFolder to $SCRIPT:WorkspaceFolder"
    & sudo ln -s $SCRIPT:WorkspaceFolder $defaultWorkspaceFolder
    git config --global --add safe.directory $defaultWorkspaceFolder
}

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

#Install latest .NET specified by the PowerShell global.json file. Start-PSBootstrap can do this but we want to ensure it goes into the devcontainer global directory.
Push-Location /tmp
try {
    & curl -LOs https://dot.net/v1/dotnet-install.sh
    & chmod +x dotnet-install.sh
    log 'Checking if PowerShell global.json dotnet SDK version is installed'
    & sudo ./dotnet-install.sh --install-dir /usr/share/dotnet --jsonfile $WorkspaceFolder/global.json
} finally {
    Pop-Location
}
