# Integration Examples - Microsoft.PowerShell.Development

This guide demonstrates how to combine multiple features from the Microsoft.PowerShell.Development module to create powerful workflows for AI-assisted development.

## Table of Contents

1. [Feature Overview](#feature-overview)
2. [Basic Integrations](#basic-integrations)
3. [Advanced Workflows](#advanced-workflows)
4. [Real-World Scenarios](#real-world-scenarios)
5. [AI Assistant Integrations](#ai-assistant-integrations)

---

## Feature Overview

The module provides these complementary features:

| Feature | Purpose | Cmdlets |
|---------|---------|---------|
| **Project Context** | Auto-detect project type | Get-ProjectContext |
| **DevCommand** | Async CLI execution | Start-DevCommand, Get-DevCommandStatus, Wait-DevCommand, Stop-DevCommand, Receive-DevCommandOutput |
| **Format-ForAI** | AI-friendly output | Format-ForAI |
| **CLI Tool Registry** | Normalized tool interface | Register-CliTool, Get-CliTool, Invoke-CliTool, Unregister-CliTool |
| **AI Error Context** | Intelligent error analysis | Get-AIErrorContext |
| **Workflow Recording** | Record & replay workflows | Start-WorkflowRecording, Stop-WorkflowRecording, Save-WorkflowStep, Get-Workflow, Invoke-Workflow, Remove-Workflow |

---

## Basic Integrations

### 1. Project Context + Workflow Recording

Auto-detect project type and create project-specific workflows.

```powershell
# Detect the current project
$project = Get-ProjectContext

# Create a project-specific workflow
Start-WorkflowRecording -Name "Build_$($project.Type)" `
    -Description "Build workflow for $($project.Type) project" `
    -Tags $project.Type.ToLower(), "build"

# Use project's suggested commands
foreach ($cmd in $project.SuggestedCommands) {
    Write-Host "Running: $cmd" -ForegroundColor Cyan
    Invoke-Expression $cmd
    if ($LASTEXITCODE -eq 0) {
        Save-WorkflowStep -Command $cmd -ExitCode 0
    } else {
        Save-WorkflowStep -Command $cmd -Failed -ExitCode $LASTEXITCODE
        break
    }
}

Stop-WorkflowRecording -Save

# Now you can replay this build workflow anytime
Invoke-Workflow -Name "Build_$($project.Type)"
```

### 2. DevCommand + AI Error Context

Run async commands and analyze errors intelligently.

```powershell
# Start a long-running build
$buildJob = Start-DevCommand -Tool "cargo" -Arguments "build --release"

# Do other work while it runs...
Write-Host "Build running in background..."

# Wait for completion
Wait-DevCommand -Job $buildJob

# Check status and analyze errors if failed
$status = Get-DevCommandStatus -Job $buildJob

if ($status.ExitCode -ne 0) {
    Write-Host "Build failed! Analyzing errors..." -ForegroundColor Red

    # Get the error output
    $output = Receive-DevCommandOutput -Job $buildJob

    # Analyze with AI Error Context
    $errorContext = Get-AIErrorContext -Last 5

    # Format for AI assistant
    $errorContext | Format-ForAI | Out-File build-errors.json

    Write-Host "Error analysis saved to build-errors.json" -ForegroundColor Yellow
    Write-Host "Share this file with Claude Code for assistance!" -ForegroundColor Green
}
```

### 3. CLI Tool Registry + Format-ForAI

Normalize tool output and format for AI consumption.

```powershell
# Register a custom tool
Register-CliTool -Name "myapp" -ExecutablePath "./bin/myapp" `
    -ParameterMappings @{
        Config = "--config"
        Verbose = "-v"
    } `
    -ErrorPatterns @{
        "ERROR:" = "Error"
        "FATAL:" = "Critical"
    }

# Invoke the tool
$result = Invoke-CliTool -Name "myapp" -Arguments @("--config", "prod.json")

# Format the result for AI
$result | Format-ForAI -OutputType Json -Depth 5 | Out-File myapp-result.json

# Now you can share myapp-result.json with Claude Code
```

### 4. Workflow Recording + Format-ForAI

Record workflows and export execution results for AI analysis.

```powershell
# Run a workflow
$result = Invoke-Workflow -Name "Deploy"

# Export detailed results for AI
$aiReport = @{
    WorkflowName = $result.WorkflowName
    Success = $result.Success
    Duration = ($result.EndTime - $result.StartTime).TotalSeconds
    Steps = $result.Steps | ForEach-Object {
        @{
            StepNumber = $_.StepNumber
            Command = $_.ExecutedCommand
            Success = $_.Success
            Duration = $_.Duration.TotalSeconds
            Output = $_.Output -join "`n"
            Error = $_.Error
        }
    }
}

$aiReport | Format-ForAI | Out-File deployment-report.json

Write-Host "Deployment report ready for AI analysis: deployment-report.json"
```

---

## Advanced Workflows

### 1. Full CI/CD Pipeline with Error Recovery

Combines: DevCommand, Workflow Recording, AI Error Context, Format-ForAI

```powershell
function Run-CIPipeline {
    param(
        [string]$Environment = "staging",
        [switch]$RecordWorkflow
    )

    # Start recording if requested
    if ($RecordWorkflow) {
        Start-WorkflowRecording -Name "CI_Pipeline_$Environment" `
            -Description "Full CI/CD pipeline for $Environment" `
            -Tags "ci", "deployment", $Environment
    }

    # Step 1: Run tests asynchronously
    Write-Host "Starting tests..." -ForegroundColor Cyan
    $testJob = Start-DevCommand -Tool "dotnet" -Arguments "test --configuration Release"

    # Step 2: Build in parallel
    Write-Host "Starting build..." -ForegroundColor Cyan
    $buildJob = Start-DevCommand -Tool "dotnet" -Arguments "build --configuration Release"

    # Wait for both
    Wait-DevCommand -Job $testJob
    Wait-DevCommand -Job $buildJob

    # Check results
    $testStatus = Get-DevCommandStatus -Job $testJob
    $buildStatus = Get-DevCommandStatus -Job $buildJob

    if ($testStatus.ExitCode -ne 0) {
        Write-Host "Tests failed!" -ForegroundColor Red

        if ($RecordWorkflow) {
            Save-WorkflowStep -Command "dotnet test --configuration Release" `
                -Failed -ExitCode $testStatus.ExitCode
        }

        # Analyze test failures
        $errors = Get-AIErrorContext -Last 10
        $errorReport = @{
            Stage = "Testing"
            Errors = $errors
            Output = Receive-DevCommandOutput -Job $testJob
        }

        $errorReport | Format-ForAI | Out-File "ci-test-errors.json"
        Write-Host "Error report: ci-test-errors.json" -ForegroundColor Yellow

        if ($RecordWorkflow) { Stop-WorkflowRecording -Save }
        return $false
    }

    if ($buildStatus.ExitCode -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red

        if ($RecordWorkflow) {
            Save-WorkflowStep -Command "dotnet build --configuration Release" `
                -Failed -ExitCode $buildStatus.ExitCode
        }

        # Analyze build failures
        $errors = Get-AIErrorContext -Last 10
        $errorReport = @{
            Stage = "Build"
            Errors = $errors
            Output = Receive-DevCommandOutput -Job $buildJob
        }

        $errorReport | Format-ForAI | Out-File "ci-build-errors.json"
        Write-Host "Error report: ci-build-errors.json" -ForegroundColor Yellow

        if ($RecordWorkflow) { Stop-WorkflowRecording -Save }
        return $false
    }

    # Both succeeded
    Write-Host "Tests and build passed!" -ForegroundColor Green

    if ($RecordWorkflow) {
        Save-WorkflowStep -Command "dotnet test --configuration Release" -ExitCode 0
        Save-WorkflowStep -Command "dotnet build --configuration Release" -ExitCode 0
    }

    # Step 3: Docker build
    Write-Host "Building Docker image..." -ForegroundColor Cyan
    $dockerJob = Start-DevCommand -Tool "docker" `
        -Arguments "build -t myapp:${Environment} ."

    Wait-DevCommand -Job $dockerJob
    $dockerStatus = Get-DevCommandStatus -Job $dockerJob

    if ($dockerStatus.ExitCode -ne 0) {
        Write-Host "Docker build failed!" -ForegroundColor Red

        if ($RecordWorkflow) {
            Save-WorkflowStep -Command "docker build -t myapp:${Environment} ." `
                -Failed -ExitCode $dockerStatus.ExitCode
            Stop-WorkflowRecording -Save
        }

        return $false
    }

    if ($RecordWorkflow) {
        Save-WorkflowStep -Command "docker build -t myapp:`${Environment} ." -ExitCode 0
    }

    # Step 4: Deploy
    Write-Host "Deploying to $Environment..." -ForegroundColor Cyan

    if ($Environment -eq "production") {
        # Use WhatIf for production
        Write-Host "Production deployment - reviewing changes..." -ForegroundColor Yellow
        # In real scenario, you'd have approval here
    }

    $deployCmd = "kubectl set image deployment/myapp myapp=myapp:${Environment}"
    Invoke-Expression $deployCmd

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Deployment successful!" -ForegroundColor Green

        if ($RecordWorkflow) {
            Save-WorkflowStep -Command "kubectl set image deployment/myapp myapp=myapp:`${Environment}" `
                -ExitCode 0
        }
    } else {
        Write-Host "Deployment failed!" -ForegroundColor Red

        if ($RecordWorkflow) {
            Save-WorkflowStep -Command "kubectl set image deployment/myapp myapp=myapp:`${Environment}" `
                -Failed -ExitCode $LASTEXITCODE
        }
    }

    if ($RecordWorkflow) {
        Stop-WorkflowRecording -Save
        Write-Host "Workflow saved! Replay with: Invoke-Workflow -Name 'CI_Pipeline_$Environment' -Variables @{Environment='$Environment'}" -ForegroundColor Cyan
    }

    return $LASTEXITCODE -eq 0
}

# Record the workflow once
Run-CIPipeline -Environment "staging" -RecordWorkflow

# Later, replay it
Invoke-Workflow -Name "CI_Pipeline_staging" -Variables @{Environment="staging"}
```

### 2. Multi-Language Project Build

Combines: Get-ProjectContext, DevCommand, Workflow Recording

```powershell
function Build-MultiProjectWorkspace {
    param(
        [string]$WorkspaceRoot = ".",
        [switch]$Parallel,
        [switch]$RecordWorkflow
    )

    if ($RecordWorkflow) {
        Start-WorkflowRecording -Name "BuildWorkspace" `
            -Description "Build all projects in workspace" `
            -Tags "build", "multi-project"
    }

    # Find all projects
    $projects = Get-ChildItem -Path $WorkspaceRoot -Directory | ForEach-Object {
        Push-Location $_.FullName
        $context = Get-ProjectContext -ErrorAction SilentlyContinue
        Pop-Location

        if ($context) {
            @{
                Name = $_.Name
                Path = $_.FullName
                Type = $context.Type
                BuildCommand = $context.SuggestedCommands | Where-Object { $_ -like "*build*" } | Select-Object -First 1
            }
        }
    } | Where-Object { $_ -ne $null }

    Write-Host "Found $($projects.Count) projects" -ForegroundColor Cyan
    $projects | ForEach-Object { Write-Host "  - $($_.Name) ($($_.Type))" }

    $buildJobs = @()

    foreach ($proj in $projects) {
        Write-Host "`nBuilding $($proj.Name)..." -ForegroundColor Yellow

        Push-Location $proj.Path

        if ($Parallel) {
            # Async build
            $job = Start-DevCommand -CommandLine $proj.BuildCommand
            $buildJobs += @{
                Project = $proj
                Job = $job
            }
        } else {
            # Sync build
            Invoke-Expression $proj.BuildCommand

            if ($RecordWorkflow) {
                if ($LASTEXITCODE -eq 0) {
                    Save-WorkflowStep -Command "cd $($proj.Path) && $($proj.BuildCommand)" -ExitCode 0
                } else {
                    Save-WorkflowStep -Command "cd $($proj.Path) && $($proj.BuildCommand)" `
                        -Failed -ExitCode $LASTEXITCODE
                }
            }
        }

        Pop-Location
    }

    # Wait for parallel builds
    if ($Parallel) {
        Write-Host "`nWaiting for builds to complete..." -ForegroundColor Cyan

        foreach ($buildJob in $buildJobs) {
            Wait-DevCommand -Job $buildJob.Job
            $status = Get-DevCommandStatus -Job $buildJob.Job

            if ($status.ExitCode -eq 0) {
                Write-Host "$($buildJob.Project.Name): SUCCESS" -ForegroundColor Green
            } else {
                Write-Host "$($buildJob.Project.Name): FAILED" -ForegroundColor Red

                # Get error output
                $output = Receive-DevCommandOutput -Job $buildJob.Job
                Write-Host $output.StandardError -ForegroundColor Red
            }

            if ($RecordWorkflow) {
                if ($status.ExitCode -eq 0) {
                    Save-WorkflowStep -Command "cd $($buildJob.Project.Path) && $($buildJob.Project.BuildCommand)" `
                        -ExitCode 0
                } else {
                    Save-WorkflowStep -Command "cd $($buildJob.Project.Path) && $($buildJob.Project.BuildCommand)" `
                        -Failed -ExitCode $status.ExitCode
                }
            }
        }
    }

    if ($RecordWorkflow) {
        Stop-WorkflowRecording -Save
        Write-Host "`nWorkflow saved! Replay with: Invoke-Workflow -Name 'BuildWorkspace'" -ForegroundColor Cyan
    }
}

# Build all projects in parallel and record
Build-MultiProjectWorkspace -Parallel -RecordWorkflow
```

### 3. Intelligent Error Recovery Workflow

Combines: Workflow Recording, AI Error Context, Format-ForAI, DevCommand

```powershell
function Invoke-WorkflowWithRecovery {
    param(
        [string]$WorkflowName,
        [hashtable]$Variables = @{},
        [int]$MaxRetries = 3
    )

    $attempt = 0
    $success = $false
    $allErrors = @()

    while (-not $success -and $attempt -lt $MaxRetries) {
        $attempt++
        Write-Host "Attempt $attempt of $MaxRetries..." -ForegroundColor Cyan

        # Execute workflow
        $result = Invoke-Workflow -Name $WorkflowName -Variables $Variables

        if ($result.Success) {
            $success = $true
            Write-Host "Workflow completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Workflow failed on attempt $attempt" -ForegroundColor Red

            # Collect failed steps
            $failedSteps = $result.Steps | Where-Object { -not $_.Success }

            foreach ($step in $failedSteps) {
                Write-Host "  Failed: $($step.ExecutedCommand)" -ForegroundColor Yellow

                # Analyze error
                $errorContext = Get-AIErrorContext |
                    Where-Object { $_.SimplifiedMessage -like "*$($step.Error)*" } |
                    Select-Object -First 1

                if ($errorContext) {
                    $allErrors += @{
                        Attempt = $attempt
                        Step = $step.StepNumber
                        Command = $step.ExecutedCommand
                        Error = $step.Error
                        Category = $errorContext.Category
                        RootCause = $errorContext.RootCause
                        SuggestedFixes = $errorContext.SuggestedFixes
                    }

                    Write-Host "  Category: $($errorContext.Category)" -ForegroundColor Magenta
                    Write-Host "  Root Cause: $($errorContext.RootCause)" -ForegroundColor Magenta
                    Write-Host "  Suggested Fixes:" -ForegroundColor Magenta
                    $errorContext.SuggestedFixes | ForEach-Object { Write-Host "    - $_" -ForegroundColor Cyan }
                }
            }

            # Wait before retry
            if ($attempt -lt $MaxRetries) {
                $waitTime = [Math]::Pow(2, $attempt)
                Write-Host "Waiting $waitTime seconds before retry..." -ForegroundColor Yellow
                Start-Sleep -Seconds $waitTime
            }
        }
    }

    # Generate comprehensive report
    $report = @{
        WorkflowName = $WorkflowName
        Variables = $Variables
        Attempts = $attempt
        Success = $success
        FinalResult = $result
        ErrorHistory = $allErrors
    }

    # Save report for AI analysis
    $report | Format-ForAI | Out-File "workflow-recovery-report.json"

    if ($success) {
        Write-Host "`nWorkflow succeeded after $attempt attempt(s)" -ForegroundColor Green
    } else {
        Write-Host "`nWorkflow failed after $MaxRetries attempts" -ForegroundColor Red
        Write-Host "Detailed error report saved to: workflow-recovery-report.json" -ForegroundColor Yellow
        Write-Host "Share this with Claude Code for assistance!" -ForegroundColor Cyan
    }

    return $report
}

# Use it
$report = Invoke-WorkflowWithRecovery -WorkflowName "Deploy" `
    -Variables @{Environment="staging"} `
    -MaxRetries 3
```

---

## Real-World Scenarios

### Scenario 1: New Developer Onboarding

Help new developers get started with automatic project detection and setup.

```powershell
# onboard.ps1 - Run this on a new machine

function Start-DeveloperOnboarding {
    Write-Host "=== Developer Onboarding ===" -ForegroundColor Green

    # Detect project
    Write-Host "`n1. Detecting project type..." -ForegroundColor Cyan
    $project = Get-ProjectContext

    Write-Host "  Project: $($project.Type)" -ForegroundColor Yellow
    Write-Host "  Language: $($project.Language)" -ForegroundColor Yellow
    Write-Host "  Build Tool: $($project.BuildTool)" -ForegroundColor Yellow

    # Create setup workflow
    Write-Host "`n2. Creating setup workflow..." -ForegroundColor Cyan
    Start-WorkflowRecording -Name "DevSetup_$($project.Type)" `
        -Description "Developer environment setup for $($project.Type)" `
        -Tags "setup", "onboarding", $project.Type.ToLower()

    # Run setup commands
    Write-Host "`n3. Running setup commands..." -ForegroundColor Cyan

    foreach ($cmd in $project.SuggestedCommands) {
        Write-Host "  Running: $cmd" -ForegroundColor Gray

        $job = Start-DevCommand -CommandLine $cmd
        Wait-DevCommand -Job $job
        $status = Get-DevCommandStatus -Job $job

        if ($status.ExitCode -eq 0) {
            Write-Host "    ✓ Success" -ForegroundColor Green
            Save-WorkflowStep -Command $cmd -ExitCode 0
        } else {
            Write-Host "    ✗ Failed" -ForegroundColor Red
            Save-WorkflowStep -Command $cmd -Failed -ExitCode $status.ExitCode

            # Analyze error
            $errors = Get-AIErrorContext -Last 5
            $errors | Format-ForAI | Out-File "setup-error-$($cmd.Replace(' ', '-')).json"

            Write-Host "    Error details saved for AI assistance" -ForegroundColor Yellow
        }
    }

    # Save workflow
    Stop-WorkflowRecording -Save

    Write-Host "`n4. Setup complete!" -ForegroundColor Green
    Write-Host "   Workflow saved: DevSetup_$($project.Type)" -ForegroundColor Cyan
    Write-Host "   Other developers can run: Invoke-Workflow -Name 'DevSetup_$($project.Type)'" -ForegroundColor Cyan

    # Generate onboarding report
    $report = @{
        Project = $project
        SetupDate = Get-Date
        WorkflowName = "DevSetup_$($project.Type)"
        MachineName = $env:COMPUTERNAME
        User = $env:USERNAME
    }

    $report | Format-ForAI | Out-File "onboarding-report.json"
}

Start-DeveloperOnboarding
```

### Scenario 2: Daily Development Workflow

Automate daily development tasks with integrated error handling.

```powershell
# daily-dev.ps1

function Start-DailyDevelopment {
    param(
        [string]$Branch = "main",
        [switch]$CreateWorkflow
    )

    if ($CreateWorkflow) {
        Start-WorkflowRecording -Name "DailyDev" `
            -Description "Daily development routine" `
            -Tags "daily", "development"
    }

    Write-Host "=== Daily Development Workflow ===" -ForegroundColor Green

    # 1. Update code
    Write-Host "`n1. Updating code from $Branch..." -ForegroundColor Cyan
    git fetch origin $Branch
    git pull origin $Branch

    if ($LASTEXITCODE -eq 0) {
        if ($CreateWorkflow) {
            Save-WorkflowStep -Command "git pull origin `${Branch}" -ExitCode 0
        }
    } else {
        Write-Host "Git pull failed!" -ForegroundColor Red
        $errors = Get-AIErrorContext -Last 3
        $errors | Format-ForAI | Out-File "git-error.json"
        if ($CreateWorkflow) { Stop-WorkflowRecording -Save }
        return
    }

    # 2. Restore dependencies
    Write-Host "`n2. Restoring dependencies..." -ForegroundColor Cyan
    $project = Get-ProjectContext
    $restoreCmd = $project.SuggestedCommands | Where-Object {
        $_ -like "*restore*" -or $_ -like "*install*"
    } | Select-Object -First 1

    if ($restoreCmd) {
        $restoreJob = Start-DevCommand -CommandLine $restoreCmd
        Wait-DevCommand -Job $restoreJob
        $status = Get-DevCommandStatus -Job $restoreJob

        if ($status.ExitCode -eq 0) {
            Write-Host "  ✓ Dependencies restored" -ForegroundColor Green
            if ($CreateWorkflow) {
                Save-WorkflowStep -Command $restoreCmd -ExitCode 0
            }
        } else {
            Write-Host "  ✗ Restore failed" -ForegroundColor Red
            $output = Receive-DevCommandOutput -Job $restoreJob
            Write-Host $output.StandardError -ForegroundColor Red
            if ($CreateWorkflow) { Stop-WorkflowRecording -Save }
            return
        }
    }

    # 3. Run build
    Write-Host "`n3. Building project..." -ForegroundColor Cyan
    $buildCmd = $project.SuggestedCommands | Where-Object {
        $_ -like "*build*"
    } | Select-Object -First 1

    if ($buildCmd) {
        $buildJob = Start-DevCommand -CommandLine $buildCmd

        # Show progress
        Write-Host "  Building..." -ForegroundColor Gray
        Wait-DevCommand -Job $buildJob

        $status = Get-DevCommandStatus -Job $buildJob

        if ($status.ExitCode -eq 0) {
            Write-Host "  ✓ Build successful" -ForegroundColor Green
            if ($CreateWorkflow) {
                Save-WorkflowStep -Command $buildCmd -ExitCode 0
            }
        } else {
            Write-Host "  ✗ Build failed" -ForegroundColor Red

            # Analyze build errors
            $errors = Get-AIErrorContext -Last 10
            $buildErrors = $errors | Where-Object {
                $_.Category -eq "CompilationError" -or $_.Category -eq "TypeMismatch"
            }

            if ($buildErrors) {
                Write-Host "`n  Compilation Errors Found:" -ForegroundColor Yellow
                $buildErrors | ForEach-Object {
                    Write-Host "    $($_.Location)" -ForegroundColor Cyan
                    Write-Host "    $($_.SimplifiedMessage)" -ForegroundColor White
                    Write-Host "    Suggested: $($_.SuggestedFixes[0])" -ForegroundColor Gray
                }

                # Save for AI
                @{
                    Stage = "Build"
                    Errors = $buildErrors
                    Output = Receive-DevCommandOutput -Job $buildJob
                } | Format-ForAI | Out-File "build-errors.json"

                Write-Host "`n  Error report: build-errors.json" -ForegroundColor Yellow
            }

            if ($CreateWorkflow) { Stop-WorkflowRecording -Save }
            return
        }
    }

    # 4. Run tests
    Write-Host "`n4. Running tests..." -ForegroundColor Cyan
    $testCmd = $project.SuggestedCommands | Where-Object {
        $_ -like "*test*"
    } | Select-Object -First 1

    if ($testCmd) {
        $testJob = Start-DevCommand -CommandLine $testCmd
        Wait-DevCommand -Job $testJob
        $status = Get-DevCommandStatus -Job $testJob

        if ($status.ExitCode -eq 0) {
            Write-Host "  ✓ All tests passed" -ForegroundColor Green
            if ($CreateWorkflow) {
                Save-WorkflowStep -Command $testCmd -ExitCode 0
            }
        } else {
            Write-Host "  ✗ Some tests failed" -ForegroundColor Red
            $output = Receive-DevCommandOutput -Job $testJob

            # Parse test output (example for common patterns)
            $testFailures = $output.StandardOutput | Select-String "FAILED|Failed|✗"

            if ($testFailures) {
                Write-Host "`n  Failed Tests:" -ForegroundColor Yellow
                $testFailures | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
            }

            if ($CreateWorkflow) {
                Save-WorkflowStep -Command $testCmd -Failed -ExitCode $status.ExitCode
            }
        }
    }

    if ($CreateWorkflow) {
        Stop-WorkflowRecording -Save
        Write-Host "`n✓ Workflow saved as 'DailyDev'" -ForegroundColor Green
        Write-Host "  Replay with: Invoke-Workflow -Name 'DailyDev' -Variables @{Branch='$Branch'}" -ForegroundColor Cyan
    }

    Write-Host "`n=== Daily Workflow Complete ===" -ForegroundColor Green
}

# Create the workflow once
Start-DailyDevelopment -Branch "main" -CreateWorkflow

# Use it daily
Invoke-Workflow -Name "DailyDev" -Variables @{Branch="main"}
```

### Scenario 3: Claude Code Integration

Prepare context and reports specifically for Claude Code.

```powershell
# claude-context.ps1

function Send-ToClaudeCode {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Error", "Build", "Test", "Deploy", "Custom")]
        [string]$ContextType,

        [string]$CustomDescription,
        [string]$OutputFile
    )

    if (-not $OutputFile) {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $OutputFile = "claude-context-$ContextType-$timestamp.json"
    }

    Write-Host "Preparing context for Claude Code..." -ForegroundColor Cyan

    # Gather project context
    $project = Get-ProjectContext

    # Gather recent errors
    $recentErrors = Get-AIErrorContext -Last 10

    # Get git status
    $gitStatus = git status --porcelain 2>$null
    $gitBranch = git branch --show-current 2>$null

    # Build context object
    $context = @{
        ContextType = $ContextType
        Timestamp = Get-Date -Format "o"
        Description = $CustomDescription

        Environment = @{
            WorkingDirectory = (Get-Location).Path
            GitBranch = $gitBranch
            GitStatus = $gitStatus
            PowerShellVersion = $PSVersionTable.PSVersion.ToString()
        }

        Project = @{
            Type = $project.Type
            Language = $project.Language
            BuildTool = $project.BuildTool
            TestFramework = $project.TestFramework
            ConfigFiles = $project.ConfigFiles
        }
    }

    # Add context-specific information
    switch ($ContextType) {
        "Error" {
            $context.Errors = $recentErrors | ForEach-Object {
                @{
                    Category = $_.Category
                    Message = $_.SimplifiedMessage
                    Location = $_.Location
                    RootCause = $_.RootCause
                    Severity = $_.Severity
                    SuggestedFixes = $_.SuggestedFixes
                }
            }
        }

        "Build" {
            # Get recent build output
            $buildCmd = $project.SuggestedCommands | Where-Object { $_ -like "*build*" } | Select-Object -First 1
            if ($buildCmd) {
                $buildJob = Start-DevCommand -CommandLine $buildCmd
                Wait-DevCommand -Job $buildJob
                $output = Receive-DevCommandOutput -Job $buildJob
                $status = Get-DevCommandStatus -Job $buildJob

                $context.Build = @{
                    Command = $buildCmd
                    ExitCode = $status.ExitCode
                    Success = $status.ExitCode -eq 0
                    Output = $output.StandardOutput
                    Errors = $output.StandardError
                }

                if ($status.ExitCode -ne 0) {
                    $context.Errors = Get-AIErrorContext -Last 10
                }
            }
        }

        "Test" {
            # Get recent test output
            $testCmd = $project.SuggestedCommands | Where-Object { $_ -like "*test*" } | Select-Object -First 1
            if ($testCmd) {
                $testJob = Start-DevCommand -CommandLine $testCmd
                Wait-DevCommand -Job $testJob
                $output = Receive-DevCommandOutput -Job $testJob
                $status = Get-DevCommandStatus -Job $testJob

                $context.Tests = @{
                    Command = $testCmd
                    ExitCode = $status.ExitCode
                    Success = $status.ExitCode -eq 0
                    Output = $output.StandardOutput
                    Errors = $output.StandardError
                }
            }
        }

        "Deploy" {
            # Get deployment workflows
            $deployWorkflows = Get-Workflow -Tag "deploy*"
            $context.DeploymentWorkflows = $deployWorkflows | ForEach-Object {
                @{
                    Name = $_.Name
                    Description = $_.Description
                    Steps = $_.Steps.Count
                    LastExecuted = $_.LastModified
                    ExecutionCount = $_.ExecutionCount
                }
            }
        }
    }

    # Recent commands (from history)
    $context.RecentCommands = Get-History -Count 20 | ForEach-Object {
        @{
            Id = $_.Id
            CommandLine = $_.CommandLine
            ExecutionStatus = $_.ExecutionStatus.ToString()
            StartTime = $_.StartExecutionTime
            EndTime = $_.EndExecutionTime
        }
    }

    # Format and save
    $context | Format-ForAI -Depth 10 | Out-File $OutputFile

    Write-Host "✓ Context saved to: $OutputFile" -ForegroundColor Green
    Write-Host "`nYou can now share this file with Claude Code:" -ForegroundColor Cyan
    Write-Host "  1. Open Claude Code" -ForegroundColor Gray
    Write-Host "  2. Attach the file: $OutputFile" -ForegroundColor Gray
    Write-Host "  3. Ask Claude Code to analyze it" -ForegroundColor Gray

    return $OutputFile
}

# Usage examples:

# Send error context
Send-ToClaudeCode -ContextType Error -CustomDescription "Build failing with type errors"

# Send build context
Send-ToClaudeCode -ContextType Build -CustomDescription "Release build issues"

# Send test context
Send-ToClaudeCode -ContextType Test -CustomDescription "Integration tests failing"

# Send deployment context
Send-ToClaudeCode -ContextType Deploy -CustomDescription "Production deployment prep"
```

---

## AI Assistant Integrations

### Integration 1: Automatic Error Reporting

```powershell
# Set up automatic error reporting
$Global:ErrorActionPreference = "Continue"

# Error handler
$Global:ErrorView = "ConciseView"

function Report-ErrorToAI {
    param($ErrorRecord)

    # Get AI context
    $context = Get-AIErrorContext -Last 1 | Select-Object -First 1

    if ($context) {
        Write-Host "`n╔══════════════════════════════════════╗" -ForegroundColor Red
        Write-Host "║   AI Error Analysis Available       ║" -ForegroundColor Red
        Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Red

        Write-Host "`nCategory: " -NoNewline -ForegroundColor Yellow
        Write-Host $context.Category -ForegroundColor White

        Write-Host "Root Cause: " -NoNewline -ForegroundColor Yellow
        Write-Host $context.RootCause -ForegroundColor White

        Write-Host "`nSuggested Fixes:" -ForegroundColor Yellow
        $context.SuggestedFixes | ForEach-Object {
            Write-Host "  • $_" -ForegroundColor Cyan
        }

        # Save detailed report
        $report = @{
            Error = $ErrorRecord
            Analysis = $context
            Timestamp = Get-Date
            WorkingDirectory = (Get-Location).Path
        }

        $filename = "error-$(Get-Date -Format 'HHmmss').json"
        $report | Format-ForAI | Out-File $filename

        Write-Host "`nDetailed report: " -NoNewline -ForegroundColor Gray
        Write-Host $filename -ForegroundColor White
        Write-Host "Share with Claude Code for assistance!`n" -ForegroundColor Green
    }
}

# Attach to error event
$ExecutionContext.InvokeCommand.CommandNotFoundAction = {
    param($CommandName, $CommandLookupEventArgs)

    Report-ErrorToAI -ErrorRecord $CommandLookupEventArgs
}
```

### Integration 2: Workflow Suggestion System

```powershell
function Get-WorkflowSuggestion {
    param(
        [string]$TaskDescription
    )

    # Get project context
    $project = Get-ProjectContext

    # Get existing workflows
    $workflows = Get-Workflow

    # Analyze task and suggest workflow
    $suggestion = @{
        TaskDescription = $TaskDescription
        ProjectType = $project.Type
        ExistingWorkflows = $workflows | ForEach-Object {
            @{
                Name = $_.Name
                Description = $_.Description
                Steps = $_.Steps.Count
                Tags = $_.Tags
            }
        }
        SuggestedCommands = $project.SuggestedCommands
        Recommendation = ""
    }

    # Simple keyword matching for suggestions
    if ($TaskDescription -like "*build*") {
        $buildWorkflows = $workflows | Where-Object { $_.Tags -contains "build" }
        if ($buildWorkflows) {
            $suggestion.Recommendation = "Use existing build workflow: $($buildWorkflows[0].Name)"
        } else {
            $suggestion.Recommendation = "Create new build workflow with: $($project.SuggestedCommands | Where-Object { $_ -like '*build*' })"
        }
    } elseif ($TaskDescription -like "*deploy*") {
        $deployWorkflows = $workflows | Where-Object { $_.Tags -contains "deploy" -or $_.Tags -contains "deployment" }
        if ($deployWorkflows) {
            $suggestion.Recommendation = "Use existing deployment workflow: $($deployWorkflows[0].Name)"
        } else {
            $suggestion.Recommendation = "Create deployment workflow - consider using Invoke-Workflow with variables for different environments"
        }
    } elseif ($TaskDescription -like "*test*") {
        $testWorkflows = $workflows | Where-Object { $_.Tags -contains "test" }
        if ($testWorkflows) {
            $suggestion.Recommendation = "Use existing test workflow: $($testWorkflows[0].Name)"
        } else {
            $suggestion.Recommendation = "Create test workflow with: $($project.SuggestedCommands | Where-Object { $_ -like '*test*' })"
        }
    }

    # Format for AI
    $suggestion | Format-ForAI | Out-File "workflow-suggestion.json"

    Write-Host "Workflow Suggestion:" -ForegroundColor Cyan
    Write-Host "  $($suggestion.Recommendation)" -ForegroundColor White
    Write-Host "`nFull analysis saved to: workflow-suggestion.json" -ForegroundColor Gray

    return $suggestion
}

# Usage
Get-WorkflowSuggestion -TaskDescription "I need to build and deploy to staging"
```

---

## Summary

The Microsoft.PowerShell.Development module becomes incredibly powerful when features are combined:

| Combination | Use Case | Benefit |
|-------------|----------|---------|
| **Project Context + Workflow** | Auto-generate project-specific workflows | Save time, ensure consistency |
| **DevCommand + Error Context** | Async execution with intelligent error analysis | Handle long builds, quick debugging |
| **Workflow + Format-ForAI** | Export execution reports | Share with Claude Code for help |
| **CLI Tools + Workflow** | Normalize and record tool usage | Reproducible, shareable procedures |
| **All Features** | Complete CI/CD pipeline | Fully automated, self-documenting |

These integrations transform PowerShell into an AI-assisted development powerhouse, perfect for working with tools like Claude Code!
