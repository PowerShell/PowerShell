# Docker image file that describes an Ubuntu16.04 image with PowerShell installed from Microsoft APT Repo

FROM mcr.microsoft.com/powershell:ubuntu-16.04@sha256:1225d68fb0706f738bd930f44b09ed550eb176ed7f8fd3aeda74dbb4051f2e8d
LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>"

# Install dependencies and clean up
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
	apt-utils \
	apt-utils \
        libc6 \
        libcurl3 \
        ca-certificates \
        libgcc1 \
        libicu55 \
        libssl1.0.0 \
        libstdc++6 \
        libtinfo5 \
        libunwind8 \
        libuuid1 \
	libcroco3 \
	libgraphite2-3 \
        zlib1g \
        curl \
        git \
	apt-transport-https \
	locales \
	wget \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

COPY PowerShellPackage.ps1 /

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Use PowerShell as the default shell
ENTRYPOINT [ "pwsh" ]
