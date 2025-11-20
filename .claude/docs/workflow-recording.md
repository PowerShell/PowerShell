# Workflow Recording & Replay

## 🎯 The Problem

Complex development tasks often require running the same sequence of commands repeatedly:

```powershell
# Every time you deploy:
git pull
dotnet restore
dotnet build
dotnet test
docker build -t myapp:latest .
docker push myapp:latest
kubectl apply -f deployment.yaml
```

**Manual Problems:**
- Typing errors introduce bugs
- Forgetting steps causes failures
- Hard to share exact procedures with team
- Difficult to parameterize for different environments
- No way to track what was done

## ✨ The Solution: Workflow Recording

Record command sequences once, replay them anytime with variable substitution.

### Quick Example

```powershell
# Start recording
Start-WorkflowRecording -Name "Deploy" -Description "Deploy app to production"

# Run your commands
git pull
dotnet restore
dotnet build --configuration Release
dotnet test
docker build -t myapp:v1.0 .
docker push myapp:v1.0
kubectl apply -f deployment.yaml

# Stop and save
Stop-WorkflowRecording -Save

# Later, replay with different version
Invoke-Workflow -Name "Deploy" -Variables @{Version="v1.1"}
```

---

## 📖 Core Concepts

### 1. Recording Workflows

**Automatic Recording**: Captures commands as you run them
**Manual Recording**: Use `Save-WorkflowStep` to add steps explicitly

```powershell
# Start recording
Start-WorkflowRecording -Name "BuildPipeline"

# Your commands are automatically captured by hooks
# OR manually save steps:
Save-WorkflowStep -Command "dotnet build"
Save-WorkflowStep -Command "dotnet test" -ExitCode 0

# Stop and save
Stop-WorkflowRecording -Save -PassThru
```

### 2. Variable Substitution

Use `${VarName}` or `$VarName` syntax in commands:

```powershell
Start-WorkflowRecording -Name "DeployToEnv"

# Command with variables
Save-WorkflowStep -Command "docker build -t myapp:${Version} ."
Save-WorkflowStep -Command "kubectl apply -f ${Environment}/deployment.yaml"

Stop-WorkflowRecording -Save

# Replay with different values
Invoke-Workflow -Name "DeployToEnv" -Variables @{
    Version = "v2.0"
    Environment = "staging"
}
```

### 3. Workflow Storage

Workflows are stored as JSON files in `~/.pwsh/workflows/`:

```
~/.pwsh/workflows/
├── BuildPipeline.json
├── Deploy.json
└── RunTests.json
```

---

## 📚 Cmdlet Reference

### Start-WorkflowRecording

Start recording a new workflow.

```powershell
# Basic usage
Start-WorkflowRecording -Name "MyWorkflow"

# With description and tags
Start-WorkflowRecording -Name "Deploy" `
    -Description "Deploy to production" `
    -Tags "deployment", "production"

# Force stop existing recording
Start-WorkflowRecording -Name "NewWorkflow" -Force
```

**Parameters:**
- `Name` (required): Workflow name
- `Description`: Human-readable description
- `Tags`: Array of tags for categorization
- `Force`: Stop existing recording and start new one

**Returns:** WorkflowRecorder object

---

### Stop-WorkflowRecording

Stop current workflow recording.

```powershell
# Stop and save
Stop-WorkflowRecording -Save

# Stop and return workflow without saving
$workflow = Stop-WorkflowRecording -PassThru

# Stop, save, and return workflow
$workflow = Stop-WorkflowRecording -Save -PassThru
```

**Parameters:**
- `Save`: Save workflow to repository
- `PassThru`: Return workflow object

**Returns:** Workflow object (if -PassThru)

---

### Save-WorkflowStep

Manually record a workflow step.

```powershell
# Record successful step
Save-WorkflowStep -Command "dotnet build"

# Record failed step
Save-WorkflowStep -Command "dotnet test" -Failed -ExitCode 1

# Record with specific exit code
Save-WorkflowStep -Command "cargo build" -ExitCode 0
```

**Parameters:**
- `Command` (required): Command that was executed
- `ExitCode`: Exit code (default: 0)
- `Failed`: Mark step as failed

**Notes:**
- Requires active workflow recording
- Automatically captures working directory
- Timestamp recorded automatically

---

### Get-Workflow

Retrieve saved workflows.

```powershell
# Get all workflows
Get-Workflow

# Get specific workflow
Get-Workflow -Name "Deploy"

# Get workflows matching pattern
Get-Workflow -Name "Deploy*"

# Get workflows by tag
Get-Workflow -Tag "production"

# Get workflows with multiple tags
Get-Workflow -Tag "deployment", "production"
```

**Parameters:**
- `Name`: Workflow name (supports wildcards)
- `Tag`: Filter by tags

**Returns:** Array of Workflow objects

---

### Invoke-Workflow

Execute a saved workflow.

```powershell
# Basic execution
Invoke-Workflow -Name "Deploy"

# With variable substitution
Invoke-Workflow -Name "Deploy" -Variables @{
    Version = "v1.2.3"
    Environment = "production"
}

# Preview without executing (WhatIf)
Invoke-Workflow -Name "Deploy" -WhatIf

# Stop on first error
Invoke-Workflow -Name "Deploy" -StopOnError
```

**Parameters:**
- `Name` (required): Workflow name
- `Variables`: Hashtable of variable values
- `WhatIf`: Preview execution without running
- `StopOnError`: Stop on first failed step

**Returns:** WorkflowExecutionResult object

**Variable Substitution:**
- Merges default variables from workflow with provided variables
- Supports `${VarName}` and `$VarName` syntax
- Case-sensitive matching

---

### Remove-Workflow

Delete a saved workflow.

```powershell
# Remove workflow (prompts for confirmation)
Remove-Workflow -Name "OldWorkflow"

# Remove without confirmation
Remove-Workflow -Name "OldWorkflow" -Confirm:$false

# Remove from pipeline
Get-Workflow -Name "Test*" | Remove-Workflow
```

**Parameters:**
- `Name` (required): Workflow name
- Supports `-Confirm` and `-WhatIf`

**Confirmation:** High impact operation, prompts by default

---

## 🚀 Real-World Examples

### Example 1: CI/CD Pipeline

```powershell
# Record the pipeline once
Start-WorkflowRecording -Name "CIPipeline" `
    -Description "Full CI/CD pipeline" `
    -Tags "ci", "build", "test"

# Run your pipeline
git pull origin main
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet publish --configuration Release --output ./publish

Stop-WorkflowRecording -Save

# Now run it anytime
Invoke-Workflow -Name "CIPipeline"
```

### Example 2: Environment-Specific Deployment

```powershell
# Record with variables
Start-WorkflowRecording -Name "DeployApp"

Save-WorkflowStep -Command "docker build -t myapp:${Version} ."
Save-WorkflowStep -Command "docker tag myapp:${Version} myregistry.io/myapp:${Version}"
Save-WorkflowStep -Command "docker push myregistry.io/myapp:${Version}"
Save-WorkflowStep -Command "kubectl set image deployment/myapp myapp=myregistry.io/myapp:${Version} -n ${Namespace}"
Save-WorkflowStep -Command "kubectl rollout status deployment/myapp -n ${Namespace}"

Stop-WorkflowRecording -Save

# Deploy to staging
Invoke-Workflow -Name "DeployApp" -Variables @{
    Version = "v1.0.0"
    Namespace = "staging"
}

# Deploy to production
Invoke-Workflow -Name "DeployApp" -Variables @{
    Version = "v1.0.0"
    Namespace = "production"
}
```

### Example 3: Database Migration Workflow

```powershell
Start-WorkflowRecording -Name "DatabaseMigration" -Tags "database", "migration"

# Backup
Save-WorkflowStep -Command "pg_dump -h ${DbHost} -U ${DbUser} ${DbName} > backup_$(Get-Date -Format 'yyyyMMdd').sql"

# Run migrations
Save-WorkflowStep -Command "dotnet ef database update --project ${ProjectPath}"

# Verify
Save-WorkflowStep -Command "dotnet ef migrations list --project ${ProjectPath}"

Stop-WorkflowRecording -Save

# Run migration on different environments
Invoke-Workflow -Name "DatabaseMigration" -Variables @{
    DbHost = "staging-db.internal"
    DbUser = "admin"
    DbName = "myapp_staging"
    ProjectPath = "./src/MyApp.Data"
}
```

### Example 4: Testing Workflow with Error Handling

```powershell
Start-WorkflowRecording -Name "RunAllTests" -Tags "testing"

Save-WorkflowStep -Command "dotnet test ./tests/Unit.Tests"
Save-WorkflowStep -Command "dotnet test ./tests/Integration.Tests"
Save-WorkflowStep -Command "dotnet test ./tests/E2E.Tests"

Stop-WorkflowRecording -Save

# Run all tests, continue on error to see all results
$result = Invoke-Workflow -Name "RunAllTests"

# Check results
if ($result.Success) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed:" -ForegroundColor Red
    $result.Steps | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "  Step $($_.StepNumber): $($_.OriginalCommand)" -ForegroundColor Red
        Write-Host "  Error: $($_.Error)" -ForegroundColor Red
    }
}

# Stop on first failure
Invoke-Workflow -Name "RunAllTests" -StopOnError
```

### Example 5: Multi-Project Build

```powershell
Start-WorkflowRecording -Name "BuildAllProjects" -Tags "build", "multi-project"

Save-WorkflowStep -Command "cd ${ProjectRoot}/frontend && npm install"
Save-WorkflowStep -Command "cd ${ProjectRoot}/frontend && npm run build"
Save-WorkflowStep -Command "cd ${ProjectRoot}/backend && dotnet restore"
Save-WorkflowStep -Command "cd ${ProjectRoot}/backend && dotnet build --configuration Release"
Save-WorkflowStep -Command "cd ${ProjectRoot}/mobile && flutter pub get"
Save-WorkflowStep -Command "cd ${ProjectRoot}/mobile && flutter build apk"

Stop-WorkflowRecording -Save

# Build everything
Invoke-Workflow -Name "BuildAllProjects" -Variables @{
    ProjectRoot = "/home/user/myproject"
}
```

### Example 6: Preview Before Execution

```powershell
# Preview what would be executed
Invoke-Workflow -Name "DeployApp" -Variables @{
    Version = "v2.0.0"
    Namespace = "production"
} -WhatIf

# Output shows:
# What if: Performing the operation "Execute workflow step" on target "docker build -t myapp:v2.0.0 .".
# What if: Performing the operation "Execute workflow step" on target "kubectl set image deployment/myapp myapp=myregistry.io/myapp:v2.0.0 -n production".
```

### Example 7: Integration with DevCommand

```powershell
# Record a workflow that uses DevCommand for long-running tasks
Start-WorkflowRecording -Name "LongBuild"

# Use DevCommand for async execution within workflow
$buildJob = Start-DevCommand -Tool "cargo" -Arguments "build --release"
Wait-DevCommand -Job $buildJob

if ((Get-DevCommandStatus -Job $buildJob).ExitCode -eq 0) {
    Save-WorkflowStep -Command "cargo build --release" -ExitCode 0
} else {
    Save-WorkflowStep -Command "cargo build --release" -Failed -ExitCode 1
}

Stop-WorkflowRecording -Save
```

---

## 📊 Workflow Object Structure

### Workflow Properties

```powershell
$workflow = Get-Workflow -Name "Deploy"
$workflow.Name              # "Deploy"
$workflow.Description       # "Deploy to production"
$workflow.CreatedDate       # DateTime
$workflow.LastModified      # DateTime?
$workflow.Steps             # List<WorkflowStep>
$workflow.DefaultVariables  # Dictionary<string, string>
$workflow.Tags              # List<string>
$workflow.ExecutionCount    # int (increments on each execution)
```

### WorkflowStep Properties

```powershell
$step = $workflow.Steps[0]
$step.StepNumber         # 1
$step.Command            # "dotnet build"
$step.WorkingDirectory   # "/home/user/project"
$step.Timestamp          # DateTime
$step.Duration           # TimeSpan?
$step.ExitCode           # int?
$step.Success            # bool
$step.Output             # List<string>
$step.Errors             # List<string>
$step.Variables          # Dictionary<string, string>
```

### WorkflowExecutionResult Properties

```powershell
$result = Invoke-Workflow -Name "Deploy"
$result.WorkflowName     # "Deploy"
$result.StartTime        # DateTime
$result.EndTime          # DateTime
$result.Success          # bool (true if all steps succeeded)
$result.Steps            # List<StepExecutionResult>
```

### StepExecutionResult Properties

```powershell
$stepResult = $result.Steps[0]
$stepResult.StepNumber        # 1
$stepResult.OriginalCommand   # "dotnet build"
$stepResult.ExecutedCommand   # "dotnet build" (after variable substitution)
$stepResult.Success           # bool
$stepResult.StartTime         # DateTime
$stepResult.EndTime           # DateTime?
$stepResult.Duration          # TimeSpan?
$stepResult.Output            # List<string>
$stepResult.Error             # string (error message if failed)
```

---

## 🎯 Best Practices

### 1. Use Descriptive Names and Tags

```powershell
# Good
Start-WorkflowRecording -Name "DeployProductionAPI" `
    -Description "Deploy API to production cluster" `
    -Tags "deployment", "production", "api"

# Not ideal
Start-WorkflowRecording -Name "deploy1"
```

### 2. Parameterize Variable Parts

```powershell
# Good - uses variables
Save-WorkflowStep -Command "docker build -t myapp:${Version} ."
Save-WorkflowStep -Command "kubectl apply -f ${Environment}/config.yaml"

# Not ideal - hardcoded
Save-WorkflowStep -Command "docker build -t myapp:v1.0 ."
Save-WorkflowStep -Command "kubectl apply -f production/config.yaml"
```

### 3. Handle Errors Appropriately

```powershell
# For critical pipelines - stop on error
Invoke-Workflow -Name "DatabaseMigration" -StopOnError

# For test suites - see all failures
$result = Invoke-Workflow -Name "RunAllTests"
$failures = $result.Steps | Where-Object { -not $_.Success }
```

### 4. Use WhatIf for Verification

```powershell
# Always preview production deployments
Invoke-Workflow -Name "DeployProduction" -Variables @{Version="v2.0"} -WhatIf

# Then execute
Invoke-Workflow -Name "DeployProduction" -Variables @{Version="v2.0"}
```

### 5. Organize with Tags

```powershell
# Tag by purpose
Start-WorkflowRecording -Name "BuildFrontend" -Tags "build", "frontend"
Start-WorkflowRecording -Name "BuildBackend" -Tags "build", "backend"
Start-WorkflowRecording -Name "DeployAll" -Tags "deployment", "production"

# Query by tag
Get-Workflow -Tag "build"
Get-Workflow -Tag "production"
```

### 6. Version Workflows

```powershell
# Include version in workflow name for major changes
Start-WorkflowRecording -Name "DeployAPI_v2"
Start-WorkflowRecording -Name "DatabaseMigration_2024"
```

### 7. Document with Descriptions

```powershell
Start-WorkflowRecording -Name "SetupDevEnvironment" `
    -Description "Install all dependencies and configure local environment for development"
```

---

## 🔧 Advanced Usage

### Combining with Format-ForAI

```powershell
# Get workflow execution result in AI-friendly format
$result = Invoke-Workflow -Name "Deploy"
$result | Format-ForAI | Out-File deployment-report.json

# Analyze failures with AI Error Context
if (-not $result.Success) {
    Get-AIErrorContext -Last 10 | Format-ForAI
}
```

### Conditional Workflows

```powershell
$result = Invoke-Workflow -Name "RunTests"

if ($result.Success) {
    Write-Host "Tests passed, deploying..." -ForegroundColor Green
    Invoke-Workflow -Name "Deploy"
} else {
    Write-Host "Tests failed, aborting deployment" -ForegroundColor Red
    $result.Steps | Where-Object { -not $_.Success } | Format-List
}
```

### Workflow Chaining

```powershell
# Chain workflows together
$workflows = @("Build", "Test", "Package", "Deploy")

foreach ($workflowName in $workflows) {
    Write-Host "Executing: $workflowName" -ForegroundColor Cyan
    $result = Invoke-Workflow -Name $workflowName -StopOnError

    if (-not $result.Success) {
        Write-Error "Workflow $workflowName failed!"
        break
    }
}
```

### Workflow Execution Tracking

```powershell
# Track execution count
$workflow = Get-Workflow -Name "Deploy"
Write-Host "This workflow has been executed $($workflow.ExecutionCount) times"

# After execution, count increments
Invoke-Workflow -Name "Deploy"
$workflow = Get-Workflow -Name "Deploy"  # Reload
Write-Host "Now executed $($workflow.ExecutionCount) times"
```

---

## 💡 Tips and Tricks

### 1. Quick Replay of Recent Commands

```powershell
# Record your last session
Start-WorkflowRecording -Name "QuickFix"
# ... work on your fix ...
Stop-WorkflowRecording -Save

# Replay on another machine
Invoke-Workflow -Name "QuickFix"
```

### 2. Environment-Specific Variables

```powershell
# Create environment variable sets
$stagingVars = @{
    DbHost = "staging-db.internal"
    ApiUrl = "https://staging-api.example.com"
    Namespace = "staging"
}

$productionVars = @{
    DbHost = "prod-db.internal"
    ApiUrl = "https://api.example.com"
    Namespace = "production"
}

# Use them
Invoke-Workflow -Name "DeployApp" -Variables $stagingVars
Invoke-Workflow -Name "DeployApp" -Variables $productionVars
```

### 3. Export/Import Workflows

```powershell
# Workflows are stored as JSON, so you can copy them
# ~/.pwsh/workflows/MyWorkflow.json

# Copy to another machine or share with team
cp ~/.pwsh/workflows/Deploy.json /shared/workflows/

# On another machine
cp /shared/workflows/Deploy.json ~/.pwsh/workflows/

Get-Workflow -Name "Deploy"  # Now available
```

### 4. Debugging Failed Workflows

```powershell
$result = Invoke-Workflow -Name "Deploy"

# Find failed steps
$failedSteps = $result.Steps | Where-Object { -not $_.Success }

foreach ($step in $failedSteps) {
    Write-Host "Failed: $($step.ExecutedCommand)" -ForegroundColor Red
    Write-Host "Error: $($step.Error)" -ForegroundColor Yellow

    # Analyze with AI Error Context
    # (if the error is in $Error)
    Get-AIErrorContext | Where-Object { $_.SimplifiedMessage -like "*$($step.Error)*" }
}
```

---

## 📝 Implementation Notes

### File Locations

**Source Code:**
- `src/Microsoft.PowerShell.Development/Workflows/WorkflowModel.cs`
  - WorkflowStep class
  - Workflow class
  - WorkflowRepository (JSON persistence)
  - WorkflowRecorder (recording engine)

- `src/Microsoft.PowerShell.Development/Workflows/WorkflowCmdlets.cs`
  - Start-WorkflowRecording cmdlet
  - Stop-WorkflowRecording cmdlet
  - Save-WorkflowStep cmdlet
  - Get-Workflow cmdlet
  - Invoke-Workflow cmdlet
  - Remove-Workflow cmdlet
  - WorkflowExecutionResult class
  - StepExecutionResult class

**Storage:**
- Workflows: `~/.pwsh/workflows/*.json`

### Thread Safety

- WorkflowRepository uses locks for thread-safe access
- WorkflowRecorder uses locks for concurrent step recording
- Safe for parallel workflow execution

### Variable Substitution

- Supports `${VarName}` and `$VarName` syntax
- Case-sensitive matching
- Default variables merged with provided variables
- Provided variables override defaults

### Working Directory Handling

- Each step records its working directory
- On replay, changes to step's working directory
- Restores original directory after step execution
- Handles directory changes safely with try/finally

---

## 🎓 Learning Path

### 1. Start Simple

```powershell
# Record a simple 2-step workflow
Start-WorkflowRecording -Name "HelloWorkflow"
Save-WorkflowStep -Command "echo 'Step 1'"
Save-WorkflowStep -Command "echo 'Step 2'"
Stop-WorkflowRecording -Save

# Replay it
Invoke-Workflow -Name "HelloWorkflow"
```

### 2. Add Variables

```powershell
Start-WorkflowRecording -Name "GreetUser"
Save-WorkflowStep -Command "echo 'Hello, ${Name}!'"
Stop-WorkflowRecording -Save

Invoke-Workflow -Name "GreetUser" -Variables @{Name="Alice"}
Invoke-Workflow -Name "GreetUser" -Variables @{Name="Bob"}
```

### 3. Build Real Workflows

```powershell
# Record your actual development workflow
Start-WorkflowRecording -Name "MyDevWorkflow" -Tags "development"
# ... do your actual work ...
Stop-WorkflowRecording -Save

# Use it daily
Invoke-Workflow -Name "MyDevWorkflow"
```

### 4. Integrate with Other Features

```powershell
# Combine with DevCommand for async execution
$job = Start-DevCommand cargo build --release
Wait-DevCommand -Job $job

# Use Get-AIErrorContext for error analysis
if ((Get-DevCommandStatus -Job $job).ExitCode -ne 0) {
    Get-AIErrorContext -Last 5 | Format-ForAI
}

# Save the workflow
Stop-WorkflowRecording -Save
```

---

## 🔗 Related Features

- **DevCommand**: Use for long-running async tasks within workflows
- **Format-ForAI**: Export workflow results for AI analysis
- **Get-AIErrorContext**: Analyze workflow step failures
- **Get-ProjectContext**: Auto-detect project for workflow context
- **CLI Tool Registry**: Normalize tool invocations in workflows

---

*Part of the Microsoft.PowerShell.Development module*
