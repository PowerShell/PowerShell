#!/usr/bin/env bash

clear
echo "Script to download latest release of PowerShell on *nix platforms" 

echo "Verify if GITHUB_TOKEN variable is defined"
[[ -n $GITHUB_TOKEN ]] || { echo >&2 "GITHUB_TOKEN variable is undefined, Generate one using https://help.github.com/articles/creating-an-access-token-for-command-line-use/"; exit 1; }

# Authorizes with read-only access to GitHub API
curl_() {
    curl -s -i -H "Authorization: token $GITHUB_TOKEN" "$@"
}

# Retrieves asset ID and package name of asset ending in argument
# $info looks like: "id": 1698239, "name": "powershell_0.4.0-1_amd64.deb",
get_info() {
    curl_ https://api.github.com/repos/PowerShell/PowerShell/releases/latest | grep -B 1 "name.*$1"
}

echo "Get OS specific asset ID and package name"
case "$OSTYPE" in
    linux*)
        source /etc/os-release
        echo $ID
        # Install curl and wget to download package
        case "$ID" in
            centos*)
                sudo yum install -y curl wget
                info=$(get_info rpm)
                ;;
            ubuntu)
                sudo apt-get install -y curl wget
                info=$(get_info deb)
                ;;
            *)
                exit 2 >&2 "Package $NAME is not supported on $OSTYPE!"
        esac
        ;;
    darwin*)
        info=$(get_info pkg)
        ;;
    *)
        exit 2 >&2 "$OSTYPE is not supported!"
        ;;
esac

# Parses $info for asset ID and package name
read asset package <<< $(echo $info | sed 's/[,"]//g' | awk '{ print $2; print $4 }')

# Downloads asset to file
curl_ -H 'Accept: application/octet-stream' https://api.github.com/repos/PowerShell/PowerShell/releases/assets/$asset |
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
                sudo apt-get install -y libunwind8 libicu52
                sudo dpkg -i "./$package"
                ;;
            *)
        esac
        ;;
    darwin*)
        sudo installer -pkg ./$package -target /
        ;;
esac

get_smadll_location() {
    dpkg-query --listfiles PowerShell | grep "System.Management.Automation.dll"
}

get_powershell_location() {
    dirname $(get_smadll_location)
}

get_powershell_symlink() {
	dir /usr/bin/powershell
}

install_location=$(get_powershell_location)
powershell_symlink=$(get_powershell_symlink)

if [ $install_location ]
then
	echo "Congratulations! PowerShell \"$package\" is installed @ \"$install_location\""
fi

if [ $powershell_symlink ]
then
	echo "Symlink is available @ \"$powershell_symlink\""
else
	echo "PowerShell install failed! Check this script's output for information"
	exit -1
fi
