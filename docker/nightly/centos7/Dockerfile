FROM powershell/powershell:stable-centos7
MAINTAINER Andrew Schwartzmeyer <andschwa@microsoft.com>

ARG fork=PowerShell
ARG branch=master

COPY bootstrap.ps1 /
RUN powershell -f bootstrap.ps1 \
    && yum install -y PowerShell/powershell*.rpm
