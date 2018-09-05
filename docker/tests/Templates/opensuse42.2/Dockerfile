FROM opensuse:42.2

ARG PACKAGENAME
ARG PACKAGELOCATION
ARG PREVIEWSUFFIX=
ARG TESTLIST=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG TESTDOWNLOADCOMMAND="git clone --recursive https://github.com/PowerShell/PowerShell.git"

ARG POWERSHELL_LINKFILE=/usr/bin/pwsh

# Install dependencies
RUN zypper --non-interactive update --skip-interactive \
    && zypper --non-interactive install \
        glibc-locale \
        glibc-i18ndata \
        tar \
        curl \
        libunwind \
        libicu \
        openssl \
        git

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

RUN curl -L -o $PACKAGENAME $PACKAGELOCATION/$PACKAGENAME

# Create the target folder where powershell will be placed
RUN mkdir -p /opt/microsoft/powershell
# Expand powershell to the target folder
RUN tar zxf $PACKAGENAME -C /opt/microsoft/powershell

# Create the symbolic link that points to powershell
RUN ln -s /opt/microsoft/powershell/pwsh $POWERSHELL_LINKFILE

RUN $TESTDOWNLOADCOMMAND
RUN pwsh -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';\$null=New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLIST -PassThru).FailedCount"
