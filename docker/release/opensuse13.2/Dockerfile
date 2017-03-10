# Docker image file that describes an OpenSUSE 13.2 (AKA harlequin) image with PowerShell installed from PowerShell Release
FROM opensuse:harlequin
LABEL maintainer Travis Plunk <travis.plunk@microsoft.com>

ARG POWERSHELL_VERSION=6.0.0_alpha.17
ARG POWERSHELL_RELEASE=v6.0.0-alpha.17
ARG POWERSHELL_PACKAGE=powershell-$POWERSHELL_VERSION-1.suse.13.2.x86_64.rpm

# Update, Install packages to generate localedef and CURL which is used by RPM
# add the Microsoft key as trusted to install packgages using RPM
# Install PowerShell then clean up
RUN zypper --non-interactive update --skip-interactive \
    && zypper --non-interactive install \
        glibc-locale \
        glibc-i18ndata \
        curl \
    && rpm --import https://packages.microsoft.com/keys/microsoft.asc \
    && zypper --non-interactive install https://github.com/PowerShell/PowerShell/releases/download/$POWERSHELL_RELEASE/$POWERSHELL_PACKAGE \
    && zypper --non-interactive clean --all

# Setup the locale
ENV LANG en_US.UTF-8
ENV LC_ALL $LANG
RUN localedef --charmap=UTF-8 --inputfile=en_US $LANG

# Use array to avoid Docker prepending /bin/sh -c
ENTRYPOINT [ "powershell" ]
