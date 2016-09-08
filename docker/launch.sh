#!/usr/bin/env bash

set -e

# Build both sets by default
if [[ -z "$BUILDS" ]]; then
    BUILDS="stable unstable"
fi

# Build specified distributions
if [[ -z $DISTROS ]]; then
    DISTROS="ubuntu14.04 ubuntu16.04"
fi

for build in $BUILDS; do
    # cd so $distro is only the distro name
    cd $build
    for distro in $DISTROS; do
        echo "Building $build/$distro"
        # copy the common script because it lives outside the docker build context
        if [[ "$build" = "unstable" ]]; then
            cp bootstrap.ps1 $distro
        fi
        # build and tag the image so they can be derived from
        docker build -t powershell/powershell:$build-$distro $distro
    done
    cd ..
done
