# escape=`
#0.3.6 (no powershell 6)
FROM travisez13/microsoft.windowsservercore.build-tools:latest
LABEL maintainer='PowerShell Team <powershellteam@hotmail.com>'
LABEL description="This Dockerfile for Windows Server Core with git installed via chocolatey."

SHELL ["powershell"]

COPY PowerShellPackage.ps1 /

ENTRYPOINT ["powershell"]
