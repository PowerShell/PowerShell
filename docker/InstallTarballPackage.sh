#!/bin/sh

# Exit on errors and unset variables
set -eu

# Example use:
#     ./InstallTarballPackage.sh "7.6.2" "powershell-7.6.2-linux-x64.tar.gz"

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

POWERSHELL_LINKFILE=${POWERSHELL_LINKFILE:-/usr/bin/pwsh}
POWERSHELL_INSTALL_ROOT=${POWERSHELL_INSTALL_ROOT:-/opt/microsoft/powershell}
SHELLS_FILE=${SHELLS_FILE:-/etc/shells}
TEMP_DIR=$(mktemp -d)
cleanup() {
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        rm -rf -- "$TEMP_DIR"
    fi
}
trap cleanup EXIT
trap 'cleanup; exit 1' HUP INT TERM
PACKAGE_PATH="$TEMP_DIR/powershell.tar.gz"
HASHES_PATH="$TEMP_DIR/hashes.sha256"
DOWNLOAD_URL="https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/${POWERSHELL_PACKAGE}"
HASHES_URL="https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/hashes.sha256"
INSTALL_DIR="$POWERSHELL_INSTALL_ROOT/$POWERSHELL_VERSION"

# Print the SHA-256 hash for the file path passed as the first argument.
get_file_sha256() {
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$1" | awk '{ print $1 }'
    elif command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | awk '{ print $1 }'
    else
        echo "Unable to verify package integrity: sha256sum or shasum is required." >&2
        return 1
    fi
}

# Download the powershell .tar.gz package and release checksum file.
curl --fail --location --show-error --proto '=https' --tlsv1.2 -o "$PACKAGE_PATH" "$DOWNLOAD_URL"
curl --fail --location --show-error --proto '=https' --tlsv1.2 -o "$HASHES_PATH" "$HASHES_URL"

expected_hash=$(tr -d '\000\r' < "$HASHES_PATH" | awk -v package="$POWERSHELL_PACKAGE" '
    {
        filename = $2
        sub(/^\*/, "", filename)
        if (filename == package) {
            print $1
            found = 1
            exit
        }
    }
    END { if (!found) { exit 1 } }
') || {
    echo "Unable to find SHA-256 checksum for $POWERSHELL_PACKAGE in hashes.sha256." >&2
    exit 1
}

actual_hash=$(get_file_sha256 "$PACKAGE_PATH")
if [ "$actual_hash" != "$expected_hash" ]; then
    echo "SHA-256 checksum verification failed for $POWERSHELL_PACKAGE." >&2
    echo "Expected: $expected_hash" >&2
    echo "Actual:   $actual_hash" >&2
    exit 1
fi

# Create the target folder where powershell will be placed
mkdir -p "$INSTALL_DIR"
# Expand powershell to the target folder
tar zxf "$PACKAGE_PATH" -C "$INSTALL_DIR"

# Create or update the symbolic link that points to powershell without clobbering non-symlink files.
POWERSHELL_TARGET="$INSTALL_DIR/pwsh"
if [ -L "$POWERSHELL_LINKFILE" ]; then
    current_target=$(readlink "$POWERSHELL_LINKFILE")
    if [ "$current_target" != "$POWERSHELL_TARGET" ]; then
        rm -- "$POWERSHELL_LINKFILE"
        ln -s "$POWERSHELL_TARGET" "$POWERSHELL_LINKFILE"
    fi
elif [ -e "$POWERSHELL_LINKFILE" ]; then
    echo "$POWERSHELL_LINKFILE already exists and is not a symbolic link; refusing to replace it." >&2
    exit 1
else
    ln -s "$POWERSHELL_TARGET" "$POWERSHELL_LINKFILE"
fi

# Add the symbolic link path to /etc/shells
if [ ! -f "$SHELLS_FILE" ]; then
    printf '%s\n' "$POWERSHELL_LINKFILE" > "$SHELLS_FILE" ;
else
    grep -Fqx -- "$POWERSHELL_LINKFILE" "$SHELLS_FILE" || printf '%s\n' "$POWERSHELL_LINKFILE" >> "$SHELLS_FILE" ;
fi
