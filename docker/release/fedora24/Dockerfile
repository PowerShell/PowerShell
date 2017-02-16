FROM fedora:24

# TODO: Until a release of PowerShell for Fedora 24 is available,
# this Dockerfile installs the CentOS 7 version for compatibility.

ARG POWERSHELL_VERSION=6.0.0-alpha.13
ARG POWERSHELL_RELEASE=v6.0.0-alpha.13
ARG POWERSHELL_PACKAGE=powershell-6.0.0_alpha.13-1.el7.centos.x86_64.rpm

# The CentOS 7 release relies on an older version of libicu, so we'll be downloading it
# from the University of Kent (GB) mirror service.
ARG LIBICU50_SOURCE=https://www.mirrorservice.org/sites/mirror.centos.org/7.2.1511/os/x86_64/Packages
ARG LIBICU50_PACKAGE=libicu-50.1.2-15.el7.x86_64.rpm
ARG LIBICU50_PACKAGE_MD5=c3c1ebaabc8d1619377d535698784953

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
      org.label-schema.docker.cmd="docker run -it microsoft/powershell:fedora24"

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
