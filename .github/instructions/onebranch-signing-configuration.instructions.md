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
        ob_restore_phase: true  # Steps before first signing operation
    - pwsh: |
        # Prepare artifacts for signing
      env:
        ob_restore_phase: true  # Steps before first signing operation
    - task: onebranch.pipeline.signing@1
      displayName: 'Sign artifacts'
      # Signing step runs in build phase (no ob_restore_phase)
    - pwsh: |
        # Post-signing validation
      # Post-signing steps run in build phase (no ob_restore_phase)
```

## Restore Phase Usage with Signing

**The restore phase (`ob_restore_phase: true`) should only be used in jobs that perform signing operations.** It separates preparation steps from the actual signing and build steps.

### When to Use Restore Phase

Use `ob_restore_phase: true` **only** in jobs where `ob_signing_setup_enabled: true`:

```yaml
jobs:
- job: sign_artifacts
  variables:
    - name: ob_signing_setup_enabled
      value: true  # Signing enabled
  steps:
    # Steps BEFORE first signing operation: use restore phase
    - checkout: self
      env:
        ob_restore_phase: true
    - template: prepare-for-signing.yml
      parameters:
        ob_restore_phase: true

    # SIGNING STEP: runs in build phase (no ob_restore_phase)
    - task: onebranch.pipeline.signing@1
      displayName: 'Sign artifacts'

    # Steps AFTER signing: run in build phase (no ob_restore_phase)
    - pwsh: |
        # Validation or packaging
```

### When NOT to Use Restore Phase

**Do not use restore phase in build-only jobs** where `ob_signing_setup_enabled: false`:

```yaml
jobs:
- job: build_artifacts
  variables:
    - name: ob_signing_setup_enabled
      value: false  # No signing
    - name: ob_sdl_codeSignValidation_enabled
      value: false
  steps:
    - checkout: self
      # NO ob_restore_phase - not needed without signing
    - pwsh: |
        Start-PSBuild
```

**Why?** The restore phase is part of OneBranch's signing infrastructure. Using it without signing enabled adds unnecessary overhead without benefit.

## Related Variables

Other OneBranch signing-related variables:

- `ob_sdl_binskim_enabled`: Controls BinSkim security analysis (can be false in build-only, true in signing stages)

## Best Practices

1. **Separate build and signing stages**: Build artifacts in one job, sign in another
2. **Disable signing in build stages**: Improves performance and clarifies intent
3. **Only use restore phase with signing**: The restore phase should only be used in jobs where signing is enabled (`ob_signing_setup_enabled: true`)
4. **Restore phase before first signing step**: All steps before the first signing operation should use `ob_restore_phase: true`
5. **Always validate after signing**: Enable validation in signing stages to catch issues
6. **Document the reason**: Add comments explaining why signing is disabled or why restore phase is used

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
