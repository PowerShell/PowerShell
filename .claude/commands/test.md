---
description: Run Pester tests with optional filter pattern
---

Run PowerShell tests using the Pester framework.

**Usage:**
- `/test` - Run all CI-tagged tests
- `/test <pattern>` - Run tests matching the pattern (e.g., `/test "Language*"`)

**Steps to execute:**

1. Import the build module: `Import-Module ./build.psm1 -Force`

2. If a pattern argument is provided:
   - Run: `Start-PSPester -Tests "<pattern>" -UseNuGetOrg`

3. If no pattern is provided:
   - Run: `Start-PSPester -UseNuGetOrg`

4. Report test results:
   - Total tests run
   - Passed count
   - Failed count (with details if any failures)
   - Skipped count

5. If tests fail:
   - Summarize which test files/areas failed
   - Provide guidance on how to investigate failures

**Test Tags:**
- `[CI]` - Fast tests run on every PR (default)
- `[Feature]` - Slower tests run daily
- `[Scenario]` - Integration tests
- `[Slow]` - Tests taking >1 second

**Common Test Patterns:**
- `Language*` - Language feature tests
- `Engine*` - Engine behavior tests
- `Modules*` - Module tests
- `Host*` - Console host tests

**Note:** Tests are located in `test/powershell/` directory. For C# unit tests, use `Start-PSxUnit` instead.
