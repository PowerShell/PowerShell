# Build Configuration Guide

## Choosing the Right Configuration

### For Testing

**Use: Default (Debug)**

```yaml
- name: Build for Testing
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild
```

**Why Debug:**
- Includes debugging symbols
- Better error messages
- Faster build times
- Suitable for xUnit and Pester tests

**Do NOT use:**
- `-Configuration 'Release'` (unnecessary for tests)
- `-ReleaseTag` (not needed for tests)
- `-CI` (unless you specifically need Pester module)

### For Release/Packaging

**Use: Release with version tag**

```yaml
- name: Build for Release
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    $releaseTag = Get-ReleaseTag
    Start-PSBuild -Configuration 'Release' -ReleaseTag $releaseTag
```

**Why Release:**
- Optimized binaries
- No debug symbols (smaller size)
- Production-ready

### For Code Coverage

**Use: CodeCoverage configuration**

```yaml
- name: Build with Coverage
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild -Configuration 'CodeCoverage'
```

## Platform Considerations

### All Platforms

Same commands work across Linux, Windows, and macOS:

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
runs-on: ${{ matrix.os }}
steps:
  - name: Build PowerShell
    shell: pwsh
    run: |
      Import-Module ./tools/ci.psm1
      Start-PSBuild
```

### Output Locations

**Linux/macOS:**
```
src/powershell-unix/bin/Debug/net9.0/<runtime>/publish/
```

**Windows:**
```
src/powershell-win-core/bin/Debug/net9.0/<runtime>/publish/
```

## Best Practices

1. Use default configuration for testing
2. Avoid redundant parameters
3. Match configuration to purpose
4. Use `-CI` only when needed
5. Always specify `-ReleaseTag` for release builds
