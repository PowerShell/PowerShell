# AI Integration Features - Microsoft.PowerShell.Development

The Microsoft.PowerShell.Development module includes powerful AI integration features designed specifically for working with AI coding assistants like Claude Code. These features make it easy to capture context, gather code, and build structured prompts for AI analysis.

## Table of Contents

1. [Overview](#overview)
2. [Get-TerminalSnapshot](#get-terminalsnapshot)
3. [Get-CodeContext](#get-codecontext)
4. [New-AIPrompt](#new-aiprompt)
5. [Real-World Workflows](#real-world-workflows)
6. [Best Practices](#best-practices)

---

## Overview

### The Three Pillars of AI Integration

| Feature | Purpose | Key Use Case |
|---------|---------|--------------|
| **Get-TerminalSnapshot** | Capture terminal state | "Here's my current environment" |
| **Get-CodeContext** | Gather relevant code files | "Here's the code I'm working on" |
| **New-AIPrompt** | Build structured prompts | "Help me with this specific task" |

### Quick Example

```powershell
# Capture everything and build an error analysis prompt
New-AIPrompt -Template Error `
    -IncludeAll `
    -OutputFile "claude-prompt.md" `
    -ToClipboard

# Now paste into Claude Code and get instant help!
```

---

## Get-TerminalSnapshot

Captures the current state of your terminal environment for AI analysis.

### Synopsis

```powershell
Get-TerminalSnapshot [[-IncludeGit]] [[-IncludeHistory]] [[-IncludeEnvironment]]
                     [[-IncludeProcesses]] [[-IncludeErrors]] [[-IncludeProject]]
                     [[-All]] [[-HistoryCount] <int>]
```

### Description

Captures a comprehensive snapshot of your terminal state including:
- Working directory
- Git repository status (branch, changes, commits)
- Recent command history
- Environment variables
- Running development processes
- Recent PowerShell errors
- Project context
- System information

### Parameters

#### `-IncludeGit`
Include Git repository information (branch, modified files, commits, etc.).

```powershell
Get-TerminalSnapshot -IncludeGit
```

#### `-IncludeHistory`
Include recent command history.

```powershell
Get-TerminalSnapshot -IncludeHistory -HistoryCount 30
```

#### `-IncludeEnvironment`
Include relevant environment variables (PATH, NODE_ENV, DOTNET_ROOT, etc.).

```powershell
Get-TerminalSnapshot -IncludeEnvironment
```

#### `-IncludeProcesses`
Include running development-related processes (dotnet, node, docker, etc.).

```powershell
Get-TerminalSnapshot -IncludeProcesses
```

#### `-IncludeErrors`
Include recent PowerShell errors.

```powershell
Get-TerminalSnapshot -IncludeErrors
```

#### `-IncludeProject`
Include project context (type, language, build tool).

```powershell
Get-TerminalSnapshot -IncludeProject
```

#### `-All`
Include all available information (shortcut for all switches).

```powershell
Get-TerminalSnapshot -All
```

#### `-HistoryCount <int>`
Number of recent commands to include (1-100, default: 20).

```powershell
Get-TerminalSnapshot -IncludeHistory -HistoryCount 50
```

### Aliases

- `gts`
- `snapshot`

### Examples

#### Example 1: Quick snapshot with git info

```powershell
gts -IncludeGit
```

Output includes:
- Current branch
- Modified files
- Untracked files
- Commits ahead/behind remote
- Last commit message

#### Example 2: Full environment snapshot

```powershell
snapshot -All
```

Captures everything for complete context.

#### Example 3: Recent activity snapshot

```powershell
Get-TerminalSnapshot -IncludeHistory -IncludeErrors -HistoryCount 30
```

Shows what you've been doing and any errors encountered.

#### Example 4: Share with Claude Code

```powershell
# Capture snapshot and format for AI
Get-TerminalSnapshot -All | Format-ForAI | Out-File "my-environment.json"

# Upload to Claude Code for analysis
```

### Output Structure

```powershell
$snapshot = Get-TerminalSnapshot -All

$snapshot.Timestamp          # When snapshot was taken
$snapshot.WorkingDirectory   # Current directory
$snapshot.Git                # Git information
$snapshot.RecentCommands     # Command history
$snapshot.RelevantEnvironment # Environment variables
$snapshot.RelevantProcesses  # Running processes
$snapshot.RecentErrors       # Recent errors
$snapshot.Project            # Project context
$snapshot.System             # System information
```

#### Git Information

```powershell
$snapshot.Git.IsRepository      # true/false
$snapshot.Git.CurrentBranch     # "main"
$snapshot.Git.ModifiedFiles     # ["file1.cs", "file2.cs"]
$snapshot.Git.UntrackedFiles    # ["newfile.txt"]
$snapshot.Git.AheadBy           # 2 commits
$snapshot.Git.BehindBy          # 0 commits
$snapshot.Git.RemoteUrl         # "https://github.com/user/repo"
$snapshot.Git.LastCommit        # "abc1234 Fix bug in parser"
```

#### Command History

```powershell
$snapshot.RecentCommands[0].Id              # 123
$snapshot.RecentCommands[0].CommandLine     # "dotnet build"
$snapshot.RecentCommands[0].ExecutionStatus # "Completed"
$snapshot.RecentCommands[0].StartTime       # DateTime
$snapshot.RecentCommands[0].Duration        # TimeSpan
```

---

## Get-CodeContext

Intelligently gathers relevant code files for AI analysis.

### Synopsis

```powershell
Get-CodeContext [[-Path] <string>] [[-RecentlyModified]] [[-Hours] <int>]
                [[-Include] <string[]>] [[-Exclude] <string[]>]
                [[-MaxFileSizeKB] <int>] [[-MaxFiles] <int>]
                [[-IncludeContent]] [[-IncludeMetrics]] [[-IncludeDependencies]]
                [[-Files] <string[]>]
```

### Description

Gathers code files with intelligent filtering:
- Auto-detect code files (ignores node_modules, bin, obj, etc.)
- Filter by modification time
- Include/exclude by pattern
- Extract dependencies
- Calculate code metrics
- Respect file size limits

### Parameters

#### `-Path <string>`
Root path to search (default: current directory).

```powershell
Get-CodeContext -Path "./src"
```

#### `-RecentlyModified`
Only include files modified recently.

```powershell
Get-CodeContext -RecentlyModified -Hours 48
```

#### `-Hours <int>`
Number of hours for recently modified (1-720, default: 24).

#### `-Include <string[]>`
File patterns to include (supports wildcards).

```powershell
Get-CodeContext -Include "*.cs", "*.csproj"
```

#### `-Exclude <string[]>`
Additional patterns to exclude.

```powershell
Get-CodeContext -Exclude "*Test.cs", "*Mock.cs"
```

#### `-MaxFileSizeKB <int>`
Maximum file size in KB (1-10240, default: 500).

```powershell
Get-CodeContext -MaxFileSizeKB 1000
```

#### `-MaxFiles <int>`
Maximum number of files (1-1000, default: 50).

```powershell
Get-CodeContext -MaxFiles 100
```

#### `-IncludeContent`
Include file contents (required for AI analysis).

```powershell
Get-CodeContext -IncludeContent
```

#### `-IncludeMetrics`
Calculate code metrics (lines, comments, complexity indicators).

```powershell
Get-CodeContext -IncludeContent -IncludeMetrics
```

#### `-IncludeDependencies`
Extract import/using statements.

```powershell
Get-CodeContext -IncludeContent -IncludeDependencies
```

#### `-Files <string[]>`
Specific files to include (from pipeline).

```powershell
Get-CodeContext -Files "Program.cs", "Startup.cs" -IncludeContent
```

### Aliases

- `gcc`
- `context`

### Examples

#### Example 1: Recent changes

```powershell
# Get code changed in last 24 hours
gcc -RecentlyModified -IncludeContent
```

#### Example 2: Specific file types

```powershell
# Only C# files
Get-CodeContext -Include "*.cs" -IncludeContent -IncludeMetrics
```

#### Example 3: Full analysis

```powershell
# Get complete code context
Get-CodeContext -RecentlyModified `
    -IncludeContent `
    -IncludeMetrics `
    -IncludeDependencies `
    -MaxFiles 100
```

#### Example 4: Specific files for review

```powershell
# Review specific files
"UserService.cs", "UserController.cs", "UserRepository.cs" |
    Get-CodeContext -IncludeContent -IncludeMetrics
```

#### Example 5: Share with Claude Code

```powershell
# Gather context and format for AI
Get-CodeContext -RecentlyModified -IncludeContent |
    Format-ForAI |
    Out-File "code-context.json"
```

### Output Structure

```powershell
$context = Get-CodeContext -IncludeContent -IncludeMetrics

$context.RootPath        # "/home/user/project"
$context.Files           # Array of CodeFile objects
$context.Metadata        # Counts, statistics
$context.TotalLines      # Total lines across all files
$context.TotalSize       # Total size in bytes
$context.GatheredAt      # When context was gathered
```

#### File Information

```powershell
$file = $context.Files[0]

$file.Path              # "/home/user/project/src/Program.cs"
$file.RelativePath      # "src/Program.cs"
$file.Language          # "C#"
$file.Content           # Full file content
$file.LineCount         # 150
$file.Size              # 4523 bytes
$file.LastModified      # DateTime
$file.Dependencies      # ["System", "System.IO", "MyApp.Core"]
$file.Metrics           # Code metrics
```

#### Code Metrics

```powershell
$file.Metrics.EmptyLines         # 20
$file.Metrics.CommentLines       # 15
$file.Metrics.CodeLines          # 115
$file.Metrics.AverageLineLength  # 35
$file.Metrics.MaxLineLength      # 120
```

### Supported Languages

Automatically detects and labels:
- C#, F#, Visual Basic (.NET)
- JavaScript, TypeScript (including JSX/TSX)
- Python
- Java, Kotlin, Scala
- C, C++
- Rust
- Go
- Ruby
- PHP
- Swift
- SQL
- PowerShell
- Shell scripts
- YAML, JSON, XML
- Markdown

### Default Exclusions

Automatically excludes common non-code directories:
- `node_modules`
- `bin`, `obj`, `target`, `dist`, `build`
- `.git`, `.svn`, `.hg`
- `packages`, `vendor`
- `__pycache__`, `.pytest_cache`
- `.vs`, `.vscode`, `.idea`

---

## New-AIPrompt

Builds structured prompts for AI assistants with automatic context gathering.

### Synopsis

```powershell
New-AIPrompt [-Template] <string> [[-CustomPrompt] <string>]
             [[-IncludeSnapshot]] [[-IncludeCode]] [[-IncludeErrors]]
             [[-IncludeProject]] [[-IncludeAll]] [[-Files] <string[]>]
             [[-AdditionalContext] <hashtable>] [[-OutputFormat] <string>]
             [[-OutputFile] <string>] [[-ToClipboard]]
```

### Description

Builds structured prompts for AI assistants by:
- Using pre-defined templates for common tasks
- Automatically gathering relevant context
- Formatting for readability
- Supporting multiple output formats
- Copying to clipboard for easy sharing

### Parameters

#### `-Template <string>`
Template type for the prompt (required).

**Valid values:**
- `Error` - Error analysis and debugging
- `CodeReview` - Code review request
- `Debug` - Debugging assistance
- `Explain` - Code explanation
- `Refactor` - Refactoring suggestions
- `Test` - Test generation
- `Deploy` - Deployment assistance
- `Custom` - Custom prompt (use with `-CustomPrompt`)

```powershell
New-AIPrompt -Template Error
```

#### `-CustomPrompt <string>`
Custom prompt text (for Custom template).

```powershell
New-AIPrompt -Template Custom -CustomPrompt "Help me optimize this database query"
```

#### `-IncludeSnapshot`
Include terminal snapshot.

```powershell
New-AIPrompt -Template Debug -IncludeSnapshot
```

#### `-IncludeCode`
Include code context.

```powershell
New-AIPrompt -Template CodeReview -IncludeCode
```

#### `-IncludeErrors`
Include recent errors.

```powershell
New-AIPrompt -Template Error -IncludeErrors
```

#### `-IncludeProject`
Include project context.

```powershell
New-AIPrompt -Template Deploy -IncludeProject
```

#### `-IncludeAll`
Include all context (shortcut).

```powershell
New-AIPrompt -Template Debug -IncludeAll
```

#### `-Files <string[]>`
Specific files to include.

```powershell
New-AIPrompt -Template CodeReview -Files "UserService.cs", "UserController.cs"
```

#### `-AdditionalContext <hashtable>`
Additional context to include.

```powershell
New-AIPrompt -Template Custom `
    -CustomPrompt "Optimize this" `
    -AdditionalContext @{
        "Performance Target" = "< 100ms"
        "Current Latency" = "250ms"
    }
```

#### `-OutputFormat <string>`
Output format (default: Markdown).

**Valid values:**
- `Text` - Plain text
- `Markdown` - Markdown formatted
- `Json` - JSON format

```powershell
New-AIPrompt -Template Error -OutputFormat Markdown
```

#### `-OutputFile <string>`
Save to file.

```powershell
New-AIPrompt -Template Error -IncludeAll -OutputFile "prompt.md"
```

#### `-ToClipboard`
Copy to clipboard.

```powershell
New-AIPrompt -Template Error -IncludeAll -ToClipboard
```

### Aliases

- `prompt`
- `aiprompt`

### Examples

#### Example 1: Quick error analysis

```powershell
# Build error prompt and copy to clipboard
prompt -Template Error -IncludeAll -ToClipboard

# Paste into Claude Code
```

#### Example 2: Code review request

```powershell
# Review specific files
New-AIPrompt -Template CodeReview `
    -Files "UserService.cs", "UserRepository.cs" `
    -IncludeProject `
    -OutputFile "review-request.md"
```

#### Example 3: Debugging assistance

```powershell
# Get help debugging
aiprompt -Template Debug `
    -IncludeSnapshot `
    -IncludeCode `
    -IncludeErrors `
    -ToClipboard
```

#### Example 4: Test generation

```powershell
# Generate tests for specific file
New-AIPrompt -Template Test `
    -Files "UserService.cs" `
    -IncludeProject `
    -OutputFormat Markdown `
    -OutputFile "test-request.md"
```

#### Example 5: Custom prompt with context

```powershell
# Custom request with full context
New-AIPrompt -Template Custom `
    -CustomPrompt "Help me refactor this code to use async/await patterns" `
    -IncludeCode `
    -Files "DataAccess.cs" `
    -AdditionalContext @{
        "Framework" = ".NET 8"
        "Target" = "Improve throughput"
    } `
    -OutputFile "refactor-request.md"
```

### Template Details

#### Error Template

Analyzes errors and suggests fixes.

**Includes:**
- Recent errors with context
- Terminal state
- Related code
- Request for:
  - Root cause analysis
  - Specific fixes
  - Code examples
  - Explanation

#### CodeReview Template

Requests comprehensive code review.

**Reviews for:**
- Code quality and best practices
- Potential bugs
- Performance improvements
- Security vulnerabilities
- Maintainability

#### Debug Template

Requests debugging assistance.

**Provides:**
- Current state
- Error context
- Code context
- Requests:
  - Root cause identification
  - Step-by-step debugging approach
  - Areas to investigate
  - Potential solutions

#### Explain Template

Requests code explanation.

**Asks for:**
- What the code does
- How it works
- Key design decisions
- Potential improvements

#### Refactor Template

Requests refactoring suggestions.

**Goals:**
- Improve readability
- Enhance maintainability
- Follow best practices
- Optimize performance

**Deliverables:**
- Refactored code
- Explanation of changes
- Benefits

#### Test Template

Requests test generation.

**Requirements:**
- Unit tests for public methods
- Edge case coverage
- Negative test cases
- Integration tests

#### Deploy Template

Requests deployment assistance.

**Provides:**
- Deployment strategy recommendations
- Potential issues
- Rollback plan
- Post-deployment verification

#### Custom Template

Fully customizable prompt with context.

### Output Structure

```powershell
$prompt = New-AIPrompt -Template Error -IncludeAll

$prompt.Template          # "Error"
$prompt.RenderedPrompt    # Full formatted prompt text
$prompt.Context           # Dictionary of all context
$prompt.CreatedAt         # DateTime
$prompt.EstimatedTokens   # Rough token estimate
```

---

## Real-World Workflows

### Workflow 1: Error Analysis

```powershell
# Something broke - get help from Claude Code
New-AIPrompt -Template Error `
    -IncludeAll `
    -OutputFile "error-help.md" `
    -ToClipboard

# 1. Prompt is copied to clipboard
# 2. Saved to error-help.md for reference
# 3. Paste into Claude Code
# 4. Get instant analysis and fixes
```

### Workflow 2: Pre-Commit Code Review

```powershell
# Get files changed in git
$changedFiles = git diff --name-only HEAD

# Review them with AI
New-AIPrompt -Template CodeReview `
    -Files $changedFiles `
    -IncludeProject `
    -OutputFile "pre-commit-review.md"

# Review AI suggestions before committing
```

### Workflow 3: Daily Standup Context

```powershell
# Generate daily standup context
$snapshot = Get-TerminalSnapshot -IncludeGit -IncludeHistory

$standupContext = @{
    "Yesterday" = $snapshot.RecentCommands |
        Where-Object { $_.StartTime.Date -eq (Get-Date).AddDays(-1).Date }
    "Today_Plan" = "Continue work on authentication module"
    "Blockers" = $snapshot.RecentErrors
}

$standupContext | Format-ForAI | Out-File "standup-$(Get-Date -Format 'yyyy-MM-dd').json"
```

### Workflow 4: Documentation Generation

```powershell
# Generate documentation for recent changes
$recentCode = Get-CodeContext -RecentlyModified -Hours 48 -IncludeContent

New-AIPrompt -Template Custom `
    -CustomPrompt "Generate API documentation for these recent changes" `
    -AdditionalContext @{
        Code = $recentCode
    } `
    -OutputFile "doc-request.md"
```

### Workflow 5: Performance Investigation

```powershell
# Investigate performance issue
New-AIPrompt -Template Debug `
    -IncludeSnapshot `
    -IncludeProcesses `
    -Files "SlowEndpoint.cs", "DatabaseContext.cs" `
    -AdditionalContext @{
        "Issue" = "API endpoint takes 2 seconds"
        "Expected" = "< 200ms"
        "Request Volume" = "1000 req/min"
    } `
    -ToClipboard
```

### Workflow 6: Onboarding New Team Member

```powershell
# Create comprehensive project context for new developer
$codeContext = Get-CodeContext -Include "*.cs" -MaxFiles 100 -IncludeContent -IncludeMetrics
$snapshot = Get-TerminalSnapshot -All

$onboardingContext = @{
    ProjectOverview = $codeContext
    DevelopmentSetup = $snapshot
    GettingStartedGuide = "See README.md"
}

$onboardingContext | Format-ForAI | Out-File "onboarding-package.json"
```

---

## Best Practices

### 1. Start Small, Add Context as Needed

```powershell
# Start with minimal context
New-AIPrompt -Template Error -IncludeErrors

# If AI needs more, add incrementally
New-AIPrompt -Template Error -IncludeErrors -IncludeSnapshot

# Full context for complex issues
New-AIPrompt -Template Error -IncludeAll
```

### 2. Use Specific Files for Focused Reviews

```powershell
# Better than including all files
New-AIPrompt -Template CodeReview `
    -Files "ChangedFile1.cs", "ChangedFile2.cs"

# Instead of
New-AIPrompt -Template CodeReview -IncludeAll
```

### 3. Combine with Git for Context

```powershell
# Review only changed files
$changedFiles = git diff --cached --name-only

New-AIPrompt -Template CodeReview -Files $changedFiles
```

### 4. Save Prompts for Documentation

```powershell
# Always save important prompts
New-AIPrompt -Template Debug `
    -IncludeAll `
    -OutputFile "issues/$(Get-Date -Format 'yyyy-MM-dd')-bug-123.md"

# Build a history of AI interactions
```

### 5. Use Clipboard for Quick Iterations

```powershell
# Quick clipboard workflow
prompt -Template Error -IncludeAll -ToClipboard

# Paste into Claude Code
# Get response
# Iterate quickly
```

### 6. Token Management

```powershell
# Check estimated tokens
$prompt = New-AIPrompt -Template CodeReview -IncludeAll
Write-Host "Estimated tokens: $($prompt.EstimatedTokens)"

# If too large, reduce scope
$prompt = New-AIPrompt -Template CodeReview `
    -Files "SpecificFile.cs" `
    -IncludeProject
Write-Host "Estimated tokens: $($prompt.EstimatedTokens)"
```

### 7. Combine with Workflows

```powershell
# Record workflow of getting AI help
Start-WorkflowRecording -Name "GetAIHelp" -Tags "ai", "helper"

Save-WorkflowStep -Command "Get-TerminalSnapshot -All | Out-File snapshot.json"
Save-WorkflowStep -Command "Get-CodeContext -RecentlyModified -IncludeContent | Out-File code.json"
Save-WorkflowStep -Command "New-AIPrompt -Template Error -IncludeAll -OutputFile prompt.md"

Stop-WorkflowRecording -Save

# Replay anytime
Invoke-Workflow -Name "GetAIHelp"
```

### 8. Environment-Specific Context

```powershell
# Development context
New-AIPrompt -Template Debug `
    -AdditionalContext @{Environment="Development"}

# Production context
New-AIPrompt -Template Debug `
    -AdditionalContext @{Environment="Production"; AlertLevel="Critical"}
```

---

## Tips and Tricks

### Keyboard Shortcuts with Aliases

```powershell
# Quick snapshot
gts -All | fai

# Quick code context
gcc -RecentlyModified | fai

# Quick error prompt
prompt -Template Error -IncludeAll -ToClipboard
```

### Chain with Format-ForAI

```powershell
# Everything as JSON for AI
Get-TerminalSnapshot -All | Format-ForAI -OutputType Json
Get-CodeContext -RecentlyModified | Format-ForAI -OutputType Yaml
```

### Automated Daily Reports

```powershell
# Add to your profile
function Get-DailyDevReport {
    $date = Get-Date -Format "yyyy-MM-dd"

    Get-TerminalSnapshot -IncludeGit -IncludeHistory |
        Format-ForAI |
        Out-File "daily-reports/report-$date.json"

    Get-CodeContext -RecentlyModified -Hours 24 -IncludeMetrics |
        Format-ForAI |
        Out-File "daily-reports/code-$date.json"
}

# Run daily
Get-DailyDevReport
```

### Integration with CI/CD

```powershell
# In build script - generate context on failure
if ($buildFailed) {
    New-AIPrompt -Template Error `
        -IncludeErrors `
        -OutputFile "build-failure-context.md"

    # Send to incident management system
}
```

---

## Summary

The AI Integration features transform PowerShell into a powerful AI assistant companion:

| Feature | What It Does | When to Use |
|---------|-------------|-------------|
| **Get-TerminalSnapshot** | Captures current state | Sharing environment with AI |
| **Get-CodeContext** | Gathers relevant code | Code review, explanation |
| **New-AIPrompt** | Builds structured prompts | Any AI interaction |

**The Power Combo:**
```powershell
New-AIPrompt -Template Error -IncludeAll -ToClipboard
```

One command gives Claude Code everything it needs to help you!

---

*Part of the Microsoft.PowerShell.Development module - Making AI-assisted development seamless*
