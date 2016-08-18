#!/usr/bin/env bash

# Retrieves asset ID and package name of asset ending in argument
# $info looks like: "id": 1698239, "name": "powershell_0.4.0-1_amd64.deb",
get_info() {
    curl -s https://api.github.com/repos/PowerShell/PowerShell/releases/latest | grep -B 1 "name.*$1"
}

# Get OS specific asset ID and package name
case "$OSTYPE" in
    linux*)
        source /etc/os-release
        echo $ID
        # Install curl and wget to download package
        case "$ID" in
            centos*)
                sudo yum install -y curl wget
                version=rpm
                ;;
            ubuntu)
                sudo apt-get install -y curl wget
                case "$VERSION_ID" in
                    14.04)
                        version=ubuntu1.14.04.1_amd64.deb
                        ;;
                    16.04)
                        version=ubuntu1.16.04.1_amd64.deb
                        ;;
                    *)
                        exit 2 >&2 "Ubuntu $VERSION_ID is not supported!"
                esac
                ;;
            *)
                exit 2 >&2 "$NAME is not supported!"
        esac
        ;;
    darwin*)
        version=pkg
        ;;
    *)
        exit 2 >&2 "$OSTYPE is not supported!"
        ;;
esac

info=$(get_info $version)

# Parses $info for asset ID and package name
read asset package <<< $(echo $info | sed 's/[,"]//g' | awk '{ print $2; print $4 }')

# Downloads asset to file
curl -s -i -H 'Accept: application/octet-stream' https://api.github.com/repos/PowerShell/PowerShell/releases/assets/$asset |
    grep location | sed 's/location: //g' | wget -i - -O $package

# Installs PowerShell package
case "$OSTYPE" in
    linux*)
        source /etc/os-release
        # Install dependencies
        case "$ID" in
            centos)
                sudo yum install -y libicu libunwind
                sudo yum install "./$package"
                ;;
            ubuntu)
                case "$VERSION_ID" in
                    14.04)
                        icupackage=libicu52
                        ;;
                    16.04)
                        icupackage=libicu55
                        ;;
                esac
                sudo apt-get install -y libunwind8 $icupackage
                sudo dpkg -i "./$package"
                ;;
            *)
        esac
        ;;
    darwin*)
        sudo installer -pkg ./$package -target /
        ;;
esac

powershell -noprofile -c '"Congratulations! PowerShell is installed at $PSHOME"'

if [[ $? != 0 ]]; then
    echo "ERROR! PowerShell didn't install. Check script output"
    exit 1
fi
