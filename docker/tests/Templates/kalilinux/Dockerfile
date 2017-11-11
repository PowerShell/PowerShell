FROM kalilinux/kali-linux-docker:latest

ARG PSVERSIONSTUB
ARG PSVERSIONSTUBRPM
ARG PACKAGELOCATIONSTUB
ARG TESTLISTSTUB

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

RUN curl -L -o libssl1.0.0_1.0.1t-1+deb8u7_amd64.deb http://security.debian.org/debian-security/pool/updates/main/o/openssl/libssl1.0.0_1.0.1t-1+deb8u7_amd64.deb
RUN curl -L -o powershell_$PSVERSIONSTUB-1.ubuntu.16.04_amd64.deb $PACKAGELOCATIONSTUB/powershell_$PSVERSIONSTUB-1.ubuntu.16.04_amd64.deb
RUN dpkg -i libssl1.0.0_1.0.1t-1+deb8u7_amd64.deb || :
RUN dpkg -i powershell_$PSVERSIONSTUB-1.ubuntu.16.04_amd64.deb || :
RUN apt-get install -y -f
RUN git clone --recursive https://github.com/PowerShell/PowerShell.git
RUN pwsh -c "Import-Module /PowerShell/build.psm1;exit (Invoke-Pester $TESTLISTSTUB -PassThru).FailedCount"
