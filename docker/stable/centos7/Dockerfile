FROM centos:7
MAINTAINER Andrew Schwartzmeyer <andschwa@microsoft.com>

ARG POWERSHELL_RELEASE=v6.0.0-alpha.10
ARG POWERSHELL_PACKAGE=powershell-6.0.0_alpha.10-1.el7.centos.x86_64.rpm

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
