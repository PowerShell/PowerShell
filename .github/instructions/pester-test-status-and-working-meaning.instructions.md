---
applyTo: "**/*.Tests.ps1"
---

# Pester Test Status Meanings and Working Tests

## Purpose

This guide clarifies Pester test outcomes and what it means for a test to be "working" - which requires both **passing** AND **actually validating functionality**.

## Test Statuses in Pester

### Passed ✓
**Status Code**: `Passed` 
**Exit Result**: Test ran successfully, all assertions passed

**What it means**:
- Test executed without errors
- All `Should` statements evaluated to true
- Test setup and teardown completed without issues
- Test is **validating** the intended functionality

**What it does NOT mean**:
- The feature is working (assertions could be wrong)
- The test is meaningful (could be testing wrong thing)
- The test exercises all code paths

### Failed ✗
**Status Code**: `Failed`
**Exit Result**: Test ran but assertions failed

**What it means**:
- Test executed but an assertion returned false
- Expected value did not match actual value
- Test detected a problem with the functionality

**Examples**:
```
Expected $true but got $false
Expected 5 items but got 3
Expected no error but got: Cannot find parameter
```

### Error ⚠
**Status Code**: `Error`
**Exit Result**: Test crashed with an exception

**What it means**:
- Test failed to complete
- An exception was thrown during test execution
- Could be in test setup, test body, or test cleanup
- Often indicates environmental issue, not code functional issue

**Examples**:
```
Cannot bind argument to parameter 'Path' because it is null
File not found: C:\expected\config.json
Access denied writing to registry
```

### Pending ⏳
**Status Code**: `Pending`
**Exit Result**: Test ran but never completed assertions

**What it means**:
- Test was explicitly marked as not ready to run
- `Set-ItResult -Pending` was called
- Used to indicate: known bugs, missing features, environmental issues

**When to use Pending**:
- Test for feature in development
- Test disabled due to known bug (issue #1234)
- Test disabled due to intermittent failures being fixed
- Platform-specific issues being resolved

**⚠️ WARNING**: Pending tests are NOT validating functionality. They hide problems.

### Skipped ⊘
**Status Code**: `Skipped`  
**Exit Result**: Test did not run (detected at start)

**What it means**:
- Test was intentionally not executed
- `-Skip` parameter or `It -Skip:$condition` was used
- Environment doesn't support this test

**When to use Skip**:
- Test not applicable to current platform (Windows-only test on Linux)
- Test requires feature that's not available (admin privileges)
- Test requires specific configuration not present

**Difference from Pending**:
- **Skip**: "This test shouldn't run here" (known upfront)
- **Pending**: "This test should eventually run but can't now"

### Ignored ✛
**Status Code**: `Ignored`
**Exit Result**: Test marked as not applicable

**What it means**:
- Test has `[Ignore("reason")]` attribute
- Test is permanently disabled in this location
- Not the same as Skipped (which is conditional)

**When to use Ignore**:
- Test for deprecated feature
- Test for bug that won't be fixed
- Test moved to different test file

---

## What Does "Working" Actually Mean?

A test is **working** when it meets BOTH criteria:

### 1. **Test Status is PASSED** ✓
```powershell
It "Test name" {
    # Test executes
    # All assertions pass
    # Returns Passed status
}
```

### 2. **Test Actually Validates Functionality**
```powershell
# ✓ GOOD: Tests actual functionality
It "Get-Item returns files from directory" -Tags @('Unit') {
    $testDir = New-Item -ItemType Directory -Force
    New-Item -Path $testDir -Name "file.txt" -ItemType File | Out-Null
    
    $result = Get-Item -Path "$testDir\file.txt"
    
    $result.Name | Should -Be "file.txt"
    $result | Should -Exist
    
    Remove-Item $testDir -Recurse -Force
}

# ✗ BAD: Returns Passed but doesn't validate functionality
It "Get-Item returns files from directory" -Tags @('Unit') {
    $result = Get-Item -Path somepath  # May not exist, may not actually test
    $result | Should -Not -BeNullOrEmpty  # Too vague
}

# ✗ BAD: Test marked Pending - validation is hidden
It "Get-Item returns files from directory" -Tags @('Unit') {
    Set-ItResult -Pending -Because "File system not working"
    return
    # No validation happens at all
}
```

---

## The Problem with Pending Tests

### Why Pending Tests Hide Problems

```powershell
# BAD: Test marked Pending - looks like "working" status but validation is skipped
It "Download help from web" {
    Set-ItResult -Pending -Because "Web connectivity issues"
    return
    
    # This code never runs:
    Update-Help -Module PackageManagement -Force -ErrorAction Stop
    Get-Help Get-Package | Should -Not -BeNullOrEmpty
}
```

**Result**:
- ✗ Feature is broken (Update-Help fails)
- ✓ Test shows "Pending" (looks acceptable)
- ✗ Problem is hidden and never fixed

### The Right Approach

**Option A: Fix the root cause**
```powershell
It "Download help from web" {
    # Use local assets that are guaranteed to work
    Update-Help -Module PackageManagement -SourcePath ./assets -Force -ErrorAction Stop
    
    Get-Help Get-Package | Should -Not -BeNullOrEmpty
}
```

**Option B: Gracefully skip when unavailable**
```powershell
It "Download help from web" -Skip:$(-not $hasInternet) {
    Update-Help -Module PackageManagement -Force -ErrorAction Stop
    Get-Help Get-Package | Should -Not -BeNullOrEmpty
}
```

**Option C: Add retry logic for intermittent issues**
```powershell
It "Download help from web" {
    $maxRetries = 3
    $attempt = 0
    
    while ($attempt -lt $maxRetries) {
        try {
            Update-Help -Module PackageManagement -Force -ErrorAction Stop
            break
        }
        catch {
            $attempt++
            if ($attempt -ge $maxRetries) { throw }
            Start-Sleep -Seconds 2
        }
    }
    
    Get-Help Get-Package | Should -Not -BeNullOrEmpty
}
```

---

## Test Status Summary Table

| Status | Passed? | Validates? | Counts as "Working"? | Use When |
|--------|---------|------------|----------------------|----------|
| **Passed** | ✓ | ✓ | **YES** | Feature is working and test proves it |
| **Failed** | ✗ | ✓ | NO | Feature is broken or test has wrong expectation |
| **Error** | ✗ | ✗ | NO | Test infrastructure broken, can't validate |
| **Pending** | - | ✗ | **NO** ⚠️ | Temporary - test should eventually pass |
| **Skipped** | - | ✗ | NO | Test not applicable to this environment |
| **Ignored** | - | ✗ | NO | Test permanently disabled |

---

## Recommended Patterns

### Pattern 1: Resilient Test with Fallback
```powershell
It "Feature works with web or local source" {
    $useLocal = $false
    
    try {
        Update-Help -Module Package -Force -ErrorAction Stop
    }
    catch {
        $useLocal = $true
        Update-Help -Module Package -SourcePath ./assets -Force -ErrorAction Stop
    }
    
    # Validate functionality regardless of source
    Get-Help Get-Package | Should -Not -BeNullOrEmpty
}
```

### Pattern 2: Conditional Skip with Clear Reason
```powershell
Describe "Update-Help from Web" -Skip $(-not (Test-InternetConnectivity)) {
    It "Downloads help successfully" {
        Update-Help -Module PackageManagement -Force -ErrorAction Stop
        Get-Help Get-Package | Should -Not -BeNullOrEmpty
    }
}
```

### Pattern 3: Separate Suites by Dependency
```powershell
Describe "Help Content Tests - Web" {
    # Tests that require internet - can be skipped if unavailable
    It "Downloads from web" { ... }
}

Describe "Help Content Tests - Local" {
    # Tests with local assets - should always pass
    It "Loads from local assets" {
        Update-Help -Module Package -SourcePath ./assets -Force
        Get-Help Get-Package | Should -Not -BeNullOrEmpty
    }
}
```

---

## Checklist: Is Your Test "Working"?

- [ ] Test status is **Passed** (not Pending, not Skipped, not Failed)
- [ ] Test actually **executes** the feature being tested
- [ ] Test has **specific assertions** (not just `Should -Not -BeNullOrEmpty`)
- [ ] Test includes **cleanup** (removes temp files, restores state)
- [ ] Test can run **multiple times** without side effects
- [ ] Test failure **indicates a real problem** (not flaky assertions)
- [ ] Test success **proves the feature works** (not just "didn't crash")

If any of these is false, your test may be passing but not "working" properly.

---

## See Also

- [Pester Documentation](https://pester.dev/)
- [Set-ItResult Documentation](https://pester.dev/docs/commands/Set-ItResult)
- [Test Isolation and Dependencies](../test-isolation-guide.md)
- [Flaky Test Patterns](../flaky-tests-guide.md)
