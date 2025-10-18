# Using Start-PSBuild in GitHub Workflows

## Purpose

This document provides guidance for using `Start-PSBuild` in GitHub Actions workflows to build PowerShell from source.

## Prerequisites

### Required Workflow Steps

Before calling `Start-PSBuild`, ensure these steps are completed:

1. **Checkout with sufficient git history**
   ```yaml
   - name: Checkout
     uses: actions/checkout@v4
     with:
       fetch-depth: 1000
   ```

2. **Setup .NET**
   ```yaml
   - name: Setup .NET
     uses: actions/setup-dotnet@v4
     with:
       global-json-file: ./global.json
   ```

3. **Bootstrap and sync tags**
   ```yaml
   - name: Bootstrap
     shell: pwsh
     run: |
       Import-Module ./tools/ci.psm1
       Invoke-CIInstall -SkipUser
       Sync-PSTags -AddRemoteIfMissing
   ```

## Git Requirements

### Fetch Depth

**Required:** `fetch-depth: 1000`

The PowerShell build process uses `git describe --abbrev=60 --long` to generate version information. This requires access to git history and tags.

**Without sufficient fetch depth, builds fail with:**
```
error MSB3073: The command "git describe --abbrev=60 --long" exited with code 128.
```

### Tag Synchronization

**Required:** `Sync-PSTags -AddRemoteIfMissing`

The build process needs git tags to properly version the build. The `Sync-PSTags` function ensures all tags are available locally.

## Start-PSBuild Usage

### Default Configuration (Recommended)

For most scenarios, use Start-PSBuild with default parameters:

```yaml
- name: Build PowerShell
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild
```

**Default behavior:**
- Configuration: `Debug` (suitable for testing)
- PSModuleRestore: Enabled by default
- No Pester restoration (use `-CI` flag if needed)

### Common Parameters

#### Configuration Types

```powershell
# Debug build (default)
Start-PSBuild

# Release build
Start-PSBuild -Configuration 'Release'

# Code coverage build
Start-PSBuild -Configuration 'CodeCoverage'
```

#### CI Builds

```powershell
# CI flag restores Pester module
Start-PSBuild -CI

# With release tag
Start-PSBuild -Configuration 'Release' -CI -ReleaseTag $releaseTag
```

#### Module Restoration

```powershell
# PSModuleRestore is now default, but can be explicit
Start-PSBuild -PSModuleRestore

# Skip module restore
Start-PSBuild -NoPSModuleRestore
```

## Configuration Guidelines

### For Testing (xUnit, Pester)

Use default Debug configuration:

```yaml
- name: Build PowerShell for Testing
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild
```

**Why Debug:**
- Includes debugging symbols
- Better error messages
- Faster build times than Release
- Suitable for test execution

### For Release/Packaging

Use Release configuration with version tags:

```yaml
- name: Build PowerShell for Release
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    $releaseTag = Get-ReleaseTag
    Start-PSBuild -Configuration 'Release' -CI -ReleaseTag $releaseTag
```

### For Code Coverage

```yaml
- name: Build PowerShell with Code Coverage
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild -Configuration 'CodeCoverage' -PSModuleRestore
```

## Complete Workflow Example

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 1000

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: ./global.json

      - name: Bootstrap
        shell: pwsh
        run: |
          Import-Module ./tools/ci.psm1
          Invoke-CIInstall -SkipUser
          Sync-PSTags -AddRemoteIfMissing

      - name: Build PowerShell
        shell: pwsh
        run: |
          Import-Module ./tools/ci.psm1
          Start-PSBuild
```

## Troubleshooting

### Error: "git describe --abbrev=60 --long" exited with code 128

**Cause:** Insufficient git history (shallow clone)

**Solution:** Add `fetch-depth: 1000` to checkout step

### Error: Version information is incorrect

**Cause:** Git tags not synchronized

**Solution:** Run `Sync-PSTags -AddRemoteIfMissing` in bootstrap step

### Error: CoreCLR pwsh.exe was not built

**Cause:** Build failed or using wrong configuration

**Solution:** 
- Check build logs for errors
- Ensure prerequisites are met
- Verify correct configuration for your use case

### Build is slow or fails to restore modules

**Cause:** Network issues or module cache problems

**Solution:**
- Retry the build
- Check network connectivity
- Consider using `-NoPSModuleRestore` if modules not needed

## Platform-Specific Considerations

### Linux/macOS
- Default runtime is auto-detected
- Build output: `src/powershell-unix/bin/Debug/net9.0/<runtime>/publish/`

### Windows
- Default runtime is auto-detected
- Build output: `src/powershell-win-core/bin/Debug/net9.0/<runtime>/publish/`

### Cross-Platform Workflows

Use the same commands across all platforms - the build system handles platform-specific details:

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
runs-on: ${{ matrix.os }}
steps:
  # Same steps work on all platforms
  - name: Build PowerShell
    shell: pwsh
    run: |
      Import-Module ./tools/ci.psm1
      Start-PSBuild
```

## Best Practices

1. **Always use `fetch-depth: 1000`** in checkout for builds
2. **Always run `Sync-PSTags`** after bootstrap
3. **Use default configuration** unless you have a specific reason not to
4. **Avoid redundant parameters** - many are now defaults
5. **Use `-CI` flag** only when you need Pester module restored
6. **Match configuration to purpose** - Debug for testing, Release for distribution
7. **Check build output location** - varies by platform and configuration

## Related Commands

- `Invoke-CIBuild` - Higher-level CI build command (includes Start-PSBuild)
- `Get-PSOptions` - Retrieve current build options
- `Save-PSOptions` - Save build options for later use
- `Restore-PSOptions` - Restore previously saved build options
- `Get-ReleaseTag` - Get release version tag

## Additional Resources

- Build system documentation: `build.psm1`
- CI utilities: `tools/ci.psm1`
- Build properties: `PowerShell.Common.props`
