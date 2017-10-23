FROM archlinux/base AS build-env

# Install requirements to bulid package
RUN pacman -Syuq --noconfirm base-devel git

# makepkg won't run as root, so setup a user and folder to run it.
RUN useradd -m -g wheel build
RUN mkdir /build
RUN chown build:wheel /build
USER build
RUN ls -l
RUN git clone https://aur.archlinux.org/powershell-bin.git /build/powershell-bin
RUN cd /build/powershell-bin; \
    makepkg -s
RUN ls /build/powershell-bin

FROM archlinux/base

LABEL maintainer="PowerShell Community <powershellteam@hotmail.com>" \
      readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md" \
      description="This Dockerfile will install the latest release of PS." \
      org.label-schema.usage="https://github.com/PowerShell/PowerShell/tree/master/docker#run-the-docker-image-you-built" \
      org.label-schema.url="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md" \
      org.label-schema.vcs-url="https://github.com/PowerShell/PowerShell" \
      org.label-schema.name="powershell" \
      org.label-schema.vendor="PowerShell" \
      org.label-schema.schema-version="1.0" \
      org.label-schema.docker.cmd="docker run ${IMAGE_NAME} pwsh -c '$psversiontable'" \
      org.label-schema.docker.cmd.devel="docker run ${IMAGE_NAME}" \
      org.label-schema.docker.cmd.test="docker run ${IMAGE_NAME} pwsh -c Invoke-Pester" \
      org.label-schema.docker.cmd.help="docker run ${IMAGE_NAME} pwsh -c Get-Help"

COPY --from=build-env /build/powershell-bin/*.xz /powershell-package/
RUN ls /powershell-package/*
RUN pacman -Syu --noconfirm libunwind; \
    pacman -U --noconfirm /powershell-package/*.xz
