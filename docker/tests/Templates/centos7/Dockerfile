FROM centos:7

ARG PSVERSIONSTUB=6.0.1
ARG PSVERSIONSTUBRPM=6.0.1
ARG PACKAGELOCATIONSTUB
ARG TESTLISTSTUB=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG GITLOCATION=https://github.com/PowerShell/PowerShell.git

# Install dependencies
RUN yum install -y \
        curl \
        glibc-locale-source \
        git

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

RUN curl -L -o powershell-$PSVERSIONSTUBRPM-1.rhel.7.x86_64.rpm $PACKAGELOCATIONSTUB/powershell-$PSVERSIONSTUBRPM-1.rhel.7.x86_64.rpm
RUN yum install -y powershell-$PSVERSIONSTUBRPM-1.rhel.7.x86_64.rpm
RUN git clone --recursive $GITLOCATION
RUN pwsh -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLISTSTUB -PassThru).FailedCount"
