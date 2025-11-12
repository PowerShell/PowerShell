---
applyTo: ".pipelines/**/*.{yml,yaml}"
---

# OneBranch Pipeline Condition Syntax

## Overview
Azure Pipelines (OneBranch) uses specific syntax for referencing variables and parameters in condition expressions. Using the wrong syntax will cause conditions to fail silently or behave unexpectedly.

## Variable Reference Patterns

### In Condition Expressions

**✅ Correct Pattern:**
```yaml
condition: eq(variables['VariableName'], 'value')
condition: or(eq(variables['VAR1'], 'true'), eq(variables['VAR2'], 'true'))
condition: and(succeeded(), eq(variables['Architecture'], 'fxdependent'))
```

**❌ Incorrect Patterns:**
```yaml
# Don't use $(VAR) string expansion in conditions
condition: eq('$(VariableName)', 'value')

# Don't use direct variable references
condition: eq($VariableName, 'value')
```

### In Script Content (pwsh, bash, etc.)

**✅ Correct Pattern:**
```yaml
- pwsh: |
    $value = '$(VariableName)'
    Write-Host "Value: $(VariableName)"
```

### In Input Fields

**✅ Correct Pattern:**
```yaml
inputs:
  serviceEndpoint: '$(ServiceEndpoint)'
  sbConfigPath: '$(SBConfigPath)'
```

## Parameter References

### Template Parameters (Compile-Time)

**✅ Correct Pattern:**
```yaml
parameters:
  - name: OfficialBuild
    type: boolean
    default: false

steps:
  - task: SomeTask@1
    condition: eq('${{ parameters.OfficialBuild }}', 'true')
```

Note: Parameters use `${{ parameters.Name }}` because they're evaluated at template compile-time.

### Runtime Variables (Execution-Time)

**✅ Correct Pattern:**
```yaml
steps:
  - pwsh: |
      Write-Host "##vso[task.setvariable variable=MyVar]somevalue"
    displayName: Set Variable

  - task: SomeTask@1
    condition: eq(variables['MyVar'], 'somevalue')
```

## Common Scenarios

### Scenario 1: Check if Variable Equals Value

```yaml
- task: DoSomething@1
  condition: eq(variables['PREVIEW'], 'true')
```

### Scenario 2: Multiple Variable Conditions (OR)

```yaml
- task: DoSomething@1
  condition: or(eq(variables['STABLE'], 'true'), eq(variables['LTS'], 'true'))
```

### Scenario 3: Multiple Variable Conditions (AND)

```yaml
- task: DoSomething@1
  condition: and(succeeded(), eq(variables['Architecture'], 'fxdependent'))
```

### Scenario 4: Complex Conditions

```yaml
- task: DoSomething@1
  condition: and(
    succeededOrFailed(),
    ne(variables['UseAzDevOpsFeed'], ''),
    eq(variables['Build.SourceBranch'], 'refs/heads/master')
  )
```

### Scenario 5: Built-in Variables

```yaml
- task: CodeQL3000Init@0
  condition: eq(variables['Build.SourceBranch'], 'refs/heads/master')

- step: finalize
  condition: eq(variables['Agent.JobStatus'], 'SucceededWithIssues')
```

### Scenario 6: Parameter vs Variable

```yaml
parameters:
  - name: OfficialBuild
    type: boolean

steps:
  # Parameter condition (compile-time)
  - task: SignFiles@1
    condition: eq('${{ parameters.OfficialBuild }}', 'true')

  # Variable condition (runtime)
  - task: PublishArtifact@1
    condition: eq(variables['PUBLISH_ENABLED'], 'true')
```

## Why This Matters

**String Expansion `$(VAR)` in Conditions:**
- When you use `'$(VAR)'` in a condition, Azure Pipelines attempts to expand it as a string
- If the variable is undefined or empty, it becomes an empty string `''`
- The condition `eq('', 'true')` will always be false
- This makes debugging difficult because there's no error message

**Variables Array Syntax `variables['VAR']`:**
- This is the proper way to reference runtime variables in conditions
- Azure Pipelines correctly evaluates the variable's value
- Undefined variables are handled properly by the condition evaluator
- This is the standard pattern used throughout Azure Pipelines

## Reference Examples

Working examples can be found in:
- `.pipelines/templates/linux.yml` - Build.SourceBranch conditions
- `.pipelines/templates/windows-hosted-build.yml` - Architecture conditions
- `.pipelines/templates/compliance/apiscan.yml` - CODEQL_ENABLED conditions
- `.pipelines/templates/insert-nuget-config-azfeed.yml` - Complex AND/OR conditions

## Quick Reference Table

| Context | Syntax | Example |
|---------|--------|---------|
| Condition expression | `variables['Name']` | `condition: eq(variables['PREVIEW'], 'true')` |
| Script content | `$(Name)` | `pwsh: Write-Host "$(PREVIEW)"` |
| Task input | `$(Name)` | `inputs: path: '$(Build.SourcesDirectory)'` |
| Template parameter | `${{ parameters.Name }}` | `condition: eq('${{ parameters.Official }}', 'true')` |

## Troubleshooting

### Condition Always False
If your condition is always evaluating to false:
1. Check if you're using `'$(VAR)'` instead of `variables['VAR']`
2. Verify the variable is actually set (add a debug step to print the variable)
3. Check the variable value is exactly what you expect (case-sensitive)

### Variable Not Found
If you get errors about variables not being found:
1. Ensure the variable is set before the condition is evaluated
2. Check that the variable name is spelled correctly
3. Verify the variable is in scope (job vs. stage vs. pipeline level)

## Best Practices

1. **Always use `variables['Name']` in conditions** - This is the correct Azure Pipelines pattern
2. **Use `$(Name)` for string expansion** in scripts and inputs
3. **Use `${{ parameters.Name }}` for template parameters** (compile-time)
4. **Add debug steps** to verify variable values when troubleshooting conditions
5. **Follow existing patterns** in the repository - grep for `condition:` to see examples

## Common Mistakes

❌ **Mistake 1: String expansion in condition**
```yaml
condition: eq('$(PREVIEW)', 'true')  # WRONG
```

✅ **Fix:**
```yaml
condition: eq(variables['PREVIEW'], 'true')  # CORRECT
```

❌ **Mistake 2: Missing quotes around parameter**
```yaml
condition: eq(${{ parameters.Official }}, true)  # WRONG
```

✅ **Fix:**
```yaml
condition: eq('${{ parameters.Official }}', 'true')  # CORRECT
```

❌ **Mistake 3: Mixing syntax**
```yaml
condition: or(eq('$(STABLE)', 'true'), eq(variables['LTS'], 'true'))  # INCONSISTENT
```

✅ **Fix:**
```yaml
condition: or(eq(variables['STABLE'], 'true'), eq(variables['LTS'], 'true'))  # CORRECT
```
