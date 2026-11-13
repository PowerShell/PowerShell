---
applyTo:
  - "**/*.Tests.ps1"
---

# Pester Set-ItResult Pattern for Pending and Skipped Tests

## Purpose

This instruction explains when and how to use `Set-ItResult` in Pester tests to mark tests as Pending or Skipped dynamically within test execution.

## When to Use Set-ItResult

Use `Set-ItResult` when you need to conditionally mark a test as Pending or Skipped based on runtime conditions that can't be determined at test definition time.

### Pending vs Skipped

**Pending**: Use for tests that should be enabled but temporarily can't run due to:
- Intermittent external service failures (network, APIs)
- Known bugs being fixed
- Missing features being implemented
- Environmental issues that are being resolved

**Skipped**: Use for tests that aren't applicable to the current environment:
- Platform-specific tests running on wrong platform
- Tests requiring specific hardware/configuration not present
- Tests requiring elevated permissions when not available
- Feature-specific tests when feature is disabled

## Pattern

### Basic Usage

```powershell
It "Test description" {
    if ($shouldBePending) {
        Set-ItResult -Pending -Because "Explanation of why test is pending"
        return
    }
    
    if ($shouldBeSkipped) {
        Set-ItResult -Skipped -Because "Explanation of why test is skipped"
        return
    }
    
    # Test code here
}
```

### Important: Always Return After Set-ItResult

After calling `Set-ItResult`, you **must** return from the test to prevent further execution:

```powershell
It "Test that checks environment" {
    if ($env:SKIP_TESTS -eq 'true') {
        Set-ItResult -Skipped -Because "SKIP_TESTS environment variable is set"
        return  # This is required!
    }
    
    # Test assertions
    $result | Should -Be $expected
}
```

**Why?** Without `return`, the test continues executing and may fail with errors unrelated to the pending/skipped condition.

## Examples from the Codebase

### Example 1: Pending for Intermittent Network Issues

```powershell
It "Validate Update-Help for module" {
    if ($markAsPending) {
        Set-ItResult -Pending -Because "Update-Help from the web has intermittent connectivity issues. See issues #2807 and #6541."
        return
    }
    
    Update-Help -Module $moduleName -Force
    # validation code...
}
```

### Example 2: Skipped for Missing Environment

```powershell
It "Test requires CI environment" {
    if (-not $env:CI) {
        Set-ItResult -Skipped -Because "Test requires CI environment to safely install Pester"
        return
    }
    
    Install-CIPester -ErrorAction Stop
}
```

### Example 3: Pending for Platform-Specific Issue

```powershell
It "Clear-Host works correctly" {
    if ($IsARM64) {
        Set-ItResult -Pending -Because "ARM64 runs in non-interactively mode and Clear-Host does not work."
        return
    }
    
    & { Clear-Host; 'hi' } | Should -BeExactly 'hi'
}
```

### Example 4: Skipped for Missing Feature

```powershell
It "Test ACR authentication" {
    if ($env:ACRTESTS -ne 'true') {
        Set-ItResult -Skipped -Because "The tests require the ACRTESTS environment variable to be set to 'true' for ACR authentication."
        return
    }
    
    $psgetModuleInfo = Find-PSResource -Name $ACRTestModule -Repository $ACRRepositoryName
    # test assertions...
}
```

## Alternative: Static -Skip and -Pending Parameters

For conditions that can be determined at test definition time, use the static parameters instead:

```powershell
# Static skip - condition known at definition time
It "Windows-only test" -Skip:(-not $IsWindows) {
    # test code
}

# Static pending - always pending
It "Test for feature being implemented" -Pending {
    # test code that will fail until feature is done
}
```

**Use Set-ItResult when**:
- Condition depends on runtime state
- Condition is determined inside a helper function
- Need to check multiple conditions sequentially

**Use static parameters when**:
- Condition is known at test definition
- Condition doesn't change during test run
- Want Pester to show the condition in test discovery

## Best Practices

1. **Always include -Because parameter** with a clear explanation
2. **Always return after Set-ItResult** to prevent further execution
3. **Reference issues or documentation** when relevant (e.g., "See issue #1234")
4. **Be specific in the reason** - explain what's wrong and what's needed
5. **Use Pending sparingly** - it indicates a problem that should be fixed
6. **Prefer Skipped over Pending** when test truly isn't applicable

## Common Mistakes

### ❌ Mistake 1: Forgetting to Return

```powershell
It "Test" {
    if ($condition) {
        Set-ItResult -Pending -Because "Reason"
        # Missing return - test code will still execute!
    }
    $value | Should -Be $expected  # This runs and fails
}
```

### ❌ Mistake 2: Vague Reason

```powershell
Set-ItResult -Pending -Because "Doesn't work"  # Too vague
```

### ✅ Correct:

```powershell
It "Test" {
    if ($condition) {
        Set-ItResult -Pending -Because "Update-Help has intermittent network timeouts. See issue #2807."
        return
    }
    $value | Should -Be $expected
}
```

## See Also

- [Pester Documentation: Set-ItResult](https://pester.dev/docs/commands/Set-ItResult)
- [Pester Documentation: It](https://pester.dev/docs/commands/It)
- Examples in the codebase:
  - `test/powershell/Host/ConsoleHost.Tests.ps1`
  - `test/infrastructure/ciModule.Tests.ps1`
  - `tools/packaging/releaseTests/sbom.tests.ps1`
