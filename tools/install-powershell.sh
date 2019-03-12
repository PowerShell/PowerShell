#!/bin/bash
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

install(){
    #Companion code for the blog https://cloudywindows.com

    #call this code direction from the web with:
    #bash <(wget -qO - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh) <ARGUMENTS>
    #wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh | bash -s <ARGUMENTS>
    #bash <(curl -s https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh) <ARGUMENTS>


    #Usage - if you do not have the ability to run scripts directly from the web,
    #        pull all files in this repo folder and execute, this script
    #        automatically prefers local copies of sub-scripts

    #Completely automated install requires a root account or sudo with a password requirement

    #Switches
    # -includeide         - installs VSCode and VSCode PowerShell extension (only relevant to machines with desktop environment)
    # -interactivetesting - do a quick launch test of VSCode (only relevant when used with -includeide)
    # -skip-sudo-check    - use sudo without verifying its availability (hard to accurately do on some distros)
    # -preview            - installs the latest preview release of PowerShell core side-by-side with any existing production releases

    #gitrepo paths are overrideable to run from your own fork or branch for testing or private distribution

    local VERSION="1.2.0"
    local gitreposubpath="PowerShell/PowerShell/master"
    local gitreposcriptroot="https://raw.githubusercontent.com/$gitreposubpath/tools"
    local gitscriptname="install-powershell.psh"

    echo "Get-PowerShell Core MASTER Installer Version $VERSION"
    echo "Installs PowerShell Core and Optional The Development Environment"
    echo "  Original script is at: $gitreposcriptroot\\$gitscriptname"

    echo "Arguments used: $*"
    echo ""

    # Let's quit on interrupt of subcommands
    trap '
    trap - INT # restore default INT handler
    echo "Interrupted"
    kill -s INT "$$"
    ' INT

    lowercase(){
        echo "$1" | tr "[:upper:]" "[:lower:]"
    }

    local OS
    OS=$(lowercase "$(uname)")
    local KERNEL
    KERNEL=$(uname -r)
    local MACH
    MACH=$(uname -m)
    local DIST
    local DistroBasedOn
    local PSUEDONAME
    local REV

    if [ "${OS}" == "windowsnt" ]; then
        OS=windows
        DistroBasedOn=windows
        SCRIPTFOLDER=$(dirname "$(readlink -f "$0")")
    elif [ "${OS}" == "darwin" ]; then
        OS=osx
        DistroBasedOn=osx
        # readlink doesn't work the same on macOS
        SCRIPTFOLDER=$(dirname "$0")
    else
        SCRIPTFOLDER=$(dirname "$(readlink -f "$0")")
        OS=$(uname)
        if [ "${OS}" == "SunOS" ] ; then
            OS=solaris
            ARCH=$(uname -p)
            OSSTR="${OS} ${REV}(${ARCH} $(uname -v))"
            DistroBasedOn=sunos
        elif [ "${OS}" == "AIX" ] ; then
            OSSTR="${OS} $(oslevel) ($(oslevel -r))"
            DistroBasedOn=aix
        elif [ "${OS}" == "Linux" ] ; then
            if [ -f /etc/redhat-release ] ; then
                DistroBasedOn='redhat'
                DIST=$(sed s/\ release.*// < /etc/redhat-release)
                PSUEDONAME=$( (sed s/.*\(// | sed s/\)//) < /etc/redhat-release )
                REV=$( (sed s/.*release\ // | sed s/\ .*//) < /etc/redhat-release )
            elif [ -f /etc/system-release ] ; then
                DIST=$(sed s/\ release.*// < /etc/system-release)
                PSUEDONAME=$( (sed s/.*\(// | sed s/\)//) < /etc/system-release )
                REV=$( (sed s/.*release\ // | sed s/\ .*//) < /etc/system-release )
                if [[ $DIST == *"Amazon Linux"* ]] ; then
                    DistroBasedOn='amazonlinux'
                else
                    DistroBasedOn='redhat'
                fi
            elif [ -f /etc/SuSE-release ] ; then
                DistroBasedOn='suse'
                PSUEDONAME=$( (tr "\n" ' '| sed s/VERSION.*//) < /etc/SuSE-release )
                REV=$( (grep 'VERSION' | sed s/.*=\ //) < /etc/SuSE-release )
            elif [ -f /etc/mandrake-release ] ; then
                DistroBasedOn='mandrake'
                PSUEDONAME=$( (sed s/.*\(// | sed s/\)//) < /etc/mandrake-release )
                REV=$( (sed s/.*release\ // | sed s/\ .*//) < /etc/mandrake-release )
            elif [ -f /etc/debian_version ] ; then
                DistroBasedOn='debian'
                DIST=$( (grep '^DISTRIB_ID' | awk -F=  '{ print $2 }') < /etc/lsb-release )
                PSUEDONAME=$( (grep '^DISTRIB_CODENAME' | awk -F=  '{ print $2 }') < /etc/lsb-release )
                REV=$( (grep '^DISTRIB_RELEASE' | awk -F=  '{ print $2 }') < /etc/lsb-release)
            fi
            if [ -f /etc/UnitedLinux-release ] ; then
                DIST="${DIST}[$( (tr "\n" ' ' | sed s/VERSION.*//) < /etc/UnitedLinux-release )]"
            fi
            OS=$(lowercase $OS)
            DistroBasedOn=$(lowercase $DistroBasedOn)
        fi
    fi

    echo "Operating System Details:"
    echo "  OS: $OS"
    echo "  DIST: $DIST"
    echo "  DistroBasedOn: $DistroBasedOn"
    echo "  PSUEDONAME: $PSUEDONAME"
    echo "  REV: $REV"
    echo "  KERNEL: $KERNEL"
    echo "  MACH: $MACH"
    echo "  OSSTR: $OSSTR"


    if [ "$DistroBasedOn" == "redhat" ] || [ "$DistroBasedOn" == "debian" ] || [ "$DistroBasedOn" == "osx" ] || [ "$DistroBasedOn" == "suse" ] || [ "$DistroBasedOn" == "amazonlinux" ]; then
        echo "Configuring PowerShell Core Environment for: $DistroBasedOn $DIST $REV"
        if [ -f "$SCRIPTFOLDER/installpsh-$DistroBasedOn.sh" ]; then
            #Script files were copied local - use them
            # shellcheck source=/dev/null
            . "$SCRIPTFOLDER/installpsh-$DistroBasedOn.sh"
        else
            #Script files are not local - pull from remote
            echo "Could not find \"installpsh-$DistroBasedOn.sh\" next to this script..."
            echo "Pulling and executing it from \"$gitreposcriptroot/installpsh-$DistroBasedOn.sh\""
            if [ -n "$(command -v curl)" ]; then
                echo "found and using curl"
                bash <(curl -s $gitreposcriptroot/installpsh-"$DistroBasedOn".sh) "$@"
            elif [ -n "$(command -v wget)" ]; then
                echo "found and using wget"
                bash <(wget -qO- $gitreposcriptroot/installpsh-"$DistroBasedOn".sh) "$@"
            else
                echo "Could not find curl or wget, install one of these or manually download \"$gitreposcriptroot/installpsh-$DistroBasedOn.sh\""
            fi
        fi
    else
        echo "Sorry, your operating system is based on $DistroBasedOn and is not supported by PowerShell Core or this installer at this time."
    fi
}

# run the install function
install "$@";
