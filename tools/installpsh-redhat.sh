#!/bin/bash

#Companion code for the blog https://cloudywindows.com
#call this code direction from the web with:
#bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-redhat.sh) ARGUMENTS
#bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/installpsh-redhat.sh) <ARGUMENTS>

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
thisinstallerdistro=redhat
repobased=true
gitscriptname="installpsh-redhat.psh"

echo
echo "*** PowerShell Core Development Environment Installer $VERSION for $thisinstallerdistro"
echo "***    Current PowerShell Core Version: $currentpshversion"
echo "***    Original script is at: $gitreposcriptroot/$gitscriptname"
echo
echo "*** Arguments used: $* "
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
        elif [ -f /etc/system-release ] ; then
            DistroBasedOn='redhat'
        elif [ -f /etc/SuSE-release ] ; then
            DistroBasedOn='suse'
        elif [ -f /etc/mandrake-release ] ; then
            DistroBasedOn='mandrake'
        elif [ -f /etc/debian_version ] ; then
            DistroBasedOn='debian'
        fi
        if [ -f /etc/UnitedLinux-release ] ; then
            DIST="${DIST}[`cat /etc/UnitedLinux-release | tr "\n" ' ' | sed s/VERSION.*//`]"
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

#END Collect any variation details if required for this distro

#If there are known incompatible versions of this distro, put the test, message and script exit here:

#END Verify The Installer Choice

##END Check requirements and prerequisites

echo
echo "*** Installing PowerShell Core for $DistroBasedOn..."
if ! hash curl 2>/dev/null; then
    echo "curl not found, installing..."
    $SUDO yum install -y curl
fi
release=`curl https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v// | sed s/,//g | sed s/\ //g`

echo "*** Current version on git is: $release, repo version may differ slightly..."

echo "*** Setting up PowerShell Core repo..."
$SUDO curl https://packages.microsoft.com/config/rhel/7/prod.repo > /etc/yum.repos.d/microsoft.repo
$SUDO yum install -y powershell

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
    $SUDO rpm --import https://packages.microsoft.com/keys/microsoft.asc
    $SUDO sh -c 'echo -e "[code]\nname=Visual Studio Code\nbaseurl=https://packages.microsoft.com/yumrepos/vscode\nenabled=1\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/yum.repos.d/vscode.repo'
    yum check-update
    $SUDO yum install -y code

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
fi
echo "*** Install Complete"
