---
applyTo: ".pipelines/**/*.{yml,yaml}"
---

# OneBranch Restore Phase Pattern

## Overview
When steps need to run in the OneBranch restore phase (before the main build phase), the `ob_restore_phase` environment variable must be set in the `env:` block of **each individual step**.

## Pattern

### ✅ Correct (Working Pattern)
```yaml
parameters:
- name: "ob_restore_phase"
  type: boolean
  default: true  # or false if you don't want restore phase

steps:
- powershell: |
    # script content
  displayName: 'Step Name'
  env:
    ob_restore_phase: ${{ parameters.ob_restore_phase }}
```

The key is to:
1. Define `ob_restore_phase` as a **boolean** parameter
2. Set `ob_restore_phase: ${{ parameters.ob_restore_phase }}` directly in each step's `env:` block
3. Pass `true` to run in restore phase, `false` to run in normal build phase

### ❌ Incorrect (Does Not Work)
```yaml
steps:
- powershell: |
    # script content
  displayName: 'Step Name'
  ${{ if eq(parameters.useRestorePhase, 'yes') }}:
    env:
      ob_restore_phase: true
```

Using conditionals at the same indentation level as `env:` causes only the first step to execute in restore phase.

## Parameters

Templates using this pattern should accept an `ob_restore_phase` boolean parameter:

```yaml
parameters:
- name: "ob_restore_phase"
  type: boolean
  default: true  # Set to true to run in restore phase by default
```

## Reference Examples

Working examples of this pattern can be found in:
- `.pipelines/templates/insert-nuget-config-azfeed.yml` - Demonstrates the correct pattern
- `.pipelines/templates/SetVersionVariables.yml` - Updated to use this pattern

## Why This Matters

The restore phase in OneBranch pipelines runs before signing and other build operations. Steps that need to:
- Set environment variables for the entire build
- Configure authentication
- Prepare the repository structure

Must run in the restore phase to be available when subsequent stages execute.

## Common Use Cases

- Setting `REPOROOT` variable
- Configuring NuGet feeds with authentication
- Setting version variables
- Repository preparation and validation

## Troubleshooting

If only the first step in your template is running in restore phase:
1. Check that `env:` block exists for **each step**
2. Verify the conditional `${{ if ... }}:` is **inside** the `env:` block
3. Confirm indentation is correct (conditional is indented under `env:`)
