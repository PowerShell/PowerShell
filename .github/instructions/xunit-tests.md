# xUnit Tests Reusable Workflow

## Overview

The `xunit-tests.yml` workflow is a reusable workflow that runs xUnit tests for PowerShell. It builds PowerShell from source and executes the xUnit test suite independently.

## Important Configuration Requirements

### 1. Git Fetch Depth

**Requirement:** `fetch-depth: 1000`

The checkout step must use a sufficient fetch depth to allow git operations during the build process.

**Why:** The PowerShell build process uses `git describe --abbrev=60 --long` to generate version information. This command requires access to git history and tags. A shallow clone (default `fetch-depth: 1`) will cause the build to fail with:

```
error MSB3073: The command "git describe --abbrev=60 --long" exited with code 128.
```

**Solution:** Always use `fetch-depth: 1000` in the checkout step:

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 1000
```

### 2. Git Tags Synchronization

**Requirement:** Run `Sync-PSTags -AddRemoteIfMissing` after bootstrap

**Why:** The build process needs git tags to properly version the build. The `Sync-PSTags` function ensures all tags are available locally.

**Solution:** Include this in the bootstrap step:

```yaml
- name: Bootstrap
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Invoke-CIInstall -SkipUser
    Sync-PSTags -AddRemoteIfMissing
```

### 3. Build Configuration

**Default Configuration:** Debug build (no parameters needed)

The xUnit tests require a Debug build of PowerShell, which is the default configuration for `Start-PSBuild`.

- **Do not** specify `-Configuration 'Release'`
- **Do not** specify `-ReleaseTag` (not needed for tests)
- **Do not** specify `-CI` (only restores Pester, which xUnit doesn't need)
- `-PSModuleRestore` is now the default behavior

**Solution:** Use the simplest form:

```yaml
- name: Build PowerShell
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild
```

## Workflow Structure

```yaml
jobs:
  xunit:
    runs-on: ${{ inputs.runner_os }}
    steps:
      - Checkout (with fetch-depth: 1000)
      - Setup .NET
      - Bootstrap (Invoke-CIInstall + Sync-PSTags)
      - Build PowerShell (Start-PSBuild with defaults)
      - Run xUnit Tests (Invoke-CIxUnit without -SkipFailing)
      - Upload Results
```

## Common Issues

### Issue: Build fails with "git describe" error

**Symptom:**
```
error MSB3073: The command "git describe --abbrev=60 --long" exited with code 128.
```

**Cause:** Insufficient git history (shallow clone)

**Fix:** Add `fetch-depth: 1000` to the checkout step

### Issue: Version information is incorrect

**Symptom:** Build produces incorrect version numbers

**Cause:** Git tags are not synchronized

**Fix:** Ensure `Sync-PSTags -AddRemoteIfMissing` is called in the bootstrap step

### Issue: Tests fail to run

**Symptom:**
```
Exception: CoreCLR pwsh.exe was not built
```

**Cause:** Using wrong build configuration (Release instead of Debug) or build artifacts not present

**Fix:** Use `Start-PSBuild` with default parameters (Debug build)

## Benefits of This Workflow

1. **Independent Execution**: Builds PowerShell from source, no dependency on build artifacts
2. **Retryability**: Can be retried independently without re-running other jobs
3. **Immediate Failure**: Tests fail immediately when they fail (no masking)
4. **Cross-Platform**: Works on Linux, Windows, and macOS with the same configuration
5. **Simplicity**: Uses sensible defaults, minimal configuration required
