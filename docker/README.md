Docker
======

These Dockerfiles enable building and running PowerShell in a container for each Linux distribution we support.
There are two sets: release and nightly.

This requires an up-to-date version of Docker, such as 1.12.
It also expects you to be able to run Docker without `sudo`.
Please follow [Docker's official instructions][install] to install `docker` correctly.

[install]: https://docs.docker.com/engine/installation/

Release
-------

The release containers derive from the official distribution image,
such as `centos:7`, then install dependencies,
and finally install the PowerShell package.

These containers live at [hub.docker.com/r/microsoft/powershell][docker-release].

At about 440 megabytes, they are decently minimal,
with their size being the sum of the base image (200 megabytes)
plus the uncompressed package (120 megabytes),
and about 120 megabytes of .NET Core and bootstrapping dependencies.

[docker-release]: https://hub.docker.com/r/microsoft/powershell/

Nightly
-------

The nightly containers derive from their respective release images,
such as `microsoft/powershell:centos7`,
then run the `bootstrap.ps1` script which clones the repository,
runs `Start-PSBootstrap -Package` to install building and packaging tools,
runs `Start-PSBuild -Crossgen -PSModuleRestore` to build PowerShell with native-image DLLs,
runs `Start-PSPackage` to generate the platform's package,
and finally installs the generated package.

These containers live at [hub.docker.com/r/microsoft/powershell-nightly][docker-nightly].

At about 4 gigabytes,
these images are *much* larger due to having all the tools installed.
This is intended so that the containers are useful for reproducing packages.

[docker-nightly]: https://hub.docker.com/r/microsoft/powershell-nightly/

Automation
==========

A script, `launch.sh`, exists to automatically build all the respective images.
It has a few configuration options,
passed in as environment variables:

* `FORK`: the fork to clone in nightly builds (default: `PowerShell`)
  * Set `FORK=andschwa` to clone https://github.com/andschwa/PowerShell

* `BRANCH`: the branch to checkout in nightly builds (default: `master`)
  * Set `BRANCH=feature-A` to checkout the `feature-A` branch

* `BUILDS`: the type of builds to run (default: `release nightly`)
  * This is order dependent! Release images must exist for nightly images to be built
  * Set `BUILDS=release` to run the script for just the release images

* `DISTROS`: the distributions of Linux to run (default: `ubuntu14.04 ubuntu16.04 centos7`)
  * Set `DISTROS=centos7` to build just CentOS 7 images
  * Combine with `BUILDS` to filter

* `TEST`: set to 1 to `docker run Start-PSPester` inside the containers
  * The containers *must* first exist; this skips the build step

* `BUILDARGS`: additional arguments to be passed to `docker build` during building
  * Set `BUILDARGS="--no-cache"` to rebuild and ignore cached layers

* `RUNARGS`: additional arguments to be passed to `docker run` during testing
  * Set `RUNARGS="--rm"` to automatically delete the test container when finished

For each build type (release and nightly),
the selected distributions will run in parallel.
The output is sent to log files in `docker/logs`.
For nightly builds, the generated packages are copied to `docker/packages`.

This script is very new, and there may be bugs.
Use `set -x` to see exactly what commands it is running.

Examples
--------

To run PowerShell from using a container:

```sh
$ docker run -it microsoft/powershell
Unable to find image 'microsoft/powershell:latest' locally
latest: Pulling from microsoft/powershell
cad964aed91d: Already exists
3a80a22fea63: Already exists
50de990d7957: Already exists
61e032b8f2cb: Already exists
9f03ce1741bf: Already exists
adf6ad28fa0e: Pull complete
10db13a8ca02: Pull complete
75bdb54ff5ae: Pull complete
Digest: sha256:92c79c5fcdaf3027626643aef556344b8b4cbdaccf8443f543303319949c7f3a
Status: Downloaded newer image for microsoft/powershell:latest
PowerShell
Copyright (C) 2016 Microsoft Corporation. All rights reserved.

PS /> Write-Host "Hello, World!"
Hello, World!
```

To build a CentOS container with the latest released package:

```sh
$ BUILDS=release DISTROS=centos7 ./launch.sh
Logging to docker/logs/release/centos7.log
Waiting for release containers to finish; tail the logs for more information.

$ docker images
REPOSITORY                 TAG                    IMAGE ID            CREATED             SIZE
microsoft/powershell       centos7                f0a11a8009b7        20 minutes ago      438.3 MB
...
```

To generate an Ubuntu 16.04 package from andschwa's `docker` branch:

```sh
$ DISTROS=ubuntu16.04 FORK=andschwa BRANCH=docker ./launch.sh
Logging to docker/logs/release/ubuntu16.04.log
Waiting for release containers to finish; tail the logs for more information.
Logging to docker/logs/nightly/ubuntu16.04.log
Waiting for nightly containers to finish; tail the logs for more information.

$ ls packages
powershell_6.0.0-alpha.10-310-g5ded651-1ubuntu1.16.04.1_amd64.deb

$ tail logs/nightly/ubuntu16.04.log
Step 5 : COPY bootstrap.ps1 /
 ---> Using cache
 ---> d18a28ff1e3d
Step 6 : RUN powershell -f bootstrap.ps1     && dpkg -i PowerShell/powershell*.deb
 ---> Using cache
 ---> 9cd8b1ef79b3
Successfully built 9cd8b1ef79b3
LOG: Saving package to docker/packages
~/src/PowerShell/docker ~/src/PowerShell/docker
'/PowerShell/powershell_6.0.0-alpha.10-310-g5ded651-1ubuntu1.16.04.1_amd64.deb' -> '/mnt/powershell_6.0.0-alpha.10-310-g5ded651-1ubuntu1.16.04.1_amd64.deb'
```
