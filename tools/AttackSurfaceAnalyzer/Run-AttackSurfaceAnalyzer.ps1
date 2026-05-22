# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Run Attack Surface Analyzer test locally using Docker to analyze PowerShell MSI installation.

.DESCRIPTION
    This script runs Attack Surface Analyzer in a clean Windows container to analyze
    the attack surface changes when installing PowerShell MSI. It takes a baseline
    snapshot, installs the MSI, takes a post-installation snapshot, and exports the
    comparison results.

.PARAMETER MsiPath
    Path to the official signed PowerShell MSI file to test. This must be a released,
    signed MSI from the official PowerShell releases.

.PARAMETER OutputPath
    Directory where results will be saved. Defaults to './asa-results' subdirectory.

.PARAMETER ContainerImage
    Docker container image to use. Defaults to mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022

.PARAMETER KeepWorkDirectory
    If specified, keeps the temporary work directory after the test completes.

.EXAMPLE
    .\Run-AttackSurfaceAnalyzer.ps1 -MsiPath "C:\path\to\PowerShell-7.4.0-win-x64.msi"

.EXAMPLE
    .\Run-AttackSurfaceAnalyzer.ps1 -MsiPath ".\PowerShell-7.4.0-win-x64.msi" -OutputPath "C:\asa-results"

.NOTES
    Requires Docker Desktop with Windows containers enabled.
    Requires an official signed PowerShell MSI file from a released build.

    Docker Desktop Handling:
    - If Docker Desktop is installed but not running, the script will start it automatically
    - If Docker Desktop is not installed, the script will prompt to install it using winget
    - Waits up to 60 seconds for Docker to become available after starting

    MSI Requirements:
    - The MSI must be digitally signed by Microsoft Corporation
    - The MSI must be from an official PowerShell release
    - Local builds or unsigned MSIs are not supported

    Supports -WhatIf and -Confirm for Docker installation and startup.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$MsiPath,

    [Parameter()]
    [string]$OutputPath = (Join-Path $PWD "asa-results"),

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

function Test-MsiSignature {
    param(
        [Parameter(Mandatory)]
        [string]$MsiPath
    )

    Write-Log "Verifying MSI signature..." -Level INFO

    try {
        # Get the digital signature information
        $signature = Get-AuthenticodeSignature -FilePath $MsiPath

        if ($signature.Status -ne 'Valid') {
            Write-Log "MSI signature is not valid. Status: $($signature.Status)" -Level ERROR
            return $false
        }

        # Check if signed by Microsoft Corporation
        $signerCertificate = $signature.SignerCertificate
        if (-not $signerCertificate) {
            Write-Log "No signer certificate found" -Level ERROR
            return $false
        }

        $subject = $signerCertificate.Subject
        Write-Log "Certificate subject: $subject" -Level INFO

        # Check for Microsoft Corporation in the subject
        if ($subject -notmatch "Microsoft Corporation" -and $subject -notmatch "CN=Microsoft Corporation") {
            Write-Log "MSI is not signed by Microsoft Corporation" -Level ERROR
            Write-Log "Expected: Microsoft Corporation" -Level ERROR
            Write-Log "Found: $subject" -Level ERROR
            return $false
        }

        # Check certificate validity
        $validFrom = $signerCertificate.NotBefore
        $validTo = $signerCertificate.NotAfter
        $now = Get-Date

        if ($now -lt $validFrom -or $now -gt $validTo) {
            Write-Log "Certificate is not valid for current date" -Level ERROR
            Write-Log "Valid from: $validFrom to: $validTo" -Level ERROR
            return $false
        }

        Write-Log "MSI signature verification passed" -Level SUCCESS
        Write-Log "Signed by: $($signerCertificate.Subject)" -Level SUCCESS
        Write-Log "Valid from: $validFrom to: $validTo" -Level SUCCESS

        return $true
    }
    catch {
        Write-Log "Error verifying MSI signature: $_" -Level ERROR
        return $false
    }
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

# Verify MSI exists and is properly signed
if (-not (Test-Path $MsiPath)) {
    Write-Log "MSI file not found: $MsiPath" -Level ERROR
    exit 1
}

$MsiPath = Resolve-Path $MsiPath
Write-Log "Using MSI: $MsiPath"

# Verify MSI signature
if (-not (Test-MsiSignature -MsiPath $MsiPath)) {
    Write-Log "MSI signature verification failed. Only official signed PowerShell MSIs are supported." -Level ERROR
    Write-Log "Please download an official PowerShell MSI from: https://github.com/PowerShell/PowerShell/releases" -Level ERROR
    exit 1
}

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
    # Use the static Dockerfile from the docker subfolder
    $dockerContextPath = Join-Path $PSScriptRoot "docker"

    # Copy MSI to Docker build context
    $msiFileName = Split-Path $MsiPath -Leaf
    $destMsiPath = Join-Path $dockerContextPath $msiFileName
    Write-Log "Copying MSI to Docker build context..."
    Copy-Item $MsiPath -Destination $destMsiPath
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

    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Building ASA test container..." -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "This may take several minutes..."

    # Build the asa-reports stage specifically
    $reportsImageName = "powershell-asa-reports:latest"
    docker build --target asa-reports -t $reportsImageName -f $staticDockerfilePath $dockerContextPath

    if ($LASTEXITCODE -ne 0) {
        Write-Log "Docker build failed with exit code: $LASTEXITCODE" -Level ERROR
        exit 1
    }

    Write-Log "Build completed successfully" -Level SUCCESS

    # Extract reports from the built image
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Extracting reports to: $OutputPath" -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS

    $tempContainerName = "asa-reports-extract-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

    try {
        # Create a container from the reports image (but don't run it)
        docker create --name $tempContainerName $reportsImageName

        if ($LASTEXITCODE -ne 0) {
            Write-Log "Failed to create temporary container for extraction" -Level ERROR
            exit 1
        }

        # Try to extract known report file patterns individually
        Write-Log "Extracting report files..." -Level INFO

        # Extract standardized report files directly (no file listing needed)
        # Extract files with standardized names (no wildcards needed)
        Write-Log "Extracting standardized report files..." -Level INFO
        $reportFilePatterns = @(
            "asa.sqlite",
            "asa-results.json",
            "install.log"
        )

        $extractedAny = $false

        foreach ($filename in $reportFilePatterns) {
            try {
                Write-Log "Trying to extract file: $filename" -Level INFO
                docker cp "${tempContainerName}:/$filename" $OutputPath 2>$null

                if ($LASTEXITCODE -eq 0) {
                    Write-Log "Successfully extracted: $filename" -Level SUCCESS
                    $extractedAny = $true
                } else {
                    Write-Log "File not found: $filename" -Level INFO
                }
            }
            catch {
                Write-Log "Error extracting file $filename : $_" -Level WARNING
            }
        }

        # Alternative approach: extract the entire reports directory if individual files don't work
        if (-not $extractedAny) {
            Write-Log "Trying to extract entire directory..." -Level INFO
            docker cp "${tempContainerName}:/" "$OutputPath/reports" 2>$null

            if ($LASTEXITCODE -eq 0) {
                Write-Log "Successfully extracted reports directory" -Level SUCCESS
                $extractedAny = $true
            }
        }

        if ($extractedAny) {
            Write-Log "Report extraction completed successfully" -Level SUCCESS
        } else {
            Write-Log "No reports could be extracted - this may be normal if no issues were found" -Level WARNING
        }
    }
    finally {
        # Clean up the temporary container
        docker rm $tempContainerName -f 2>$null
    }

    # Check what files were extracted
    Write-Host ""
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Checking extracted results..." -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS

    $resultFiles = Get-ChildItem -Path $OutputPath -ErrorAction SilentlyContinue
    $copiedCount = $resultFiles.Count

    if ($copiedCount -eq 0) {
        Write-Log "Warning: No result files found in extracted output" -Level WARNING
    }
    else {
        Write-Log "Successfully extracted $copiedCount file(s):" -Level SUCCESS
        $resultFiles | ForEach-Object {
            if ($_.PSIsContainer) {
                Write-Log "  - $($_.Name) (directory)" -Level SUCCESS
            } else {
                Write-Log "  - $($_.Name) ($([math]::Round($_.Length/1KB, 2)) KB)" -Level SUCCESS
            }
        }
    }

    Write-Host ""
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Attack Surface Analyzer test completed!" -Level SUCCESS
    Write-Log "=========================================" -Level SUCCESS
    Write-Log "Results saved to: $OutputPath" -Level SUCCESS

    # Check for ASA GUI availability and launch interactive analysis
    $dbPath = Join-Path $OutputPath "asa.sqlite"
    $jsonPath = Join-Path $OutputPath "asa-results.json"

    if (Test-Path $dbPath) {
        # Check if ASA CLI is available
        $asaAvailable = $false
        try {
            $asaVersion = asa --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                $asaAvailable = $true
                Write-Log "Attack Surface Analyzer CLI detected: $($asaVersion.Trim())" -Level INFO
            }
        }
        catch {
            # ASA not available via PATH
        }

        # Try dotnet tool global path if ASA not found in PATH
        if (-not $asaAvailable) {
            $globalToolsPath = "$env:USERPROFILE\.dotnet\tools\asa.exe"
            if (Test-Path $globalToolsPath) {
                try {
                    $asaVersion = & $globalToolsPath --version 2>$null
                    if ($LASTEXITCODE -eq 0) {
                        $asaAvailable = $true
                        Write-Log "Attack Surface Analyzer found in global tools: $($asaVersion.Trim())" -Level INFO
                        # Use full path for subsequent commands
                        $asaCommand = $globalToolsPath
                    }
                }
                catch {
                    # Global tools ASA not working
                }
            }
        } else {
            $asaCommand = "asa"
        }

        if ($asaAvailable) {
            Write-Log "Launching Attack Surface Analyzer GUI for interactive analysis..." -Level SUCCESS
            try {
                # Launch ASA GUI with the database file
                $asaProcess = Start-Process -FilePath $asaCommand -ArgumentList "gui", "--databasefilename", "`"$dbPath`"" -PassThru -NoNewWindow:$false

                if ($asaProcess) {
                    Write-Log "ASA GUI launched successfully (PID: $($asaProcess.Id))" -Level SUCCESS
                    Write-Log "Interactive analysis interface is now available" -Level INFO
                } else {
                    Write-Log "Failed to launch ASA GUI" -Level WARNING
                }
            }
            catch {
                Write-Log "Error launching ASA GUI: $_" -Level WARNING
                Write-Log "You can manually launch the GUI with: asa gui --databasefilename `"$dbPath`"" -Level INFO
            }
        } else {
            Write-Log "Attack Surface Analyzer CLI not found" -Level INFO
            Write-Log "Install ASA globally to enable GUI analysis: dotnet tool install -g Microsoft.CST.AttackSurfaceAnalyzer.CLI" -Level INFO
            Write-Log "Then launch GUI manually with: asa gui --databasefilename `"$dbPath`"" -Level INFO
        }
    } else {
        Write-Log "Database file not found - cannot launch ASA GUI" -Level WARNING
    }

    # Also check for VS Code integration for JSON analysis
    if (Test-Path $jsonPath) {
        # Detect if running in VS Code
        $isVSCode = $false

        if ($env:VSCODE_PID -or $env:TERM_PROGRAM -eq "vscode" -or $env:VSCODE_INJECTION -eq "1") {
            $isVSCode = $true
        }

        # Check if 'code' command is available
        if (-not $isVSCode) {
            try {
                $null = & code --version 2>$null
                if ($LASTEXITCODE -eq 0) {
                    $isVSCode = $true
                }
            }
            catch {
                # 'code' command not available
            }
        }

        if ($isVSCode) {
            Write-Log "VS Code detected - opening JSON results for analysis..." -Level INFO
            try {
                & code $jsonPath
                if ($LASTEXITCODE -eq 0) {
                    Write-Log "JSON results file opened in VS Code: $jsonPath" -Level SUCCESS
                } else {
                    Write-Log "Failed to open JSON file in VS Code" -Level WARNING
                }
            }
            catch {
                Write-Log "Error opening JSON file in VS Code: $_" -Level WARNING
            }
        } else {
            Write-Log "JSON analysis results available at: $jsonPath" -Level INFO
            Write-Log "Open this file in VS Code or any JSON viewer for detailed analysis" -Level INFO
        }
    }
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

    # Always cleanup MSI file from Docker build context
    if ($destMsiPath -and (Test-Path $destMsiPath)) {
        Write-Log "Cleaning up MSI file from Docker context..."
        Remove-Item -Path $destMsiPath -Force -ErrorAction SilentlyContinue
    }
}
