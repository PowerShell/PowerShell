#!/bin/bash

#Companion code for the blog https://cloudywindows.com
#call this code direction from the web with:
#bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-suse.sh) ARGUMENTS
#bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-suse.sh) <ARGUMENTS>

#Usage - if you do not have the ability to run scripts directly from the web,
#        pull all files in this repo folder and execute, this script
#        automatically prefers local copies of sub-scripts

#Completely automated install requires a root account or sudo with a password requirement

#Switches
# -includeide         - installs VSCode and VSCode PowerShell extension (only relevant to machines with desktop environment)
# -interactivetesting - do a quick launch test of VSCode (only relevant when used with -includeide)
# -skip-sudo-check    - use sudo without verifying its availability (this is required to run in the VSTS Hosted Linux Preview)
# -preview            - installs the latest preview release of PowerShell side-by-side with any existing production releasesS

#gitrepo paths are overrideable to run from your own fork or branch for testing or private distribution

# PowerShell Version
VERSION="1.2.0"
gitreposubpath="PowerShell/PowerShell/master"
gitreposcriptroot="https://raw.githubusercontent.com/$gitreposubpath/tools"
thisinstallerdistro=suse
repobased=false
gitscriptname="installpsh-suse.psh"
pwshlink=/usr/bin/pwsh

echo
echo "*** PowerShell Development Environment Installer $VERSION for $thisinstallerdistro"
echo "***    Original script is at: $gitreposcriptroot/$gitscriptname"
echo
echo "*** Arguments used: $*"
echo

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
        elif [ -f /etc/mandrake-release ] ; then
            DistroBasedOn='mandrake'
        elif [ -f /etc/debian_version ] ; then
            DistroBasedOn='debian'
        fi
        if [ -f /etc/UnitedLinux-release ] ; then
            DIST="${DIST}[$( (tr "\n" ' ' | sed s/VERSION.*//) < /etc/UnitedLinux-release )]"
            DistroBasedOn=unitedlinux
        fi
		osname=$(source /etc/os-release; echo $PRETTY_NAME)
        if [[ $osname = *SUSE* ]]; then
            DistroBasedOn='suse'
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
    if [[ ("'$*'" =~ skip-sudo-check) && ("$(whereis sudo)" == *'/'* && "$(sudo -nv 2>&1)" != 'Sorry, user'*) ]]; then
        SUDO='sudo'
    else
        echo "ERROR: You must either be root or be able to use sudo" >&2
        #exit 5
    fi
fi

#Collect any variation details if required for this distro
# shellcheck disable=SC1091
source /etc/os-release
MAJORREV=${VERSION_ID/\.*/}
#END Collect any variation details if required for this distro

#If there are known incompatible versions of this distro, put the test, message and script exit here:
if [[ $ID == 'opensuse' && $MAJORREV -lt 42 ]]; then
    echo "OpenSUSE $VERSION_ID is not supported!" >&2
    exit 2
fi
if [[ $ID == 'sles' && $MAJORREV -lt 12 ]]; then
    echo "SLES $VERSION_ID is not supported!" >&2
    exit 2
fi

#END Verify The Installer Choice

echo
echo "*** Installing prerequisites for PowerShell..."
$SUDO zypper --non-interactive install \
        glibc-locale \
        glibc-i18ndata \
        tar \
        curl \
        libunwind \
        libicu \
        openssl \
    && zypper --non-interactive clean --all

##END Check requirements and prerequisites

echo
echo "*** Installing PowerShell for $DistroBasedOn..."

echo "ATTENTION: As of version 1.2.0 this script no longer uses pre-releases unless the '-preview' switch is used"

if [[ "'$*'" =~ preview ]] ; then
    echo
    echo "-preview was used, the latest preview release will be installed (side-by-side with your production release)"
    release=$(curl https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v// | sed s/,//g | sed s/\ //g)
    pwshlink=/usr/bin/pwsh-preview
else
    echo "Finding the latest production release"
    release=$(curl https://api.github.com/repos/PowerShell/PowerShell/releases | grep -Po '"tag_name":(\d*?,|.*?[^\\]",)' | grep -Po '\d+.\d+.\d+[\da-z.-]*' | grep -v '[a-z]' | sort | tail -n1)
fi
#DIRECT DOWNLOAD
package=powershell-${release}-linux-x64.tar.gz
downloadurl=https://github.com/PowerShell/PowerShell/releases/download/v$release/$package


#REPO BASED (Not ready yet)
#echo "*** Setting up PowerShell repo..."
#echo "*** Current version on git is: $release, repo version may differ slightly..."
## Install the Microsoft public key so that zypper trusts the package
#sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
##Add the Repo
#$SUDO sh -c 'echo -e "[code]\nname=PowerShell\nbaseurl=https://packages.microsoft.com/yumrepos/microsoft-sles12-prod\nenabled=1\ntype=rpm-md\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/zypp/repos.d/powershellcore.repo'
## Update zypper
#$SUDO zypper refresh
## Install PowerShell
#$SUDO zypper --non-interactive install powershell

echo "Destination file: $package"
echo "Source URL: $downloadurl"

curl -L -o "$package" "$downloadurl"

if [[ ! -r "$package" ]]; then
    echo "ERROR: $package failed to download! Aborting..." >&2
    exit 1
fi

echo "Installing PowerShell to /opt/microsoft/powershell/$release in overwrite mode"
## Create the target folder where powershell will be placed
$SUDO mkdir -p "/opt/microsoft/powershell/$release"
## Expand powershell to the target folder
$SUDO tar zxf "$package" -C "/opt/microsoft/powershell/$release"

## Change the mode of 'pwsh' to 'rwxr-xr-x' to allow execution
$SUDO chmod 755 "/opt/microsoft/powershell/$release/pwsh"
## Create the symbolic link that points to powershell
$SUDO ln -sfn "/opt/microsoft/powershell/$release/pwsh" $pwshlink

## Add the symbolic link path to /etc/shells
if [ ! -f /etc/shells ] ; then
    echo $pwshlink | $SUDO tee /etc/shells ;
else
    grep -q "^${pwshlink}$" /etc/shells || echo $pwshlink | $SUDO tee --append /etc/shells > /dev/null ;
fi

## Remove the downloaded package file
rm -f "$package"

# shellcheck disable=SC2016
pwsh -noprofile -c '"Congratulations! PowerShell is installed at $PSHOME.
Run `"pwsh`" to start a PowerShell session."'

success=$?

if [[ "$success" != 0 ]]; then
    echo "ERROR: PowerShell failed to install!" >&2
    exit "$success"
fi

if [[ "'$*'" =~ includeide ]] ; then
    echo
    echo "*** Installing VS Code PowerShell IDE..."
    echo "*** Setting up VS Code repo..."
    $SUDO sh -c 'echo -e "[code]\nname=Visual Studio Code\nbaseurl=https://packages.microsoft.com/yumrepos/vscode\nenabled=1\ntype=rpm-md\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/zypp/repos.d/vscode.repo'
    $SUDO zypper refresh
    $SUDO zypper --non-interactive install code

    echo
    echo "*** Installing VS Code PowerShell Extension"
    code --install-extension ms-vscode.PowerShell
    if [[ "'$*'" =~ -interactivetesting ]] ; then
        echo "*** Loading test code in VS Code"
        curl -O ./testpowershell.ps1 https://raw.githubusercontent.com/DarwinJS/CloudyWindowsAutomationCode/master/pshcoredevenv/testpowershell.ps1
        code ./testpowershell.ps1
    fi
fi

if [[ "$repobased" == true ]] ; then
  echo "*** NOTE: Run your regular package manager update cycle to update PowerShell"
else
  echo "*** NOTE: Re-run this script to update PowerShell"
fi
echo "*** Install Complete"
