#!/usr/bin/env bash

# Let's quit on interrupt of subcommands
trap '
  trap - INT # restore default INT handler
  echo "Interrupted"
  kill -s INT "$$"
' INT

# Retrieves asset ID and package name of asset ending in argument
# $info looks like: "id": 1698239, "name": "powershell_0.4.0-1_amd64.deb",
get_info() {
    curl -s https://api.github.com/repos/PowerShell/PowerShell/releases/latest | grep -B 1 "name.*$1"
}

# Get OS specific asset ID and package name
case "$OSTYPE" in
    linux*)
        source /etc/os-release
        # Install curl and wget to download package
        case "$ID" in
            centos*)
                if ! hash curl 2>/dev/null; then
                    echo "curl not found, installing..."
                    sudo yum install -y curl
                fi

                version=rpm
                ;;
            ubuntu)
                if ! hash curl 2>/dev/null; then
                    echo "curl not found, installing..."
                    sudo apt-get install -y curl
                fi

                case "$VERSION_ID" in
                    14.04)
                        version=ubuntu1.14.04.1_amd64.deb
                        ;;
                    16.04)
                        version=ubuntu1.16.04.1_amd64.deb
                        ;;
                    *)
                        echo "Ubuntu $VERSION_ID is not supported!" >&2
                        exit 2
                esac
                ;;
            *)
                echo "$NAME is not supported!" >&2
                exit 2
        esac
        ;;
    darwin*)
        # We don't check for curl as macOS should have a system version
        version=pkg
        ;;
    *)
        echo "$OSTYPE is not supported!" >&2
        exit 2
        ;;
esac

info=$(get_info "$version")

# Parses $info for asset ID and package name
read asset package <<< $(echo "$info" | sed 's/[,"]//g' | awk '{ print $2; print $4 }')

# Downloads asset to file
packageuri=$(curl -s -i -H 'Accept: application/octet-stream' "https://api.github.com/repos/PowerShell/PowerShell/releases/assets/$asset" |
    grep location | sed 's/location: //g')
curl -C - -o "$package" ${packageuri%$'\r'}

# Installs PowerShell package
case "$OSTYPE" in
    linux*)
        source /etc/os-release
        # Install dependencies
        case "$ID" in
            centos)
                echo "Installing libicu, libunwind, and $package with sudo ..."
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
                echo "Installing $libicupackage, libunwind8, and $package with sudo ..."
                sudo apt-get install -y libunwind8 "$icupackage"
                sudo dpkg -i "./$package"
                ;;
            *)
        esac
        ;;
    darwin*)
        if hash brew 2>/dev/null; then
            if [[ ! -d $(brew --prefix openssl) ]]; then
               echo "Installing OpenSSL with brew..."
               if ! brew install openssl; then
                   echo "ERROR: OpenSSL failed to install! Crypto functions will not work..." >&2
                   # Don't abort because it is not fatal
               fi
            fi
        else
            echo "ERROR: brew not found! OpenSSL may not be available..." >&2
            # Don't abort because it is not fatal
        fi

        echo "Installing $package with sudo ..."
        sudo installer -pkg "./$package" -target /
        ;;
esac

powershell -noprofile -c '"Congratulations! PowerShell is installed at $PSHOME"'
success=$?

if [[ "$success" != 0 ]]; then
    echo "ERROR: PowerShell failed to install!" >&2
    exit "$success"
fi
