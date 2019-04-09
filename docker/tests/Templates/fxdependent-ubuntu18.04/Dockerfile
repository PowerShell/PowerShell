FROM microsoft/dotnet:2.1.7-runtime-bionic

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
        git

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
