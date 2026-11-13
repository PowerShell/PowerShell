---
applyTo:
  - "build.psm1"
  - "tools/ci.psm1"
  - ".github/**/*.yml"
  - ".github/**/*.yaml"
---

# Troubleshooting Build Issues

## Git Describe Error

**Error:**
```
error MSB3073: The command "git describe --abbrev=60 --long" exited with code 128.
```

**Cause:** Insufficient git history (shallow clone)

**Solution:** Add `fetch-depth: 1000` to checkout step

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 1000
```

## Version Information Incorrect

**Symptom:** Build produces wrong version numbers

**Cause:** Git tags not synchronized

**Solution:** Run `Sync-PSTags -AddRemoteIfMissing`:

```yaml
- name: Bootstrap
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Invoke-CIInstall -SkipUser
    Sync-PSTags -AddRemoteIfMissing
```

## PowerShell Binary Not Built

**Error:**
```
Exception: CoreCLR pwsh.exe was not built
```

**Causes:**
1. Build failed (check logs)
2. Wrong configuration used
3. Build output location incorrect

**Solutions:**
1. Check build logs for errors
2. Verify correct configuration for use case
3. Use default parameters: `Start-PSBuild`

## Module Restore Issues

**Symptom:** Slow build or module restore failures

**Causes:**
- Network issues
- Module cache problems
- Package source unavailable

**Solutions:**
1. Retry the build
2. Check network connectivity
3. Use `-NoPSModuleRestore` if modules not needed
4. Clear package cache if persistent

## .NET SDK Not Found

**Symptom:** Build can't find .NET SDK

**Solution:** Ensure .NET setup step runs first:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    global-json-file: ./global.json
```

## Bootstrap Failures

**Symptom:** Invoke-CIInstall fails

**Causes:**
- Missing dependencies
- Network issues
- Platform-specific requirements not met

**Solution:** Check prerequisites for your platform in build system docs
