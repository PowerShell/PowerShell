---
applyTo:
  - "build.psm1"
  - "tools/ci.psm1"
  - ".github/**/*.yml"
  - ".github/**/*.yaml"
---

# Log Grouping Guidelines for GitHub Actions

## Purpose

Guidelines for using `Write-LogGroupStart` and `Write-LogGroupEnd` to create collapsible log sections in GitHub Actions CI/CD runs.

## Key Principles

### 1. Groups Cannot Be Nested

GitHub Actions does not support nested groups. Only use one level of grouping.

**❌ Don't:**
```powershell
Write-LogGroupStart -Title "Outer Group"
Write-LogGroupStart -Title "Inner Group"
# ... operations ...
Write-LogGroupEnd -Title "Inner Group"
Write-LogGroupEnd -Title "Outer Group"
```

**✅ Do:**
```powershell
Write-LogGroupStart -Title "Operation A"
# ... operations ...
Write-LogGroupEnd -Title "Operation A"

Write-LogGroupStart -Title "Operation B"
# ... operations ...
Write-LogGroupEnd -Title "Operation B"
```

### 2. Groups Should Be Substantial

Only create groups for operations that generate substantial output (5+ lines). Small groups add clutter without benefit.

**❌ Don't:**
```powershell
Write-LogGroupStart -Title "Generate Resource Files"
Write-Log -message "Run ResGen"
Start-ResGen
Write-LogGroupEnd -Title "Generate Resource Files"
```

**✅ Do:**
```powershell
Write-Log -message "Run ResGen (generating C# bindings for resx files)"
Start-ResGen
```

### 3. Groups Should Represent Independent Operations

Each group should be a logically independent operation that users might want to expand/collapse separately.

**✅ Good examples:**
- Install Native Dependencies
- Install .NET SDK
- Build PowerShell
- Restore NuGet Packages

**❌ Bad examples:**
- Individual project restores (too granular)
- Small code generation steps (too small)
- Sub-steps of a larger operation (would require nesting)

### 4. One Group Per Iteration Is Excessive

Avoid putting log groups inside loops where each iteration creates a separate group.  This would probably cause nesting.

**❌ Don't:**
```powershell
$projects | ForEach-Object {
    Write-LogGroupStart -Title "Restore Project: $_"
    dotnet restore $_
    Write-LogGroupEnd -Title "Restore Project: $_"
}
```

**✅ Do:**
```powershell
Write-LogGroupStart -Title "Restore All Projects"
$projects | ForEach-Object {
    Write-Log -message "Restoring $_"
    dotnet restore $_
}
Write-LogGroupEnd -Title "Restore All Projects"
```

## Usage Pattern

```powershell
Write-LogGroupStart -Title "Descriptive Operation Name"
try {
    # ... operation code ...
    Write-Log -message "Status updates"
}
finally {
    # Ensure group is always closed
}
Write-LogGroupEnd -Title "Descriptive Operation Name"
```

## When to Use Log Groups

Use log groups for:
- Major build phases (bootstrap, restore, build, test, package)
- Installation operations (dependencies, SDKs, tools)
- Operations that produce 5+ lines of output
- Operations where users might want to collapse verbose output

Don't use log groups for:
- Single-line operations
- Code that's already inside another group
- Loop iterations with minimal output per iteration
- Diagnostic or debug output that should always be visible

## Examples from build.psm1

### Good Usage

```powershell
function Start-PSBootstrap {
    # Multiple independent operations, each with substantial output
    Write-LogGroupStart -Title "Install Native Dependencies"
    # ... apt-get/yum/brew install commands ...
    Write-LogGroupEnd -Title "Install Native Dependencies"

    Write-LogGroupStart -Title "Install .NET SDK"
    # ... dotnet installation ...
    Write-LogGroupEnd -Title "Install .NET SDK"
}
```

### Avoid

```powershell
# Too small - just 2-3 lines
Write-LogGroupStart -Title "Generate Resource Files (ResGen)"
Write-Log -message "Run ResGen"
Start-ResGen
Write-LogGroupEnd -Title "Generate Resource Files (ResGen)"
```

## GitHub Actions Syntax

These functions emit GitHub Actions workflow commands:
- `Write-LogGroupStart` → `::group::Title`
- `Write-LogGroupEnd` → `::endgroup::`

In the GitHub Actions UI, this renders as collapsible sections with the specified title.

## Testing

Test log grouping locally:
```powershell
$env:GITHUB_ACTIONS = 'true'
Import-Module ./build.psm1
Write-LogGroupStart -Title "Test"
Write-Log -Message "Content"
Write-LogGroupEnd -Title "Test"
```

Output should show:
```
::group::Test
Content
::endgroup::
```

## References

- [GitHub Actions: Grouping log lines](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#grouping-log-lines)
- `build.psm1`: `Write-LogGroupStart` and `Write-LogGroupEnd` function definitions
