FROM centos:7

ARG PACKAGENAME
ARG PACKAGELOCATION
ARG PREVIEWSUFFIX=
ARG TESTLIST=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG TESTDOWNLOADCOMMAND="git clone --recursive https://github.com/PowerShell/PowerShell.git"

# Install dependencies
RUN yum install -y \
        curl \
        glibc-locale-source \
        git

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

RUN curl -L -o $PACKAGENAME $PACKAGELOCATION/$PACKAGENAME
RUN yum install -y $PACKAGENAME
RUN $TESTDOWNLOADCOMMAND
RUN pwsh$PREVIEWSUFFIX -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';\$null=New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLIST -PassThru).FailedCount"
