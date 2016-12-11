FROM fedora:24
MAINTAINER Andrew Schwartzmeyer <andschwa@microsoft.com>

# This release Dockerfile uses the CentOS 7 build for the purposes of enabling
# nightlies

ARG POWERSHELL_VERSION=6.0.0-alpha.13
ARG POWERSHELL_RELEASE=v6.0.0-alpha.13
ARG POWERSHELL_PACKAGE=powershell-6.0.0_alpha.13-1.el7.centos.x86_64.rpm

# The CentOS 7 release relies on an older version of libicu, so we'll be downloading it
# from the University of Kent (GB) mirror service.
ARG LIBICU50_SOURCE=https://www.mirrorservice.org/sites/mirror.centos.org/7.2.1511/os/x86_64/Packages
ARG LIBICU50_PACKAGE=libicu-50.1.2-15.el7.x86_64.rpm
ARG LIBICU50_PACKAGE_MD5=c3c1ebaabc8d1619377d535698784953

# Install the English language pack first

RUN dnf install -y glibc glibc-langpack-en glibc-locale-source

ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

# Disable Delta RPMS
RUN echo "deltarpm=False" >> /etc/dnf/dnf.conf

# Install dependencies and clean up
RUN dnf install -y \
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
        cpio \
    && dnf update -y \
    && dnf clean all

# Install PowerShell package
RUN curl -SLO https://github.com/PowerShell/PowerShell/releases/download/$POWERSHELL_RELEASE/$POWERSHELL_PACKAGE \
    && dnf install -y $POWERSHELL_PACKAGE \
    && rm $POWERSHELL_PACKAGE

# Once the next release of PowerShell is done, this libicu50 & LD_LIBRARY_PATH hackery can be dropped.
RUN curl -SLO ${LIBICU50_SOURCE}/${LIBICU50_PACKAGE} \
  && md5sum $LIBICU50_PACKAGE | grep $LIBICU50_PACKAGE_MD5  \
  && cd /opt/microsoft/powershell/$POWERSHELL_VERSION \
  && rpm2cpio /$LIBICU50_PACKAGE | cpio -idmv \
  && rm /$LIBICU50_PACKAGE

# Append the old library to LD_LIBRARY_PATH

ENV LD_LIBRARY_PATH /opt/microsoft/powershell/$POWERSHELL_VERSION/usr/lib64

# Use array to avoid Docker prepending /bin/sh -c
ENTRYPOINT [ "powershell" ]
