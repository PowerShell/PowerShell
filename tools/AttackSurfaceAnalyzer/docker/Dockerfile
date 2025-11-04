# Dockerfile for Attack Surface Analyzer Testing
# This builds a container image with Attack Surface Analyzer pre-installed
# and a built-in test script for testing PowerShell MSI installations

# Use Windows Server Core with .NET SDK as base image
FROM mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022

# Set shell to PowerShell for easier scripting
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install Attack Surface Analyzer as a global .NET tool
RUN dotnet tool install -g Microsoft.CST.AttackSurfaceAnalyzer.CLI --version 2.3.328

# Add .NET tools directory to PATH
RUN $env:PATH += ';C:/Users/ContainerAdministrator/.dotnet/tools'; \
    [Environment]::SetEnvironmentVariable('PATH', $env:PATH, [EnvironmentVariableTarget]::Machine)

# Set working directory
WORKDIR C:/work

# Create the ASA test script directly in the container
RUN @' \
# Attack Surface Analyzer Test Script \
Write-Host "=========================================" \
Write-Host "Installing Attack Surface Analyzer..." \
Write-Host "=========================================" \
dotnet tool install -g Microsoft.CST.AttackSurfaceAnalyzer.CLI --version 2.3.328 \
if ($LASTEXITCODE -ne 0) { \
    Write-Error "Failed to install Attack Surface Analyzer" \
    exit 1 \
} \
$env:PATH += ";$env:USERPROFILE\.dotnet\tools" \
\
# Verify ASA is available \
Write-Host "" \
Write-Host "Verifying ASA installation..." \
& "$env:USERPROFILE\.dotnet\tools\asa.exe" --version \
\
# Take baseline snapshot \
Write-Host "" \
Write-Host "=========================================" \
Write-Host "Taking baseline snapshot..." \
Write-Host "=========================================" \
& "$env:USERPROFILE\.dotnet\tools\asa.exe" collect -f -s -r -u -p -l --directories "C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell" \
if ($LASTEXITCODE -ne 0) { \
    Write-Error "Failed to take baseline snapshot" \
    exit 1 \
} \
\
# Install the MSI \
Write-Host "" \
Write-Host "=========================================" \
Write-Host "Installing PowerShell MSI..." \
Write-Host "=========================================" \
$msiFile = Get-ChildItem -Path C:\work -Filter *.msi | Select-Object -First 1 -ExpandProperty FullName \
Write-Host "MSI file: $msiFile" \
Start-Process msiexec.exe -ArgumentList "/i", $msiFile, "/quiet", "/norestart", "/l*v", "C:\work\install.log" -Wait -NoNewWindow \
if ($LASTEXITCODE -ne 0) { \
    Write-Warning "MSI installation returned exit code: $LASTEXITCODE" \
    Write-Host "Check install.log for details" \
} \
\
# Take post-installation snapshot \
Write-Host "" \
Write-Host "=========================================" \
Write-Host "Taking post-installation snapshot..." \
Write-Host "=========================================" \
& "$env:USERPROFILE\.dotnet\tools\asa.exe" collect -f -s -r -u -p -l --directories "C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell" \
if ($LASTEXITCODE -ne 0) { \
    Write-Error "Failed to take post-installation snapshot" \
    exit 1 \
} \
\
# Export results \
Write-Host "" \
Write-Host "=========================================" \
Write-Host "Exporting comparison results..." \
Write-Host "=========================================" \
& "$env:USERPROFILE\.dotnet\tools\asa.exe" export-collect --outputsarif --savetodatabase \
if ($LASTEXITCODE -ne 0) { \
    Write-Warning "Failed to export results with exit code: $LASTEXITCODE" \
} \
\
# Copy results to work directory \
Write-Host "" \
Write-Host "=========================================" \
Write-Host "Copying results to work directory..." \
Write-Host "=========================================" \
Get-ChildItem -Path "*.txt" | ForEach-Object { \
    Write-Host "Copying: $($_.Name)" \
    Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction SilentlyContinue \
} \
Get-ChildItem -Path "*.sarif" | ForEach-Object { \
    Write-Host "Copying: $($_.Name)" \
    Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction SilentlyContinue \
} \
if (Test-Path "asa.sqlite") { \
    Write-Host "Copying: asa.sqlite" \
    Copy-Item -Path "asa.sqlite" -Destination C:\work\ -ErrorAction SilentlyContinue \
} \
\
Write-Host "" \
Write-Host "=========================================" \
Write-Host "Attack Surface Analyzer test completed!" \
Write-Host "=========================================" \
'@ | Out-File -FilePath C:\Scripts\Run-ASA-Test.ps1 -Encoding utf8

# Create Scripts directory
RUN New-Item -ItemType Directory -Path C:\Scripts -Force | Out-Null

# Default entrypoint runs the ASA test script automatically
ENTRYPOINT ["powershell", "-ExecutionPolicy", "Bypass", "-File", "C:\\Scripts\\Run-ASA-Test.ps1"]

# Label for documentation
LABEL description="Windows container for running Attack Surface Analyzer tests on PowerShell MSI installations"
LABEL version="1.0"
LABEL maintainer="PowerShell Team"
