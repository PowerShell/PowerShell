#!/bin/sh

# Exit on errors and unset variables
set -eu

# Example use:
#     ./InstallTarballPackage.sh "6.0.0-beta.9" "powershell-6.0.0-beta.9-linux-x64.tar.gz"

usage() {
    echo "usage: $0 <powershell version> <powershell package name>"
    exit 1
}

POWERSHELL_VERSION=${1:-}
if [ -z "$POWERSHELL_VERSION" ]; then
    usage
fi

POWERSHELL_PACKAGE=${2:-}
if [ -z "$POWERSHELL_PACKAGE" ]; then
    usage
fi

case "$POWERSHELL_VERSION" in
    *[!0-9A-Za-z._+-]* | .* | *..* | *- | "")
        echo "Invalid PowerShell version: $POWERSHELL_VERSION" >&2
        usage
        ;;
esac

case "$POWERSHELL_PACKAGE" in
    */* | *[!0-9A-Za-z._+-]* | .* | *..*)
        echo "Invalid PowerShell package name: $POWERSHELL_PACKAGE" >&2
        usage
        ;;
esac

case "$POWERSHELL_PACKAGE" in
    *.tar.gz)
        ;;
    *)
        echo "PowerShell package must be a .tar.gz file: $POWERSHELL_PACKAGE" >&2
        usage
        ;;
esac

POWERSHELL_LINKFILE=/usr/bin/pwsh
TEMP_DIR=$(mktemp -d)
cleanup() {
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        rm -rf -- "$TEMP_DIR"
    fi
}
trap cleanup EXIT HUP INT TERM
PACKAGE_PATH="$TEMP_DIR/powershell.tar.gz"
DOWNLOAD_URL="https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/${POWERSHELL_PACKAGE}"
INSTALL_DIR="/opt/microsoft/powershell/$POWERSHELL_VERSION"

# Download the powershell .tar.gz package
curl --fail --location --show-error --proto '=https' --tlsv1.2 -o "$PACKAGE_PATH" "$DOWNLOAD_URL"

# Create the target folder where powershell will be placed
mkdir -p "$INSTALL_DIR"
# Expand powershell to the target folder
tar zxf "$PACKAGE_PATH" -C "$INSTALL_DIR"

# Create the symbolic link that points to powershell
ln -sfn "$INSTALL_DIR/pwsh" "$POWERSHELL_LINKFILE"
# Add the symbolic link path to /etc/shells
if [ ! -f /etc/shells ]; then
    echo "$POWERSHELL_LINKFILE" > /etc/shells ;
else
    grep -q "^${POWERSHELL_LINKFILE}$" /etc/shells || echo "$POWERSHELL_LINKFILE" >> /etc/shells ;
fi
