FROM microsoft/powershell:centos7

LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>"
LABEL readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL description="This Dockerfile will install the latest release of PS."

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
RUN yum install -y PowerShell/powershell*.rpm