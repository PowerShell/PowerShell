# Docker image file that describes an OpenSUSE 42.2 image with PowerShell installed from PowerShell Release
ARG FromTag=42.2

FROM opensuse:${FromTag}

ARG POWERSHELL_VERSION=6.0.2
ARG POWERSHELL_PACKAGE=powershell-6.0.2-linux-x64.tar.gz
ARG IMAGE_NAME=microsoft/powershell:opensuse42.2

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

# Update, Install packages to generate localedef and CURL which is used by RPM
# add the Microsoft key as trusted to install packgages using RPM
# Install PowerShell then clean up
RUN zypper --non-interactive update --skip-interactive \
    && zypper --non-interactive install \
        glibc-locale \
        glibc-i18ndata \
        tar \
        curl \
        libunwind \
        libicu \
        openssl \
    && zypper --non-interactive clean --all

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

# Get the InstallTarballPackage.sh script
ADD https://raw.githubusercontent.com/PowerShell/PowerShell/master/docker/InstallTarballPackage.sh /InstallTarballPackage.sh

# Add execution permission
RUN chmod +x /InstallTarballPackage.sh

# Install powershell from tarball package
RUN /InstallTarballPackage.sh $POWERSHELL_VERSION $POWERSHELL_PACKAGE

# Remove the script
RUN rm -f /InstallTarballPackage.sh

CMD [ "pwsh" ]
