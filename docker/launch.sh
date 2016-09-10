#!/usr/bin/env bash

set -e

if [[ -z "$FORK" ]]; then
    FORK=PowerShell
fi

if [[ -z "$BRANCH" ]]; then
    BRANCH=master
fi

# Build both sets by default
if [[ -z "$BUILDS" ]]; then
    BUILDS="stable unstable"
fi

# Build specified distributions
if [[ -z $DISTROS ]]; then
    DISTROS="ubuntu14.04 ubuntu16.04 centos7"
fi

for build in $BUILDS; do
    # cd so $distro is only the distro name
    cd $build
    for distro in $DISTROS; do
        image="$build/$distro"
        if [[ "$TEST" -eq 1 ]]; then
            echo "Testing $image"
            command="cd PowerShell; Import-Module ./build.psm1; Install-Dotnet -NoSudo; Start-PSPester -powershell powershell -Pester ./src/Modules/Shared/Pester"
            # clone repo for stable images because it's not already done
            if [[ "$build" = stable ]]; then
                command="git clone --recursive https://github.com/$FORK/PowerShell -b $BRANCH; $command"
            fi
            # run Pester tests inside container
            # RUNARGS can be set in the environment
            docker run -it $RUNARGS powershell/powershell:$build-$distro -c "$command"
        else
            echo "Building $image"
            # copy the common script because it lives outside the docker build context
            if [[ "$build" = unstable ]]; then
                cp bootstrap.ps1 $distro
                buildargs="--build-arg fork=$FORK --build-arg branch=$BRANCH"
            fi
            # build and tag the image so they can be derived from
            # BUILDARGS can be set in the environment
            docker build $buildargs $BUILDARGS -t powershell/powershell:$build-$distro $distro
        fi
    done
    cd ..
done
