# Attack Surface Analyzer Testing

This directory contains tools for running Attack Surface Analyzer (ASA) tests on PowerShell MSI installations using Docker.

## Overview

Attack Surface Analyzer is a Microsoft tool that helps analyze changes to a system's attack surface. These scripts allow you to run ASA tests locally in a clean Windows container to analyze what changes when PowerShell is installed.

## Files

- **Run-AttackSurfaceAnalyzer.ps1** - PowerShell script to run ASA tests with official MSIs
- **Summarize-AsaResults.ps1** - PowerShell script to analyze and summarize ASA results
- **docker/Dockerfile** - Multi-stage Dockerfile for building a container image with ASA pre-installed
- **README.md** - This documentation file

## Docker Architecture

The Docker implementation uses a multi-stage build to optimize the testing and result extraction process:

### Multi-Stage Build Stages

1. **asa-runner**: Main execution environment
   - Base: `mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022`
   - Contains Attack Surface Analyzer CLI tools
   - Runs the complete test workflow
   - Generates reports in both `C:\work` and `C:\reports` directories

1. **asa-reports**: Minimal results layer
   - Base: `mcr.microsoft.com/windows/nanoserver:ltsc2022`
   - Contains only the test reports from the runner stage
   - Enables clean extraction of results without container internals

1. **final**: Default stage (inherits from asa-runner)
   - Provides backward compatibility
   - Used when no specific build target is specified

### Benefits

- **Clean Result Extraction**: Reports are isolated in a dedicated layer
- **Efficient Transfer**: Only test results are copied, not the entire container filesystem
- **Fallback Support**: Script includes fallback to volume-based extraction if needed
- **Minimal Footprint**: Final results layer contains only the necessary output files

## Prerequisites

- Windows 10/11 or Windows Server
- Docker Desktop with Windows containers enabled
- PowerShell 5.1 or later
- **An official signed PowerShell MSI file** from a released build

### MSI Requirements

**Important:** This tool now requires an official, digitally signed PowerShell MSI from Microsoft releases:

- **Must be signed** by Microsoft Corporation
- **Must be from an official release** (downloaded from [PowerShell Releases](https://github.com/PowerShell/PowerShell/releases))
- **Local builds are not supported** - unsigned or development MSIs will be rejected
- The script automatically verifies the digital signature before proceeding

**Where to get official MSIs:**

- Download from: https://github.com/PowerShell/PowerShell/releases
- Look for files like: `PowerShell-7.x.x-win-x64.msi`

## Quick Start

### Option 1: Using the PowerShell Script (Recommended)

The script requires an official signed PowerShell MSI file:

```powershell
# Run ASA test with official MSI (MsiPath is required)
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -MsiPath "C:\path\to\PowerShell-7.4.0-win-x64.msi"

# Specify custom output directory for results
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -MsiPath ".\PowerShell-7.4.0-win-x64.msi" -OutputPath "C:\asa-results"

# Keep the temporary work directory for debugging
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -MsiPath ".\PowerShell-7.4.0-win-x64.msi" -KeepWorkDirectory
```

The script will:

1. **Verify MSI signature** - Ensures the MSI is officially signed by Microsoft Corporation
1. Create a temporary work directory
1. Build a custom Docker container from the static Dockerfile
1. Start the Windows container with Attack Surface Analyzer
1. Take a baseline snapshot
1. Install the PowerShell MSI
1. Take a post-installation snapshot
1. Export comparison results
1. Copy results back to your specified output directory

**Security Note:** The script will reject any MSI that is not digitally signed by Microsoft Corporation to ensure analysis is performed only on official releases.

### Option 2: Using the Dockerfile

If you prefer to build and use the container image directly:

```powershell
# Build the Docker image (Dockerfile is in docker subfolder with clean context)
docker build -f tools\AttackSurfaceAnalyzer\docker\Dockerfile -t powershell-asa-test tools\AttackSurfaceAnalyzer\docker\

# Run the container with your MSI (script is built into the container)
docker run --rm --isolation process `
  -v "C:\path\to\msi\directory:C:\work" `
  powershell-asa-test
```

## Output Files

The test will generate output files in the `./asa-results/` directory (or your specified `-OutputPath`):

- **`asa.sqlite`** - SQLite database with full analysis data (primary result file)
- **`install.log`** - MSI installation log file
- **`*_summary.json.txt`** - Summary of detected changes (if generated)
- **`*_results.json.txt`** - Detailed results in JSON format (if generated)
- **`*.sarif`** - SARIF format results (if generated, can be viewed in VS Code)

## Analyzing Results

### Using the Summary Script (Recommended)

Use the included summary script to get a comprehensive analysis:

```powershell
# Basic summary of ASA results
.\tools\AttackSurfaceAnalyzer\Summarize-AsaResults.ps1

# Detailed analysis with rule breakdowns
.\tools\AttackSurfaceAnalyzer\Summarize-AsaResults.ps1 -ShowDetails

# Analyze results from a specific location
.\tools\AttackSurfaceAnalyzer\Summarize-AsaResults.ps1 -Path "C:\custom\path\asa-results.json" -ShowDetails
```

The summary script provides:

- **Overall statistics** - Total findings, analysis levels, category breakdowns
- **Rule analysis** - Which security rules were triggered and how often
- **File analysis** - Detailed breakdown of file-related security issues by rule type
- **Category cross-reference** - Shows which rules affect which categories

### Using VS Code

The SARIF files can be opened directly in VS Code with the SARIF Viewer extension to see a formatted view of the findings.

### Using PowerShell

```powershell
# Read the JSON results directly
$results = Get-Content "asa-results\asa-results.json" | ConvertFrom-Json
$results.Results.FILE_CREATED.Count  # Number of files created

# Query the SQLite database (requires SQLite tools)
# Example: List all file changes
# sqlite3 asa.sqlite "SELECT * FROM file_system WHERE change_type != 'NONE'"
```

## Troubleshooting

### Docker Not Available

The script automatically handles Docker Desktop installation and startup:

**If Docker Desktop is installed but not running:**

- The script will automatically start Docker Desktop for you
- It waits up to 60 seconds for Docker to become available
- You'll be prompted for confirmation (supports `-Confirm` and `-WhatIf`)

**If Docker Desktop is not installed:**

- The script will prompt you to install it automatically using winget
- After installation completes, start Docker Desktop and run the script again

**Manual Installation:**

1. Install Docker Desktop from https://www.docker.com/products/docker-desktop
1. Ensure Docker is running
1. Switch to Windows containers (right-click Docker tray icon → "Switch to Windows containers")

### Container Fails to Start

- Ensure you have enough disk space (containers can be large)
- Check that Windows containers are enabled in Docker settings
- Try pulling the base image manually: `docker pull mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022`

### MSI Signature Verification Fails

If you get signature verification errors:

- **Ensure you're using an official MSI** from [PowerShell Releases](https://github.com/PowerShell/PowerShell/releases)
- **Do not use local builds** - only signed release MSIs are supported  
- **Check certificate validity** - very old MSIs may have expired certificates
- **Verify file integrity** - redownload the MSI if it may be corrupted

### No Results Generated

- Check the install.log file for MSI installation errors
- Run with `-KeepWorkDirectory` to inspect the temporary work directory
- Verify the MSI file is valid and not corrupted

## Advanced Usage

### Parameters

The `Run-AttackSurfaceAnalyzer.ps1` script supports these parameters:

- **`-MsiPath`** (Required) - Path to the official signed PowerShell MSI file
- **`-OutputPath`** (Optional) - Directory for results (defaults to `./asa-results`)
- **`-ContainerImage`** (Optional) - Custom container base image
- **`-KeepWorkDirectory`** (Optional) - Keep temp directory for debugging

Example with custom container image:

```powershell
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 `
  -MsiPath ".\PowerShell-7.4.0-win-x64.msi" `
  -ContainerImage "mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022"
```

### Debugging

To debug issues, keep the work directory and examine the files:

```powershell
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -KeepWorkDirectory

# The script will print the work directory path
# You can then examine:
# - run-asa.ps1 - The script that runs in the container
# - install.log - MSI installation log
# - Any other generated files
```

## Integration with CI/CD

These tools were extracted from the GitHub Actions workflow to allow local testing. If you need to integrate ASA testing back into a CI/CD pipeline, you can:

1. Use the PowerShell script directly in your pipeline
1. Build and push the Docker image to a registry
1. Use the Dockerfile as a base for custom testing scenarios

## More Information

- [Attack Surface Analyzer on GitHub](https://github.com/microsoft/AttackSurfaceAnalyzer)
- [Docker for Windows Documentation](https://docs.docker.com/desktop/windows/)
- [SARIF Documentation](https://sarifweb.azurewebsites.net/)
