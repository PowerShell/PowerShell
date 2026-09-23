#!/bin/bash

#Companion code for the blog https://cloudywindows.com
#call this code direction from the web with:
#bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-debian.sh) ARGUMENTS
#bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-debian.sh) <ARGUMENTS>

#Usage - if you do not have the ability to run scripts directly from the web,
#        pull all files in this repo folder and execute, this script
#        automatically prefers local copies of sub-scripts

#Completely automated install requires a root account or sudo with a password requirement

#Switches
# -includeide         - installs VSCode and VSCode PowerShell extension (only relevant to machines with desktop environment)
# -interactivetesting - do a quick launch test of VSCode (only relevant when used with -includeide)
# -skip-sudo-check    - use sudo without verifying its availability (hard to accurately do on some distros)
# -preview            - installs the latest preview release of PowerShell side-by-side with any existing production releases

#gitrepo paths are overrideable to run from your own fork or branch for testing or private distribution

VERSION="1.2.1"
gitreposubpath="PowerShell/PowerShell/master"
gitreposcriptroot="https://raw.githubusercontent.com/$gitreposubpath/tools"
thisinstallerdistro=debian
repobased=true
gitscriptname="installpsh-debian.psh"
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
if [[ -f /etc/lsb-release ]]; then
    . /etc/lsb-release
    DISTRIB_ID=$(lowercase "$DISTRIB_ID")
elif [[ -f /etc/debian_version ]]; then
    DISTRIB_ID="debian"
    DISTRIB_RELEASE=$(cat /etc/debian_version)
fi
#END Collect any variation details if required for this distro

#If there are known incompatible versions of this distro, put the test, message and script exit here:

#END Verify The Installer Choice

##END Check requirements and prerequisites

echo
echo "*** Installing PowerShell for $DistroBasedOn..."
if ! hash curl 2>/dev/null; then
    echo "curl not found, installing..."
    $SUDO apt-get install -y curl
fi

# The executable to test.
PWSH=pwsh

if [[ "'$*'" =~ preview ]] ; then
    echo
    echo "-preview was used, the latest preview release will be installed (side-by-side with your production release)"
    powershellpackageid=powershell-preview
    PWSH=pwsh-preview
fi

currentversion=$(curl https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v// | sed s/,//g | sed s/\ //g)

echo "*** Current version on git is: $currentversion, repo version may differ slightly..."
echo "*** Setting up PowerShell repo..."
# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | $SUDO apt-key add -
#Add the Repo
if [[ "${DISTRIB_ID}" = "linuxmint" ]]; then
    echo "Attempting to remap linuxmint to an appropriate ubuntu version" >&2
    LINUXMINT_VERSION=${DISTRIB_RELEASE}
    #https://en.wikipedia.org/wiki/Linux_Mint_version_history
    case ${LINUXMINT_VERSION} in
        19*)
            DISTRIB_RELEASE=18.04
        ;;
        18*)
            DISTRIB_RELEASE=16.04
        ;;
        17*)
            DISTRIB_RELEASE=14.04
        ;;
        *)
            echo "ERROR: unsupported linuxmint version (${LINUXMINT_VERSION})." >&2
            echo "Supported versions: 19" >&2
            echo "For additional versions open an issue or pull request at: https://github.com/powershell/powershell" >&2
            exit 1
        ;;
    esac
    echo "Remapping linuxmint version ${LINUXMINT_VERSION} to ubuntu version ${DISTRIB_RELEASE}" >&2
fi
case $DISTRIB_ID in
    ubuntu|linuxmint)
        case $DISTRIB_RELEASE in
            22.04|20.04|18.04|16.10|16.04|15.10|14.04)
                curl https://packages.microsoft.com/config/ubuntu/$DISTRIB_RELEASE/prod.list | $SUDO tee /etc/apt/sources.list.d/microsoft.list
            ;;
            *)
                echo "ERROR: unsupported Ubuntu version ($DISTRIB_RELEASE)." >&2
                echo "Supported versions: 14.04, 15.10, 16.04, 16.10, 18.04, 20.04." >&2
                echo "For additional versions open an issue or pull request at: https://github.com/powershell/powershell" >&2
                exit 1
            ;;
        esac
    ;;
    debian)
        DISTRIB_RELEASE=${DISTRIB_RELEASE%%.*}
        case $DISTRIB_RELEASE in
            8|9|10|11)
                curl https://packages.microsoft.com/config/debian/$DISTRIB_RELEASE/prod.list | $SUDO tee /etc/apt/sources.list.d/microsoft.list
            ;;
            *)
                echo "ERROR: unsupported Debian version ($DISTRIB_RELEASE)." >&2
                echo "Supported versions: 8, 9." >&2
                echo "For additional versions open an issue or pull request at: https://github.com/powershell/powershell" >&2
                exit 1
            ;;
        esac
    ;;
    *)
        echo "ERROR: unsupported Debian-based distribution ($DISTRIB_ID)." >&2
        echo "Supported distributions: Debian, Ubuntu." >&2
        exit 1
    ;;
esac

# Update apt-get
$SUDO apt-get update
# Install PowerShell
$SUDO apt-get install -y ${powershellpackageid}

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
    echo "*** Setting up VS Code repo..."
    curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
    $SUDO mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
    $SUDO sh -c 'echo "deb [arch=amd64] http://packages.microsoft.com/repos/vscode stable main" > /etc/apt/sources.list.d/vscode.list'
    $SUDO apt-get update
    $SUDO apt-get install -y code

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
  echo
  echo "*** NOTE: Run your regular package manager update cycle to update PowerShell"
fi
echo "*** Install Complete"
