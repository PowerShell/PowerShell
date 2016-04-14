#!/usr/bin/env bash

[[ -n $GITHUB_TOKEN ]] || { echo >&2 "GITHUB_TOKEN variable is undefined, please provide token"; exit 1; }

# Set OS specific asset ID and package name
case "$OSTYPE" in
    linux*)
        asset='1536045'
        package='powershell_0.3.0-1_amd64.deb'
        # Install curl and wget to download package
        sudo apt-get install -y curl wget
        ;;
    darwin*)
        asset='1536063'
        package='powershell-0.3.0.pkg'
        ;;
    *)
        exit 2 >&2 "$OSTYPE not supported!"
        ;;
esac

# Authorizes with read-only access to GitHub API
# Retrieves URL of v0.3.0 release asset
curl -s -i \
     -H "Authorization: token $GITHUB_TOKEN " \
     -H 'Accept: application/octet-stream' \
     "https://api.github.com/repos/PowerShell/PowerShell/releases/assets/$asset" |
    grep location |
    sed 's/location: //g' |
    wget -i - -O $package

# Installs PowerShell package
case "$OSTYPE" in
    linux*)
        # Install dependencies
        sudo apt-get install -y libunwind8 libicu52
        sudo dpkg -i ./$package
        ;;
    darwin*)
        sudo installer -pkg ./$package -target /
        ;;
esac

echo "Congratulations! PowerShell is now installed."
