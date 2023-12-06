#!/bin/bash

#call this code direction from the web with:
#bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-gentoo.sh) ARGUMENTS
#bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-gentoo.sh) <ARGUMENTS>

#Usage - if you do not have the ability to run scripts directly from the web,
#        pull all files in this repo folder and execute, this script
#        automatically prefers local copies of sub-scripts

#Completely automated install requires a root account or sudo with a password requirement

#Switches
# -skip-sudo-check    - use sudo without verifying its availability (hard to accurately do on some distros)
# -preview            - installs the latest preview release of PowerShell side-by-side with any existing production releases

#gitrepo paths are overrideable to run from your own fork or branch for testing or private distribution

# PowerShell Version
VERSION="1.2.0"
gitreposubpath="PowerShell/PowerShell/master"
gitreposcriptroot="https://raw.githubusercontent.com/$gitreposubpath/tools"
thisinstallerdistro=gentoo
gitscriptname="installpsh-gentoo.psh"
powershellpackageid=powershell

echo ;
echo "*** PowerShell Development Environment Installer $VERSION for $thisinstallerdistro"
echo "***    Original script is at: $gitreposcriptroot/$gitscriptname"
echo
echo "*** Arguments used: $*"

# Let's quit on interrupt of subcommands
trap '
  trap - INT # restore default INT handler
  echo "Interrupted"
  kill -s INT "$$"
' INT

#Verify The Installer Choice (for direct runs of this script)
lowercase(){
    echo "$1" | tr "[:upper:]" "[:lower:]"
}

OS=$(lowercase "$(uname)")
if [ "${OS}" == "windowsnt" ]; then
    OS=windows
    DistroBasedOn=windows
elif [ "${OS}" == "darwin" ]; then
    OS=osx
    DistroBasedOn=osx
else
    OS=$(uname)
    if [ "${OS}" == "SunOS" ] ; then
        OS=solaris
        DistroBasedOn=sunos
    elif [ "${OS}" == "AIX" ] ; then
        DistroBasedOn=aix
    elif [ "${OS}" == "Linux" ] ; then
        if [ -f /etc/redhat-release ] ; then
            DistroBasedOn='redhat'
        elif [ -f /etc/system-release ] ; then
            DIST=$(sed s/\ release.*// < /etc/system-release)
            if [[ $DIST == *"Amazon Linux"* ]] ; then
                DistroBasedOn='amazonlinux'
            else
                DistroBasedOn='redhat'
            fi
        elif [ -f /etc/SuSE-release ] ; then
            DistroBasedOn='suse'
        elif [ -f /etc/mandrake-release ] ; then
            DistroBasedOn='mandrake'
        elif [ -f /etc/debian_version ] ; then
            DistroBasedOn='debian'
        elif [ "$(lsb_release --id 2>/dev/null | sed -E 's/^.*:[[:space:]]*//')" = Gentoo ] ; then
            DistroBasedOn='gentoo'
        fi
        if [ -f /etc/UnitedLinux-release ] ; then
            DIST="${DIST}[$( (tr "\n" ' ' | sed s/VERSION.*//) < /etc/UnitedLinux-release )]"
            DistroBasedOn=unitedlinux
        fi
        OS=$(lowercase "$OS")
        DistroBasedOn=$(lowercase "$DistroBasedOn")
    fi
fi

if [ "$DistroBasedOn" != "$thisinstallerdistro" ]; then
  echo "*** This installer is only for $thisinstallerdistro and you are running $DistroBasedOn, please run \"$gitreposcriptroot\install-powershell.sh\" to see if your distro is supported AND to auto-select the appropriate installer if it is."
  exit 1
fi

## Check requirements and prerequisites

#Check for sudo if not root
if [[ "${CI}" == "true" ]]; then
    echo "Running on CI (as determined by env var CI set to true), skipping SUDO check."
    set -- "$@" '-skip-sudo-check'
fi

SUDO=''
if (( EUID != 0 )); then
    #Check that sudo is available
    if [[ ("'$*'" =~ skip-sudo-check) || ("$(whereis sudo)" == *'/'* && "$(sudo -nv 2>&1)" != 'Sorry, user'*) ]]; then
        SUDO='sudo'
    else
        echo "ERROR: You must either be root or be able to use sudo" >&2
        #exit 5
    fi
fi

#Collect any variation details if required for this distro
# shellcheck disable=SC1091
#END Collect any variation details if required for this distro

#If there are known incompatible versions of this distro, put the test, message and script exit here:

#END Verify The Installer Choice

##END Check requirements and prerequisites

echo
echo "*** Installing PowerShell for $DistroBasedOn..."
if ! hash curl 2>/dev/null; then
    echo "curl not found, installing..."
    $SUDO emerge -nv1 net-misc/curl
fi

if ! hash dpkg 2>/dev/null; then
    echo "dpkg not found, installing..."
    $SUDO emerge -nv1 app-arch/dpkg
fi

# The executable to test.
PWSH=pwsh

if [[ "'$*'" =~ preview ]] ; then
    echo
    echo "-preview was used, the latest preview release will be installed (side-by-side with your production release)"
    powershellpackageid=powershell-preview
    PWSH=pwsh-preview
fi

currentversion=$(curl -s https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v// | sed s/,//g | sed s/\ //g)

printf "\n*** Current version on git is: $currentversion, repo version may differ slightly...\n\n"

ubuntu_dist=18.04

# Find latest ubuntu packages.

for ubuntu_dist in $(curl -sL 'https://packages.microsoft.com/ubuntu/' | sed -En 's,.*href="([[:digit:]][[:digit:].]+).*,\1,p' | sort -rV); do
    if ! curl -sL "https://packages.microsoft.com/ubuntu/${ubuntu_dist}/prod/pool/main/p/${powershellpackageid}/" | grep -q '404 Not Found'; then
        break
    fi
done

printf "*** Found packages for Ubuntu $ubuntu_dist...\n\n"

latest_pkg=$(curl -sL "https://packages.microsoft.com/ubuntu/${ubuntu_dist}/prod/pool/main/p/${powershellpackageid}/" | sed -En 's/^.*href="([^"]+\.deb).*/\1/p' | sort -V | tail -1)

if [ ! -f "$latest_pkg" ]; then
    curl -sL "https://packages.microsoft.com/ubuntu/${ubuntu_dist}/prod/pool/main/p/${powershellpackageid}/${latest_pkg}" -o "$latest_pkg"
fi

$SUDO dpkg -i --force-depends "$latest_pkg" 2>/dev/null

# the postrm breaks removal
$SUDO rm -f /var/lib/dpkg/info/${powershellpackageid}.postrm

printf "\n\n"

# shellcheck disable=SC2016
$PWSH -noprofile -c '"Congratulations! PowerShell is installed at $PSHOME.
Run `"'"$PWSH"'`" to start a PowerShell session."'

success=$?

if [[ "$success" != 0 ]]; then
    echo "ERROR: PowerShell failed to install!" >&2
    exit "$success"
fi

if [[ "'$*'" =~ includeide ]] ; then
    echo
    echo "*** Installing VS Code PowerShell IDE..."

    # install overlay for flatpak and flatpak if needed
    if ! command -v flatpak >/dev/null; then
      echo "*** Setting up Flatpak for VS Code..."

      if ! ( ( command -v layman >/dev/null && layman -l | grep -q flatpak-overlay ) || [ -f /etc/portage/repos.conf/flatpak-overlay.conf ] ); then
        $SUDO sh -c 'cat >/etc/portage/repos.conf/flatpak-overlay.conf' <<EOF
[flatpak-overlay]
priority = 50
location = /usr/local/portage/flatpak-overlay
sync-type = git
sync-uri = https://github.com/fosero/flatpak-overlay.git
auto-sync = Yes
EOF
        $SUDO emerge --sync
      fi

      $SUDO emerge -nv1 sys-apps/flatpak
    fi

    $SUDO flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo

    $SUDO flatpak install -y flathub com.visualstudio.code

    echo
    echo "*** Installing VS Code PowerShell Extension"
    flatpak run com.visualstudio.code --install-extension ms-vscode.PowerShell
    if [[ "'$*'" =~ -interactivetesting ]] ; then
        echo "*** Loading test code in VS Code"
        curl -O ./testpowershell.ps1 https://raw.githubusercontent.com/DarwinJS/CloudyWindowsAutomationCode/master/pshcoredevenv/testpowershell.ps1
        flatpak run com.visualstudio.code ./testpowershell.ps1
    fi
fi

printf "\n*** NOTE: Run the newest version of this script again to update PowerShell\n\n"
echo "*** Install Complete"
