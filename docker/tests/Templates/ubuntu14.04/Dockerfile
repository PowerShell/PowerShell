FROM ubuntu:trusty

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
        curl \
        apt-transport-https \
        locales \
        git

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN locale-gen $LANG && update-locale

RUN curl -L -o $PACKAGENAME $PACKAGELOCATION/$PACKAGENAME
RUN dpkg -i $PACKAGENAME || :
RUN apt-get install -y -f
RUN $TESTDOWNLOADCOMMAND
RUN pwsh$PREVIEWSUFFIX -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';\$null=New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLIST -PassThru).FailedCount"
