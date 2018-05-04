FROM debian:jessie

ARG PSVERSIONSTUB=6.0.2
ARG PACKAGELOCATIONSTUB
ARG TESTLISTSTUB=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG GITLOCATION=https://github.com/PowerShell/PowerShell.git

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

RUN curl -L -o powershell_$PSVERSIONSTUB-1.debian.8_amd64.deb $PACKAGELOCATIONSTUB/powershell_$PSVERSIONSTUB-1.debian.8_amd64.deb
RUN dpkg -i powershell_$PSVERSIONSTUB-1.debian.8_amd64.deb || :
RUN apt-get install -y -f
RUN git clone --recursive $GITLOCATION
RUN pwsh -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLISTSTUB -PassThru).FailedCount"
