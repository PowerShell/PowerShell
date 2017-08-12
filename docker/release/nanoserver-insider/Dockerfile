# escape=`
# Args used by from statements must be defined here:
ARG NanoServerVersion=10.0.16257.1000
ARG WindowsServerCoreVersion=10.0.16257.1000
ARG WindowsServerCoreRepo=microsoft/windowsservercore-insider
ARG NanoServerRepo=microsoft/nanoserver-insider

# Use server core as an installer container to extract PowerShell,
# As this is a multi-stage build, this stage will eventually be thrown away
FROM ${WindowsServerCoreRepo}:$WindowsServerCoreVersion  AS installer-env

# Arguments for installing powershell, must be defined in the container they are used
ARG PS_VERSION=6.0.0-beta.5

ENV PS_DOWNLOAD_URL https://github.com/PowerShell/PowerShell/releases/download/v$PS_VERSION/PowerShell-$PS_VERSION-win10-win2016-x64.zip

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Invoke-WebRequest $Env:PS_DOWNLOAD_URL -OutFile powershell.zip

RUN Expand-Archive powershell.zip -DestinationPath \PowerShell

# Install PowerShell into NanoServer
FROM ${NanoServerRepo}:$NanoServerVersion

ARG VCS_REF="none"
ARG PS_VERSION=6.0.0-beta.5
ARG IMAGE_NAME=microsoft/nanoserver-insider-powershell

LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>"
LABEL readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL description="This Dockerfile will install the latest release of PS."
# Usage on URL to Usage
LABEL org.label-schema.usage="https://github.com/PowerShell/PowerShell/tree/master/docker#run-the-docker-image-you-built"
LABEL org.label-schema.url="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL org.label-schema.vcs-url="https://github.com/PowerShell/PowerShell"
# Image friendly name
LABEL org.label-schema.name="nanoserver-insider-powershell"
LABEL org.label-schema.vcs-ref=${VCS_REF}
LABEL org.label-schema.vendor="PowerShell"
LABEL org.label-schema.version=${PS_VERSION}
LABEL org.label-schema.schema-version="1.0"
LABEL org.label-schema.docker.cmd="docker run ${IMAGE_NAME} powershell -c '$psversiontable'"
LABEL org.label-schema.docker.cmd.devel="docker run ${IMAGE_NAME}"
LABEL org.label-schema.docker.cmd.test="docker run ${IMAGE_NAME} powershell -c Invoke-Pester"
LABEL org.label-schema.docker.cmd.help="docker run ${IMAGE_NAME} powershell -c Get-Help"
# Copy Powershell from the installer containter
ENV ProgramFiles C:\Program Files
COPY --from=installer-env ["\\PowerShell\\", "$ProgramFiles\\PowerShell"]

# Persist %PSCORE% ENV variable for user convenience
ENV PSCORE="$ProgramFiles\PowerShell\PowerShell.exe"

# Set the path
RUN setx PATH "%PATH%;%ProgramFiles%\PowerShell"

CMD ["PowerShell.exe"]
