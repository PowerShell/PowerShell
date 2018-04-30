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
#  -includeide - the script is being run headless, do not perform actions that require response from the console
#  -interactivetests - requires a human user in front of the machine - loads a script into the ide to test with F5 to ensure the IDE can run scripts
#  -skip-sudo-check - skips the check that the user has permission to use sudo.  This is required to run in the VSTS Hosted Linux Preview.

#gitrepo paths are overrideable to run from your own fork or branch for testing or private distribution


VERSION="1.1.2"
gitreposubpath="PowerShell/PowerShell/master"
gitreposcriptroot="https://raw.githubusercontent.com/$gitreposubpath/tools"
thisinstallerdistro=suse
repobased=false
gitscriptname="installpsh-suse.psh"

echo
echo "*** PowerShell Core Development Environment Installer $VERSION for $thisinstallerdistro"
echo "***    Current PowerShell Core Version: $currentpshversion"
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
    #echo "$1" | sed "y/ABCDEFGHIJKLMNOPQRSTUVWXYZ/abcdefghijklmnopqrstuvwxyz/"
    echo "$1" | tr [A-Z] [a-z]
}
if [ "${OS}" == "windowsnt" ]; then
    OS=windows
    DistroBasedOn=windows
elif [ "${OS}" == "darwin" ]; then
    OS=osx
    DistroBasedOn=osx
else
    OS=`uname`
    if [ "${OS}" == "SunOS" ] ; then
        OS=solaris
        ARCH=`uname -p`
        OSSTR="${OS} ${REV}(${ARCH} `uname -v`)"
        DistroBasedOn=sunos
    elif [ "${OS}" == "AIX" ] ; then
        OSSTR="${OS} `oslevel` (`oslevel -r`)"
        DistroBasedOn=aix
    elif [ "${OS}" == "Linux" ] ; then
        if [ -f /etc/redhat-release ] ; then
            DistroBasedOn='redhat'
        elif [ -f /etc/SuSE-release ] ; then
            DistroBasedOn='suse'
        elif [ -f /etc/mandrake-release ] ; then
            DistroBasedOn='mandrake'
        elif [ -f /etc/debian_version ] ; then
            DistroBasedOn='debian'
        fi
        if [ -f /etc/UnitedLinux-release ] ; then
            DIST="${DIST}[`cat /etc/UnitedLinux-release | tr "\n" ' ' | sed s/VERSION//`]"
            DistroBasedOn=unitedlinux
        fi
        OS=`lowercase $OS`
        DistroBasedOn=`lowercase $DistroBasedOn`
    fi
fi

if [ "$DistroBasedOn" != "$thisinstallerdistro" ]; then
  echo "*** This installer is only for $thisinstallerdistro and you are running $DistroBasedOn, please run \"$gitreporoot\install-powershell.sh\" to see if your distro is supported AND to auto-select the appropriate installer if it is."
  exit 0
fi

## Check requirements and prerequisites

#Only do SUDO if we are not root
SUDO=''
if (( $EUID != 0 )); then
    SUDO='sudo'
fi

#Check that sudo is available
if [[ "$SUDO" == "sudo" && ! ("'$*'" =~ skip-sudo-check) ]]; then

    $SUDO -v
    if [ $? -ne 0 ]; then
      echo "ERROR: You must either be root or be able to use sudo" >&2
      exit 5
    fi
fi

#Collect any variation details if required for this distro
source /etc/os-release
MAJORREV=`echo $VERSION_ID | sed 's/\..*//'`
#END Collect any variation details if required for this distro

#If there are known incompatible versions of this distro, put the test, message and script exit here:
if [[ $ID == 'opensuse' && $MAJORREV < 42 ]]; then
    echo "OpenSUSE $VERSION_ID is not supported!" >&2
    exit 2
fi
if [[ $ID == 'sles' && $MAJORREV < 12 ]]; then
    echo "SLES $VERSION_ID is not supported!" >&2
    exit 2
fi

#END Verify The Installer Choice

echo
echo "*** Installing prerequisites for PowerShell Core..."
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
echo "*** Installing PowerShell Core for $DistroBasedOn..."
release=`curl https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v// | sed s/,//g | sed s/\ //g`

#REPO BASED (Not ready yet)
#echo "*** Setting up PowerShell Core repo..."
#echo "*** Current version on git is: $release, repo version may differ slightly..."
## Install the Microsoft public key so that zypper trusts the package
#sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
##Add the Repo
#$SUDO sh -c 'echo -e "[code]\nname=PowerShell Core\nbaseurl=https://packages.microsoft.com/yumrepos/microsoft-sles12-prod\nenabled=1\ntype=rpm-md\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/zypp/repos.d/powershellcore.repo'
## Update zypper
#$SUDO zypper refresh
## Install PowerShell
#$SUDO zypper --non-interactive install powershell

#DIRECT DOWNLOAD
pwshlink=/usr/bin/pwsh
package=powershell-${release}-linux-x64.tar.gz
downloadurl=https://github.com/PowerShell/PowerShell/releases/download/v$release/$package

echo "Destination file: $package"
echo "Source URL: $downloadurl"

curl -L -o "$package" "$downloadurl"

if [[ ! -r "$package" ]]; then
    echo "ERROR: $package failed to download! Aborting..." >&2
    exit 1
fi

echo "Installing PowerShell to /opt/microsoft/powershell/$release in overwrite mode"
## Create the target folder where powershell will be placed
$SUDO mkdir -p /opt/microsoft/powershell/$release
## Expand powershell to the target folder
$SUDO tar zxf $package -C /opt/microsoft/powershell/$release

## Change the mode of 'pwsh' to 'rwxr-xr-x' to allow execution
$SUDO chmod 755 /opt/microsoft/powershell/$release/pwsh
## Create the symbolic link that points to powershell
$SUDO ln -sfn /opt/microsoft/powershell/$release/pwsh $pwshlink

## Add the symbolic link path to /etc/shells
if [ ! -f /etc/shells ] ; then
    echo $pwshlink | $SUDO tee /etc/shells ;
else
    grep -q "^${pwshlink}$" /etc/shells || echo $pwshlink | $SUDO tee --append /etc/shells > /dev/null ;
fi

## Remove the downloaded package file
rm -f $package

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
fi


if [[ "'$*'" =~ -interactivetesting ]] ; then
    echo "*** Loading test code in VS Code"
    curl -O ./testpowershell.ps1 https://raw.githubusercontent.com/DarwinJS/CloudyWindowsAutomationCode/master/pshcoredevenv/testpowershell.ps1
    code ./testpowershell.ps1        
fi

if [[ "$repobased" == true ]] ; then
  echo "*** NOTE: Run your regular package manager update cycle to update PowerShell Core"
else
  echo "*** NOTE: Re-run this script to update PowerShell Core"
fi
echo "*** Install Complete"
