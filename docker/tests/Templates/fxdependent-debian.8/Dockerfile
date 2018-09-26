FROM debian:jessie

ARG PACKAGENAME
ARG PACKAGELOCATION
ARG PREVIEWSUFFIX=
ARG TESTLIST=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG TESTDOWNLOADCOMMAND="git clone --recursive https://github.com/PowerShell/PowerShell.git"

# Install dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        apt-utils \
        ca-certificates \
        apt-transport-https \
        locales \
        git \
        wget

# Install dotnet-runtime
RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
RUN mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
ADD https://packages.microsoft.com/config/debian/8/prod.list .
RUN mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        dotnet-runtime-2.1 

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN locale-gen $LANG && update-locale

# Install PowerShell package
ADD $PACKAGELOCATION/$PACKAGENAME .
RUN mkdir -p /opt/microsoft/powershell
RUN tar zxf $PACKAGENAME -C /opt/microsoft/powershell

# Download and run tests
RUN $TESTDOWNLOADCOMMAND
RUN dotnet /opt/microsoft/powershell/pwsh.dll -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';\$null=New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLIST -PassThru).FailedCount"
