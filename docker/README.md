# Docker

These DockerFiles enable running PowerShell in a container for each Linux distribution we support.

This requires an up-to-date version of Docker, such as 1.12.
It also expects you to be able to run Docker without `sudo`.
Please follow [Docker's official instructions][install] to install `docker` correctly.

[install]: https://docs.docker.com/engine/installation/

## Release

The release containers derive from the official distribution image,
such as `centos:7`, then install dependencies,
and finally install the PowerShell package.

These containers live at [hub.docker.com/r/microsoft/powershell][docker-release].

At about 440 megabytes, they are decently minimal,
with their size being the sum of the base image (200 megabytes)
plus the uncompressed package (120 megabytes),
and about 120 megabytes of .NET Core and bootstrapping dependencies.

[docker-release]: https://hub.docker.com/r/microsoft/powershell/

## Community

The docker files in the community folder were contributed by the community and are not yet officially supported.

## Examples

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
Copyright (c) Microsoft Corporation. All rights reserved.

PS /> Write-Host "Hello, World!"
Hello, World!
```

## Building the images

The images are built with the [`docker image build`](https://docs.docker.com/engine/reference/commandline/image_build/) command.

### Example

```sh
PS /powershell/docker> cd ./release/ubuntu16.04/
PS /powershell/docker/release/ubuntu16.04> docker image build -t ps-ubuntu.16.04 .
Sending build context to Docker daemon 3.072 kB
Step 1/12 : FROM ubuntu:xenial
 ---> 7b9b13f7b9c0
Step 2/12 : LABEL maintainer "PowerShell Team <powershellteam@hotmail.com>"
 ---> Using cache
 ---> c6515b7d596f
Step 3/12 : LABEL readme.md "https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
 ---> Using cache
 ---> 721306ae4490
Step 4/12 : LABEL description "This Dockerfile will install the latest release of PS."
 ---> Using cache
 ---> 80c06f5481d2
Step 5/12 : RUN apt-get update     && apt-get install -y --no-install-recommends         apt-utils         ca-certificates         curl         apt-transport-https         locales    && rm -rf /var/lib/apt/lists/*
 ---> Using cache
 ---> 2d08e2300fc9
Step 6/12 : ENV LANG en_US.UTF-8
 ---> Using cache
 ---> 6dfc363111c0
Step 7/12 : ENV LC_ALL $LANG
 ---> Using cache
 ---> b7ef2cd3a7ed
Step 8/12 : RUN locale-gen $LANG && update-locale
 ---> Using cache
 ---> e75306ddf3e0
Step 9/12 : RUN curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
 ---> Using cache
 ---> f476b7be22a2
Step 10/12 : RUN curl https://packages.microsoft.com/config/ubuntu/16.04/prod.list | tee /etc/apt/sources.list.d/microsoft.list
 ---> Using cache
 ---> 909ca8e33a3b
Step 11/12 : RUN apt-get update     && apt-get install -y --no-install-recommends     powershell
 ---> Using cache
 ---> f32b54204619
Step 12/12 : ENTRYPOINT powershell
 ---> Using cache
 ---> ee667ad86a7b
Successfully built ee667ad86a7b
```

### Run the docker image you built

```sh
PS /powershell/docker/release/ubuntu16.04> docker run -it ps-ubuntu.16.04 powershell -c '$psversiontable'

Name                           Value
----                           -----
PSVersion                      6.0.0-beta
PSEdition                      Core
BuildVersion                   3.0.0.0
CLRVersion
GitCommitId                    v6.0.0-beta.2
OS                             Linux 4.9.27-moby #1 SMP Thu May 11 04:01:18 ...
Platform                       Unix
PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0...}
PSRemotingProtocolVersion      2.3
SerializationVersion           1.1.0.1
WSManStackVersion              3.0

```

## NanoServer-Insider Release Notes

Please be sure to use a build from the Windows Insider program, either [Windows Server Insider](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewserver) or the [Windows 10 Insider](https://insider.windows.com/GettingStarted),
as your Container host before trying to pull this image. Otherwise, pulling this image will **fail**.

Read more about the changes coming to Nano Server in future releases of Windows Server Insider [here](https://docs.microsoft.com/en-us/windows-server/get-started/nano-in-semi-annual-channel).

### This is pre-release software

Windows Server Insider Preview builds may be substantially modified before they are commercially released. Microsoft makes no warranties, express or implied, with respect to the information provided here.
Some product features and functionality may require additional hardware or software. These builds are for testing purposes only. Microsoft is not obligated to provide any support services for this preview software.

For more information see [Using Insider Container Images](https://github.com/Microsoft/Virtualization-Documentation/blob/live/virtualization/windowscontainers/quick-start/Using-Insider-Container-Images.md)
and [Build and run an application with or without .NET Core 2.0 or PowerShell Core 6](https://github.com/Microsoft/Virtualization-Documentation/blob/live/virtualization/windowscontainers/quick-start/Nano-RS3-.NET-Core-and-PS.md).

### Known Issues

#### PowerShell Get only works with CurrentUser Scope

Due to [known issues with the nano-server-insider](https://github.com/Microsoft/Virtualization-Documentation/blob/live/virtualization/windowscontainers/quick-start/Insider-Known-Issues.md#build-16237),
you must specify `-Scope CurrentUser` when using `Install-Module`.  Example:

```PowerShell
Install-Module <ModuleName> -Scope CurrentUser
```

#### Docker run requires full path

> **Note:** this is fixed in `10.0.16257.1000` of the NanoServer-Insider build.  The powershell version of this should be released soon.

Due to [an issue with the container not picking up the path](https://github.com/Microsoft/Virtualization-Documentation/blob/live/virtualization/windowscontainers/quick-start/Insider-Known-Issues.md#build-16237), you must specify the path
when running a command on the command line.  For example, you would expect to be able to run:

```PowerShell
PS > docker run -it microsoft/nanoserver-insider-powershell powershell -c '$psversiontable'
```

but, in `nanoserver-insider-powershell` you must run:

```PowerShell
PS > docker run -it microsoft/nanoserver-insider-powershell 'C:\program files\powershell\powershell' -c '$psversiontable'
```
