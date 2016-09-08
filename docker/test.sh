#!/usr/bin/env bash

set -e

# Build specified distributions
if [[ -z $DISTROS ]]; then
    DISTROS="ubuntu14.04 ubuntu16.04 centos7"
fi

build=unstable
command="cd PowerShell; Import-Module ./build.psm1; Start-PSPester"

cd $build
for distro in $DISTROS; do
    echo "Testing $build/$distro"
    # Run Pester tests inside container
    docker run -it powershell/powershell:$build-$distro powershell -c "$command"
done
cd ..
