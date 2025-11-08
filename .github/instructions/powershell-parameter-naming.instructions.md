---
applyTo: '**/*.ps1, **/*.psm1'
description: Naming conventions for PowerShell parameters
---

# PowerShell Parameter Naming Conventions

## Purpose

This instruction defines the naming conventions for parameters in PowerShell scripts and modules. Consistent parameter naming improves code readability, maintainability, and usability for users of PowerShell cmdlets and functions.

## Parameter Naming Rules

### General Conventions
- **Singular Nouns**: Use singular nouns for parameter names even if the parameter is expected to handle multiple values (e.g., `File` instead of `Files`).
- **Use PascalCase**: Parameter names must use PascalCase (e.g., `ParameterName`).
- **Descriptive Names**: Parameter names should be descriptive and convey their purpose clearly (e.g., `FilePath`, `UserName`).
- **Avoid Abbreviations**: Avoid using abbreviations unless they are widely recognized (e.g., `ID` for Identifier).
- **Avoid Reserved Words**: Do not use PowerShell reserved words as parameter names (e.g., `if`, `else`, `function`).

### Units and Precision
- **Include Units in Parameter Names**: When a parameter represents a value with units, include the unit in the parameter name for clarity:
  - `TimeoutSec` instead of `Timeout`
  - `RetryIntervalSec` instead of `RetryInterval`
  - `MaxSizeBytes` instead of `MaxSize`
- **Use Full Words for Clarity**: Spell out common terms to match PowerShell conventions:
  - `MaximumRetryCount` instead of `MaxRetries`
  - `MinimumLength` instead of `MinLength`

### Alignment with Built-in Cmdlets
- **Follow Existing PowerShell Conventions**: When your parameter serves a similar purpose to a built-in cmdlet parameter, use the same or similar naming:
  - Match `Invoke-WebRequest` parameters when making HTTP requests: `TimeoutSec`, `MaximumRetryCount`, `RetryIntervalSec`
  - Follow common parameter patterns like `Path`, `Force`, `Recurse`, `WhatIf`, `Confirm`
- **Consistency Within Scripts**: If multiple parameters relate to the same concept, use consistent naming patterns (e.g., `TimeoutSec`, `RetryIntervalSec` both use `Sec` suffix).

## Examples

### Good Parameter Names
```powershell
param(
    [string[]]$File,                    # Singular, even though it accepts arrays
    [int]$TimeoutSec = 30,              # Unit included
    [int]$MaximumRetryCount = 2,        # Full word "Maximum"
    [int]$RetryIntervalSec = 2,         # Consistent with TimeoutSec
    [string]$Path,                      # Standard PowerShell convention
    [switch]$Force                      # Common PowerShell parameter
)
```

### Names to Avoid
```powershell
param(
    [string[]]$Files,                   # Should be singular: File
    [int]$Timeout = 30,                 # Missing unit: TimeoutSec
    [int]$MaxRetries = 2,               # Should be: MaximumRetryCount
    [int]$RetryInterval = 2,            # Missing unit: RetryIntervalSec
    [string]$FileLoc,                   # Avoid abbreviations: FilePath
    [int]$Max                           # Ambiguous: MaximumWhat?
)
```

## Exceptions
- **Common Terms**: Some common terms may be used in plural form if they are widely accepted in the context (e.g., `Credentials`, `Permissions`).
- **Legacy Code**: Existing code that does not follow these conventions may be exempted to avoid breaking changes, but new code should adhere to these guidelines.
- **Well Established Naming Patterns**: If a naming pattern is well established in the PowerShell community, it may be used even if it does not strictly adhere to these guidelines.

## References
- [PowerShell Cmdlet Design Guidelines](https://learn.microsoft.com/powershell/scripting/developer/cmdlet/strongly-encouraged-development-guidelines)
- [About Parameters - PowerShell Documentation](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_parameters)
