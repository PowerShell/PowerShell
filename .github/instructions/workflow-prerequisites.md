# Workflow Prerequisites for Building PowerShell

## Required Steps Before Start-PSBuild

These steps must run before calling `Start-PSBuild`:

### 1. Checkout

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 1000  # Required for version generation
```

### 2. Setup .NET

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    global-json-file: ./global.json
```

### 3. Bootstrap

```yaml
- name: Bootstrap
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Invoke-CIInstall -SkipUser
    Sync-PSTags -AddRemoteIfMissing
```

## Complete Prerequisites Example

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

## Why Each Step Matters

**Checkout with fetch-depth:**
- Build needs git history for versioning
- Without it: `git describe` fails

**Setup .NET:**
- Provides SDK for building
- Uses version from global.json

**Bootstrap:**
- Installs dependencies
- Syncs git tags
- Prepares build environment

## Optional Steps

### Environment Capture (Debugging)

```yaml
- name: Capture Environment
  run: |
    Get-ChildItem -Path env: | Out-String -width 9999 -Stream | Write-Verbose -Verbose
  shell: pwsh
```
