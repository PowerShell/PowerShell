FROM microsoft/powershell:fedora24

# Metadata as defined at http://label-schema.org
# Additional labels are inherited from microsoft/powershell:fedora24
ARG VCS_REF
ARG BUILD_DATE
LABEL org.label-schema.build-date=$BUILD_DATE \
      org.label-schema.vcs-ref=$VCS_REF \
      org.label-schema.docker.cmd="docker run -it microsoft/powershell-nightly:fedora24"

ARG fork=PowerShell
ARG branch=master
SHELL ["powershell", "-command"]
RUN git clone --recursive https://github.com/$env:fork/PowerShell.git -b $env:branch; \
    Set-Location PowerShell; \
    Import-Module ./build.psm1; \
    Start-PSBootstrap -Package -NoSudo; \
    Start-PSBuild -Crossgen -PSModuleRestore; \
    Start-PSPackage;

SHELL ["/bin/sh", "-c"]
RUN dnf install -y PowerShell/powershell*.rpm
