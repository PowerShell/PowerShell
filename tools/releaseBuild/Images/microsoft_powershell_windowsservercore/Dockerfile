# escape=`
#0.3.6 (no powershell 6)
FROM mcr.microsoft.com/powershell:windowsservercore
LABEL maintainer='PowerShell Team <powershellteam@hotmail.com>'
LABEL description="This Dockerfile for Windows Server Core with git installed via chocolatey."

SHELL ["C:\\Program Files\\PowerShell\\latest\\pwsh.exe", "-command"]
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

# Install makeappx and makepri
ADD https://pscoretestdata.blob.core.windows.net/build-files/makeappx/makeappx.zip?sp=r&st=2019-04-05T18:02:52Z&se=2020-04-06T02:02:52Z&spr=https&sv=2018-03-28&sig=t07uC1K3uFLtINQsmorHobgPh%2B%2BBgjFnmHEJGNZT6Hk%3D&sr=b /makeappx.zip
RUN Expand-Archive /makeappx.zip

COPY PowerShellPackage.ps1 /

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

ENTRYPOINT ["C:\\Program Files\\PowerShell\\latest\\pwsh.exe", "-command"]
