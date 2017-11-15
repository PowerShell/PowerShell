#!/bin/bash

#Companion code for the blog https://cloudywindows.com
#call this code direction from the web with:
#bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-amazonlinux.sh) ARGUMENTS
#bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-amazonlinux.sh) <ARGUMENTS>

#Usage - if you do not have the ability to run scripts directly from the web, 
#        pull all files in this repo folder and execute, this script
#        automatically prefers local copies of sub-scripts

#Completely automated install requires a root account or sudo with a password requirement

#Switches
#  -includeide - the script is being run headless, do not perform actions that require response from the console
#  -interactivetests - requires a human user in front of the machine - loads a script into the ide to test with F5 to ensure the IDE can run scripts

#gitrepo paths are overrideable to run from your own fork or branch for testing or private distribution


VERSION="1.1.2"
gitreposubpath="PowerShell/PowerShell/master"
gitreposcriptroot="https://raw.githubusercontent.com/$gitreposubpath/tools"
thisinstallerdistro=amazonlinux
repobased=false
gitscriptname="installpsh-amazonlinux.psh"

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

lowercase(){
    echo "$1" | sed "y/ABCDEFGHIJKLMNOPQRSTUVWXYZ/abcdefghijklmnopqrstuvwxyz/"
}

OS=`lowercase \`uname\``
KERNEL=`uname -r`
MACH=`uname -m`

if [ "${OS}" == "windowsnt" ]; then
    OS=windows
    DistroBasedOn=windows
    SCRIPTFOLDER=$(dirname $(readlink -f $0))
elif [ "${OS}" == "darwin" ]; then
    OS=osx
    DistroBasedOn=osx
    # readlink doesn't work the same on macOS
    SCRIPTFOLDER=$(dirname $0)
else
    SCRIPTFOLDER=$(dirname $(readlink -f $0))
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
            DIST=`cat /etc/redhat-release |sed s/\ release.*//`
            PSUEDONAME=`cat /etc/redhat-release | sed s/.*\(// | sed s/\)//`
            REV=`cat /etc/redhat-release | sed s/.*release\ // | sed s/\ .*//`
        elif [ -f /etc/system-release ] ; then
            DIST=`cat /etc/system-release |sed s/\ release.*//`
            PSUEDONAME=`cat /etc/system-release | sed s/.*\(// | sed s/\)//`
            REV=`cat /etc/system-release | sed s/.*release\ // | sed s/\ .*//`
            if [[ $DIST == *"Amazon Linux"* ]] ; then
                DistroBasedOn='amazonlinux'
            else
                DistroBasedOn='redhat'
            fi
        elif [ -f /etc/SuSE-release ] ; then
            DistroBasedOn='suse'
            PSUEDONAME=`cat /etc/SuSE-release | tr "\n" ' '| sed s/VERSION.*//`
            REV=`cat /etc/SuSE-release | grep 'VERSION' | sed s/.*=\ //`
        elif [ -f /etc/mandrake-release ] ; then
            DistroBasedOn='mandrake'
            PSUEDONAME=`cat /etc/mandrake-release | sed s/.*\(// | sed s/\)//`
            REV=`cat /etc/mandrake-release | sed s/.*release\ // | sed s/\ .*//`
        elif [ -f /etc/debian_version ] ; then
            DistroBasedOn='debian'
            DIST=`cat /etc/lsb-release | grep '^DISTRIB_ID' | awk -F=  '{ print $2 }'`
            PSUEDONAME=`cat /etc/lsb-release | grep '^DISTRIB_CODENAME' | awk -F=  '{ print $2 }'`
            REV=`cat /etc/lsb-release | grep '^DISTRIB_RELEASE' | awk -F=  '{ print $2 }'`
        fi
        if [ -f /etc/UnitedLinux-release ] ; then
            DIST="${DIST}[`cat /etc/UnitedLinux-release | tr "\n" ' ' | sed s/VERSION.*//`]"
        fi
        OS=`lowercase $OS`
        DistroBasedOn=`lowercase $DistroBasedOn`
        readonly OS
        readonly DIST
        readonly DistroBasedOn
        readonly PSUEDONAME
        readonly REV
        readonly KERNEL
        readonly MACH
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
if [[ "$SUDO" -eq "sudo" ]]; then

    $SUDO -v
    if [ $? -ne 0 ]; then
      echo "ERROR: You must either be root or be able to use sudo" >&2
      exit 5
    fi
fi

#Collect any variation details if required for this distro

#END Verify The Installer Choice

echo
echo "*** Installing prerequisites for PowerShell Core..."
$SUDO yum install -y \
        curl \
        libunwind \
        libicu \
        libcurl \
        openssl \
        libuuid.x86_64 \
    && yum clean all

##END Check requirements and prerequisites

echo
echo "*** Installing PowerShell Core for $DistroBasedOn..."
release=`curl https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v//g | sed s/,//g | sed s/\ //g`

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
    echo "Amazon Linux does not have a desktop manager to support vscode, ignoring -includeide"
fi

if [[ "'$*'" =~ -interactivetesting ]] ; then
    echo
    echo "Amazon Linux does not have a desktop manager to support vscode, ignoring -includeide"
fi

if [[ "$repobased" == true ]] ; then
  echo "*** NOTE: Run your regular package manager update cycle to update PowerShell Core"
else
  echo "*** NOTE: Re-run this script to update PowerShell Core"
fi
echo "*** Install Complete"
