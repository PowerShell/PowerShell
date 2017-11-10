#!/bin/bash

#Companion code for the blog https://cloudywindows.com
#call this code direction from the web with:
#bash <(wget -O - https://raw.githubusercontent.com/DarwinJS/CloudyWindowsAutomationCode/master/pshcoredevenv/pshcoredevenv-debian.sh) ARGUMENTS
#bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh) <ARGUMENTS>

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
thisinstallerdistro=osx
repobased=true
gitscriptname="installpsh-osx.sh"

echo "*** PowerShell Core Development Environment Installer $VERSION for $thisinstallerdistro"
echo "***    Current PowerShell Core Version: $currentpshversion"
echo "***    Original script is at: $gitreposcriptroot/$gitscriptname"
echo "*** Arguments used: $*"

# Let's quit on interrupt of subcommands
trap '
  trap - INT # restore default INT handler
  echo "Interrupted"
  kill -s INT "$$"
' INT

#Verify The Installer Choice (for direct runs of this script)
lowercase(){
    echo "$1" | tr [A-Z] [a-z]
}

OS=`lowercase \`uname\``

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
#if [[ "$SUDO" -eq "sudo" ]]; then
#    $SUDO -v
#    if [ $? -ne 0 ]; then
#      echo "ERROR: You must either be root or be able to use sudo" >&2
#      exit 5
#    fi
#fi

#END Collect any variation details if required for this distro

#If there are known incompatible versions of this distro, put the test, message and script exit here:

#END Verify The Installer Choice

##END Check requirements and prerequisites

echo "*** Installing PowerShell Core for $DistroBasedOn..."

#release=`curl https://api.github.com/repos/powershell/powershell/releases/latest | sed '/tag_name/!d' | sed s/\"tag_name\"://g | sed s/\"//g | sed s/v//g | sed s/,//g | sed s/\ //g`

if ! hash brew 2>/dev/null; then
    echo "Homebrew is not found, installing..."
    ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)" < /dev/null 2> /dev/null
else
    echo "Howebrew is already installed, skipping..."
fi

if ! hash brew 2>/dev/null; then
    echo "ERROR: brew did not install correctly, exiting..." >&2
    exit 3
fi

# Suppress output, it's very noisy on travis-ci
echo "Refreshing Homebrew cache..."
for count in {1..2}; do
    # Try the update twice if the first time fails
    brew update > /dev/null && break

    # If the update fails again after increasing the Git buffer size, exit with error.
    if [[ $count == 2 ]]; then
        echo "ERROR: Refreshing Homebrew cache failed..." >&2
        exit 2
    fi

    # The update failed for the first try. An error we see a lot in our CI is "RPC failed; curl 56 SSLRead() return error -36".
    # What 'brew update' does is to fetch the newest version of Homebrew from GitHub using git, and the error comes from git.
    # A potential solution is to increase the Git buffer size to a larger number, say 150 mb. The default buffer size is 1 mb.
    echo "First attempt of update failed. Increase Git buffer size and try again ..."
    git config --global http.postBuffer 157286400
    sleep 5
done

# Suppress output, it's very noisy on travis-ci
if [[ ! -d $(brew --prefix cask) ]]; then
    echo "Installing cask..."
    if ! brew tap caskroom/cask >/dev/null; then
        echo "ERROR: Cask failed to install! Cannot install powershell..." >&2
        exit 2
    fi
fi

if ! hash pwsh 2>/dev/null; then
    echo "Installing PowerShell..."
    if ! brew cask install powershell; then
        echo "ERROR: PowerShell failed to install! Cannot install powershell..." >&2
    fi
else
    echo "PowerShell is already installed, skipping..."
fi

if [[ "'$*'" =~ includeide ]] ; then
    echo "*** Installing VS Code PowerShell IDE..."
    if [[ ! -d $(brew --prefix visual-studio-code) ]]; then
        if ! brew cask install visual-studio-code; then
            echo "ERROR: Visual Studio Code failed to install..." >&2
            exit 1
        fi
    else
        brew upgrade visual-studio-code
    fi

    echo "*** Installing VS Code PowerShell Extension"
    code --install-extension ms-vscode.PowerShell
fi

pwsh -noprofile -c '"Congratulations! PowerShell is installed at $PSHOME.
Run `"pwsh`" to start a PowerShell session."'

success=$?

if [[ "$success" != 0 ]]; then
    echo "ERROR: PowerShell failed to install!" >&2
    exit "$success"
fi

if [[ "'$*'" =~ -interactivetesting ]] ; then
    echo "*** Loading test code in VS Code"
    $SUDO curl -O ./testpowershell.ps1 https://raw.githubusercontent.com/DarwinJS/CloudyWindowsAutomationCode/master/pshcoredevenv/testpowershell.ps1
    code ./testpowershell.ps1        
fi

if [[ "$repobased" == true ]] ; then
  echo "*** NOTE: Run your regular package manager update cycle to update PowerShell Core"
fi
echo "*** Install Complete"
