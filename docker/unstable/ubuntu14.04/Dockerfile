FROM powershell/powershell:stable-ubuntu14.04
MAINTAINER Andrew Schwartzmeyer <andschwa@microsoft.com>

COPY bootstrap.ps1 /
RUN powershell -f bootstrap.ps1 \
    && dpkg -i PowerShell/powershell*.deb
