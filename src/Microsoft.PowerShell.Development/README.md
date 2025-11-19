# Microsoft.PowerShell.Development Module

PowerShell module for AI-assisted software development and CLI tool integration.

## Overview

This module adds features to PowerShell specifically designed to enhance productivity when working with AI coding assistants like Claude Code and development CLI tools.

## Features Implemented

### 1. Project Context Detection (`Get-ProjectContext`)

Auto-detects project type based on files in the directory.

**Usage:**
```powershell
# Detect project in current directory
Get-ProjectContext

# Detect project in specific path
Get-ProjectContext -Path ~/myproject

# Search parent directories if not found
Get-ProjectContext -SearchParent
```

**Supported Project Types:**
- Node.js (package.json)
- Rust (Cargo.toml)
- .NET (*.csproj, *.sln)
- Python (setup.py, pyproject.toml, requirements.txt)
- Go (go.mod)
- Java Maven (pom.xml)
- Java Gradle (build.gradle)
- Ruby (Gemfile)
- PHP (composer.json)
- PowerShell Module (*.psd1)

**Example Output:**
```powershell
ProjectType      : Node.js
DetectedFrom     : package.json
RootPath         : /home/user/myproject
BuildTool        : npm
TestFramework    : jest/mocha
Language         : JavaScript
SuggestedCommands: {npm install, npm run build, npm test}
```

---

### 2. Async Development Command Execution (`Start-DevCommand`)

Run long-running CLI commands asynchronously without timeout issues.

**Problem Solved:**
- Claude Code has a 2-minute command timeout
- Long builds/tests get killed
- No way to monitor progress of running commands

**Solution:**
```powershell
# Start a long-running build
$job = Start-DevCommand -Tool "cargo" -Args "build --release"

# Check status while it's running
Get-DevCommandStatus -Job $job

# Get new output lines
Receive-DevCommandOutput -Job $job -NewOnly

# Wait for completion with progress
Wait-DevCommand -Job $job -ShowProgress

# Stop if needed
Stop-DevCommand -Job $job
```

**Real-World Example:**
```powershell
# Start build in background
$build = Start-DevCommand npm run build

# Do other work...
Get-Process | Format-ForAI

# Check if build is done
$status = Get-DevCommandStatus -Job $build
if ($status.State -eq "Completed") {
    Receive-DevCommandOutput -Job $build
}
```

**Status Information:**
```powershell
Get-DevCommandStatus -Job $job

Id             : 12345
Tool           : cargo
Arguments      : build --release
State          : Running
StartTime      : 2025-11-19 10:30:00
Duration       : 00:02:15
OutputLines    : 234
ErrorLines     : 12
CurrentOutput  : [2/5] Compiling main.rs
```

---

### 3. AI-Friendly Formatting (`Format-ForAI`)

Convert PowerShell objects to structured formats (JSON/YAML) for AI consumption.

**Problem Solved:**
- AI must parse text-based table output (brittle, error-prone)
- Information is lost in text conversion
- Different tools have different output formats

**Solution:**
```powershell
# Format as JSON (default)
Get-Process | Format-ForAI

# Format as YAML
Get-ChildItem | Format-ForAI -OutputType Yaml

# Compact JSON (no whitespace)
Get-Service | Format-ForAI -OutputType Compact

# Include type information
Get-Date | Format-ForAI -IncludeTypeInfo

# Control serialization depth
$complexObject | Format-ForAI -Depth 5
```

**Example Output:**
```json
[
  {
    "Name": "pwsh",
    "Id": 12345,
    "CPU": 45.3,
    "WorkingSet": 123456789,
    "StartTime": "2025-11-19T10:00:00"
  },
  {
    "Name": "code",
    "Id": 67890,
    "CPU": 123.7,
    "WorkingSet": 987654321,
    "StartTime": "2025-11-19T09:30:00"
  }
]
```

**Use Cases:**
```powershell
# Claude can now parse this reliably
dotnet build | Format-ForAI

# Extract specific data programmatically
$result = npm test | Format-ForAI | ConvertFrom-Json
$failures = $result.tests | Where-Object { $_.status -eq "failed" }
```

---

## Cmdlet Reference

### Get-ProjectContext
- **Alias:** `gpc`
- **Parameters:**
  - `-Path` - Directory to analyze (default: current directory)
  - `-SearchParent` - Search parent directories if not found

### Start-DevCommand
- **Alias:** `devcmd`
- **Parameters:**
  - `-Tool` - Command/tool to run (e.g., "npm", "cargo", "dotnet")
  - `-Arguments` - Arguments to pass to the tool
  - `-WorkingDirectory` - Working directory (default: current)
  - `-Name` - Job name (default: auto-generated)
- **Returns:** DevCommandJob object

### Get-DevCommandStatus
- **Parameters:**
  - `-Job` - Job to check status for
  - `-All` - Show status of all DevCommand jobs
- **Returns:** DevCommandStatus object

### Wait-DevCommand
- **Parameters:**
  - `-Job` - Job to wait for
  - `-Timeout` - Timeout in seconds (default: infinite)
  - `-ShowProgress` - Display output as it arrives
- **Returns:** DevCommandJob object

### Stop-DevCommand
- **Parameters:**
  - `-Job` - Job to stop
- **Returns:** Nothing

### Receive-DevCommandOutput
- **Parameters:**
  - `-Job` - Job to receive output from
  - `-NewOnly` - Only get new output since last call
  - `-IncludeErrors` - Include error stream
  - `-Last` - Get last N lines only
- **Returns:** String array of output lines

### Format-ForAI
- **Alias:** `fai`
- **Parameters:**
  - `-OutputType` - Format: Json (default), Yaml, Compact
  - `-Depth` - Max serialization depth (default: 10)
  - `-IncludeTypeInfo` - Include PowerShell type information
  - `-InputObject` - Object(s) to format (from pipeline)
- **Returns:** Formatted string

---

## Architecture

### DevCommandJob System

Built on PowerShell's native Job infrastructure:

1. **Job Class Extension:** Inherits from `System.Management.Automation.Job`
2. **Async Process Management:** Uses `System.Diagnostics.Process` with async I/O
3. **Stream Buffering:** Captures stdout/stderr in queryable buffers
4. **State Management:** Tracks Running/Completed/Failed/Stopped states
5. **Progress Tracking:** Maintains output line counts and timing

**Key Benefits:**
- No timeout limits
- Works with PowerShell's job cmdlets (Get-Job, Remove-Job, etc.)
- Integrates with existing workflows
- Full output history preserved

### Format-ForAI Implementation

Custom formatter that:

1. Preserves PowerShell object structure
2. Handles nested objects up to configurable depth
3. Supports multiple output formats (JSON/YAML)
4. Maintains type fidelity
5. Handles edge cases (null, circular references, etc.)

**Integration Point:**
- Works with any PowerShell cmdlet via pipeline
- Compatible with ConvertFrom-Json for round-tripping
- Can be combined with other formatters

---

## Building

The module is automatically built as part of the PowerShell build process.

```powershell
# Bootstrap (one-time setup)
Import-Module ./build.psm1
Start-PSBootstrap

# Build PowerShell with new module
Start-PSBuild -Clean -PSModuleRestore
```

## Testing

```powershell
# Test project detection
Get-ProjectContext

# Test async command
$job = Start-DevCommand echo "Hello World"
Wait-DevCommand -Job $job
Receive-DevCommandOutput -Job $job

# Test formatting
Get-Process | Select-Object -First 5 | Format-ForAI
```

---

## Future Enhancements (Not Yet Implemented)

### Phase 2 Features
- `Register-CliTool` - Register CLI tools with normalized interfaces
- `Invoke-CliTool` - Invoke registered tools with standard syntax
- `Get-CliTool` - List registered tools

### Phase 3 Features
- `Get-AIErrorContext` - Enhanced error analysis for AI
- Workflow recording and replay
- Smart command suggestions
- Progress percentage tracking for known tools

---

## Implementation Notes

### Files Created

**Core Classes:**
- `ProjectContext/ProjectContext.cs` - Project detection logic
- `ProjectContext/GetProjectContextCommand.cs` - Get-ProjectContext cmdlet
- `DevCommand/DevCommandJob.cs` - Async job implementation
- `DevCommand/DevCommandCmdlets.cs` - DevCommand management cmdlets
- `Formatters/FormatForAICommand.cs` - Format-ForAI cmdlet

**Configuration:**
- `Microsoft.PowerShell.Development.csproj` - Project file
- `Modules/Shared/Microsoft.PowerShell.Development/Microsoft.PowerShell.Development.psd1` - Module manifest

**Integration:**
- Added to `PowerShell.sln`
- Referenced in `Microsoft.PowerShell.SDK.csproj`
- Automatically deployed with PowerShell build

### Dependencies

- `System.Text.Json` - JSON serialization
- `YamlDotNet` - YAML serialization
- `System.Management.Automation` - PowerShell APIs

---

## License

Copyright (c) Microsoft Corporation. Licensed under the MIT License.
