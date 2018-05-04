FROM fedora:27

ARG PSVERSIONSTUB=6.0.2
ARG PACKAGELOCATIONSTUB
ARG TESTLISTSTUB=/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module
ARG GITLOCATION=https://github.com/PowerShell/PowerShell.git

# Install dependencies
RUN dnf install -y \
        curl \
        glibc-locale-source \
        git \
        compat-openssl10 \
    && dnf upgrade-minimal -y --security \
    && dnf clean all

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

RUN curl -L -o powershell-$PSVERSIONSTUB-1.rhel.7.x86_64.rpm $PACKAGELOCATIONSTUB/powershell-$PSVERSIONSTUB-1.rhel.7.x86_64.rpm
RUN dnf install -y powershell-$PSVERSIONSTUB-1.rhel.7.x86_64.rpm
RUN git clone --recursive $GITLOCATION
RUN pwsh -c "Import-Module /PowerShell/build.psm1;\$dir='/usr/local/share/powershell/Modules';New-Item -Type Directory -Path \$dir -ErrorAction SilentlyContinue;Restore-PSPester -Destination \$dir;exit (Invoke-Pester $TESTLISTSTUB -PassThru).FailedCount"
