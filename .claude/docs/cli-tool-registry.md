# CLI Tool Registry - Normalize All Your Development Tools

## 🎯 The Problem

Every CLI tool has different conventions:

```bash
# Git uses single-dash flags
git commit -m "message" -v

# npm uses double-dash
npm install --save-dev --verbose

# cargo uses mixed
cargo build --release -v

# dotnet uses double-dash with full words
dotnet build --configuration Release --verbosity detailed
```

**For AI tools like Claude Code:**
- Must remember syntax for each tool
- Can't normalize commands across tools
- Error parsing is tool-specific and fragile
- No consistent way to invoke tools

## ✨ The Solution: CLI Tool Registry

Register tools once, invoke them with a normalized interface.

### Quick Example

```powershell
# Get pre-registered tools
Get-CliTool

# Invoke with normalized interface
Invoke-CliTool git commit -Parameters @{Message="fix: bug"; Verbose=$true}
# Automatically translates to: git commit -m "fix: bug" -v

Invoke-CliTool npm install -Parameters @{SaveDev=$true; Verbose=$true}
# Automatically translates to: npm install --save-dev --verbose

Invoke-CliTool cargo build -Parameters @{Release=$true; Verbose=$true}
# Automatically translates to: cargo build --release -v
```

---

## 📦 Pre-Registered Tools

The following tools are automatically registered with PowerShell:

| Tool | Description | Common Commands |
|------|-------------|-----------------|
| `git` | Version control | status, add, commit, push, pull, clone, log |
| `npm` | Node.js package manager | install, test, run, publish, update, start |
| `cargo` | Rust build tool | build, test, run, check, clean, update |
| `dotnet` | .NET SDK | build, test, run, restore, publish, clean |
| `pip` | Python package installer | install, uninstall, list, freeze, show |
| `docker` | Container platform | build, run, ps, stop, rm, images, pull, push |
| `kubectl` | Kubernetes CLI | get, describe, apply, delete, logs, exec |

---

## 📖 Cmdlet Reference

### Get-CliTool

List registered CLI tools.

**Syntax:**
```powershell
Get-CliTool [-Name <string>]
```

**Examples:**
```powershell
# List all registered tools
Get-CliTool

# Get specific tool
Get-CliTool git

# Wildcards supported
Get-CliTool *net
```

**Output:**
```
Name         : git
ExecutablePath: git
Description  : Distributed version control system
ParameterMappings : {Message=-m, Verbose=-v, All=-a, Force=-f...}
ErrorPatterns    : {fatal:=Critical, error:=Error, warning:=Warning...}
CommonCommands   : {status, add, commit, push, pull, clone, log}
HelpUrl      : https://git-scm.com/docs
```

---

### Register-CliTool

Register a new CLI tool or update an existing one.

**Syntax:**
```powershell
Register-CliTool -Name <string>
    [-ExecutablePath <string>]
    [-Description <string>]
    [-ParameterMappings <hashtable>]
    [-ErrorPatterns <hashtable>]
    [-ExitCodeMappings <hashtable>]
    [-CommonCommands <string[]>]
    [-HelpUrl <string>]
    [-PassThru]
```

**Example 1: Basic Registration**
```powershell
Register-CliTool -Name "myapp" -Description "My custom application"
```

**Example 2: With Parameter Mappings**
```powershell
Register-CliTool -Name "gradle" `
    -Description "Gradle build tool" `
    -ParameterMappings @{
        "Verbose" = "-v"
        "Quiet" = "-q"
        "Build" = "build"
        "Clean" = "clean"
    } `
    -ErrorPatterns @{
        "FAILURE:" = "Error"
        "WARNING:" = "Warning"
    } `
    -CommonCommands @("build", "test", "clean", "assemble")
```

**Example 3: Complex Tool**
```powershell
Register-CliTool -Name "terraform" `
    -ExecutablePath "/usr/local/bin/terraform" `
    -Description "Infrastructure as Code tool" `
    -ParameterMappings @{
        "AutoApprove" = "-auto-approve"
        "Verbose" = "-verbose"
        "VarFile" = "-var-file"
    } `
    -ErrorPatterns @{
        "Error:" = "Error"
        "Warning:" = "Warning"
    } `
    -ExitCodeMappings @{
        "Success" = 0
        "Error" = 1
        "PlanChanges" = 2
    } `
    -CommonCommands @("plan", "apply", "destroy", "init", "validate") `
    -HelpUrl "https://www.terraform.io/docs" `
    -PassThru
```

---

### Unregister-CliTool

Remove a registered CLI tool.

**Syntax:**
```powershell
Unregister-CliTool -Name <string> [-WhatIf] [-Confirm]
```

**Examples:**
```powershell
# Unregister a tool
Unregister-CliTool myapp

# With confirmation
Unregister-CliTool terraform -Confirm

# Preview what would happen
Unregister-CliTool terraform -WhatIf
```

---

### Invoke-CliTool

Execute a registered CLI tool with normalized parameters.

**Syntax:**
```powershell
Invoke-CliTool -Tool <string>
    [-Arguments <string[]>]
    [-Parameters <hashtable>]
    [-WorkingDirectory <string>]
    [-Timeout <int>]
    [-CategorizeErrors]
```

**Example 1: Simple Invocation**
```powershell
Invoke-CliTool git status
```

**Example 2: With Normalized Parameters**
```powershell
Invoke-CliTool git commit -Parameters @{
    Message = "fix: critical bug in auth"
    Verbose = $true
}
# Executes: git commit -m "fix: critical bug in auth" -v
```

**Example 3: Mixed Arguments and Parameters**
```powershell
Invoke-CliTool npm install express -Parameters @{
    Save = $true
    Verbose = $true
}
# Executes: npm install express --save --verbose
```

**Example 4: With Error Categorization**
```powershell
$result = Invoke-CliTool cargo build -Parameters @{Release=$true} -CategorizeErrors

# Check result
$result.Success          # True/False
$result.ExitCode         # Numeric exit code
$result.ExitCodeCategory # "Success", "CompilationError", etc.
$result.Output           # Array of output lines
$result.Errors           # Array of error lines
$result.CategorizedErrors # Structured error objects
$result.Duration         # TimeSpan

# Access categorized errors
foreach ($error in $result.CategorizedErrors) {
    Write-Host "$($error.Severity): $($error.Message)"
    if ($error.File) {
        Write-Host "  at $($error.File):$($error.Line):$($error.Column)"
    }
}
```

**Example 5: With Timeout**
```powershell
# 10 second timeout
$result = Invoke-CliTool dotnet build -Timeout 10000 -Parameters @{
    Configuration = "Release"
}
```

---

## 🔥 Real-World Scenarios

### Scenario 1: Normalized Git Workflow

```powershell
# Before (different tools, different syntax)
git add .
git commit -m "feat: add new feature" -v
git push origin main

# After (normalized)
Invoke-CliTool git add .
Invoke-CliTool git commit -Parameters @{Message="feat: add new feature"; Verbose=$true}
Invoke-CliTool git push origin main
```

### Scenario 2: Build with Error Analysis

```powershell
$build = Invoke-CliTool cargo build -Parameters @{Release=$true} -CategorizeErrors

if (!$build.Success) {
    Write-Host "Build failed with $($build.CategorizedErrors.Count) errors:"

    $build.CategorizedErrors |
        Where-Object {$_.Severity -eq "Error"} |
        ForEach-Object {
            Write-Host "  [$($_.File):$($_.Line)] $($_.Message)"
        }
}
```

### Scenario 3: Cross-Tool CI/CD Pipeline

```powershell
# Normalized pipeline that works across project types
$ctx = Get-ProjectContext

switch ($ctx.ProjectType) {
    "Node.js" {
        Invoke-CliTool npm install
        $test = Invoke-CliTool npm test -CategorizeErrors
    }
    "Rust" {
        Invoke-CliTool cargo build -Parameters @{Release=$true}
        $test = Invoke-CliTool cargo test -CategorizeErrors
    }
    ".NET" {
        Invoke-CliTool dotnet restore
        Invoke-CliTool dotnet build -Parameters @{Configuration="Release"}
        $test = Invoke-CliTool dotnet test -CategorizeErrors
    }
}

# Common error handling
if (!$test.Success) {
    $test.CategorizedErrors | Format-ForAI | Out-File errors.json
}
```

### Scenario 4: AI-Friendly Output

```powershell
# Claude Code can easily parse this
$result = Invoke-CliTool dotnet build -CategorizeErrors
$result | Format-ForAI

# Output (JSON):
{
  "ToolName": "dotnet",
  "Command": "dotnet build",
  "ExitCode": 1,
  "ExitCodeCategory": "Error",
  "Success": false,
  "Duration": "00:00:12.3456",
  "Output": [...],
  "Errors": [...],
  "CategorizedErrors": [
    {
      "Category": "Error",
      "Message": "error CS1002: ; expected",
      "File": "Program.cs",
      "Line": 23,
      "Column": 45,
      "Severity": "Error"
    }
  ]
}
```

---

## 🎓 How It Works

### Parameter Mapping

When you invoke a tool with normalized parameters:

```powershell
Invoke-CliTool git commit -Parameters @{Message="test"; Verbose=$true}
```

**The system:**
1. Looks up `git` in the registry
2. Finds parameter mappings: `{Message="-m", Verbose="-v"}`
3. Translates parameters:
   - `Message="test"` → `-m "test"`
   - `Verbose=$true` → `-v`
4. Executes: `git commit -m "test" -v`

### Error Pattern Matching

When `-CategorizeErrors` is specified:

```powershell
$result = Invoke-CliTool cargo build -CategorizeErrors
```

**The system:**
1. Captures stdout and stderr
2. Matches lines against error patterns:
   - `"error:"` → Category="Error"
   - `"warning:"` → Category="Warning"
   - `"note:"` → Category="Information"
3. Extracts file/line/column using regex: `file:line:col`
4. Returns structured `CliToolError` objects

### Exit Code Categorization

Tools can define exit code meanings:

```powershell
Register-CliTool terraform -ExitCodeMappings @{
    "Success" = 0
    "Error" = 1
    "PlanChanges" = 2
}

$result = Invoke-CliTool terraform plan
# $result.ExitCodeCategory = "PlanChanges" (if exit code = 2)
```

---

## 🚀 Benefits for AI Coding Tools

### Before CLI Tool Registry

**Claude Code must:**
- Remember syntax for each tool (git: `-m`, npm: `--message`, cargo: `-m`)
- Parse unstructured error text
- Guess what exit codes mean
- Handle each tool differently

**Example:**
```
User: Commit changes with message "fix bug"
Claude: git commit -m "fix bug"
[Output: text to parse]
Claude: [parses text to determine success]
```

### After CLI Tool Registry

**Claude Code can:**
- Use normalized interface for all tools
- Get structured error data
- Know exactly what errors mean
- Handle all tools consistently

**Example:**
```powershell
User: Commit changes with message "fix bug"
Claude:
  $result = Invoke-CliTool git commit `
    -Parameters @{Message="fix bug"} `
    -CategorizeErrors

  if ($result.Success) {
    Write-Host "✓ Committed successfully"
  } else {
    # Structured error analysis
    $result.CategorizedErrors | Format-ForAI
  }
```

---

## 📊 Comparison

| Aspect | Before | After |
|--------|--------|-------|
| **Syntax** | Tool-specific (`-m` vs `--message`) | Normalized (`Message`) |
| **Error Parsing** | Text regex (brittle) | Structured objects |
| **Exit Codes** | Numeric only | Named categories |
| **Consistency** | Each tool different | All tools same |
| **AI Friendliness** | Low | High |

---

## 🔮 Advanced Usage

### Custom Tool with Complex Mappings

```powershell
Register-CliTool -Name "mycompiler" `
    -ExecutablePath "~/bin/mycompiler" `
    -ParameterMappings @{
        "Optimize" = "-O"
        "Debug" = "-g"
        "Output" = "-o"
        "Include" = "-I"
        "Define" = "-D"
        "Warnings" = "-Wall"
    } `
    -ErrorPatterns @{
        "fatal error:" = "Critical"
        "error:" = "Error"
        "warning:" = "Warning"
        "note:" = "Information"
    } `
    -ExitCodeMappings @{
        "Success" = 0
        "CompileError" = 1
        "LinkerError" = 2
    }

# Use it
$result = Invoke-CliTool mycompiler main.c -Parameters @{
    Optimize = 3
    Debug = $true
    Output = "main.exe"
    Warnings = $true
} -CategorizeErrors
```

### Pipeline Integration

```powershell
# Get all registered tools
Get-CliTool |
    Where-Object {$_.CommonCommands -contains "test"} |
    ForEach-Object {
        Write-Host "Testing with $($_.Name)..."
        Invoke-CliTool $_.Name test -CategorizeErrors
    }
```

### Dynamic Tool Registration

```powershell
# Register tools from configuration file
$config = Get-Content tools.json | ConvertFrom-Json

foreach ($tool in $config.tools) {
    Register-CliTool -Name $tool.name `
        -ExecutablePath $tool.path `
        -ParameterMappings $tool.parameters `
        -ErrorPatterns $tool.errors
}
```

---

## 🎯 Key Takeaways

1. **Normalized Interface** - All tools use same parameter naming
2. **Structured Errors** - No more text parsing brittleness
3. **Exit Code Meaning** - Know what codes mean per tool
4. **Pre-configured** - 7 common tools already registered
5. **Extensible** - Easy to add your own tools
6. **AI-Friendly** - Perfect for Claude Code automation

---

## 📝 Implementation Notes

**Files:**
- `src/Microsoft.PowerShell.Development/CliTools/CliToolRegistry.cs` - Registry and models
- `src/Microsoft.PowerShell.Development/CliTools/CliToolCmdlets.cs` - Cmdlets

**Pre-registered tools:**
- git, npm, cargo, dotnet, pip, docker, kubectl

**Cmdlets:**
- `Register-CliTool`
- `Get-CliTool`
- `Unregister-CliTool`
- `Invoke-CliTool`

---

*Part of the Microsoft.PowerShell.Development module*
