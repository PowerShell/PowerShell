#!/usr/bin/env bash

# Let's quit on interrupt of subcommands
trap '
  trap - INT # restore default INT handler
  echo "Interrupted"
  kill -s INT "$$"
' INT

get_url() {
    release=v6.0.0-alpha.17
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

                package=powershell-6.0.0_alpha.17-1.el7.centos.x86_64.rpm
                ;;
            ubuntu)
                if ! hash curl 2>/dev/null; then
                    echo "curl not found, installing..."
                    sudo apt-get install -y curl
                fi

                case "$VERSION_ID" in
                    14.04)
                        package=powershell_6.0.0-alpha.17-1ubuntu1.14.04.1_amd64.deb
                        ;;
                    16.04)
                        package=powershell_6.0.0-alpha.17-1ubuntu1.16.04.1_amd64.deb
                        ;;
                    *)
                        echo "Ubuntu $VERSION_ID is not supported!" >&2
                        exit 2
                esac
                ;;
            opensuse)
                if ! hash curl 2>/dev/null; then
                    echo "curl not found, installing..."
                    sudo zypper install -y curl
                fi

                package=powershell-6.0.0_alpha.17-1.suse.13.2.x86_64.rpm
                ;;
            *)
                echo "$NAME is not supported!" >&2
                exit 2
        esac
        ;;
    darwin*)
        # We don't check for curl as macOS should have a system version
        package=powershell-6.0.0-alpha.17.pkg
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
        echo "Installing PowerShell with sudo..."
        case "$ID" in
            centos)
                # yum automatically resolves dependencies for local packages
                sudo yum install "./$package"
                ;;
            ubuntu)
                # dpkg does not automatically resolve dependencies, but spouts ugly errors
                sudo dpkg -i "./$package" &> /dev/null
                # Resolve dependencies
                sudo apt-get install -f
                ;;
            opensuse)
                # Install the Microsoft public key so that zypper trusts the package
                sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
                # zypper automatically resolves dependencies for local packages
                sudo zypper --non-interactive install "./$package" &> /dev/null
                ;;
            *)
        esac
        ;;
    darwin*)
        patched=0
        if hash brew 2>/dev/null; then
            if [[ ! -d $(brew --prefix openssl) ]]; then
               echo "Installing OpenSSL with brew..."
               if ! brew install openssl; then
                   echo "ERROR: OpenSSL failed to install! Crypto functions will not work..." >&2
                   # Don't abort because it is not fatal
               elif ! brew install curl --with-openssl; then
                   echo "ERROR: curl failed to build against OpenSSL; SSL functions will not work..." >&2
                   # Still not fatal
               else
                   # OpenSSL installation succeeded; remember to patch System.Net.Http after PowerShell installation
                   patched=1
               fi
            fi

        else
            echo "ERROR: brew not found! OpenSSL may not be available..." >&2
            # Don't abort because it is not fatal
        fi

        echo "Installing $package with sudo ..."
        sudo installer -pkg "./$package" -target /
        if [[ $patched -eq 1 ]]; then
            echo "Patching System.Net.Http for libcurl and OpenSSL..."
            find /usr/local/microsoft/powershell -name System.Net.Http.Native.dylib | xargs sudo install_name_tool -change /usr/lib/libcurl.4.dylib /usr/local/opt/curl/lib/libcurl.4.dylib
        fi
        ;;
esac

powershell -noprofile -c '"Congratulations! PowerShell is installed at $PSHOME"'
success=$?

if [[ "$success" != 0 ]]; then
    echo "ERROR: PowerShell failed to install!" >&2
    exit "$success"
fi
