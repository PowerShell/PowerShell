# Start-PSBuild Basics

## Purpose

`Start-PSBuild` builds PowerShell from source. It's defined in `build.psm1` and used in CI/CD workflows.

## Default Usage

For most scenarios, use with no parameters:

```powershell
Import-Module ./tools/ci.psm1
Start-PSBuild
```

**Default behavior:**
- Configuration: `Debug`
- PSModuleRestore: Enabled
- Runtime: Auto-detected for platform

## Common Configurations

### Debug Build (Default)

```powershell
Start-PSBuild
```

Use for:
- Testing (xUnit, Pester)
- Development
- Debugging

### Release Build

```powershell
Start-PSBuild -Configuration 'Release'
```

Use for:
- Production packages
- Distribution
- Performance testing

### Code Coverage Build

```powershell
Start-PSBuild -Configuration 'CodeCoverage'
```

Use for:
- Code coverage analysis
- Test coverage reports

## Common Parameters

### -Configuration

Values: `Debug`, `Release`, `CodeCoverage`, `StaticAnalysis`

Default: `Debug`

### -CI

Restores Pester module for CI environments.

```powershell
Start-PSBuild -CI
```

### -PSModuleRestore

Now enabled by default. Use `-NoPSModuleRestore` to skip.

### -ReleaseTag

Specifies version tag for release builds:

```powershell
$releaseTag = Get-ReleaseTag
Start-PSBuild -Configuration 'Release' -ReleaseTag $releaseTag
```

## Workflow Example

```yaml
- name: Build PowerShell
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild
```
