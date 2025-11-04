# Multi-stage Dockerfile for Attack Surface Analyzer Testing
# Stage 1: Build and run ASA tests
# Stage 2: Extract reports to scratch layer

# Stage 1: Test execution environment
FROM mcr.microsoft.com/dotnet/sdk:9.0-windowsservercore-ltsc2022 AS asa-runner

# Set shell to PowerShell for easier scripting
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install Attack Surface Analyzer as a global .NET tool
RUN dotnet tool install -g Microsoft.CST.AttackSurfaceAnalyzer.CLI --version 2.3.328

# Add .NET tools directory to PATH
RUN $env:PATH += ';C:/Users/ContainerAdministrator/.dotnet/tools'; \
    [Environment]::SetEnvironmentVariable('PATH', $env:PATH, [EnvironmentVariableTarget]::Machine)

# Set working directory and create reports directory
WORKDIR C:/work
RUN New-Item -ItemType Directory -Path C:\reports -Force | Out-Null

# Take baseline snapshot before installation
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Taking baseline snapshot..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    asa collect -f -s -r -u -p -l --directories 'C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell'; \
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to take baseline snapshot"; exit 1 }

# Install PowerShell MSI
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Installing PowerShell MSI..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    $msiFile = Get-ChildItem -Path C:\work -Filter *.msi | Select-Object -First 1 -ExpandProperty FullName; \
    Write-Host "MSI file: $msiFile"; \
    Start-Process msiexec.exe -ArgumentList "/i", $msiFile, "/quiet", "/norestart", "/l*v", "C:\work\install.log" -Wait -NoNewWindow; \
    if ($LASTEXITCODE -ne 0) { Write-Warning "MSI installation returned exit code: $LASTEXITCODE"; Write-Host "Check install.log for details" }

# Take post-installation snapshot
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Taking post-installation snapshot..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    asa collect -f -s -r -u -p -l --directories 'C:\Program Files\PowerShell,C:\Program Files (x86)\PowerShell'; \
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to take post-installation snapshot"; exit 1 }

# Export comparison results
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Exporting comparison results..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    asa export-collect --outputsarif --savetodatabase; \
    if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to export results with exit code: $LASTEXITCODE" }

# Copy results to both work and reports directories
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Copying results to output directories..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    Get-ChildItem -Path "*.txt" | ForEach-Object { \
        Write-Host "Copying: $($_.Name)"; \
        Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction SilentlyContinue; \
        Copy-Item -Path $_.FullName -Destination C:\reports\ -ErrorAction SilentlyContinue \
    }; \
    Get-ChildItem -Path "*.sarif" | ForEach-Object { \
        Write-Host "Copying: $($_.Name)"; \
        Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction SilentlyContinue; \
        Copy-Item -Path $_.FullName -Destination C:\reports\ -ErrorAction SilentlyContinue \
    }; \
    if (Test-Path "asa.sqlite") { \
        Write-Host "Copying: asa.sqlite"; \
        Copy-Item -Path "asa.sqlite" -Destination C:\work\ -ErrorAction SilentlyContinue; \
        Copy-Item -Path "asa.sqlite" -Destination C:\reports\ -ErrorAction SilentlyContinue \
    }; \
    Copy-Item -Path "C:\work\install.log" -Destination C:\reports\ -ErrorAction SilentlyContinue; \
    Write-Host "Attack Surface Analyzer test completed!" -ForegroundColor Green

# Default command shows completion message
CMD Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Container ready. Reports available in C:\reports\" -ForegroundColor Cyan; \
    Write-Host "========================================="

# Stage 2: Scratch layer with only the reports
FROM scratch AS asa-reports

# Copy reports from the runner stage to scratch
COPY --from=asa-runner C:/reports/ /reports/

# Stage 3: Final stage (still the runner for backward compatibility)
FROM asa-runner AS final

# Label for documentation
LABEL description="Windows container for running Attack Surface Analyzer tests on PowerShell MSI installations"
LABEL version="1.0"
LABEL maintainer="PowerShell Team"
