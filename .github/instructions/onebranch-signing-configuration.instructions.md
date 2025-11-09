---
applyTo:
  - ".pipelines/**/*.yml"
  - ".pipelines/**/*.yaml"
---

# OneBranch Signing Configuration

This guide explains how to configure OneBranch signing variables in Azure Pipeline jobs, particularly when signing is not required.

## Purpose

OneBranch pipelines include signing infrastructure by default. For build-only jobs where signing happens in a separate stage, you should disable signing setup to improve performance and avoid unnecessary overhead.

## Disable Signing for Build-Only Jobs

When a job does not perform signing (e.g., it only builds artifacts that will be signed in a later stage), disable both signing setup and code sign validation:

```yaml
variables:
  - name: ob_signing_setup_enabled
    value: false  # Disable signing setup - this is a build-only stage
  - name: ob_sdl_codeSignValidation_enabled
    value: false  # Skip signing validation in build-only stage
```

### Why Disable These Variables?

**`ob_signing_setup_enabled: false`**
- Prevents OneBranch from setting up the signing infrastructure
- Reduces job startup time
- Avoids unnecessary credential validation
- Only disable when the job will NOT sign any artifacts

**`ob_sdl_codeSignValidation_enabled: false`**
- Skips validation that checks if files are properly signed
- Appropriate for build stages where artifacts are unsigned
- Must be enabled in signing/release stages to validate signatures

## Common Patterns

### Build-Only Job (No Signing)

```yaml
jobs:
- job: build_artifacts
  variables:
    - name: ob_signing_setup_enabled
      value: false
    - name: ob_sdl_codeSignValidation_enabled
      value: false
  steps:
    - checkout: self
    - pwsh: |
        # Build unsigned artifacts
        Start-PSBuild
```

### Signing Job

```yaml
jobs:
- job: sign_artifacts
  variables:
    - name: ob_signing_setup_enabled
      value: true
    - name: ob_sdl_codeSignValidation_enabled
      value: true
  steps:
    - checkout: self
      env:
        ob_restore_phase: true
    - pwsh: |
        # Sign artifacts
```

## Related Variables

Other OneBranch signing-related variables:

- `ob_sdl_binskim_enabled`: Controls BinSkim security analysis (can be false in build-only, true in signing stages)

## Best Practices

1. **Separate build and signing stages**: Build artifacts in one job, sign in another
2. **Disable signing in build stages**: Improves performance and clarifies intent
3. **Always validate after signing**: Enable validation in signing stages to catch issues
4. **Document the reason**: Add comments explaining why signing is disabled

## Example: Split Build and Sign Pipeline

```yaml
stages:
  - stage: Build
    jobs:
    - job: build_windows
      variables:
        - name: ob_signing_setup_enabled
          value: false  # Build-only, no signing
        - name: ob_sdl_codeSignValidation_enabled
          value: false  # Artifacts are unsigned
      steps:
        - template: templates/build-unsigned.yml

  - stage: Sign
    dependsOn: Build
    jobs:
    - job: sign_windows
      variables:
        - name: ob_signing_setup_enabled
          value: true   # Enable signing infrastructure
        - name: ob_sdl_codeSignValidation_enabled
          value: true   # Validate signatures
      steps:
        - template: templates/sign-artifacts.yml
```

## Troubleshooting

**Job fails with signing-related errors but signing is disabled:**
- Verify `ob_signing_setup_enabled: false` is set in variables
- Check that no template is overriding the setting
- Ensure `ob_sdl_codeSignValidation_enabled: false` is also set

**Signed artifacts fail validation:**
- Confirm `ob_sdl_codeSignValidation_enabled: true` in signing job
- Verify signing actually occurred
- Check certificate configuration

## Reference

- PowerShell signing templates: `.pipelines/templates/packaging/windows/sign.yml`
