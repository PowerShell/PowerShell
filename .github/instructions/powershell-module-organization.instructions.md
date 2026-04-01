---
applyTo:
  - "tools/ci.psm1"
  - "build.psm1"
  - "tools/packaging/**/*.psm1"
  - ".github/**/*.yml"
  - ".github/**/*.yaml"
---

# Guidelines for PowerShell Code Organization

## When to Move Code from YAML to PowerShell Modules

PowerShell code in GitHub Actions YAML files should be kept minimal. Move code to a module when:

### Size Threshold
- **More than ~30 lines** of PowerShell in a YAML file step
- **Any use of .NET types** like `[regex]`, `[System.IO.Path]`, etc.
- **Complex logic** requiring multiple nested loops or conditionals
- **Reusable functionality** that might be needed elsewhere

### Indicators to Move Code
1. Using .NET type accelerators (`[regex]`, `[PSCustomObject]`, etc.)
2. Complex string manipulation or parsing
3. File system operations beyond basic reads/writes
4. Logic that would benefit from unit testing
5. Code that's difficult to read/maintain in YAML format

## Which Module to Use

### ci.psm1 (`tools/ci.psm1`)
**Purpose**: CI/CD-specific operations and workflows

**Use for**:
- Build orchestration (invoking builds, tests, packaging)
- CI environment setup and configuration
- Test execution and result processing
- Artifact handling and publishing
- CI-specific validations and checks
- Environment variable management for CI

**Examples**:
- `Invoke-CIBuild` - Orchestrates build process
- `Invoke-CITest` - Runs Pester tests
- `Test-MergeConflictMarker` - Validates files for conflicts
- `Set-BuildVariable` - Manages CI variables

**When NOT to use**:
- Core build operations (use build.psm1)
- Package creation logic (use packaging.psm1)
- Platform-specific build steps

### build.psm1 (`build.psm1`)
**Purpose**: Core build operations and utilities

**Use for**:
- Compiling source code
- Resource generation
- Build configuration management
- Core build utilities (New-PSOptions, Get-PSOutput, etc.)
- Bootstrap operations
- Cross-platform build helpers

**Examples**:
- `Start-PSBuild` - Main build function
- `Start-PSBootstrap` - Bootstrap dependencies
- `New-PSOptions` - Create build configuration
- `Start-ResGen` - Generate resources

**When NOT to use**:
- CI workflow orchestration (use ci.psm1)
- Package creation (use packaging.psm1)
- Test execution

### packaging.psm1 (`tools/packaging/packaging.psm1`)
**Purpose**: Package creation and distribution

**Use for**:
- Creating distribution packages (MSI, RPM, DEB, etc.)
- Package-specific metadata generation
- Package signing operations
- Platform-specific packaging logic

**Examples**:
- `Start-PSPackage` - Create packages
- `New-MSIPackage` - Create Windows MSI
- `New-DotnetSdkContainerFxdPackage` - Create container packages

**When NOT to use**:
- Building binaries (use build.psm1)
- Running tests (use ci.psm1)
- General utilities

## Best Practices

### Keep YAML Minimal
```yaml
# ❌ Bad - too much logic in YAML
- name: Check files
  shell: pwsh
  run: |
    $files = Get-ChildItem -Recurse
    foreach ($file in $files) {
      $content = Get-Content $file -Raw
      if ($content -match $pattern) {
        # ... complex processing ...
      }
    }

# ✅ Good - call function from module
- name: Check files
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Test-SomeCondition -Path ${{ github.workspace }}
```

### Document Functions
Always include comment-based help for functions:
```powershell
function Test-MyFunction
{
    <#
    .SYNOPSIS
        Brief description
    .DESCRIPTION
        Detailed description
    .PARAMETER ParameterName
        Parameter description
    .EXAMPLE
        Test-MyFunction -ParameterName Value
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $ParameterName
    )
    # Implementation
}
```

### Error Handling
Use proper error handling in modules:
```powershell
try {
    # Operation
}
catch {
    Write-Error "Detailed error message: $_"
    throw
}
```

### Verbose Output
Use `Write-Verbose` for debugging information:
```powershell
Write-Verbose "Processing file: $filePath"
```

## Module Dependencies

- **ci.psm1** imports both `build.psm1` and `packaging.psm1`
- **build.psm1** is standalone (minimal dependencies)
- **packaging.psm1** imports `build.psm1`

When adding new functions, consider these import relationships to avoid circular dependencies.

## Testing Modules

Functions in modules should be testable:
```powershell
# Test locally
Import-Module ./tools/ci.psm1 -Force
Test-MyFunction -Parameter Value

# Can be unit tested with Pester
Describe "Test-MyFunction" {
    It "Should return expected result" {
        # Test implementation
    }
}
```

## Migration Checklist

When moving code from YAML to a module:

1. ✅ Determine which module is appropriate (ci, build, or packaging)
2. ✅ Create function with proper parameter validation
3. ✅ Add comment-based help documentation
4. ✅ Use `[CmdletBinding()]` for advanced function features
5. ✅ Include error handling
6. ✅ Add verbose output for debugging
7. ✅ Test the function independently
8. ✅ Update YAML to call the new function
9. ✅ Verify the workflow still works end-to-end

## References

- PowerShell Advanced Functions: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_functions_advanced
- Comment-Based Help: https://learn.microsoft.com/powershell/scripting/developer/help/writing-help-for-windows-powershell-scripts-and-functions
