#!/bin/bash
if [ $1 == "Install" ]; then
    # Install dependencies and clean up
    ./install-powershell.sh
    nvm install 6.4.0
    npm install -g markdown-spellcheck@0.11.0;
    apt-get install less
elif [ $1 == "Bootstrap" ]; then
    pwsh -command ". ./vsts.ps1; Invoke-PSBootstrap"
elif [ $1 == "Build" ]; then
    pwsh -command ". ./vsts.ps1; Invoke-PSBuild"
elif [ $1 == "Test" ]; then
    cd ..
    pwsh -command ". ./tools/vsts.ps1; Invoke-PSTest"
elif [ $1 == "AfterTest" ]; then
    pwsh -command ". ./vsts.ps1; Invoke-PSAfterTest"
fi
