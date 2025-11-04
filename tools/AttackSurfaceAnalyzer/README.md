# Attack Surface Analyzer Testing

This directory contains tools for running Attack Surface Analyzer (ASA) tests on PowerShell MSI installations using Docker.

## Overview

Attack Surface Analyzer is a Microsoft tool that helps analyze changes to a system's attack surface. These scripts allow you to run ASA tests locally in a clean Windows container to analyze what changes when PowerShell is installed.

## Files

- **Run-AttackSurfaceAnalyzer.ps1** - PowerShell script to run ASA tests locally
- **Dockerfile** - Dockerfile for building a container image with ASA pre-installed
- **README.md** - This documentation file

## Prerequisites

- Windows 10/11 or Windows Server
- Docker Desktop with Windows containers enabled
- PowerShell 5.1 or later
- (Optional) A pre-built PowerShell MSI file to test, or the script will build one for you

### Build Prerequisites (if not providing -MsiPath)

If you want the script to build the MSI automatically, ensure you have:
- .NET SDK (as specified in global.json)
- All PowerShell build dependencies (the script will use Start-PSBuild and Start-PSPackage)
- See the main PowerShell README for full build prerequisites

## Quick Start

### Option 1: Using the PowerShell Script (Recommended)

The simplest way to run ASA tests is using the provided PowerShell script:

```powershell
# Build MSI and run ASA test automatically (default behavior)
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1

# Run with specific MSI file (skips build)
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -MsiPath "C:\path\to\PowerShell.msi"

# Search for existing MSI without building
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -NoBuild

# Specify output directory for results
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -OutputPath "C:\results"

# Keep the temporary work directory for debugging
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 -KeepWorkDirectory
```

The script will:

1. Build PowerShell MSI (if not provided via -MsiPath or -NoBuild)
1. Find or use the specified MSI file
1. Create a temporary work directory
1. Start a Windows container
1. Install Attack Surface Analyzer in the container
1. Take a baseline snapshot
1. Install the PowerShell MSI
1. Take a post-installation snapshot
1. Export comparison results
1. Copy results back to your specified output directory

### Option 2: Using the Dockerfile

If you prefer to build a custom image with ASA pre-installed:

```powershell
# Build the Docker image
docker build -f tools\AttackSurfaceAnalyzer\Dockerfile -t powershell-asa-test .

# Run the container with your MSI
docker run --rm --isolation process `
  -v "C:\path\to\msi\directory:C:\work" `
  powershell-asa-test `
  powershell -File C:\Scripts\Run-ASA-Test.ps1 -MsiPath C:\work\PowerShell.msi
```

## Output Files

The test will generate several output files:

- **`*_summary.json.txt`** - Summary of detected changes
- **`*_results.json.txt`** - Detailed results in JSON format
- **`*.sarif`** - SARIF format results (can be viewed in VS Code)
- **`asa.sqlite`** - SQLite database with full analysis data
- **`install.log`** - MSI installation log file

## Analyzing Results

### Using VS Code

The SARIF files can be opened directly in VS Code with the SARIF Viewer extension to see a formatted view of the findings.

### Using PowerShell

```powershell
# Read the summary file
Get-Content "*_summary.json.txt" | ConvertFrom-Json | Format-List

# Query the SQLite database (requires SQLite tools)
# Example: List all file changes
# sqlite3 asa.sqlite "SELECT * FROM file_system WHERE change_type != 'NONE'"
```

## Troubleshooting

### Docker Not Available

If you get an error that Docker is not available:
1. Install Docker Desktop from https://www.docker.com/products/docker-desktop
2. Ensure Docker is running
3. Switch to Windows containers (right-click Docker tray icon → "Switch to Windows containers")

### Container Fails to Start

- Ensure you have enough disk space (containers can be large)
- Check that Windows containers are enabled in Docker settings
- Try pulling the base image manually: `docker pull mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022`

### No Results Generated

- Check the install.log file for MSI installation errors
- Run with `-KeepWorkDirectory` to inspect the temporary work directory
- Verify the MSI file is valid and not corrupted

## Advanced Usage

### Custom Container Image

You can specify a different container image:

```powershell
.\tools\AttackSurfaceAnalyzer\Run-AttackSurfaceAnalyzer.ps1 `
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
2. Build and push the Docker image to a registry
3. Use the Dockerfile as a base for custom testing scenarios

## More Information

- [Attack Surface Analyzer on GitHub](https://github.com/microsoft/AttackSurfaceAnalyzer)
- [Docker for Windows Documentation](https://docs.docker.com/desktop/windows/)
- [SARIF Documentation](https://sarifweb.azurewebsites.net/)
