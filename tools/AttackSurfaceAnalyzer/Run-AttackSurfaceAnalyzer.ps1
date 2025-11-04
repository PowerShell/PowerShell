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

    Docker Desktop Handling:
    - If Docker Desktop is installed but not running, the script will start it automatically
    - If Docker Desktop is not installed, the script will prompt to install it using winget
    - Waits up to 60 seconds for Docker to become available after starting

    Build Behavior:
    - If MsiPath is not provided and NoBuild is not specified, the script will
      import build.psm1 and build a new MSI package

    Supports -WhatIf and -Confirm for Docker installation and startup.
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

function Test-DockerDesktopInstalled {
    # Check if Docker Desktop executable exists
    $dockerDesktopPaths = @(
        "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe",
        "${env:ProgramFiles(x86)}\Docker\Docker\Docker Desktop.exe",
        "${env:LOCALAPPDATA}\Programs\Docker\Docker Desktop.exe"
    )

    foreach ($path in $dockerDesktopPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    return $null
}

function Test-DockerDesktopRunning {
    $process = Get-Process -Name "Docker Desktop" -ErrorAction SilentlyContinue
    return $null -ne $process
}

function Start-DockerDesktopApp {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    $dockerDesktopPath = Test-DockerDesktopInstalled

    if (-not $dockerDesktopPath) {
        Write-Log "Docker Desktop executable not found." -Level ERROR
        return $false
    }

    if (Test-DockerDesktopRunning) {
        Write-Log "Docker Desktop is already running." -Level SUCCESS
        return $true
    }

    if ($PSCmdlet.ShouldProcess("Docker Desktop", "Start application")) {
        Write-Log "Starting Docker Desktop..." -Level SUCCESS
        Write-Log "This may take a minute for Docker to fully start..."

        try {
            Start-Process -FilePath $dockerDesktopPath -WindowStyle Hidden

            # Wait for Docker to become available (up to 60 seconds)
            $maxWaitSeconds = 60
            $waitedSeconds = 0

            while ($waitedSeconds -lt $maxWaitSeconds) {
                Start-Sleep -Seconds 5
                $waitedSeconds += 5
                Write-Log "Waiting for Docker to start... ($waitedSeconds/$maxWaitSeconds seconds)"

                if (Test-DockerAvailable) {
                    Write-Log "Docker Desktop started successfully!" -Level SUCCESS
                    return $true
                }
            }

            Write-Log "Docker Desktop was started but is not responding yet. Please wait a moment and try again." -Level WARNING
            return $false
        }
        catch {
            Write-Log "Error starting Docker Desktop: $_" -Level ERROR
            return $false
        }
    }
    else {
        Write-Log "Starting Docker Desktop cancelled by user." -Level WARNING
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
    Write-Log "Docker is not responding." -Level WARNING

    # Check if Docker Desktop is installed but not running
    if (Test-DockerDesktopInstalled) {
        Write-Log "Docker Desktop is installed but not running." -Level WARNING

        if (Start-DockerDesktopApp) {
            Write-Log "Docker Desktop is now running and ready." -Level SUCCESS
        }
        else {
            Write-Log "Failed to start Docker Desktop or it's taking longer than expected." -Level ERROR
            Write-Log "Please start Docker Desktop manually and ensure Windows containers are enabled, then run this script again." -Level ERROR
            exit 1
        }
    }
    else {
        # Docker Desktop is not installed
        Write-Log "Docker Desktop is not installed." -Level WARNING
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

            # Import packaging module
            Write-Log "Importing packaging module..."
            $packagingModulePath = Join-Path $repoRoot "tools\packaging\packaging.psm1"
            if (Test-Path $packagingModulePath) {
                Import-Module $packagingModulePath -Force
            }
            else {
                Write-Log "Could not find packaging.psm1 at: $packagingModulePath" -Level ERROR
                exit 1
            }

            # Build PowerShell
            Write-Log "Starting PowerShell build (this may take several minutes)..."
            Write-Log "Running: Start-PSBuild -Runtime win7-x64 -Configuration Release"
            Start-PSBuild -Runtime win7-x64 -Configuration Release -ErrorAction Stop

            if ($LASTEXITCODE -ne 0) {
                Write-Log "Build failed with exit code: $LASTEXITCODE" -Level ERROR
                exit 1
            }

            Write-Log "Build completed successfully" -Level SUCCESS

            # Package the MSI
            Write-Log "Creating MSI package..."
            Write-Log "Running: Start-PSPackage -Type msi -WindowsRuntime win7-x64"
            Start-PSPackage -Type msi -WindowsRuntime win7-x64 -SkipReleaseChecks

            if ($LASTEXITCODE -ne 0) {
                Write-Log "Packaging failed with exit code: $LASTEXITCODE" -Level ERROR
                exit 1
            }

            Write-Log "MSI packaging completed successfully" -Level SUCCESS

            # Find the newly created MSI at the repo root
            Write-Log "Looking for MSI at repo root: $repoRoot"
            $msiFiles = Get-ChildItem -Path $repoRoot -Filter "*.msi" -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending
            if ($msiFiles) {
                $MsiPath = $msiFiles[0].FullName
                Write-Log "Built MSI: $MsiPath" -Level SUCCESS
            }
            else {
                # Also check artifacts directory as fallback
                Write-Log "MSI not found at repo root, checking artifacts directory..."
                $artifactsPath = Join-Path $repoRoot "artifacts"
                if (Test-Path $artifactsPath) {
                    $msiFiles = Get-ChildItem -Path $artifactsPath -Filter "*.msi" -Recurse -ErrorAction SilentlyContinue |
                        Sort-Object LastWriteTime -Descending
                    if ($msiFiles) {
                        $MsiPath = $msiFiles[0].FullName
                        Write-Log "Found MSI in artifacts: $MsiPath" -Level SUCCESS
                    }
                }
            }

            if (-not $MsiPath) {
                Write-Log "MSI was built but could not be found in repo root or artifacts directory" -Level ERROR
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

    # Use the static Dockerfile from the docker subfolder
    $dockerContextPath = Join-Path $PSScriptRoot "docker"
    $staticDockerfilePath = Join-Path $dockerContextPath "Dockerfile"
    Write-Log "Using static Dockerfile: $staticDockerfilePath"
    
    if (-not (Test-Path $staticDockerfilePath)) {
        Write-Log "Static Dockerfile not found at: $staticDockerfilePath" -Level ERROR
        exit 1
    }
    
    Write-Log "Docker build context: $dockerContextPath"

    # Build custom container image from static Dockerfile
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Building custom Attack Surface Analyzer container..." -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS
    
    $imageName = "powershell-asa-local:latest"
    Write-Log "Building image: $imageName"
    Write-Log "This may take several minutes..."
    
    docker build -t $imageName -f $staticDockerfilePath $dockerContextPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Docker build failed with exit code: $LASTEXITCODE" -Level ERROR
        exit 1
    }
    
    Write-Log "Container image built successfully" -Level SUCCESS
    
    # Run container with volume mount
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Starting Windows container..." -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Container image: $imageName"
    Write-Host ""

    docker run --rm `
        --isolation process `
        -v "${containerWorkDir}:C:\work" `
        $imageName

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
