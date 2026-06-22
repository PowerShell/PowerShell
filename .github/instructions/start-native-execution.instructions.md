---
applyTo:
  - "**/*.ps1"
  - "**/*.psm1"
---

# Using Start-NativeExecution for Native Command Execution

## Purpose

`Start-NativeExecution` is the standard function for executing native commands (external executables) in PowerShell scripts within this repository. It provides consistent error handling and better diagnostics when native commands fail.

## When to Use

Use `Start-NativeExecution` whenever you need to:
- Execute external commands (e.g., `git`, `dotnet`, `pkgbuild`, `productbuild`, `fpm`, `rpmbuild`)
- Ensure proper exit code checking
- Get better error messages with caller information
- Handle verbose output on error

## Basic Usage

```powershell
Start-NativeExecution {
    git clone https://github.com/PowerShell/PowerShell.git
}
```

## With Parameters

Use backticks for line continuation within the script block:

```powershell
Start-NativeExecution {
    pkgbuild --root $pkgRoot `
        --identifier $pkgIdentifier `
        --version $Version `
        --scripts $scriptsDir `
        $outputPath
}
```

## Common Parameters

### -VerboseOutputOnError

Captures command output and displays it only if the command fails:

```powershell
Start-NativeExecution -VerboseOutputOnError {
    dotnet build --configuration Release
}
```

### -IgnoreExitcode

Allows the command to fail without throwing an exception:

```powershell
Start-NativeExecution -IgnoreExitcode {
    git diff --exit-code  # Returns 1 if differences exist
}
```

## Availability

The function is defined in `tools/buildCommon/startNativeExecution.ps1` and is available in:
- `build.psm1` (dot-sourced automatically)
- `tools/packaging/packaging.psm1` (dot-sourced automatically)
- Test modules that include `HelpersCommon.psm1`

To use in other scripts, dot-source the function:

```powershell
. "$PSScriptRoot/../buildCommon/startNativeExecution.ps1"
```

## Error Handling

When a native command fails (non-zero exit code), `Start-NativeExecution`:
1. Captures the exit code
2. Identifies the calling location (file and line number)
3. Throws a descriptive error with full context

Example error message:
```
Execution of {git clone ...} by /path/to/script.ps1: line 42 failed with exit code 1
```

## Examples from the Codebase

### Git Operations
```powershell
Start-NativeExecution {
    git fetch --tags --quiet upstream
}
```

### Build Operations
```powershell
Start-NativeExecution -VerboseOutputOnError {
    dotnet publish --configuration Release
}
```

### Packaging Operations
```powershell
Start-NativeExecution -VerboseOutputOnError {
    pkgbuild --root $pkgRoot --identifier $pkgId --version $version $outputPath
}
```

### Permission Changes
```powershell
Start-NativeExecution {
    find $staging -type d | xargs chmod 755
    find $staging -type f | xargs chmod 644
}
```

## Anti-Patterns

**Don't do this:**
```powershell
& somecommand $args
if ($LASTEXITCODE -ne 0) {
    throw "Command failed"
}
```

**Do this instead:**
```powershell
Start-NativeExecution {
    somecommand $args
}
```

## Best Practices

1. **Always use Start-NativeExecution** for native commands to ensure consistent error handling
2. **Use -VerboseOutputOnError** for commands with useful diagnostic output
3. **Use backticks for readability** when commands have multiple arguments
4. **Don't capture output unnecessarily** - let the function handle it
5. **Use -IgnoreExitcode sparingly** - only when non-zero exit codes are expected and acceptable

## Related Documentation

- Source: `tools/buildCommon/startNativeExecution.ps1`
- Blog post: https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
