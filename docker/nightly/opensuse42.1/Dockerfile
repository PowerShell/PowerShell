# Docker image file that describes an OpenSUSE 42.1 (AKA leap 42.1) image with PowerShell installed from PowerShell Release
# TODO update to use microsoft image when available
FROM travisez13/powershell:opensuse42.1

LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>"
LABEL readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL description="This Dockerfile will install the latest release of PS."

ARG fork=PowerShell
ARG branch=master

# Update, Install packages to generate localedef and CURL which is used by RPM
# add the Microsoft key as trusted to install packgages using RPM
# Install PowerShell then clean up
SHELL ["/bin/sh", "-c"]
RUN zypper --non-interactive update --skip-interactive \
    && zypper --non-interactive install \
        git \
    && zypper --non-interactive clean --all

SHELL ["powershell", "-command"]
RUN Write-Verbose "cloning $env:fork - $env:branch" -Verbose; \
    git clone --recursive https://github.com/$env:fork/PowerShell.git -b $env:branch; \
    Set-Location PowerShell; \
    git remote add tagsource https://github.com/powershell/powershell.git; \
    git fetch --tags tagsource; \
    Import-Module ./build.psm1; \
    Start-PSBootstrap -Package -NoSudo; \
    Start-PSBuild -Crossgen -PSModuleRestore; \
    Start-PSPackage;

SHELL ["/bin/sh", "-c"]
COPY test.txt test.txt
RUN zypper --non-interactive install PowerShell/powershell*.rpm \
    && zypper --non-interactive clean --all

ENTRYPOINT [ "powershell" ]
