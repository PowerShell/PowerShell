#!/bin/sh

# Exit on errors
set -e

# Example use:
#     ./InstallTarballPackage.sh "6.0.0-beta.9" "powershell-6.0.0-beta.9-linux-x64.tar.gz"

usage() {
    echo "usage: $0 <powershell version> <powershell package name>"
    exit 1
}

POWERSHELL_VERSION=$1
if [ ! "$POWERSHELL_VERSION" ]; then
    usage
fi

POWERSHELL_PACKAGE=$2
if [ ! "$POWERSHELL_PACKAGE" ]; then
    usage
fi

POWERSHELL_LINKFILE=/usr/bin/pwsh

# Download the powershell .tar.gz package
curl -L -o /tmp/powershell.tar.gz https://github.com/PowerShell/PowerShell/releases/download/v$POWERSHELL_VERSION/$POWERSHELL_PACKAGE

# Create the target folder where powershell will be placed
mkdir -p /opt/microsoft/powershell/$POWERSHELL_VERSION
# Expand powershell to the target folder
tar zxf /tmp/powershell.tar.gz -C /opt/microsoft/powershell/$POWERSHELL_VERSION

# Create the symbolic link that points to powershell
ln -s /opt/microsoft/powershell/$POWERSHELL_VERSION/pwsh $POWERSHELL_LINKFILE
# Add the symbolic link path to /etc/shells
if [ ! -f /etc/shells ]; then
    echo $POWERSHELL_LINKFILE > /etc/shells ;
else
    grep -q "^${POWERSHELL_LINKFILE}$" /etc/shells || echo $POWERSHELL_LINKFILE >> /etc/shells ;
fi
