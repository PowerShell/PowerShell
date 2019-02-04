# Docker image file that describes an Centos7 image with PowerShell installed from Microsoft YUM Repo

FROM mcr.microsoft.com/powershell:alpine-3.8
LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>"

# Install dependencies and clean up
RUN apk update \
    && apk add libunwind libcurl cmake clang build-base git bash curl

COPY PowerShellPackage.ps1 /

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

ENTRYPOINT [ "pwsh" ]
