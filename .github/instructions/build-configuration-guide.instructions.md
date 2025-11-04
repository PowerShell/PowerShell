---
applyTo:
  - "build.psm1"
  - "tools/ci.psm1"
  - ".github/**/*.yml"
  - ".github/**/*.yaml"
---

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

**Use: Release with version tag and public NuGet feeds**

```yaml
- name: Build for Release
  shell: pwsh
  run: |
    Import-Module ./build.psm1
    Import-Module ./tools/ci.psm1
    Switch-PSNugetConfig -Source Public
    $releaseTag = Get-ReleaseTag
    Start-PSBuild -Configuration 'Release' -ReleaseTag $releaseTag
```

**Why Release:**
- Optimized binaries
- No debug symbols (smaller size)
- Production-ready

**Why Switch-PSNugetConfig -Source Public:**
- Switches NuGet package sources to public feeds (nuget.org and public Azure DevOps feeds)
- Required for CI/CD environments that don't have access to private feeds
- Uses publicly available packages instead of Microsoft internal feeds

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
src/powershell-unix/bin/Debug/<netversion>/<runtime>/publish/
```

**Windows:**
```
src/powershell-win-core/bin/Debug/<netversion>/<runtime>/publish/
```

## Best Practices

1. Use default configuration for testing
2. Avoid redundant parameters
3. Match configuration to purpose
4. Use `-CI` only when needed
5. Always specify `-ReleaseTag` for release or packaging builds
6. Use `Switch-PSNugetConfig -Source Public` in CI/CD for release builds

## NuGet Feed Configuration

### Switch-PSNugetConfig

The `Switch-PSNugetConfig` function in `build.psm1` manages NuGet package source configuration.

**Available Sources:**

- **Public**: Uses public feeds (nuget.org and public Azure DevOps feeds)
  - Required for: CI/CD environments, public builds, packaging
  - Does not require authentication
  
- **Private**: Uses internal PowerShell team feeds
  - Required for: Internal development with preview packages
  - Requires authentication credentials

- **NuGetOnly**: Uses only nuget.org
  - Required for: Minimal dependency scenarios

**Usage:**

```powershell
# Switch to public feeds (most common for CI/CD)
Switch-PSNugetConfig -Source Public

# Switch to private feeds with authentication
Switch-PSNugetConfig -Source Private -UserName $userName -ClearTextPAT $pat

# Switch to nuget.org only
Switch-PSNugetConfig -Source NuGetOnly
```

**When to Use:**

- **Always use `-Source Public`** before building in CI/CD workflows
- Use before any build that will create packages for distribution
- Use in forks or environments without access to Microsoft internal feeds
