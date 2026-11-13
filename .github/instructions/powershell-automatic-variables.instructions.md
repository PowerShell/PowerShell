---
applyTo:
  - "**/*.ps1"
  - "**/*.psm1"
---

# PowerShell Automatic Variables - Naming Guidelines

## Purpose

This instruction provides guidelines for avoiding conflicts with PowerShell's automatic variables when writing PowerShell scripts and modules.

## What Are Automatic Variables?

PowerShell has built-in automatic variables that are created and maintained by PowerShell itself. Assigning values to these variables can cause unexpected behavior and side effects.

## Common Automatic Variables to Avoid

### Critical Variables (Never Use)

- **`$matches`** - Contains the results of regular expression matches. Overwriting this can break regex operations.
- **`$_`** - Represents the current object in the pipeline. Only use within pipeline blocks.
- **`$PSItem`** - Alias for `$_`. Same rules apply.
- **`$args`** - Contains an array of undeclared parameters. Don't use as a regular variable.
- **`$input`** - Contains an enumerator of all input passed to a function. Don't reassign.
- **`$LastExitCode`** - Exit code of the last native command. Don't overwrite unless intentional.
- **`$?`** - Success status of the last command. Don't use as a variable name.
- **`$$`** - Last token in the last line received by the session. Don't use.
- **`$^`** - First token in the last line received by the session. Don't use.

### Context Variables (Use with Caution)

- **`$Error`** - Array of error objects. Don't replace, but can modify (e.g., `$Error.Clear()`).
- **`$PSBoundParameters`** - Parameters passed to the current function. Read-only.
- **`$MyInvocation`** - Information about the current command. Read-only.
- **`$PSCmdlet`** - Cmdlet object for advanced functions. Read-only.

### Other Common Automatic Variables

- `$true`, `$false`, `$null` - Boolean and null constants
- `$HOME`, `$PSHome`, `$PWD` - Path-related variables
- `$PID` - Process ID of the current PowerShell session
- `$Host` - Host application object
- `$PSVersionTable` - PowerShell version information

For a complete list, see: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_automatic_variables

## Best Practices

### ❌ Bad - Using Automatic Variable Names

```powershell
# Bad: $matches is an automatic variable used for regex capture groups
$matches = Select-String -Path $file -Pattern $pattern

# Bad: $args is an automatic variable for undeclared parameters
$args = Get-ChildItem

# Bad: $input is an automatic variable for pipeline input
$input = Read-Host "Enter value"
```

### ✅ Good - Using Descriptive Alternative Names

```powershell
# Good: Use descriptive names that avoid conflicts
$matchedLines = Select-String -Path $file -Pattern $pattern

# Good: Use specific names for arguments
$arguments = Get-ChildItem

# Good: Use specific names for user input
$userInput = Read-Host "Enter value"
```

## Naming Alternatives

When you encounter a situation where you might use an automatic variable name, use these alternatives:

| Avoid | Use Instead |
|-------|-------------|
| `$matches` | `$matchedLines`, `$matchResults`, `$regexMatches` |
| `$args` | `$arguments`, `$parameters`, `$commandArgs` |
| `$input` | `$userInput`, `$inputValue`, `$inputData` |
| `$_` (outside pipeline) | Use a named parameter or explicit variable |
| `$Error` (reassignment) | Don't reassign; use `$Error.Clear()` if needed |

## How to Check

### PSScriptAnalyzer Rule

PSScriptAnalyzer has a built-in rule that detects assignments to automatic variables:

```powershell
# This will trigger PSAvoidAssignmentToAutomaticVariable
$matches = Get-Something
```

**Rule ID**: PSAvoidAssignmentToAutomaticVariable

### Manual Review

When writing PowerShell code, always:
1. Avoid variable names that match PowerShell keywords or automatic variables
2. Use descriptive, specific names that clearly indicate the variable's purpose
3. Run PSScriptAnalyzer on your code before committing
4. Review code for variable naming during PR reviews

## Examples from the Codebase

### Example 1: Regex Matching

```powershell
# ❌ Bad - Overwrites automatic $matches variable
$matches = [regex]::Matches($content, $pattern)

# ✅ Good - Uses descriptive name
$regexMatches = [regex]::Matches($content, $pattern)
```

### Example 2: Select-String Results

```powershell
# ❌ Bad - Conflicts with automatic $matches
$matches = Select-String -Path $file -Pattern $pattern

# ✅ Good - Clear and specific
$matchedLines = Select-String -Path $file -Pattern $pattern
```

### Example 3: Collecting Arguments

```powershell
# ❌ Bad - Conflicts with automatic $args
function Process-Items {
    $args = $MyItems
    # ... process items
}

# ✅ Good - Descriptive parameter name
function Process-Items {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]]$Items
    )
    # ... process items
}
```

## References

- [PowerShell Automatic Variables Documentation](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_automatic_variables)
- [PSScriptAnalyzer Rules](https://github.com/PowerShell/PSScriptAnalyzer/blob/master/docs/Rules/README.md)
- [PowerShell Best Practices](https://learn.microsoft.com/powershell/scripting/developer/cmdlet/strongly-encouraged-development-guidelines)

## Summary

**Key Takeaway**: Always use descriptive, specific variable names that clearly indicate their purpose and avoid conflicts with PowerShell's automatic variables. When in doubt, choose a longer, more descriptive name over a short one that might conflict.
