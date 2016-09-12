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
    # each distro can be done in parallel; but stable must be done before unstable
    for distro in $DISTROS; do
        logfile="$build-$distro.log"
        if [[ "$TEST" -eq 1 ]]; then logfile="test-$logfile"; fi
        mkdir -p logs
        logfile="logs/$logfile"
        echo "Logging to docker/$logfile"
        (
            image="powershell/powershell:$build-$distro"
            cd "$build"
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
            fi
        ) &>> "$logfile" &
    done
    echo "Waiting for $build containers to finish; tail the logs for more information."
    wait
done
