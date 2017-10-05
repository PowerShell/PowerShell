FROM fedora:24

# TODO: Until a release of PowerShell for Fedora 24 is available,
# this Dockerfile installs the CentOS 7 version for compatibility.

ARG POWERSHELL_VERSION=6.0.0-beta.8
ARG POWERSHELL_RELEASE=v6.0.0-beta.8
ARG POWERSHELL_PACKAGE=powershell-6.0.0_beta.8-1.rhel.7.x86_64.rpm
ARG IMAGE_NAME=microsoft/powershell:fedora24

LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>" \
      readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md" \
      description="This Dockerfile will install the latest release of PS." \
      org.label-schema.usage="https://github.com/PowerShell/PowerShell/tree/master/docker#run-the-docker-image-you-built" \
      org.label-schema.url="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md" \
      org.label-schema.vcs-url="https://github.com/PowerShell/PowerShell" \
      org.label-schema.name="powershell" \
      org.label-schema.vendor="PowerShell" \
      org.label-schema.version=${POWERSHELL_VERSION} \
      org.label-schema.schema-version="1.0" \
      org.label-schema.docker.cmd="docker run ${IMAGE_NAME} powershell -c '$psversiontable'" \
      org.label-schema.docker.cmd.devel="docker run ${IMAGE_NAME}" \
      org.label-schema.docker.cmd.test="docker run ${IMAGE_NAME} powershell -c Invoke-Pester" \
      org.label-schema.docker.cmd.help="docker run ${IMAGE_NAME} powershell -c Get-Help"

# TODO: addd LABEL org.label-schema.vcs-ref=${VCS_REF}

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
CMD [ "powershell" ]
