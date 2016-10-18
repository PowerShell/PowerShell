# escape=`
FROM microsoft/nanoserver
MAINTAINER brycem@microsoft.com
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL Description="This Dockerfile will install and build the latest reslease of PS on NanoServer."

ARG POWERSHELL_ZIP=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.10/powershell-6.0.0-alpha.10-win10-x64.zip
ARG POWERSHELL_SHA256=f394b51b6c8a865c0a1dd0c3645bb354e65339862ea95b60c9ff0226a307ded6

# Preparing shell environment - Log-to > C:\Docker.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "-command"]

RUN [ScriptBlock]$Prof= `
    { `
      $ErrorActionPreference='Stop'; `
      $ConfirmPreference='None'; `
      $VerbosePreference='Continue'; `
      Start-Transcript -path C:\Dockerfile.log -append -IncludeInvocationHeader ; `
      $PSVersionTable | Write-Output ; `
      $RegPath = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' ; `
      $BuildString = $(Get-ItemProperty -Path $RegPath).BuildLabEx ; `
      $EditionId   = $(Get-ItemProperty -Path $RegPath).EditionId ; `
      $ProductName = $(Get-ItemProperty -Path $RegPath).ProductName ; `
      $InstallationType = $(Get-ItemProperty -Path $RegPath).InstallationType ; `
      Write-Output -InputObject $('FullBuildString: '+$BuildString) ; `
      Write-Output -InputObject $('OperatingSystem:  '+$ProductName+' '+$EditionId+' '+$InstallationType) `
    } ; Write-Output $Prof | Out-File $(New-Item -Path $PROFILE -ItemType File -Force).FullName -Encoding utf8 -NoNewline ;

# Setup PowerShell
ADD $POWERSHELL_ZIP /powershell-win10-x64.zip
ENV POWERSHELL_SHA256=$POWERSHELL_SHA256

# Install PowerShell package and clean up
RUN [System.IO.FileInfo]$ZipFile = Get-Item -Path ./powershell-win10-x64.zip ; `
    [String]$ZipHash=(Get-FileHash  -Path $ZipFile -Algorithm SHA256 | select -ExpandProperty Hash) ; `
    If ($ZipHash -notmatch $Env:POWERSHELL_SHA256) { `
      Throw [String]$('['+$ZipHash+'] does not match ['+$Env:POWERSHELL_SHA256+']!') `
    } ; `
    New-Item -Path $Env:ProgramFiles/PowerShell -ItemType Directory -Force | out-null ; `
    [System.IO.DirectoryInfo]$PsFolder=New-Item -Path $Env:ProgramFiles\PowerShell -ItemType Directory -Force ; `
    try { Add-Type -AssemblyName System.IO.Compression.FileSystem `
    } catch {Add-Type -AssemblyName System.IO.Compression.ZipFile `
    } finally { [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipFile,$PsFolder) } ; `
    if (Get-ChildItem -Path $PsFolder/powershell.exe) { `
      Remove-Item -Path $PROFILE ; Remove-Item -Path $ZipFile ; `
      $psexe=Get-Item -Path $Env:ProgramFiles\PowerShell\powershell.exe ; `
      $psexe; `
      New-Item -Type SymbolicLink -Path $PsFolder\ -Name Core -Value $PsFolder `
    } else { throw 'Installation failed!  See c:\Dockerfile.log' } ;

SHELL ["C:\\Program Files\\PowerShell\\Core\\PowerShell.exe", "-command"]

# Verify New Powershell.exe runs
RUN $ErrorActionPreference='Stop'; `
    If ($PSVersionTable.PSEdition -Match 'Core') { `
      Write-Output $PSVersionTable `
    } else { `
      Throw [String]$('['+$PSVersionTable.PSEdition+'] is not [Core]!') `
    } ;
