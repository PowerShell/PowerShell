FROM ubuntu:16.04

RUN apt-get update && apt-get install -y curl  libunwind8 libicu55 libcurl3
RUN curl -LO https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/powershell_6.0.0-alpha.9-1ubuntu1.16.04.1_amd64.deb
RUN dpkg -i powershell_6.0.0-alpha.9-1ubuntu1.16.04.1_amd64.deb

Entrypoint [ "powershell" ]
