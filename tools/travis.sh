set -x
ulimit -n 4096
# Only build packages for branches, not pull requests
if [[ "$TRAVIS_PULL_REQUEST" == "false" ]]; then
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap -Package; Start-PSBuild -CrossGen; Start-PSPackage; Start-PSPester -ThrowOnFailure; Test-PSPesterResults; Start-PSxUnit"
else
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap; Start-PSBuild -CrossGen; Start-PSPester -ThrowOnFailure; Start-PSxUnit"
fi
