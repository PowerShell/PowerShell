# Docker image file that describes an Centos7 image with PowerShell installed from Microsoft YUM Repo

FROM microsoft/dotnet:2.1-sdk-alpine
LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>" 

# Install dependencies and clean up
RUN apk update \
    && apk add cmake clang build-base git bash

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

COPY build-and-run-pwsh.sh /
