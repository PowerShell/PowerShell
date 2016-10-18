# escape=`
FROM microsoft/windowsservercore
MAINTAINER brycem@microsoft.com
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md"
LABEL Description="This Dockerfile will install and build the latest reslease of PS on WindowsServerCore."

ARG POWERSHELL_MSI=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.10/PowerShell_6.0.0.10-alpha.10-win10-x64.msi
ARG POWERSHELL_SHA256=f669482aeab8de04f4da5ac03a36ce6b4e9f6569401b4cc842a4cd59196756a0

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
ADD $POWERSHELL_MSI /PowerShell-win10-x64.msi
ENV POWERSHELL_SHA256=$POWERSHELL_SHA256

# Install PowerShell package and clean up
RUN [System.IO.FileInfo]$MsiFile = Get-Item -Path ./PowerShell-win10-x64.msi ; `
    [String]$MsiHash=(Get-FileHash -Path $MSiFile -Algorithm SHA256 | select -ExpandProperty Hash) ; `
    If ($MsiHash -notmatch $Env:POWERSHELL_SHA256) { `
      Throw [String]$('['+$MsiHash+'] does not match ['+$Env:POWERSHELL_SHA256+']!') `
    } ; `
    Start-Process -FilePath msiexec.exe -ArgumentList '-qn','-i c:\PowerShell-win10-x64.msi', `
      '-log c:\PowerShell-win10-x64.msi.log','-norestart' -wait ; `
    $log=get-content -Path C:\PowerShell-win10-x64.msi.log -Last 10 ; `
    if ($log -match 'Installation success or error status: 0') { `
      Remove-Item -Path $PROFILE ; Remove-Item -Path $MsiFile ; `
      $psexe=Get-Item -Path $Env:ProgramFiles\PowerShell\*\powershell.exe ; `
      $psexe; `
      New-Item -Type SymbolicLink -Path $Env:ProgramFiles\PowerShell\ -Name Core -Value $psexe.DirectoryName `
    } else { throw 'Installation failed!  See c:\PowerShell-win10-x64.msi.log' } ;

SHELL ["C:\\Program Files\\PowerShell\\Core\\PowerShell.exe", "-command"]

# Verify new Core Powershell.exe runs
RUN $ErrorActionPreference='Stop'; `
    If ($PSVersionTable.PSEdition -Match 'Core') { `
      Write-Output $PSVersionTable `
    } else { `
      Throw [String]$('['+$PSVersionTable.PSEdition+'] is not [Core]!') `
    } ;
