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

# Copy the PowerShell MSI file from build context
COPY *.msi ./powershell.msi

# Install PowerShell MSI
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Installing PowerShell MSI..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    Write-Host "MSI file: C:\work\powershell.msi"; \
    $argumentList = '/i C:\work\powershell.msi /quiet /norestart /l*vx C:\work\install.log ADD_PATH=1'; \
    Write-Host "Running: msiexec $argumentList"; \
    $msiProcess = Start-Process msiexec.exe -ArgumentList $argumentList -Wait -NoNewWindow -PassThru; \
    if ($msiProcess.ExitCode -ne 0) { \
        Write-Host "MSI installation failed with exit code: $($msiProcess.ExitCode)"; \
        throw "MSI installation failed. Check install.log for details" \
    }

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

# Copy TXT result files to both work and reports directories
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Copying TXT result files..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    $txtFiles = Get-ChildItem -Path "*.txt" -ErrorAction SilentlyContinue; \
    if ($txtFiles.Count -eq 0) { \
        Write-Warning 'No TXT files found to copy' \
    } else { \
        $txtFiles | ForEach-Object { \
            Write-Host "Copying: $($_.Name)"; \
            if (-not (Test-Path $_.FullName)) { \
                throw "Required TXT file not found: $($_.FullName)" \
            }; \
            Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction Stop; \
            Copy-Item -Path $_.FullName -Destination C:\reports\ -ErrorAction Stop \
        } \
    }

# Copy SARIF result files to both work and reports directories
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Copying SARIF result files..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    $sarifFiles = Get-ChildItem -Path "*.sarif" -ErrorAction SilentlyContinue; \
    if ($sarifFiles.Count -eq 0) { \
        Write-Warning 'No SARIF files found to copy' \
    } else { \
        $sarifFiles | ForEach-Object { \
            Write-Host "Copying: $($_.Name)"; \
            if (-not (Test-Path $_.FullName)) { \
                throw "Required SARIF file not found: $($_.FullName)" \
            }; \
            Copy-Item -Path $_.FullName -Destination C:\work\ -ErrorAction Stop; \
            Copy-Item -Path $_.FullName -Destination C:\reports\ -ErrorAction Stop \
        } \
    }

# Copy SQLite database file if it exists
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Copying SQLite database..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    if (Test-Path "asa.sqlite") { \
        Write-Host "Copying: asa.sqlite"; \
        Copy-Item -Path "asa.sqlite" -Destination C:\work\ -ErrorAction Stop; \
        Copy-Item -Path "asa.sqlite" -Destination C:\reports\ -ErrorAction Stop \
    } else { \
        Write-Warning 'SQLite database (asa.sqlite) not found - this may indicate ASA export issues' \
    }

# Copy installation log file
RUN Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Copying installation log..." -ForegroundColor Green; \
    Write-Host "========================================="; \
    if (-not (Test-Path "C:\work\install.log")) { \
        throw "Required installation log file not found: C:\work\install.log" \
    }; \
    Copy-Item -Path "C:\work\install.log" -Destination C:\reports\ -ErrorAction Stop; \
    Write-Host "Attack Surface Analyzer test completed!" -ForegroundColor Green

# Default command shows completion message
CMD Write-Host "=========================================" -ForegroundColor Green; \
    Write-Host "Container ready. Reports available in C:\reports\" -ForegroundColor Cyan; \
    Write-Host "========================================="

# Stage 2: Reports-only layer using minimal Windows base
FROM mcr.microsoft.com/windows/nanoserver:ltsc2022 AS asa-reports

# Set working directory to root
WORKDIR /

# Copy only the report files from the runner stage to root level
COPY --from=asa-runner C:/reports/ ./

# Stage 3: Final stage (defaults to the runner for backward compatibility)
FROM asa-runner AS final

# Label for documentation
LABEL description="Windows container for running Attack Surface Analyzer tests on PowerShell MSI installations"
LABEL version="1.0"
LABEL maintainer="PowerShell Team"
