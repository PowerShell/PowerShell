FROM microsoft/powershell:fedora24
MAINTAINER Andrew Schwartzmeyer <andschwa@microsoft.com>
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL Description="This Dockerfile will install and build the latest release of PS."

ARG fork=PowerShell
ARG branch=master
SHELL ["powershell", "-command"]
RUN git clone --recursive https://github.com/$env:fork/PowerShell.git -b $env:branch; \
    Set-Location PowerShell; \
    Import-Module ./build.psm1; \
    Start-PSBootstrap -Package -NoSudo; \
    Start-PSBuild -Crossgen -PSModuleRestore; \
    Start-PSPackage;

SHELL ["/bin/sh", "-c"]
RUN dnf install -y PowerShell/powershell*.rpm
