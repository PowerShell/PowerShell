# Git Requirements for Building PowerShell

## Fetch Depth

**Required:** `fetch-depth: 1000`

The PowerShell build process uses `git describe --abbrev=60 --long` to generate version information. This requires access to git history and tags.

### Problem

Without sufficient fetch depth, builds fail with:
```
error MSB3073: The command "git describe --abbrev=60 --long" exited with code 128.
```

### Solution

Always use `fetch-depth: 1000` in the checkout step:

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 1000
```

## Tag Synchronization

**Required:** `Sync-PSTags -AddRemoteIfMissing`

The build process needs git tags to properly version the build.

### Problem

Without tag synchronization:
- Version information is incorrect
- Build versioning fails

### Solution

Include tag synchronization in the bootstrap step:

```yaml
- name: Bootstrap
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Sync-PSTags -AddRemoteIfMissing
```

## Complete Example

```yaml
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
```
