FROM centos:7

ARG PACKAGENAME
ARG PACKAGELOCATION
ARG PREVIEWSUFFIX=
ARG TESTLIST=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG TESTDOWNLOADCOMMAND="git clone --recursive https://github.com/PowerShell/PowerShell.git"

# Install dependencies
RUN yum install -y \
        glibc-locale-source \
        git

# Install dotnet-runtime
RUN rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
RUN yum install -y \
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
