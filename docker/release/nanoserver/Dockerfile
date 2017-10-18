# escape=`
FROM microsoft/nanoserver:latest

ARG POWERSHELL_ZIP=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.8/PowerShell-6.0.0-beta.8-win-x64.zip
ARG POWERSHELL_VERSION=6.0.0-beta.8
ARG IMAGE_NAME=microsoft/powershell:nanoserver

LABEL maintainer="PowerShell Team <powershellteam@hotmail.com>" `
      readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md" `
      description="This Dockerfile will install the latest release of PS." `
      org.label-schema.usage="https://github.com/PowerShell/PowerShell/tree/master/docker#run-the-docker-image-you-built" `
      org.label-schema.url="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md" `
      org.label-schema.vcs-url="https://github.com/PowerShell/PowerShell" `
      org.label-schema.name="powershell" `
      org.label-schema.vendor="PowerShell" `
      org.label-schema.version=${POWERSHELL_VERSION} `
      org.label-schema.schema-version="1.0" `
      org.label-schema.docker.cmd="docker run ${IMAGE_NAME} powershell -c '$psversiontable'" `
      org.label-schema.docker.cmd.devel="docker run ${IMAGE_NAME}" `
      org.label-schema.docker.cmd.test="docker run ${IMAGE_NAME} powershell -c Invoke-Pester" `
      org.label-schema.docker.cmd.help="docker run ${IMAGE_NAME} powershell -c Get-Help"

# TODO: addd LABEL org.label-schema.vcs-ref=${VCS_REF}

RUN setx /M PATH "%ProgramFiles%\PowerShell\latest;%PATH%"
# Setup PowerShell - Log-to > C:\Docker.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "-command"]
ADD $POWERSHELL_ZIP /powershell-win-x64.zip

# Install PowerShell package and clean up
RUN $ErrorActionPreference='Stop'; `
    $ConfirmPreference='None'; `
    $VerbosePreference='Continue'; `
    Start-Transcript -path C:\Dockerfile.log -append -IncludeInvocationHeader ; `
    $PSVersionTable | Write-Output ; `
    $VerInfo = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' ; `
    ('FullBuildString: '+$VerInfo.BuildLabEx) | Write-Output ; `
    ('OperatingSystem:  '+$VerInfo.ProductName+' '+$VerInfo.EditionId+' '+$VerInfo.InstallationType)  | Write-Output ; `
    [System.IO.FileInfo]$ZipFile = Get-Item -Path ./powershell-win-x64.zip ; `
    New-Item -Path $Env:ProgramFiles/PowerShell -ItemType Directory -Force | out-null ; `
    [System.IO.DirectoryInfo]$PsFolder=New-Item -Path $Env:ProgramFiles\PowerShell -ItemType Directory -Force ; `
    Add-Type -AssemblyName System.IO.Compression.ZipFile ; `
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipFile,$PsFolder) ; `
    if (Get-ChildItem -Path $PsFolder/powershell.exe) { `
      Remove-Item -Path $ZipFile ; `
      New-Item -Type SymbolicLink -Path $PsFolder\ -Name latest -Value $PsFolder `
    } else { throw 'Installation failed!  See c:\Dockerfile.log' } ;

# Verify New Powershell.exe runs
SHELL ["C:\\Program Files\\PowerShell\\latest\\PowerShell.exe", "-command"]
RUN Start-Transcript -path C:\Dockerfile.log -append -IncludeInvocationHeader ; `
    $ErrorActionPreference='Stop'; `
    Write-Output $PSVersionTable ; `
    If (-not($PSVersionTable.PSEdition -Match 'Core')) { `
      Throw [String]$('['+$PSVersionTable.PSEdition+'] is not [Core]!') ; `
    } ;

# Persist %PSCORE% ENV variable for user convenience
ENV PSCORE='"C:\Program Files\PowerShell\latest\PowerShell.exe"'

CMD ["PowerShell.exe"]
