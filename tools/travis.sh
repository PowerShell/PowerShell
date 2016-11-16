set -x
ulimit -n 4096
# do this for our daily build test run
if [[ "$TRAVIS_EVENT_TYPE" == "cron" ]]; then
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap; Start-PSBuild -CrossGen; Start-PSPester -Tag @('CI','Feature','Scenario') -ExcludeTag RequireAdminOnWindows; Start-PSxUnit"
# Only build packages for branches, not pull requests
elif [[ "$TRAVIS_PULL_REQUEST" == "false" ]]; then
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap -Package; Start-PSBuild -CrossGen; Start-PSPackage; Start-PSPester -ThrowOnFailure; Test-PSPesterResults; Start-PSxUnit"
else
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap; Start-PSBuild -CrossGen; Start-PSPester -ThrowOnFailure; Start-PSxUnit"
fi
