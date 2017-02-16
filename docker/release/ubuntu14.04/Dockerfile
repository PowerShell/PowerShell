FROM ubuntu:trusty

ARG POWERSHELL_VERSION=6.0.0-alpha.15
ARG POWERSHELL_RELEASE=v6.0.0-alpha.15
ARG POWERSHELL_PACKAGE=powershell_6.0.0-alpha.15-1ubuntu1.14.04.1_amd64.deb

# Maintainer label and metadata as defined at http://label-schema.org
ARG VCS_REF
ARG BUILD_DATE
LABEL maintainer="Andrew Schwartzmeyer <andschwa@microsoft.com>" \
      org.label-schema.schema-version="1.0" \
      org.label-schema.vendor="Microsoft" \
      org.label-schema.name="PowerShell" \
      org.label-schema.version=$POWERSHELL_VERSION \
      org.label-schema.license="MIT" \
      org.label-schema.description="PowerShell is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with existing tools and is optimized for dealing with structured data, REST APIs, and object models. It includes a command-line shell, an associated scripting language and a framework for processing cmdlets." \
      org.label-schema.url="https://microsoft.com/powershell" \
      org.label-schema.usage="https://github.com/PowerShell/PowerShell/tree/master/docs/learning-powershell" \
      org.label-schema.build-date=$BUILD_DATE \
      org.label-schema.vcs-url="https://github.com/PowerShell/PowerShell.git" \
      org.label-schema.vcs-ref=$VCS_REF \
      org.label-schema.docker.cmd="docker run -it microsoft/powershell:ubuntu14.04"

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
