FROM centos:7

ARG POWERSHELL_VERSION=6.0.0-alpha.15
ARG POWERSHELL_RELEASE=v6.0.0-alpha.15
ARG POWERSHELL_PACKAGE=powershell-6.0.0_alpha.15-1.el7.centos.x86_64.rpm

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
      org.label-schema.vcs-type="Git" \
      org.label-schema.docker.cmd="docker run -it microsoft/powershell:centos7"

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

# Install dependencies and clean up
RUN yum install -y \
        glibc \
        libcurl \
        ca-certificates \
        libgcc \
        libicu \
        openssl \
        libstdc++ \
        ncurses-base \
        libunwind \
        uuid \
        zlib \
        which \
        curl \
        git \
    && yum clean all

# Install PowerShell package and clean up
RUN curl -SLO https://github.com/PowerShell/PowerShell/releases/download/$POWERSHELL_RELEASE/$POWERSHELL_PACKAGE \
    && yum install -y $POWERSHELL_PACKAGE \
    && rm $POWERSHELL_PACKAGE

# Use array to avoid Docker prepending /bin/sh -c
ENTRYPOINT [ "powershell" ]
