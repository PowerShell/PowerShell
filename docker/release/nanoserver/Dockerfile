# escape=`
FROM microsoft/nanoserver
MAINTAINER brycem@microsoft.com
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL Description="This Dockerfile will install the latest release of PS."

ARG POWERSHELL_ZIP=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/powershell-6.0.0-alpha.14-win10-x64.zip
ARG POWERSHELL_SHA256=3F5FD873B6E3062D9741B019BC645E6F20999BE66B2FDAA4374495FEBEDD0E03

# Setup PowerShell - Log-to > C:\Docker.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "-command"]
ADD $POWERSHELL_ZIP /powershell-win10-x64.zip
ENV POWERSHELL_SHA256=$POWERSHELL_SHA256

# Install PowerShell package and clean up
RUN $ErrorActionPreference='Stop'; `
    $ConfirmPreference='None'; `
    $VerbosePreference='Continue'; `
    Start-Transcript -path C:\Dockerfile.log -append -IncludeInvocationHeader ; `
    $PSVersionTable | Write-Output ; `
    $VerInfo = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' ; `
    ('FullBuildString: '+$VerInfo.BuildLabEx) | Write-Output ; `
    ('OperatingSystem:  '+$VerInfo.ProductName+' '+$VerInfo.EditionId+' '+$VerInfo.InstallationType)  | Write-Output ; `
    [System.IO.FileInfo]$ZipFile = Get-Item -Path ./powershell-win10-x64.zip ; `
    [String]$ZipHash=(Get-FileHash  -Path $ZipFile -Algorithm SHA256 | select -ExpandProperty Hash) ; `
    If ($ZipHash -notmatch $Env:POWERSHELL_SHA256) { `
      Throw [String]$('['+$ZipHash+'] does not match ['+$Env:POWERSHELL_SHA256+']!') `
    } ; `
    New-Item -Path $Env:ProgramFiles/PowerShell -ItemType Directory -Force | out-null ; `
    [System.IO.DirectoryInfo]$PsFolder=New-Item -Path $Env:ProgramFiles\PowerShell -ItemType Directory -Force ; `
    Add-Type -AssemblyName System.IO.Compression.ZipFile ; `
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipFile,$PsFolder) ; `
    if (Get-ChildItem -Path $PsFolder/powershell.exe) { `
      Remove-Item -Path $ZipFile ; `
      New-Item -Type SymbolicLink -Path $PsFolder\ -Name Core -Value $PsFolder `
    } else { throw 'Installation failed!  See c:\Dockerfile.log' } ;

# Verify New Powershell.exe runs
SHELL ["C:\\Program Files\\PowerShell\\Core\\PowerShell.exe", "-command"]
RUN Start-Transcript -path C:\Dockerfile.log -append -IncludeInvocationHeader ; `
    $ErrorActionPreference='Stop'; `
    Write-Output $PSVersionTable ; `
    If (-not($PSVersionTable.PSEdition -Match 'Core')) { `
      Throw [String]$('['+$PSVersionTable.PSEdition+'] is not [Core]!') ; `
    } ;

# Persist %PSCORE% ENV variable for user convenience
ENV PSCORE='"C:\Program Files\PowerShell\Core\PowerShell.exe"'
