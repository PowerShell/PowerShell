FROM ubuntu:trusty
MAINTAINER Andrew Schwartzmeyer <andschwa@microsoft.com>

ARG POWERSHELL_RELEASE=v6.0.0-alpha.10
ARG POWERSHELL_PACKAGE=powershell_6.0.0-alpha.10-1ubuntu1.14.04.1_amd64.deb

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN locale-gen $LANG && update-locale

# Install dependencies and clean up
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libc6 \
        libcurl3 \
        ca-certificates \
        libgcc1 \
        libicu52 \
        libssl1.0.0 \
        libstdc++6 \
        libtinfo5 \
        libunwind8 \
        libuuid1 \
        zlib1g \
        curl \
        git \
    && rm -rf /var/lib/apt/lists/*

# Install PowerShell package and clean up
RUN curl -SLO https://github.com/PowerShell/PowerShell/releases/download/$POWERSHELL_RELEASE/$POWERSHELL_PACKAGE \
    && dpkg -i $POWERSHELL_PACKAGE \
    && rm $POWERSHELL_PACKAGE

# Use array to avoid Docker prepending /bin/sh -c
ENTRYPOINT [ "powershell" ]
