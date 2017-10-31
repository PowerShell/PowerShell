# Docker image file that describes an Amazon Linux image with PowerShell installed from PowerShell Release

FROM amazonlinux:latest

ARG POWERSHELL_VERSION=6.0.0-beta.9
ARG POWERSHELL_PACKAGE=powershell-6.0.0-beta.9-linux-x64.tar.gz
ARG IMAGE_NAME=microsoft/powershell:amazonlinux

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
      org.label-schema.docker.cmd="docker run ${IMAGE_NAME} pwsh -c '$psversiontable'" \
      org.label-schema.docker.cmd.devel="docker run ${IMAGE_NAME}" \
      org.label-schema.docker.cmd.test="docker run ${IMAGE_NAME} pwsh -c Invoke-Pester" \
      org.label-schema.docker.cmd.help="docker run ${IMAGE_NAME} pwsh -c Get-Help"

# TODO: addd LABEL org.label-schema.vcs-ref=${VCS_REF}

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

# Install dependencies and clean up
RUN yum install -y \
        curl \
        libunwind \
        libicu \
        libcurl \
        openssl \
        libuuid.x86_64 \
    && yum clean all

# Get the InstallTarballPackage.sh script
ADD https://raw.githubusercontent.com/PowerShell/PowerShell/master/docker/InstallTarballPackage.sh /InstallTarballPackage.sh

# Add execution permission
RUN chmod +x /InstallTarballPackage.sh

# Install powershell from tarball package
RUN /InstallTarballPackage.sh $POWERSHELL_VERSION $POWERSHELL_PACKAGE

# Remove the script
RUN rm -f /InstallTarballPackage.sh

# Use PowerShell as the default shell
# Use array to avoid Docker prepending /bin/sh -c
CMD [ "pwsh" ]
