---
name: analyze-pester-failures
description: Troubleshooting guide for analyzing and investigating Pester test failures in PowerShell CI jobs. Help agents understand why tests are failing, interpret test output, navigate test result artifacts, and provide actionable recommendations for fixing test issues.
---

# Analyze Pester Test Failures

Investigate and troubleshoot Pester test failures in GitHub Actions workflows. Understand what tests are failing, why they're failing, and provide recommendations for test fixes.

| Skill | When to Use |
|-------|-----------|
| analyze-pester-failures | When investigating why Pester tests are failing in a CI job. Use when a test job shows failures and you need to understand what test failed, why it failed, what the error message means, and what might need to be fixed. Also use when asked: "why did this test fail?", "what's the test error?", "test is broken", "test failure analysis", "debug test failure", or given test failure logs and stack traces. |

## When to Use This Skill

Use this skill when you need to:

- Understand why a specific Pester test is failing  
- Interpret test failure messages and error output
- Analyze test result data from CI workflow runs (XML, logs, stack traces)
- Identify the root cause of test failures (test logic, assertion failure, exception, timeout, skip/ignore reason)
- Provide recommendations for fixing failing tests
- Compare expected vs. actual test behavior
- Debug test environment issues (missing dependencies, configuration problems)
- Understand test skip/ignored/inconclusive status reasons

**Do not use this skill for:**
- General PowerShell debugging unrelated to tests
- Test infrastructure/CI setup issues (except as they affect test failure interpretation)
- Performance analysis or benchmarking (that's a different investigation)

## Quick Start

### ⚠️ CRITICAL: The Workflow Must Be Followed IN ORDER

This skill describes a **sequential 6-step analysis workflow**. Skipping steps or jumping around leads to **incomplete analysis and incorrect conclusions**.

**The Problem**: It's easy to skip to Step 4 or 5 without doing Steps 1-2, resulting in missing data and bad conclusions.

**The Solution**: Use the automated analysis script to enforce the workflow:

```powershell
# Automatically runs Steps 1-6 in order, preventing skipping
./.github/skills/analyze-pester-failures/scripts/analyze-pr-test-failures.ps1 -PR <PR_NUMBER>

# Example:
./.github/skills/analyze-pester-failures/scripts/analyze-pr-test-failures.ps1 -PR 26800
```

This script:
1. ✓ Fetches PR status automatically
2. ✓ Downloads artifacts (can't skip, depends on Step 1)
3. ✓ Extracts failures (can't skip, depends on Step 2)
4. ✓ Analyzes error messages
5. ✓ Documents context
6. ✓ Generates recommendations

**Only use the manual commands below if you fully understand the workflow.**

### Manual Workflow (for reference)

```powershell
# Step 1: Identify the failing job
gh pr view <PR_NUMBER> --json 'statusCheckRollup' | ConvertFrom-Json | Where-Object { $_.conclusion -eq 'FAILURE' }

# Step 2: Download artifacts (extract RUN_ID from Step 1)
gh run download <RUN_ID> --dir ./artifacts
gh run view <RUN_ID> --log > test-logs.txt

# Step 3-6: Extract, analyze, and interpret
# (See Analysis Workflow section below)
```

## Common Test Failure Analysis Approaches

### 1. **Interpreting Assertion Failures**
The most common test failure is when an assertion doesn't match expectations.

**Example:**
```
Expected $true but got $false at /path/to/test.ps1:42
Assertion failed: Should -Be "expected" but was "actual"  
```

**How to analyze:**
- Read the assertion message: what was expected vs. what was actual?
- Check the test logic: is the expectation correct?
- Look for mock/stub issues: are dependencies configured correctly?
- Check parameter values: what inputs were passed to the function under test?

### 2. **Exception Failures**
Tests fail when PowerShell throws an exception instead of successful completion.

**Example:**
```
Command: Write-Host $null
Error: Cannot bind argument to parameter 'Object' because it is null.
```

**How to analyze:**
- Read the exception message: what operation failed?
- Check the stack trace: where in the test or tested code did it throw?
- Verify preconditions: does the test setup provide required values/mocks?
- Look for environmental issues: missing modules, permissions, file system state?

### 3. **Timeout Failures**
A test takes longer than the allowed timeout to complete.

**Example:**
```
Test 'Should complete in reasonable time' timed out after 30 seconds
```

**How to analyze:**
- Is the timeout appropriate for this test type? (network tests need more time)
- Is there an infinite loop in the test or tested code?
- Are there resource contention issues on the CI runner?
- Does the test hang waiting for something (file lock, network, process)?

### 4. **Skip/Ignored Reason Analysis**
Tests marked as skipped or ignored provide clues about test environment.

**Example:**
```
Test marked [Skip("Only runs on Windows")] - running on Linux
Test marked [Ignore("Known issue #12345")]
```

**How to analyze:**
- Read the skip/ignore reason: is it still valid?
- Check if environment has changed: platform, module versions, etc.
- Verify issue status: is the known issue still open? Has it been fixed?
- Determine if skip should be removed or if test needs environment changes

### 5. **Flaky/Intermittent Failures**
Tests that sometimes pass, sometimes fail indicate race conditions or environment sensitivity.

**Example:**
- Test passes locally but fails on CI
- Test passes first run of suite, fails on second run
- Test passes on Windows but fails on Linux

**How to analyze:**
- Look for timeout races: is timing involved in the test?
- Check for test isolation issues: does one test affect another?
- Verify environment differences: CI vs. local paths, permissions, versions
- Look for external dependencies: network calls, file I/O, process interactions

## Key Artifacts and Locations

| Item | Purpose | Location |
|------|---------|----------|
| Test result XML | Pester output with test cases, failures, errors | Workflow artifacts: `junit-pester-*.xml` |
| Job logs | Full job output including test execution and errors | GitHub Actions run logs or `gh run download` |
| Stack traces | Error location information from failed assertions | Within job logs and XML failure messages |
| Test files | The actual Pester test code (`.ps1` files) | `test/` directory in repository |

## Analysis Workflow

### ⚠️ Important: These Steps MUST Be Followed In Order

Each step depends on the previous one. Skipping or re-ordering steps causes incomplete analysis:

- **Step 1** (identify jobs) → You get the RUN_ID needed for Step 2
- **Step 2** (download) → You get the artifacts needed for Step 3  
- **Step 3** (extract) → You discover what failures exist for Step 4
- **Step 4** (read messages) → You understand the errors to analyze in Step 5
- **Step 5** (context) → You gather information to make recommendations in Step 6
- **Step 6** (interpret) → You use all above to recommend fixes

**Real Problem We Had**:
- ❌ Jumped to Step 3 without Step 1-2
- ❌ Used random test data from context instead of downloading PR artifacts
- ❌ Skipped Steps 5-6 entirely
- ❌ Made recommendations without full context

**Result**: Wrong analysis and recommendations that didn't actually fix the problem.

### Recommended: Use the Automated Script

```powershell
./.github/skills/analyze-pester-failures/scripts/analyze-pr-test-failures.ps1 -PR <PR_NUMBER>
```

This enforces the workflow and prevents skipping.

### Step 2: Get Test Results

Fetch the test result artifacts and job logs:

```powershell
# Download artifacts including test XML results
gh run download <RUN_ID> --dir ./artifacts

# Get job logs
gh run view <RUN_ID> --log > test-logs.txt

# Inspect test XML
$xml = [xml](Get-Content ./artifacts/junit-pester-*.xml)
$xml.'test-results' | Select-Object total, failures, errors, ignored, inconclusive
```

### Step 3: Extract Specific Failures

Find the failing test cases in the XML:

```powershell
# Get all failed test cases
$xml = [xml](Get-Content ./artifacts/junit-pester-*.xml)
$failures = $xml.SelectNodes('.//test-case[@result = "Failure"]')

# For each failure, display key info
$failures | ForEach-Object {
    [PSCustomObject]@{
        Name = $_.name
        Description = $_.description
        Message = $_.failure.message
        StackTrace = $_.failure.'stack-trace'
    }
}
```

### Step 4: Read the Error Message

The error message tells you what went wrong:

**Assertion failures:**
```
Expected $true but got $false
Expected "value1" but got "value2"
Expression should have failed with exception, but didn't
```

**Exceptions:**
```
Cannot find a parameter with name 'Name'
Property 'Property' does not exist on 'Object'
Cannot bind argument to parameter because it is null
```

**Timeouts:**
```
Test timed out after 30 seconds
Test is taking too long to complete
```

### Step 5: Understand the Context

Look at the test file to understand what was being tested:

```powershell
# Find the test file mentioned in the stack trace
# Example: /path/to/test/Feature.Tests.ps1:42

# Read the test code around that line
code <test-file-path>:<line-number>

# Understand:  
# - What assertion is on that line?
# - What is the test trying to verify?
# - What are the setup/mock/before conditions?
# - Are there recent changes to the function being tested?
```

### Step 6: Interpret the Failure

Determine the root cause category:

**Test issue (needs code fix):**
- Assertion logic is wrong
- Test expectations don't match actual behavior
- Test setup is incomplete
- Mock/stub configuration missing

**Environmental issue (needs environment change):**
- Test assumes a specific file or registry entry exists
- Test requires Windows/Linux specifically
- Test requires specific PowerShell version
- Test requires specific module version
- Timing-sensitive test affected by CI load

**Data issue (needs input data change):**
- Test data no longer valid
- External API changed format
- Configuration file has changed structure

**Flakiness (needs test hardening):**
- Race condition in test
- Timing assumptions too tight
- Resource contention on CI runner
- Non-deterministic behavior in tested code

## Common Test Failure Patterns

| Pattern | What It Means | Example | Next Step |
|---------|---------------|---------|-----------|
| `Expected $true but got $false` | Assertion on boolean result failed | Test expects function returns true, but it returns false | Check function logic for bug or test logic for wrong expectation |
| `Cannot find path` | File or directory doesn't exist | Test tries to read config file that's not present | Verify file path, check test setup, ensure CI environment has file |
| `Cannot bind argument to parameter 'X'` | Required parameter value is null or wrong type | Function called with $null where object expected | Check test mock setup, verify parameter types |
| `Test timed out after X seconds` | Test exceeded time limit | Network call or loop takes too long | Increase timeout for slow test, find infinite loop, mock network calls |
| `Expression should have failed but didn't` | Exception wasn't thrown when expected | Test expects error but function succeeds | Check if function behavior changed, update test expectation |
| `Could not find parameter 'X'` | Function doesn't have parameter | Test calls function with parameter that doesn't exist | Check PowerShell version, verify function signature, update test |
| `This platform is not supported` | Test skipped on current OS | Windows-only test running on Linux | Add platform check, update test environment, or mark as platform-specific |
| `Test marked [Ignore]` | Test explicitly disabled | Test has `[Ignore("reason")]` attribute | Check if reason still valid, remove if issue fixed |

## Interpreting Test Results

### Test Result Counts

Pester test outcomes are categorized as:

| Count | Meaning | Notes |
|-------|---------|-------|
| `total` | Total number of test cases executed | Should match: passed + failed + errors + skipped + ignored |
| `failures` | Test assertions that failed | `Expected X but got Y` type failures |
| `errors` | Tests that threw exceptions | Unhandled PowerShell exceptions during test |
| `skipped` | Tests explicitly skipped (marked with `-Skip`) | Test code recognizes condition and skips |
| `ignored` | Tests marked as ignored (marked with `-Ignore`) | Test disabled intentionally, usually notes reason |
| `inconclusive` | Tests with unclear result | Rare; usually means test framework issue |
| `passed` | Tests with passing assertions | `total - failures - errors - skipped - ignored` |

### Stack Trace Interpretation

A stack trace shows where the failure occurred:

```
at /home/runner/work/PowerShell/test/Feature.Tests.ps1:42

Means:
- File: /home/runner/work/PowerShell/test/Feature.Tests.ps1
- Line: 42
- Look at that line to see which assertion failed
```

### Understanding Skipped Tests

When XML shows `result="Ignored"` or `result="Skipped"`:

```xml
<test-case name="Test Name" result="Ignored">
  <reason>Only runs on Windows</reason>
</test-case>
```

The reason explains why test didn't run. Not a failure, but important for understanding test coverage.

## Providing Test Failure Analysis

### Investigation Questions

After gathering test output, ask yourself:

1. **Is the test code correct?**
   - Does the test assertion match the expected behavior?
   - Are test expectations still valid?
   - Has the function being tested changed?

2. **Is the test setup correct?**
   - Are mocks/stubs configured properly?
   - Does the test environment have required files/configuration?
   - Are preconditions (database, files, services) met?

3. **Is this a code bug or test issue?**
   - Does the tested function have a logic error?
   - Or does the test have incorrect expectations?

4. **Is this environment-specific?**
   - Only fails on Windows/Linux?
   - Only fails on CI but passes locally?
   - Timing-dependent or resource-dependent?

5. **Is this a known/expected failure?**
   - Is there already an issue tracking this failure?
   - Is the test marked as flaky or expected to fail?
   - Does the skip/ignore reason still apply?

### Recommendation Framework

Based on your analysis:

| Finding | Recommendation |
|---------|-----------------|
| Test logic is wrong | "Test assertion on line X is incorrect. Test expects Y but function correctly returns Z. Update test expectation." |
| Tested code has bug | "Function at file.ps1#L42 has logic error. When X happens, returns Y instead of Z. Fix the condition." |
| Missing test setup | "Test setup incomplete. Mock for dependency Y is not configured. Add `Mock Get-Y -MockWith { ... }`" |
| Environment issue | "Test is Windows-specific but running on Linux. Either add platform check or skip on non-Windows." |
| Flaky test | "Test is timing-sensitive (sleep 1 second). Increase timeout or use better synchronization." |
| Test should be skipped | "Test is marked Ignored for good reason. Keep it disabled until issue #12345 is fixed." |

### Tone and Structure

Provide analysis as:

1. **Summary** (1 sentence): What test is failing and general category  
2. **Failure Details** (2-3 sentences): What the test output says  
3. **Root Cause** (1-2 sentences): Why it's failing (test bug vs. code bug vs. environment)
4. **Recommendation** (actionable): What should be done to fix it  
5. **Context** (optional): Link to related code, issues, or recent changes

## Examples

### Example 1: Assertion Failure Due to Code Bug

**Test Output:**
```
Expected 5 but got 3 at /path/to/Test.ps1:42
```

**Investigation:**
1. Look at line 42: `$result | Should -Be 5`
2. Check the test: It expects function to return 5 items
3. Check the function: It returns `$items | Where-Object {$_.Status -eq "Active"}` but the filter is wrong
4. Root cause: Function has logic error, not test error

**Recommendation:**
```
Test failure is due to a code bug:

The test Set-Configuration should return 5 items but returns 3.

Looking at the tested function at [module.ps1#L42](module.ps1#L42):
  $activeItems = $items | Where-Object {$_.Status -eq "Active"}

The issue is the filter condition. It's currently filtering by "Active" status,
but should include "Pending" status as well. 

Fix: Change line 42 to:
  $activeItems = $items | Where-Object {$_.Status -ne "Disabled"}

Then re-run the test to verify it now returns 5 items as expected.
```

### Example 2: Test Setup Issue

**Test Output:**
```
Cannot find path '/expected/config.json' because it does not exist at /path/to/Test.ps1:15
```

**Investigation:**
1. Line 15 tries to read a config file
2. The test setup doesn't create this file
3. Works locally but fails on CI because CI doesn't have the same file

**Recommendation:**
```
Test setup is incomplete:

The test Initialize-Config fails because it expects /expected/config.json but the test doesn't create this file.

The test needs to ensure the config file exists. Currently line 12-14 doesn't set up the file:

  # Before:
  # (no setup of config file)

  # After:
  @{ setting1 = "value1"; setting2 = "value2" } | ConvertTo-Json | 
    Out-File $testConfigPath

Alternatively, the test function should accept a parameter for the config path and use a temporary file:
  param([string]$ConfigPath = (New-TemporaryFile))

Re-run the test to verify the config file is properly available.
```

### Example 3: Platform-Specific Test Failure

**Test Output:**
```
Test 'should read Windows Registry' failed on Linux runner
Cannot find path 'HKEY_LOCAL_MACHINE:\...'
```

**Investigation:**
1. Test assumes Windows Registry exists (Windows-only)
2. Running on Linux runner doesn't have Registry
3. Test should skip on non-Windows platforms

**Recommendation:**
```
Test is platform-specific but running on wrong platform:

The test "should read Windows Registry" assumes Windows Registry exists but is running on Linux.

Add a platform check to skip this test on non-Windows systems:

  It "should read Windows Registry" -Skip:$(-not $IsWindows) {
    # test code here
  }

Or group Windows-only tests in a separate Describe block with platform check:

  Describe "Windows Registry Tests" -Skip:$(-not $IsWindows) {
    # all Windows-specific tests here
  }

This allows the test to be skipped on Linux/Mac while still running on Windows CI.
```

## References

- [Pester Testing Framework](https://pester.dev/) — Official documentation, best practices for test writing
- [Test Files](../../../test/) — PowerShell test suite in repository
- [GitHub Actions Documentation](https://docs.github.com/en/actions) — Understanding workflow runs and logs
- [PowerShell Documentation](https://learn.microsoft.com/en-us/powershell/) — Language reference for understanding test code

## Tips

1. **Read the error message first:** The error message is usually the most direct clue to the problem
2. **Check test vs. code blame:** Is the test wrong or is the code wrong? Look at both sides
3. **Verify test isolation:** Does one test failure affect others? Check for shared state or test ordering dependencies
4. **Test locally first:** Try running the failing test locally to reproduce and understand it better
5. **Check for environmental assumptions:** Windows-specific paths, module versions, file locations may differ on CI
6. **Look for skip/ignore patterns:** If a test is consistently ignored, check if the reason is still valid
7. **Compare passing vs. failing:** If test passes locally but fails on CI, the difference is usually environment-related
8. **Check recent changes:** Did a recent PR change the tested code or test itself?
9. **Understand Pester output format:** Different Pester versions, different `-ErrorAction`, `-WarningAction` produce different test results
10. **Don't assume CI is wrong:** Failures on CI often reveal real issues that local testing missed (network, file permissions, parallelization, etc.)

## Additional Links

- [PowerShell Repository](https://github.com/PowerShell/PowerShell)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Pester Testing Framework](https://github.com/Pester/Pester)
