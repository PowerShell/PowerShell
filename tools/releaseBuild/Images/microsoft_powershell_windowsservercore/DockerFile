# escape=`
#0.3.6 (no powershell 6)
FROM microsoft/windowsservercore
LABEL maintainer='PowerShell Team <powershellteam@hotmail.com>'
LABEL description="This Dockerfile for Windows Server Core with git installed via chocolatey."

SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "-command"]
# Install Git, and NuGet
# Git installs to C:\Program Files\Git
# nuget installs to C:\ProgramData\chocolatey\bin\NuGet.exe
COPY dockerInstall.psm1 containerFiles/dockerInstall.psm1
RUN Import-Module ./containerFiles/dockerInstall.psm1; `
    Install-ChocolateyPackage -PackageName git -Executable git.exe; `
    Install-ChocolateyPackage -PackageName nuget.commandline -Executable nuget.exe  -Cleanup

# Install WIX
ADD https://github.com/wixtoolset/wix3/releases/download/wix311rtm/wix311-binaries.zip /wix.zip
COPY wix.psm1 containerFiles/wix.psm1  
RUN Import-Module ./containerFiles/wix.psm1; `
    Install-WixZip -zipPath \wix.Zip

COPY PowerShellPackage.ps1 /

ADD https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.ps1 \install-powershell.ps1
RUN new-item -Path 'C:\Program Files\PowerShell\latest' -ItemType Directory; `
    \install-powershell.ps1 -AddToPath -Destination 'C:\Program Files\PowerShell\latest'

ENTRYPOINT ["C:\\Program Files\\PowerShell\\latest\\pwsh.exe", "-command"]
