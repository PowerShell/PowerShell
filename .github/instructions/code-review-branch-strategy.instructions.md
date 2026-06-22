---
applyTo: "**/*"
---

# Code Review Branch Strategy Guide

This guide helps GitHub Copilot provide appropriate feedback when reviewing code changes, particularly distinguishing between issues that should be fixed in the current branch versus the default branch.

## Purpose

When reviewing pull requests, especially those targeting release branches, it's important to identify whether an issue should be fixed in:
- **The current PR/branch** - Release-specific fixes or backports
- **The default branch first** - General bugs that exist in the main codebase

## Branch Types and Fix Strategy

### Release Branches (e.g., `release/v7.5`, `release/v7.4`)

**Purpose:** Contain release-specific changes and critical backports

**Should contain:**
- Release-specific configuration changes
- Critical bug fixes that are backported from the default branch
- Release packaging/versioning adjustments

**Should NOT contain:**
- New general bug fixes that haven't been fixed in the default branch
- Refactoring or improvements that apply to the main codebase
- Workarounds for issues that exist in the default branch

### Default/Main Branch (e.g., `master`, `main`)

**Purpose:** Primary development branch for all ongoing work

**Should contain:**
- All general bug fixes
- New features and improvements
- Refactoring and code quality improvements
- Fixes that will later be backported to release branches

## Identifying Issues That Belong in the Default Branch

When reviewing a PR targeting a release branch, look for these indicators that suggest the fix should be in the default branch first:

### 1. The Root Cause Exists in Default Branch

If the underlying issue exists in the default branch's code, it should be fixed there first.

**Example:**
```yaml
# PR changes this in release/v7.5:
- $metadata = Get-Content "$repoRoot/tools/metadata.json" -Raw | ConvertFrom-Json
+ $metadata = Get-Content "$(Build.SourcesDirectory)/PowerShell/tools/metadata.json" -Raw | ConvertFrom-Json
```

**Analysis:** If `$repoRoot` is undefined because the template doesn't include its dependencies in BOTH the release branch AND the default branch, the fix should address the root cause in the default branch first.

### 2. The Fix is a Workaround Rather Than a Proper Solution

If the change introduces a workaround (hardcoded paths, special cases) rather than fixing the underlying design issue, it likely belongs in the default branch as a proper fix.

**Example:**
- Using hardcoded paths instead of fixing variable initialization
- Adding special cases instead of fixing the logic
- Duplicating code instead of fixing shared dependencies

### 3. The Issue Affects General Functionality

If the issue affects general functionality not specific to a release, it should be fixed in the default branch.

**Example:**
- Template dependencies that affect all pipelines
- Shared utility functions
- Common configuration issues

## Providing Code Review Feedback

### For Issues in the Current Branch

When an issue is specific to the current branch or is a legitimate fix for the branch being targeted, **use the default code review feedback format** without any special branch-strategy commentary.

### For Issues That Belong in the Default Branch

1. **Provide the code review feedback**
2. **Explain why it should be fixed in the default branch**
3. **Provide an issue template** in markdown format

**Example:**

```markdown
The `channelSelection.yml` template relies on `$repoRoot` being set by `SetVersionVariables.yml`, but doesn't declare this dependency. This issue exists in both the release branch and the default branch.

**This should be fixed in the default branch first**, then backported if needed. The proper fix is to ensure template dependencies are correctly declared, rather than using hardcoded paths as a workaround.

---

**Suggested Issue for Default Branch:**

### Issue Title
`channelSelection.yml` template missing dependency on `SetVersionVariables.yml`

### Description
The `channelSelection.yml` template uses the `$repoRoot` variable but doesn't ensure it's set beforehand by including `SetVersionVariables.yml`.

**Current State:**
- `channelSelection.yml` expects `$repoRoot` to be available
- Not all pipelines that use `channelSelection.yml` include `SetVersionVariables.yml` first
- This creates an implicit dependency that's not enforced

**Expected State:**
Either:
1. `channelSelection.yml` should include `SetVersionVariables.yml` as a dependency, OR
2. `channelSelection.yml` should be refactored to not depend on `$repoRoot`, OR
3. Pipelines using `channelSelection.yml` should explicitly include `SetVersionVariables.yml` first

**Files Affected:**
- `.pipelines/templates/channelSelection.yml`
- `.pipelines/templates/package-create-msix.yml`
- `.pipelines/templates/release-SetTagAndChangelog.yml`

**Priority:** Medium
**Labels:** `Issue-Bug`, `Area-Build`, `Area-Pipeline`
```

## Issue Template Format

When creating an issue template for the default branch, use this structure:

```markdown
### Issue Title
[Clear, concise description of the problem]

### Description
[Detailed explanation of the issue]

**Current State:**
- [What's happening now]
- [Why it's problematic]

**Expected State:**
- [What should happen]
- [Proposed solution(s)]

**Files Affected:**
- [List of files]

**Priority:** [Low/Medium/High/Critical]
**Labels:** [Suggested labels like `Issue-Bug`, `Area-*`]

**Additional Context:**
[Any additional information, links to related issues, etc.]
```

## Common Scenarios

### Scenario 1: Template Dependency Issues

**Indicators:**
- Missing template includes
- Undefined variables from other templates
- Assumptions about pipeline execution order

**Action:** Suggest fixing template dependencies in the default branch.

### Scenario 2: Hardcoded Values

**Indicators:**
- Hardcoded paths replacing variables
- Environment-specific values in shared code
- Magic strings or numbers

**Action:** Suggest proper variable/parameter usage in the default branch.

### Scenario 3: Logic Errors

**Indicators:**
- Incorrect conditional logic
- Missing error handling
- Race conditions

**Action:** Suggest fixing the logic in the default branch unless it's release-specific.

### Scenario 4: Legitimate Release Branch Fixes

**Indicators:**
- Version-specific configuration
- Release packaging changes
- Backport of already-fixed default branch issue

**Action:** Provide normal code review feedback for the current PR.

## Best Practices

1. **Always check if the issue exists in the default branch** before suggesting a release-branch-only fix
2. **Prefer fixing root causes over workarounds**
3. **Provide clear rationale** for why a fix belongs in the default branch
4. **Include actionable issue templates** so users can easily create issues
5. **Be helpful, not blocking** - provide the feedback even if you can't enforce where it's fixed

## Examples of Good vs. Bad Approaches

### ❌ Bad: Workaround in Release Branch Only

```yaml
# In release/v7.5 only
- pwsh: |
    $metadata = Get-Content "$(Build.SourcesDirectory)/PowerShell/tools/metadata.json" -Raw
```

**Why bad:** Hardcodes path to work around missing `$repoRoot`, doesn't fix the default branch.

### ✅ Good: Fix in Default Branch, Then Backport

```yaml
# In default branch first
- template: SetVersionVariables.yml@self  # Ensures $repoRoot is set
- template: channelSelection.yml@self     # Now can use $repoRoot
```

**Why good:** Fixes the root cause by ensuring dependencies are declared, then backport to release if needed.

## When in Doubt

If you're unsure whether an issue should be fixed in the current branch or the default branch, ask yourself:

1. Does this issue exist in the default branch?
2. Is this a workaround or a proper fix?
3. Will other branches/releases benefit from this fix?

If the answer to any of these is "yes," suggest fixing it in the default branch first.
