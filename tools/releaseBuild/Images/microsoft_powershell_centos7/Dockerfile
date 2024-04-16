# Docker image file that describes an Centos7 image with PowerShell installed from Microsoft YUM Repo

FROM mcr.microsoft.com/powershell:centos-7@sha256:c46fc589b3bc39f8021ad4c09680fe4aa98c7ac0e317b0c48d7bd25e5b75d8c7
LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>"

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
        wget \
    && yum clean all

COPY PowerShellPackage.ps1 /

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Use PowerShell as the default shell
ENTRYPOINT [ "pwsh" ]
