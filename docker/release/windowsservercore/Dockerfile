# escape=`
FROM microsoft/windowsservercore

ARG POWERSHELL_VERSION=6.0.0-alpha.14
ARG POWERSHELL_MSI=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/PowerShell_6.0.0.14-alpha.14-win10-x64.msi
ARG POWERSHELL_SHA256=503F3AD52223699765895D3E9615FBD7988194693BCB725BE90C9EF0CD594447

# Maintainer label and metadata as defined at http://label-schema.org
ARG VCS_REF
ARG BUILD_DATE
LABEL maintainer="brycem@microsoft.com" `
      org.label-schema.schema-version="1.0" `
      org.label-schema.vendor="Microsoft" `
      org.label-schema.name="PowerShell" `
      org.label-schema.version=$POWERSHELL_VERSION `
      org.label-schema.license="MIT" `
      org.label-schema.description="PowerShell is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with existing tools and is optimized for dealing with structured data, REST APIs, and object models. It includes a command-line shell, an associated scripting language and a framework for processing cmdlets." `
      org.label-schema.url="https://microsoft.com/powershell" `
      org.label-schema.usage="https://github.com/PowerShell/PowerShell/tree/master/docs/learning-powershell" `
      org.label-schema.build-date=$BUILD_DATE `
      org.label-schema.vcs-url="https://github.com/PowerShell/PowerShell.git" `
      org.label-schema.vcs-ref=$VCS_REF `
      org.label-schema.docker.cmd="docker run -it microsoft/powershell:windowsservercore"

# Setup PowerShell - Log-to > C:\Docker.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "-command"]
ADD $POWERSHELL_MSI /PowerShell-win10-x64.msi
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
    [System.IO.FileInfo]$MsiFile = Get-Item -Path ./PowerShell-win10-x64.msi ; `
    [String]$MsiHash=(Get-FileHash -Path $MSiFile -Algorithm SHA256 | select -ExpandProperty Hash) ; `
    If ($MsiHash -notmatch $Env:POWERSHELL_SHA256) { `
      Throw [String]$('['+$MsiHash+'] does not match ['+$Env:POWERSHELL_SHA256+']!') `
    } ; `
    Start-Process -FilePath msiexec.exe -ArgumentList '-qn','-i c:\PowerShell-win10-x64.msi', `
      '-log c:\PowerShell-win10-x64.msi.log','-norestart' -wait ; `
    $log=get-content -Path C:\PowerShell-win10-x64.msi.log -Last 10 ; `
    if ($log -match 'Installation success or error status: 0') { `
      Remove-Item -Path $MsiFile ; `
      $psexe=Get-Item -Path $Env:ProgramFiles\PowerShell\*\powershell.exe ; `
      New-Item -Type SymbolicLink -Path $Env:ProgramFiles\PowerShell\ -Name Core -Value $psexe.DirectoryName `
    } else { throw 'Installation failed!  See c:\PowerShell-win10-x64.msi.log' } ;

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
