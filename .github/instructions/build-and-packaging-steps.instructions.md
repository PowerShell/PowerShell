---
applyTo:
  - ".github/actions/**/*.yml"
  - ".github/workflows/**/*.yml"
---

# Build and Packaging Steps Pattern

## Important Rule

**Build and packaging must run in the same step OR you must save and restore PSOptions between steps.**

## Why This Matters

When `Start-PSBuild` runs, it creates PSOptions that contain build configuration details (runtime, configuration, output path, etc.). The packaging functions like `Start-PSPackage` and `Invoke-CIFinish` rely on these PSOptions to know where the build output is located and how it was built.

GitHub Actions steps run in separate PowerShell sessions. This means PSOptions from one step are not available in the next step.

## Pattern 1: Combined Build and Package (Recommended)

Run build and packaging in the same step to keep PSOptions in memory:

```yaml
- name: Build and Package
  run: |-
    Import-Module ./tools/ci.psm1
    $releaseTag = Get-ReleaseTag
    Start-PSBuild -Configuration 'Release' -ReleaseTag $releaseTag
    Invoke-CIFinish
  shell: pwsh
```

**Benefits:**
- Simpler code
- No need for intermediate files
- PSOptions automatically available to packaging

## Pattern 2: Separate Steps with Save/Restore

If you must separate build and packaging into different steps:

```yaml
- name: Build PowerShell
  run: |-
    Import-Module ./tools/ci.psm1
    $releaseTag = Get-ReleaseTag
    Start-PSBuild -Configuration 'Release' -ReleaseTag $releaseTag
    Save-PSOptions -PSOptionsPath "${{ runner.workspace }}/psoptions.json"
  shell: pwsh

- name: Create Packages
  run: |-
    Import-Module ./tools/ci.psm1
    Restore-PSOptions -PSOptionsPath "${{ runner.workspace }}/psoptions.json"
    Invoke-CIFinish
  shell: pwsh
```

**When to use:**
- When you need to run other steps between build and packaging
- When build and packaging require different permissions or environments

## Common Mistakes

### ❌ Incorrect: Separate steps without save/restore

```yaml
- name: Build PowerShell
  run: |-
    Start-PSBuild -Configuration 'Release'
  shell: pwsh

- name: Create Packages
  run: |-
    Invoke-CIFinish  # ❌ FAILS: PSOptions not available
  shell: pwsh
```

### ❌ Incorrect: Using artifacts without PSOptions

```yaml
- name: Download Build Artifacts
  uses: actions/download-artifact@v4
  with:
    name: build

- name: Create Packages
  run: |-
    Invoke-CIFinish  # ❌ FAILS: PSOptions not restored
  shell: pwsh
```

## Related Functions

- `Start-PSBuild` - Builds PowerShell and sets PSOptions
- `Save-PSOptions` - Saves PSOptions to a JSON file
- `Restore-PSOptions` - Loads PSOptions from a JSON file
- `Get-PSOptions` - Gets current PSOptions
- `Set-PSOptions` - Sets PSOptions
- `Start-PSPackage` - Creates packages (requires PSOptions)
- `Invoke-CIFinish` - Calls packaging (requires PSOptions on Linux/macOS)

## Examples

### Linux Packaging Action

```yaml
- name: Build and Package
  run: |-
    Import-Module ./tools/ci.psm1
    $releaseTag = Get-ReleaseTag
    Start-PSBuild -Configuration 'Release' -ReleaseTag $releaseTag
    Invoke-CIFinish
  shell: pwsh
```

### Windows Packaging Workflow

```yaml
- name: Build and Package
  run: |
    Import-Module .\tools\ci.psm1
    Invoke-CIFinish -Runtime ${{ matrix.runtimePrefix }}-${{ matrix.architecture }} -channel ${{ matrix.channel }}
  shell: pwsh
```

Note: `Invoke-CIFinish` for Windows includes both build and packaging in its logic when `Stage` contains 'Build'.
