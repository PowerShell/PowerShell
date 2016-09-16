#!/usr/bin/env bash

# Let's quit on interrupt of subcommands
trap '
  trap - INT # restore default INT handler
  echo "Interrupted"
  kill -s INT "$$"
' INT

get_url() {
    release=v6.0.0-alpha.10
    echo "https://github.com/PowerShell/PowerShell/releases/download/$release/$1"
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

                package=powershell-6.0.0_alpha.10-1.el7.centos.x86_64.rpm
                ;;
            ubuntu)
                if ! hash curl 2>/dev/null; then
                    echo "curl not found, installing..."
                    sudo apt-get install -y curl
                fi

                case "$VERSION_ID" in
                    14.04)
                        package=powershell_6.0.0-alpha.10-1ubuntu1.14.04.1_amd64.deb
                        ;;
                    16.04)
                        package=powershell_6.0.0-alpha.10-1ubuntu1.16.04.1_amd64.deb
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
        package=powershell-6.0.0-alpha.10.pkg
        ;;
    *)
        echo "$OSTYPE is not supported!" >&2
        exit 2
        ;;
esac

curl -L -o "$package" $(get_url "$package")

if [[ ! -r "$package" ]]; then
    echo "ERROR: $package failed to download! Aborting..." >&2
    exit 1
fi

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
