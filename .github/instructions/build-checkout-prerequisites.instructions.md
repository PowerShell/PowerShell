# Build and Checkout Prerequisites for PowerShell CI

This document describes the checkout and build prerequisites used in PowerShell's CI workflows. It is intended for GitHub Copilot sessions working with the build system.

## Overview

The PowerShell repository uses a standardized build process across Linux, Windows, and macOS CI workflows. Understanding the checkout configuration and the `Sync-PSTags` operation is crucial for working with the build system.

## Checkout Configuration

### Fetch Depth

All CI workflows that build or test PowerShell use `fetch-depth: 1000` in the checkout step:

```yaml
- name: checkout
  uses: actions/checkout@v5
  with:
    fetch-depth: 1000
```

**Why 1000 commits?**
- The build system needs access to Git history to determine version information
- `Sync-PSTags` requires sufficient history to fetch and work with tags
- 1000 commits provides a reasonable balance between clone speed and having enough history for version calculation
- Shallow clones (fetch-depth: 1) would break versioning logic

**Exceptions:**
- The `changes` job uses default fetch depth (no explicit `fetch-depth`) since it only needs to detect file changes
- The `analyze` job (CodeQL) uses `fetch-depth: '0'` (full history) for comprehensive security analysis
- Linux packaging uses `fetch-depth: 0` to ensure all tags are available for package version metadata

### Workflows Using fetch-depth: 1000

- **Linux CI** (`.github/workflows/linux-ci.yml`): All build and test jobs
- **Windows CI** (`.github/workflows/windows-ci.yml`): All build and test jobs  
- **macOS CI** (`.github/workflows/macos-ci.yml`): All build and test jobs

## Sync-PSTags Operation

### What is Sync-PSTags?

`Sync-PSTags` is a PowerShell function defined in `build.psm1` that ensures Git tags from the upstream PowerShell repository are synchronized to the local clone.

### Location

- **Function Definition**: `build.psm1` (line 36-76)
- **Called From**: 
  - `.github/actions/build/ci/action.yml` (Bootstrap step, line 24)
  - `tools/ci.psm1` (Invoke-CIInstall function, line 146)

### How It Works

```powershell
Sync-PSTags -AddRemoteIfMissing
```

The function:
1. Searches for a Git remote pointing to the official PowerShell repository:
   - `https://github.com/PowerShell/PowerShell`
   - `git@github.com:PowerShell/PowerShell`

2. If no upstream remote exists and `-AddRemoteIfMissing` is specified:
   - Adds a remote named `upstream` pointing to `https://github.com/PowerShell/PowerShell.git`

3. Fetches all tags from the upstream remote:
   ```bash
   git fetch --tags --quiet upstream
   ```

4. Sets `$script:tagsUpToDate = $true` to indicate tags are synchronized

### Why Sync-PSTags is Required

Tags are critical for:
- **Version Calculation**: `Get-PSVersion` uses `git describe --abbrev=0` to find the latest tag
- **Build Numbering**: CI builds use tag-based versioning for artifacts
- **Changelog Generation**: Release notes are generated based on tags
- **Package Metadata**: Package versions are derived from Git tags

Without synchronized tags:
- Version detection would fail or return incorrect versions
- Builds might have inconsistent version numbers
- The build process would error when trying to determine the version

### Bootstrap Step in CI Action

The `.github/actions/build/ci/action.yml` includes this in the Bootstrap step:

```yaml
- name: Bootstrap
  if: success()
  run: |-
    Write-Verbose -Verbose "Running Bootstrap..."
    Import-Module .\tools\ci.psm1
    Invoke-CIInstall -SkipUser
    Write-Verbose -Verbose "Start Sync-PSTags"
    Sync-PSTags -AddRemoteIfMissing
    Write-Verbose -Verbose "End Sync-PSTags"
  shell: pwsh
```

**Note**: `Sync-PSTags` is called twice:
1. Once by `Invoke-CIInstall` (in `tools/ci.psm1`)
2. Explicitly again in the Bootstrap step

This redundancy ensures tags are available even if the first call encounters issues.

## Best Practices for Copilot Sessions

When working with the PowerShell CI system:

1. **Always use `fetch-depth: 1000` or greater** when checking out code for build or test operations
2. **Understand that `Sync-PSTags` requires network access** to fetch tags from the upstream repository
3. **Don't modify the fetch-depth without understanding the impact** on version calculation
4. **If adding new CI workflows**, follow the existing pattern:
   - Use `fetch-depth: 1000` for build/test jobs
   - Call `Sync-PSTags -AddRemoteIfMissing` during bootstrap
   - Ensure the upstream remote is properly configured

5. **For local development**, developers should:
   - Have the upstream remote configured
   - Run `Sync-PSTags -AddRemoteIfMissing` before building
   - Or use `Start-PSBuild` which handles this automatically

## Related Files

- `.github/actions/build/ci/action.yml` - Main CI build action
- `.github/workflows/linux-ci.yml` - Linux CI workflow
- `.github/workflows/windows-ci.yml` - Windows CI workflow
- `.github/workflows/macos-ci.yml` - macOS CI workflow
- `build.psm1` - Contains Sync-PSTags function definition
- `tools/ci.psm1` - CI-specific build functions that call Sync-PSTags

## Summary

The PowerShell CI system depends on:
1. **Adequate Git history** (fetch-depth: 1000) for version calculation
2. **Synchronized Git tags** via `Sync-PSTags` for accurate versioning
3. **Upstream remote access** to fetch official repository tags

These prerequisites ensure consistent, accurate build versioning across all CI platforms.
