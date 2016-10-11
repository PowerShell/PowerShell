FROM microsoft/windowsservercore
MAINTAINER brycem@microsoft.com

ARG POWERSHELL_RELEASE=v6.0.0-alpha.10
ARG POWERSHELL_PACKAGE=powershell-6.0.0-alpha.10-win10-x64.zip

ENV POWERSHELL_RELEASE $POWERSHELL_RELEASE
ENV POWERSHELL_PACKAGE $POWERSHELL_PACKAGE

# Install PowerShell package and clean up
RUN powershell.exe -NonInteractive -Command \
  $ErrorActionPreference = 'Stop'; \
  $GetUri = 'https://github.com/PowerShell/PowerShell/releases/download/'+$Env:POWERSHELL_RELEASE+'/'+$Env:POWERSHELL_PACKAGE; \
  [System.IO.FileInfo]$tempFile = [System.IO.Path]::GetTempFileName() ; \
  Invoke-WebRequest -UseBasicParsing -Uri $GetUri -OutFile $tempFile ; \
  [System.IO.DirectoryInfo]$PsFolder=New-Item -Path $Env:ProgramFiles/PowerShell -ItemType Directory -Force ; \
  try {Add-Type -AssemblyName System.IO.Compression.FileSystem \
  } catch {Add-Type -AssemblyName System.IO.Compression.ZipFile \
  } finally { [System.IO.Compression.ZipFile]::ExtractToDirectory($tempFile,$PsFolder) } \
  if (-not(test-path $PsFolder/powershell.exe)) {Throw 'Powershell.exe not successfully installed.' \
  } else { Remove-Item -Path $tempFile -Force -ErrorAction Continue } \
  