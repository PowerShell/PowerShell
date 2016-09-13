#!/usr/bin/env bash

set -e

# This is borrowed from https://github.com/dotnet/cli/blob/18456af5caeae44defc23ad5579c838c1fda3c3d/run.sh
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
    DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
    SOURCE="$(readlink "$SOURCE")"
    [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

# run from directory of launch.sh so artifacts are placed appropriately
pushd "$DIR"

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
    # each distro can be done in parallel; but stable must be done before unstable
    for distro in $DISTROS; do
        logfile="$build-$distro.log"
        if [[ "$TEST" -eq 1 ]]; then logfile="test-$logfile"; fi
        mkdir -p logs
        logfile="logs/$logfile"
        echo "Logging to docker/$logfile"
        (
            image="powershell/powershell:$build-$distro"
            pushd "$build"
            if [[ "$TEST" -eq 1 ]]; then
                echo "LOG: testing $image"
                command="cd PowerShell; Import-Module ./build.psm1; Install-Dotnet -NoSudo; Start-PSPester -powershell powershell -Pester ./src/Modules/Shared/Pester"
                # clone repo for stable images because it's not already done
                if [[ "$build" = stable ]]; then
                    command="git clone --recursive https://github.com/$FORK/PowerShell -b $BRANCH; $command"
                fi
                # run Pester tests inside container
                # RUNARGS can be set in the environment
                docker run $RUNARGS "$image" -c "$command"
            else
                echo "LOG: building $image"
                # copy the common script because it lives outside the docker build context
                if [[ "$build" = unstable ]]; then
                    cp bootstrap.ps1 "$distro"
                    buildargs="--build-arg fork=$FORK --build-arg branch=$BRANCH"
                fi
                # build and tag the image so they can be derived from
                # BUILDARGS can be set in the environment
                docker build $buildargs $BUILDARGS -t "$image" "$distro"
                if [[ "$build" = unstable ]]; then
                    echo "LOG: Saving package to docker/packages"
                    popd
                    mkdir -p packages
                    command='cp -vf /PowerShell/powershell*{deb,rpm} /mnt 2> /dev/null'
                    # override entrypoint to be bash so we can use globbing
                    docker run --rm --volume "$(pwd)/packages:/mnt" --entrypoint bash "$image" -c "$command"
                fi
            fi
        ) &>> "$logfile" &
    done
    echo "Waiting for $build containers to finish; tail the logs for more information."
    wait
done

popd
