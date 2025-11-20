# AI Error Context - Intelligent Error Analysis

## 🎯 The Problem

PowerShell errors are hard for AI tools to work with:

```powershell
# Error you see:
Get-ChildItem : Cannot find path 'C:\NonExistent' because it does not exist.
At line:1 char:1
+ Get-ChildItem C:\NonExistent
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : ObjectNotFound: (C:\NonExistent:String) [Get-ChildItem], ItemNotFoundException
    + FullyQualifiedErrorId : PathNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand
```

**What AI tools need:**
- Root cause explanation
- Suggested fixes
- Documentation links
- Severity level
- Structured, parseable data

## ✨ The Solution: Get-AIErrorContext

Transform raw errors into AI-friendly context with analysis and suggestions.

### Quick Example

```powershell
# Run a command that fails
Get-ChildItem C:\NonExistent

# Analyze the error
$Error[0] | Get-AIErrorContext

# Output:
ErrorId           : PathNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand
Category          : ObjectNotFound
Severity          : Error
SimplifiedMessage : Cannot find path 'C:\NonExistent' because it does not exist
RootCause         : The specified file or path does not exist
SuggestedFixes    : {Verify the file path is correct, Use Test-Path to check...}
DocumentationLinks: {https://docs.microsoft.com/powershell/module/...}
File              :
Line              : 1
Column            : 1
```

---

## 📖 Usage

### Basic Analysis

```powershell
# Analyze the most recent error
Get-AIErrorContext

# Analyze a specific error by index
Get-AIErrorContext -Index 2  # Third most recent error ($Error[2])

# Analyze last 5 errors
Get-AIErrorContext -Last 5

# Analyze a specific error record
$Error[0] | Get-AIErrorContext

# With verbose output
Get-AIErrorContext -Detailed -Verbose
```

### Integration with Format-ForAI

```powershell
# Get structured JSON for AI consumption
Get-AIErrorContext | Format-ForAI

# Output:
{
  "ErrorId": "CommandNotFoundException",
  "Category": "ObjectNotFound",
  "Severity": "Error",
  "SimplifiedMessage": "The term 'foo' is not recognized as a cmdlet, function...",
  "RootCause": "The command or cmdlet does not exist or is not in the PATH",
  "SuggestedFixes": [
    "Verify the command name is spelled correctly",
    "Check if the module is imported: Import-Module <ModuleName>",
    "Verify the command is installed: Get-Command <CommandName>",
    "Add the executable directory to your PATH"
  ],
  "DocumentationLinks": [
    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_command_precedence"
  ]
}
```

---

## 🔍 Error Patterns Recognized

The system recognizes and provides specific guidance for:

### 1. **Command Not Found**
```powershell
# Error
foo
# Analysis provides:
- RootCause: "Command does not exist or is not in PATH"
- Fixes: Check spelling, import module, verify installation
- Links: PowerShell command precedence documentation
```

### 2. **Parameter Binding Errors**
```powershell
# Error
Get-Process -InvalidParam
# Analysis provides:
- RootCause: "Parameter value is invalid or name is incorrect"
- Fixes: Check spelling, verify type, use Get-Help
- Links: PowerShell parameters documentation
```

### 3. **File Not Found**
```powershell
# Error
Get-Content C:\NonExistent.txt
# Analysis provides:
- RootCause: "The specified file or path does not exist"
- Fixes: Verify path, use Test-Path, check typos
- Links: Test-Path documentation
```

### 4. **Permission Denied**
```powershell
# Error
Remove-Item C:\Windows\System32\file.dll
# Analysis provides:
- RootCause: "Insufficient permissions to perform the operation"
- Fixes: Run as Administrator, check permissions, verify file isn't locked
- Links: Execution policies documentation
```

### 5. **Compilation Errors** (for dotnet, etc.)
```powershell
# Error from dotnet build
error CS1002: ; expected
# Analysis provides:
- RootCause: "Code syntax error or compilation failure"
- Fixes: Check line/column, review changes, verify imports
- Links: C# compiler messages documentation
```

### 6. **Package Not Found**
```powershell
# Error from npm, nuget, cargo
cannot find package 'nonexistent'
# Analysis provides:
- RootCause: "Package does not exist or cannot be accessed"
- Fixes: Verify name/version, check registry, run restore, check network
- Links: Package registry documentation
```

### 7. **Type Mismatch**
```powershell
# Error
[int]"not a number"
# Analysis provides:
- RootCause: "Value cannot be converted to the expected type"
- Fixes: Check data type, use explicit conversion, verify format
- Links: Type conversion documentation
```

### 8. **Timeout**
```powershell
# Error
Invoke-WebRequest -TimeoutSec 1 http://slow-site.com
# Analysis provides:
- RootCause: "Operation exceeded the allowed time limit"
- Fixes: Increase timeout, check network, verify service, use async
- Links: PowerShell jobs documentation
```

### 9. **Out of Memory**
```powershell
# Error
$huge = 1..1000000000
# Analysis provides:
- RootCause: "Insufficient memory available"
- Severity: "Critical"
- Fixes: Process in chunks, use streaming, dispose objects, optimize usage
- Links: Garbage collection documentation
```

### 10. **Null Reference**
```powershell
# Error
$null.Property
# Analysis provides:
- RootCause: "Attempting to use a null object"
- Fixes: Check for null, use null-conditional, verify initialization
- Links: PowerShell operators documentation
```

### 11. **Module Not Found**
```powershell
# Error
Import-Module NonExistentModule
# Analysis provides:
- RootCause: "Module is not installed or not in module path"
- Fixes: Install module, check spelling, verify path, import explicitly
- Links: Install-Module documentation
```

---

## 🚀 Real-World Examples

### Example 1: Debugging Build Failures

```powershell
# Run build (fails)
dotnet build

# Analyze all errors from the build
Get-AIErrorContext -Last 10 | Format-ForAI | Out-File build-errors.json

# Claude Code can now:
$errors = Get-Content build-errors.json | ConvertFrom-Json
$criticalErrors = $errors | Where-Object {$_.Severity -eq "Critical"}

foreach ($error in $criticalErrors) {
    Write-Host "CRITICAL: $($error.SimplifiedMessage)"
    Write-Host "Root Cause: $($error.RootCause)"
    Write-Host "Suggested fixes:"
    $error.SuggestedFixes | ForEach-Object { Write-Host "  - $_" }
}
```

### Example 2: Automated Error Reporting

```powershell
# Wrapper function for error-prone operations
function Invoke-WithErrorAnalysis {
    param([scriptblock]$ScriptBlock)

    try {
        & $ScriptBlock
    }
    catch {
        $context = $_ | Get-AIErrorContext

        # Log structured error
        $context | Format-ForAI | Add-Content error-log.json

        # Display helpful information
        Write-Host "Error: $($context.SimplifiedMessage)" -ForegroundColor Red
        Write-Host "Root Cause: $($context.RootCause)" -ForegroundColor Yellow
        Write-Host "`nSuggested fixes:" -ForegroundColor Green
        $context.SuggestedFixes | ForEach-Object {
            Write-Host "  - $_" -ForegroundColor Green
        }

        throw
    }
}

# Usage
Invoke-WithErrorAnalysis {
    Get-ChildItem C:\NonExistent
}
```

### Example 3: Integration with DevCommand

```powershell
# Run build with error analysis
$build = Start-DevCommand dotnet build
Wait-DevCommand -Job $build

if ((Get-DevCommandStatus -Job $build).ExitCode -ne 0) {
    # Analyze build errors
    $output = Receive-DevCommandOutput -Job $build -IncludeErrors

    # Extract errors and analyze
    $errors = $output | Where-Object {$_ -match "error"}

    # Create mock error records and analyze
    Write-Host "Build failed with errors:"
    Write-Host "Common issues:" -ForegroundColor Yellow
    Get-AIErrorContext -Last 5 | ForEach-Object {
        if ($_.SuggestedFixes.Count -gt 0) {
            Write-Host "  - $($_.SuggestedFixes[0])"
        }
    }
}
```

### Example 4: AI-Assisted Debugging

```powershell
# Get error context and ask AI for help
function Get-AIHelp {
    $context = Get-AIErrorContext

    $prompt = @"
I encountered this error:

Category: $($context.Category)
Message: $($context.SimplifiedMessage)
Root Cause: $($context.RootCause)

File: $($context.File):$($context.Line)

Suggested fixes:
$($context.SuggestedFixes -join "`n")

Can you help me debug this?
"@

    # Send to AI assistant or display
    Write-Host $prompt

    # Could integrate with Claude Code API here
}
```

---

## 📊 Error Context Properties

| Property | Type | Description |
|----------|------|-------------|
| `ErrorId` | string | Fully qualified error ID |
| `Category` | string | PowerShell error category |
| `Severity` | string | Error, Warning, or Critical |
| `OriginalMessage` | string | Full original error message |
| `SimplifiedMessage` | string | Cleaned, short error message |
| `RootCause` | string | Explanation of why this error occurred |
| `SuggestedFixes` | string[] | Array of suggested solutions |
| `DocumentationLinks` | string[] | Relevant documentation URLs |
| `Tool` | string | Tool that generated the error (if applicable) |
| `File` | string | File where error occurred |
| `Line` | int? | Line number |
| `Column` | int? | Column number |
| `RelatedErrors` | string[] | Similar error patterns |
| `AdditionalContext` | dictionary | Extra contextual information |

---

## 🎯 Benefits for AI Coding Tools

### Before Get-AIErrorContext

**Claude Code sees:**
```
Get-ChildItem : Cannot find path 'C:\NonExistent' because it does not exist.
At line:1 char:1
```

**Claude must:**
- Parse unstructured text
- Guess root cause
- Search for solutions online
- May provide incorrect suggestions

### After Get-AIErrorContext

**Claude Code gets:**
```json
{
  "RootCause": "The specified file or path does not exist",
  "SuggestedFixes": [
    "Verify the file path is correct",
    "Use Test-Path to check if the file exists",
    "Check for typos in the filename"
  ],
  "DocumentationLinks": ["https://..."],
  "Severity": "Error"
}
```

**Claude can:**
- Immediately understand the problem
- Provide accurate, specific solutions
- Link to relevant documentation
- Prioritize by severity

---

## 🔧 Advanced Usage

### Custom Error Patterns

You can extend the error analyzer with custom patterns (future enhancement):

```powershell
# Planned feature
Register-ErrorPattern -Pattern "custom error" `
    -Category "CustomError" `
    -RootCause "Explanation" `
    -SuggestedFixes @("Fix 1", "Fix 2") `
    -DocumentationLinks @("https://...")
```

### Error History Tracking

Analyze trends in errors (future enhancement):

```powershell
# Planned feature
Get-ErrorHistory -Last 100 |
    Group-Object Category |
    Sort-Object Count -Descending
```

---

## 📚 Integration Examples

### With CI/CD

```powershell
# In CI pipeline
try {
    Invoke-Build
    Invoke-Test
}
catch {
    $context = $_ | Get-AIErrorContext

    # Send to CI system
    $report = $context | Format-ForAI
    Set-Content -Path "$env:BUILD_ARTIFACTSTAGINGDIRECTORY/error-analysis.json" -Value $report

    # Fail build with helpful message
    Write-Error "Build failed: $($context.RootCause)"
    exit 1
}
```

### With Logging

```powershell
# Enhanced logging function
function Write-ErrorLog {
    param($ErrorRecord)

    $context = $ErrorRecord | Get-AIErrorContext

    $logEntry = @{
        Timestamp = Get-Date
        Severity = $context.Severity
        Message = $context.SimplifiedMessage
        RootCause = $context.RootCause
        Fixes = $context.SuggestedFixes
        Location = "$($context.File):$($context.Line)"
    }

    $logEntry | ConvertTo-Json | Add-Content error.log
}
```

---

## 💡 Key Features

1. **Pattern Matching** - Recognizes 11+ common error patterns
2. **Severity Classification** - Critical, Error, Warning
3. **Root Cause Analysis** - Explains why the error occurred
4. **Actionable Suggestions** - Specific steps to fix the problem
5. **Documentation Links** - Direct links to relevant docs
6. **Location Extraction** - File, line, column information
7. **Simplified Messages** - Clean, concise error descriptions
8. **Structured Output** - Perfect for AI consumption

---

## 🎓 Best Practices

### 1. Analyze Errors Immediately
```powershell
# Don't let errors pile up
try {
    Risky-Operation
}
catch {
    $_ | Get-AIErrorContext | Format-ForAI | Out-File "error-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    throw
}
```

### 2. Use with DevCommand
```powershell
# Combine async execution with error analysis
$job = Start-DevCommand cargo build
Wait-DevCommand -Job $job
if (!(Get-DevCommandStatus -Job $job).Success) {
    Get-AIErrorContext -Last 10
}
```

### 3. Batch Analysis
```powershell
# Analyze multiple errors at once
Get-AIErrorContext -Last 20 |
    Where-Object {$_.Severity -eq "Critical"} |
    Format-ForAI
```

---

## 📝 Implementation Notes

**Files:**
- `src/Microsoft.PowerShell.Development/AIContext/AIErrorContext.cs` (500+ lines)
  - AIErrorContext model
  - ErrorPattern model
  - ErrorAnalyzer engine with 11 built-in patterns
- `src/Microsoft.PowerShell.Development/AIContext/GetAIErrorContextCommand.cs`
  - Get-AIErrorContext cmdlet

**Error Patterns Included:**
- CommandNotFoundException
- ParameterBindingException
- FileNotFoundException
- UnauthorizedAccessException
- Compilation errors
- Package not found
- Type mismatch
- Timeout
- Out of memory
- Null reference
- Module not found

---

*Part of the Microsoft.PowerShell.Development module*
