<#
.SYNOPSIS
    Run Attack Surface Analyzer test locally using Docker to analyze PowerShell MSI installation.

.DESCRIPTION
    This script runs Attack Surface Analyzer in a clean Windows container to analyze
    the attack surface changes when installing PowerShell MSI. It takes a baseline
    snapshot, installs the MSI, takes a post-installation snapshot, and exports the
    comparison results.

.PARAMETER MsiPath
    Path to the PowerShell MSI file to test. If not provided, the script will build
    a new MSI using Start-PSBuild.

.PARAMETER NoBuild
    Skip building the MSI and only search for existing MSI files.

.PARAMETER OutputPath
    Directory where results will be saved. Defaults to current directory.

.PARAMETER ContainerImage
    Docker container image to use. Defaults to mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022

.PARAMETER KeepWorkDirectory
    If specified, keeps the temporary work directory after the test completes.

.EXAMPLE
    .\Run-AttackSurfaceAnalyzer.ps1 -MsiPath "C:\path\to\PowerShell.msi"

.EXAMPLE
    .\Run-AttackSurfaceAnalyzer.ps1 -OutputPath "C:\results"

.EXAMPLE
    .\Run-AttackSurfaceAnalyzer.ps1 -NoBuild

.NOTES
    Requires Docker Desktop with Windows containers enabled.
    If Docker is not installed, the script will prompt to install it using winget.
    If MsiPath is not provided and NoBuild is not specified, the script will
    import build.psm1 and build a new MSI package.

    Supports -WhatIf and -Confirm for Docker installation.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string]$MsiPath,

    [Parameter()]
    [switch]$NoBuild,

    [Parameter()]
    [string]$OutputPath = $PWD,

    [Parameter()]
    [string]$ContainerImage = "mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022",

    [Parameter()]
    [switch]$KeepWorkDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "SUCCESS" { "Green" }
        default { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-DockerAvailable {
    try {
        $null = docker version 2>&1
        return $true
    }
    catch {
        return $false
    }
}

function Test-WingetAvailable {
    try {
        $null = Get-Command winget -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Install-DockerDesktop {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact="High")]
    param()

    if (-not (Test-WingetAvailable)) {
        Write-Log "winget is not available. Please install winget (App Installer from Microsoft Store) or install Docker Desktop manually from https://www.docker.com/products/docker-desktop" -Level ERROR
        return $false
    }

    if ($PSCmdlet.ShouldProcess("Docker Desktop", "Install using winget")) {
        Write-Log "Installing Docker Desktop using winget..." -Level SUCCESS
        Write-Log "This may take several minutes..."

        try {
            winget install docker.dockerdesktop --accept-package-agreements --accept-source-agreements

            if ($LASTEXITCODE -eq 0) {
                Write-Log "Docker Desktop installed successfully!" -Level SUCCESS
                Write-Log "Please restart Docker Desktop and ensure Windows containers are enabled, then run this script again." -Level SUCCESS
                return $true
            }
            else {
                Write-Log "Docker Desktop installation failed with exit code: $LASTEXITCODE" -Level ERROR
                return $false
            }
        }
        catch {
            Write-Log "Error installing Docker Desktop: $_" -Level ERROR
            return $false
        }
    }
    else {
        Write-Log "Docker Desktop installation cancelled by user." -Level WARNING
        return $false
    }
}

# Verify Docker is available
Write-Log "Checking Docker availability..."
if (-not (Test-DockerAvailable)) {
    Write-Log "Docker is not available." -Level WARNING
    Write-Log "Docker Desktop is required to run Attack Surface Analyzer tests in containers." -Level WARNING

    if (Install-DockerDesktop) {
        Write-Log "Docker Desktop has been installed. Please restart Docker Desktop and run this script again." -Level SUCCESS
        exit 0
    }
    else {
        Write-Log "Please install Docker Desktop manually from https://www.docker.com/products/docker-desktop and ensure it's running with Windows containers enabled." -Level ERROR
        exit 1
    }
}

# Find or build MSI if not provided
if (-not $MsiPath) {
    if ($NoBuild) {
        Write-Log "No MSI path provided, searching in artifacts directory..."
        $possiblePaths = @(
            "$PSScriptRoot\..\..\artifacts",
            "$PSScriptRoot\..\..\"
        )

        foreach ($path in $possiblePaths) {
            if (Test-Path $path) {
                $msiFiles = Get-ChildItem -Path $path -Filter "*.msi" -Recurse -ErrorAction SilentlyContinue
                if ($msiFiles) {
                    $MsiPath = $msiFiles[0].FullName
                    Write-Log "Found MSI: $MsiPath" -Level SUCCESS
                    break
                }
            }
        }

        if (-not $MsiPath) {
            Write-Log "Could not find MSI file. Please specify -MsiPath parameter or remove -NoBuild to build a new MSI." -Level ERROR
            exit 1
        }
    }
    else {
        # Build the MSI
        Write-Log "No MSI path provided, building PowerShell MSI..." -Level SUCCESS

        # Find the repository root
        $repoRoot = $PSScriptRoot
        while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot "build.psm1"))) {
            $repoRoot = Split-Path $repoRoot -Parent
        }

        if (-not $repoRoot -or -not (Test-Path (Join-Path $repoRoot "build.psm1"))) {
            Write-Log "Could not find build.psm1. Please run this script from the PowerShell repository." -Level ERROR
            exit 1
        }

        Write-Log "Repository root: $repoRoot"

        try {
            # Import build module
            Write-Log "Importing build.psm1..."
            Import-Module (Join-Path $repoRoot "build.psm1") -Force

            # Build PowerShell
            Write-Log "Starting PowerShell build (this may take several minutes)..."
            $buildOutput = Join-Path $repoRoot "out"
            Start-PSBuild -Configuration Release -Output $buildOutput

            if ($LASTEXITCODE -ne 0) {
                Write-Log "Build failed with exit code: $LASTEXITCODE" -Level ERROR
                exit 1
            }

            Write-Log "Build completed successfully" -Level SUCCESS

            # Package the MSI
            Write-Log "Creating MSI package..."
            Start-PSPackage -Type msi -WindowsRuntime win7-x64

            if ($LASTEXITCODE -ne 0) {
                Write-Log "Packaging failed with exit code: $LASTEXITCODE" -Level ERROR
                exit 1
            }

            Write-Log "MSI packaging completed successfully" -Level SUCCESS

            # Find the newly created MSI
            $artifactsPath = Join-Path $repoRoot "artifacts"
            if (Test-Path $artifactsPath) {
                $msiFiles = Get-ChildItem -Path $artifactsPath -Filter "*.msi" -Recurse -ErrorAction SilentlyContinue |
                    Sort-Object LastWriteTime -Descending
                if ($msiFiles) {
                    $MsiPath = $msiFiles[0].FullName
                    Write-Log "Built MSI: $MsiPath" -Level SUCCESS
                }
            }

            if (-not $MsiPath) {
                Write-Log "MSI was built but could not be found in artifacts directory" -Level ERROR
                exit 1
            }
        }
        catch {
            Write-Log "Error during build: $_" -Level ERROR
            Write-Log $_.ScriptStackTrace -Level ERROR
            exit 1
        }
    }
}

# Verify MSI exists
if (-not (Test-Path $MsiPath)) {
    Write-Log "MSI file not found: $MsiPath" -Level ERROR
    exit 1
}

$MsiPath = Resolve-Path $MsiPath
Write-Log "Using MSI: $MsiPath"

# Create output directory
$OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Log "Created output directory: $OutputPath"
}

# Create container work directory
$containerWorkDir = Join-Path $env:TEMP "asa-container-work-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Force -Path $containerWorkDir | Out-Null
Write-Log "Created container work directory: $containerWorkDir"

try {
    # Copy MSI to container work directory
    $msiFileName = Split-Path $MsiPath -Leaf
    $destMsiPath = Join-Path $containerWorkDir $msiFileName
    Write-Log "Copying MSI to work directory..."
    Copy-Item $MsiPath -Destination $destMsiPath

    # Create PowerShell script to run inside container
    Write-Log "Creating container execution script..."
    $scriptContent = @'
# Install .NET tool (ASA)
Write-Host "========================================="
Write-Host "Installing Attack Surface Analyzer..."
Write-Host "========================================="
dotnet tool install -g Microsoft.CST.AttackSurfaceAnalyzer.CLI --version 2.3.328
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to install Attack Surface Analyzer"
    exit 1
}
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Verify ASA is available
Write-Host ""
Write-Host "Verifying ASA installation..."
& "$env:USERPROFILE\.dotnet\tools\asa.exe" --version

# Take baseline snapshot
Write-Host ""
Write-Host "========================================="
Write-Host "Taking baseline snapshot..."
Write-Host "========================================="
& "$env:USERPROFILE\.dotnet\tools\asa.exe" collect -f -s -r -u -p -l --directories "C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to take baseline snapshot"
    exit 1
}

# Install the MSI
Write-Host ""
Write-Host "========================================="
Write-Host "Installing PowerShell MSI..."
Write-Host "========================================="
$msiFile = Get-ChildItem -Path C:\work -Filter *.msi | Select-Object -First 1 -ExpandProperty FullName
Write-Host "MSI file: $msiFile"
Start-Process msiexec.exe -ArgumentList "/i", $msiFile, "/quiet", "/norestart", "/l*v", "C:\work\install.log" -Wait -NoNewWindow
if ($LASTEXITCODE -ne 0) {
    Write-Warning "MSI installation returned exit code: $LASTEXITCODE"
    Write-Host "Check install.log for details"
}

# Take post-installation snapshot
Write-Host ""
Write-Host "========================================="
Write-Host "Taking post-installation snapshot..."
Write-Host "========================================="
& "$env:USERPROFILE\.dotnet\tools\asa.exe" collect -f -s -r -u -p -l --directories "C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to take post-installation snapshot"
    exit 1
}

# Export results
Write-Host ""
Write-Host "========================================="
Write-Host "Exporting comparison results..."
Write-Host "========================================="
& "$env:USERPROFILE\.dotnet\tools\asa.exe" export-collect --outputsarif --savetodatabase
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to export results with exit code: $LASTEXITCODE"
}

# Copy results to work directory
Write-Host ""
Write-Host "========================================="
Write-Host "Copying results to work directory..."
Write-Host "========================================="
Get-ChildItem -Path "*.txt" | ForEach-Object {
    Write-Host "Copying: $($_.Name)"
    Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction SilentlyContinue
}
Get-ChildItem -Path "*.sarif" | ForEach-Object {
    Write-Host "Copying: $($_.Name)"
    Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction SilentlyContinue
}
if (Test-Path "asa.sqlite") {
    Write-Host "Copying: asa.sqlite"
    Copy-Item -Path "asa.sqlite" -Destination C:\work\ -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "========================================="
Write-Host "Attack Surface Analyzer test completed!"
Write-Host "========================================="
'@

    $scriptContent | Set-Content -Path (Join-Path $containerWorkDir "run-asa.ps1") -Encoding UTF8

    # Build Dockerfile content for reference
    Write-Log "Creating Dockerfile for reference..."
    $dockerfileContent = @"
# Dockerfile for Attack Surface Analyzer Testing
# This file is created for reference and can be used to build a custom image
# if you prefer not to use the inline script approach

FROM mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022

# Install Attack Surface Analyzer
RUN dotnet tool install -g Microsoft.CST.AttackSurfaceAnalyzer.CLI --version 2.3.328

# Add tools to PATH
ENV PATH="\${PATH};C:/Users/ContainerAdministrator/.dotnet/tools"

WORKDIR C:/work

# The container expects:
# - MSI file to be mounted to C:/work
# - Script to be mounted to C:/work/run-asa.ps1
# Run with: docker run --rm --isolation process -v "path:C:/work" <image> powershell -ExecutionPolicy Bypass -File C:/work/run-asa.ps1
"@

    $dockerfileContent | Set-Content -Path (Join-Path $containerWorkDir "Dockerfile") -Encoding UTF8

    # Run container with volume mount
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Starting Windows container..." -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Container image: $ContainerImage"
    Write-Log "This may take several minutes..."
    Write-Host ""

    docker run --rm `
        --isolation process `
        -v "${containerWorkDir}:C:\work" `
        $ContainerImage `
        powershell -ExecutionPolicy Bypass -File C:\work\run-asa.ps1

    if ($LASTEXITCODE -ne 0) {
        Write-Log "Container execution failed with exit code: $LASTEXITCODE" -Level WARNING
    }

    # Copy results to output directory
    Write-Host ""
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Copying results to output directory..." -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS

    $resultFiles = @(
        "*_summary.json.txt",
        "*_results.json.txt",
        "*.sarif",
        "asa.sqlite",
        "install.log"
    )

    $copiedCount = 0
    foreach ($pattern in $resultFiles) {
        $files = Get-ChildItem -Path $containerWorkDir -Filter $pattern -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            $destPath = Join-Path $OutputPath $file.Name
            Copy-Item -Path $file.FullName -Destination $destPath -Force
            Write-Log "Copied: $($file.Name) -> $destPath" -Level SUCCESS
            $copiedCount++
        }
    }

    if ($copiedCount -eq 0) {
        Write-Log "Warning: No result files found to copy" -Level WARNING
    }
    else {
        Write-Log "Copied $copiedCount result file(s)" -Level SUCCESS
    }

    Write-Host ""
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Attack Surface Analyzer test completed!" -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Results saved to: $OutputPath" -Level SUCCESS
}
finally {
    # Cleanup
    if (-not $KeepWorkDirectory) {
        Write-Log "Cleaning up temporary work directory..."
        Remove-Item -Path $containerWorkDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Log "Cleanup completed"
    }
    else {
        Write-Log "Work directory preserved at: $containerWorkDir" -Level SUCCESS
    }
}
