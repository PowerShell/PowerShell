# Dockerfile for Attack Surface Analyzer Testing
# This builds a container image with Attack Surface Analyzer pre-installed
# for testing PowerShell MSI installations

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

# Create a helper script for running ASA tests
RUN @' \
# Helper script for running Attack Surface Analyzer tests \
param( \
    [Parameter(Mandatory=$true)] \
    [string]$MsiPath \
) \
\
$ErrorActionPreference = ''Stop'' \
\
Write-Host ''========================================='' \
Write-Host ''Attack Surface Analyzer Test Runner'' \
Write-Host ''========================================='' \
Write-Host '''' \
Write-Host ''MSI Path: $MsiPath'' \
Write-Host '''' \
\
# Verify ASA is available \
Write-Host ''Verifying Attack Surface Analyzer installation...'' \
& asa --version \
if ($LASTEXITCODE -ne 0) { \
    Write-Error ''Attack Surface Analyzer is not properly installed'' \
    exit 1 \
} \
\
# Take baseline snapshot \
Write-Host '''' \
Write-Host ''Taking baseline snapshot...'' \
Write-Host ''========================================='' \
& asa collect -f -s -r -u -p -l --directories ''C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell'' \
if ($LASTEXITCODE -ne 0) { \
    Write-Error ''Failed to take baseline snapshot'' \
    exit 1 \
} \
Write-Host ''Baseline snapshot completed'' \
\
# Install the MSI \
Write-Host '''' \
Write-Host ''Installing PowerShell MSI...'' \
Write-Host ''========================================='' \
if (-not (Test-Path $MsiPath)) { \
    Write-Error ''MSI file not found: $MsiPath'' \
    exit 1 \
} \
\
$logPath = ''C:\work\install.log'' \
Write-Host ''Running: msiexec.exe /i $MsiPath /quiet /norestart /l*v $logPath'' \
$process = Start-Process msiexec.exe -ArgumentList ''/i'', $MsiPath, ''/quiet'', ''/norestart'', ''/l*v'', $logPath -Wait -NoNewWindow -PassThru \
Write-Host ''MSI installation completed with exit code: $($process.ExitCode)'' \
\
if ($process.ExitCode -ne 0) { \
    Write-Warning ''MSI installation returned non-zero exit code. Check install.log for details.'' \
} \
\
# Take post-installation snapshot \
Write-Host '''' \
Write-Host ''Taking post-installation snapshot...'' \
Write-Host ''========================================='' \
& asa collect -f -s -r -u -p -l --directories ''C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell'' \
if ($LASTEXITCODE -ne 0) { \
    Write-Error ''Failed to take post-installation snapshot'' \
    exit 1 \
} \
Write-Host ''Post-installation snapshot completed'' \
\
# Export results \
Write-Host '''' \
Write-Host ''Exporting comparison results...'' \
Write-Host ''========================================='' \
& asa export-collect --outputsarif --savetodatabase \
if ($LASTEXITCODE -ne 0) { \
    Write-Warning ''Export completed with exit code: $LASTEXITCODE'' \
} \
\
# List and copy results to work directory \
Write-Host '''' \
Write-Host ''Copying results to work directory...'' \
Write-Host ''========================================='' \
$resultFiles = Get-ChildItem -Path . -Include ''*.txt'', ''*.sarif'', ''asa.sqlite'' -Recurse \
foreach ($file in $resultFiles) { \
    Copy-Item -Path $file.FullName -Destination C:\work\ -Force -ErrorAction SilentlyContinue \
    Write-Host ''Copied: $($file.Name)'' \
} \
\
Write-Host '''' \
Write-Host ''========================================='' \
Write-Host ''Attack Surface Analyzer test completed!'' \
Write-Host ''========================================='' \
Write-Host ''Results are available in C:\work directory'' \
'@ | Out-File -FilePath C:\Scripts\Run-ASA-Test.ps1 -Encoding utf8

# Create Scripts directory
RUN New-Item -ItemType Directory -Path C:\Scripts -Force | Out-Null

# Default command shows help
CMD ["powershell", "-NoProfile", "-Command", \
    "Write-Host 'Attack Surface Analyzer Container'; \
    Write-Host ''; \
    Write-Host 'Usage:'; \
    Write-Host '  docker run --rm --isolation process -v \"<local-path>:C:\\work\" <image> powershell -File C:\\Scripts\\Run-ASA-Test.ps1 -MsiPath C:\\work\\<msi-file>'; \
    Write-Host ''; \
    Write-Host 'Example:'; \
    Write-Host '  docker run --rm --isolation process -v \"C:\\temp:C:\\work\" asa-test powershell -File C:\\Scripts\\Run-ASA-Test.ps1 -MsiPath C:\\work\\PowerShell.msi'; \
    Write-Host ''; \
    Write-Host 'The local path should contain the MSI file to test.'; \
    Write-Host 'Results will be written back to the same directory.'"]

# Label for documentation
LABEL description="Windows container for running Attack Surface Analyzer tests on PowerShell MSI installations"
LABEL version="1.0"
LABEL maintainer="PowerShell Team"
