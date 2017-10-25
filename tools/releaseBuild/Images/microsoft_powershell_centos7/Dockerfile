# Docker image file that describes an Centos7 image with PowerShell installed from Microsoft YUM Repo

FROM microsoft/powershell:centos7
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
    && yum clean all

COPY PowerShellPackage.ps1 /

# Use PowerShell as the default shell
ENTRYPOINT [ "pwsh" ]
