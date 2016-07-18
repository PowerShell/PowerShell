ulimit -n 4096
# Only build packages for branches, not pull requests
if [[ $TRAVIS_PULL_REQUEST == false ]]; then
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap -Package; Start-PSBuild -Publish; Start-PSPackage; Start-PSPester; Start-PSxUnit"
else
    powershell -c "Import-Module ./build.psm1; Start-PSBootstrap; Start-PSBuild; Start-PSPester; Start-PSxUnit"
fi
