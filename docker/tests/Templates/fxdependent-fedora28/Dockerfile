FROM fedora:28

ARG PACKAGENAME
ARG PACKAGELOCATION
ARG PREVIEWSUFFIX=
ARG TESTLIST=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG TESTDOWNLOADCOMMAND="git clone --recursive https://github.com/PowerShell/PowerShell.git"

# Install dependencies
RUN dnf install -y \
        glibc-locale-source \
        git \
        compat-openssl10 \
    && dnf upgrade-minimal -y --security \
    && dnf clean all

# Install dotnet-runtime
RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc
ADD https://packages.microsoft.com/config/fedora/27/prod.repo .
RUN mv prod.repo /etc/yum.repos.d/microsoft-prod.repo
RUN dnf install -y \
        dotnet-runtime-2.1 \
    && dnf upgrade-minimal -y --security \
    && dnf clean all

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

# Install PowerShell package
ADD $PACKAGELOCATION/$PACKAGENAME .
RUN mkdir -p /opt/microsoft/powershell
RUN tar zxf $PACKAGENAME -C /opt/microsoft/powershell

# Download and run tests
RUN $TESTDOWNLOADCOMMAND
RUN dotnet /opt/microsoft/powershell/pwsh.dll -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';\$null=New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLIST -PassThru).FailedCount"
