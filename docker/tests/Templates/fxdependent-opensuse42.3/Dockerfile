FROM opensuse:42.3

ARG PACKAGENAME
ARG PACKAGELOCATION
ARG PREVIEWSUFFIX=
ARG TESTLIST=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG TESTDOWNLOADCOMMAND="git clone --recursive https://github.com/PowerShell/PowerShell.git"

# Install dependencies
RUN zypper --non-interactive update --skip-interactive \
    && zypper --non-interactive install \
        glibc-locale \
        glibc-i18ndata \
        tar \
        libunwind \
        libicu \
        openssl \
        git

# Install dotnet-runtime
ADD https://packages.microsoft.com/keys/microsoft.asc .
RUN rpmkeys --import microsoft.asc
ADD https://packages.microsoft.com/config/opensuse/42.2/prod.repo .
RUN mv prod.repo /etc/zypp/repos.d/microsoft-prod.repo
RUN zypper --non-interactive update --skip-interactive \
    && zypper --non-interactive install \
        dotnet-runtime-2.1

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
