FROM opensuse:42.2

ARG PSVERSIONSTUB
ARG PSVERSIONSTUBRPM
ARG PACKAGELOCATIONSTUB
ARG TESTLISTSTUB

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

RUN curl -L -o powershell-$PSVERSIONSTUB-linux-x64.tar.gz $PACKAGELOCATIONSTUB/powershell-$PSVERSIONSTUB-linux-x64.tar.gz

# Create the target folder where powershell will be placed
RUN mkdir -p /opt/microsoft/powershell/$PSVERSIONSTUB
# Expand powershell to the target folder
RUN tar zxf powershell-$PSVERSIONSTUB-linux-x64.tar.gz -C /opt/microsoft/powershell/$PSVERSIONSTUB

# Create the symbolic link that points to powershell
RUN ln -s /opt/microsoft/powershell/$PSVERSIONSTUB/pwsh $POWERSHELL_LINKFILE

RUN git clone --recursive https://github.com/PowerShell/PowerShell.git
RUN pwsh -c "Import-Module /PowerShell/build.psm1;exit (Invoke-Pester $TESTLISTSTUB -PassThru).FailedCount"
