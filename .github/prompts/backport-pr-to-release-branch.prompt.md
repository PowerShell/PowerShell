---
description: Guide for backporting changes to PowerShell release branches
---

# Backport a Change to a PowerShell Release Branch

## 1 — Goal

Create a backport PR that applies changes from a merged PR to a release branch (e.g., `release/v7.4`, `release/v7.5`). The backport must follow the repository's established format and include proper references to the original PR.

## 2 — Prerequisites for the model

- You have full repository access
- You can run git commands
- You can read PR information from the repository
- Ask clarifying questions if the target release branch or original PR number is unclear

## 3 — Required user inputs

If the user hasn't specified a PR number, help them find one:

### Finding PRs that need backporting

1. Ask the user which release version they want to backport to (e.g., `7.4`, `7.5`)
2. Search for PRs with the appropriate label using GitHub CLI:

```powershell
$Owner = "PowerShell"
$Repo = "PowerShell"
$version = "7.4"  # or user-specified version
$considerLabel = "Backport-$version.x-Consider"

$prsJson = gh pr list --repo "$Owner/$Repo" --label $considerLabel --state merged --json number,title,url,labels,mergedAt --limit 100 2>&1
$prs = $prsJson | ConvertFrom-Json
# Sort PRs from oldest merged to newest merged
$prs = $prs | Sort-Object mergedAt
```

3. Present the list of PRs to the user with:
   - PR number
   - PR title
   - Merged date
   - URL

4. Ask the user: "Which PR would you like to backport?" (provide the PR number)

### After selecting a PR

Once the user selects a PR (or if they provided one initially), confirm:
- **Original PR number**: The PR number that was merged to the main branch (e.g., 26193)
- **Target release**: The release number (e.g., `7.4`, `7.5`, `7.5.1`)

Example: "Backport PR 26193 to release/v7.4"

## 4 — Implementation steps (must be completed in order)

### Step 1: Verify the original PR exists and is merged

1. Fetch the original PR information using the PR number
2. Confirm the PR state is `MERGED`
3. Extract the following information:
   - Merge commit SHA
   - Original PR title
   - Original PR author
   - Original CL label (if present, typically starts with `CL-`)

If the PR is not merged, stop and inform the user.

4. Check if backport already exists or has been attempted:
   ```powershell
   gh pr list --repo PowerShell/PowerShell --search "in:title [release/v7.4] <original-title>" --state all
   ```

   If a backport PR already exists, inform the user and ask if they want to continue.

5. Check backport labels to understand status:
   - `Backport-7.4.x-Migrated`: Indicates previous backport attempt (may have failed or had issues)
   - `Backport-7.4.x-Done`: Already backported successfully
   - `Backport-7.4.x-Approved`: Ready for backporting
   - `Backport-7.4.x-Consider`: Under consideration for backporting

   If status is "Done", inform the user that backport may already be complete.

### Step 2: Create the backport branch

1. Identify the correct remote to fetch from:
   ```bash
   git remote -v
   ```

   Look for the remote that points to `https://github.com/PowerShell/PowerShell` (typically named `upstream` or `origin`). Use this remote name in subsequent commands.

2. Ensure you have the latest changes from the target release branch:
   ```bash
   git fetch <remote-name> <target-release-branch>
   ```

   Example: `git fetch upstream release/v7.4`

3. Create a new branch from the target release branch:
   ```bash
   git checkout -b backport-<pr-number> <remote-name>/<target-release-branch>
   ```

   Example: `git checkout -b backport-26193 upstream/release/v7.4`

### Step 3: Cherry-pick the merge commit

1. Cherry-pick the merge commit from the original PR:
   ```bash
   git cherry-pick <merge-commit-sha>
   ```

2. If conflicts occur:
   - Inform the user about the conflicts
   - List the conflicting files
   - Fetch the original PR diff to understand the changes:
     ```bash
     gh pr diff <pr-number> --repo PowerShell/PowerShell | Out-File pr-diff.txt
     ```
   - Review the diff to understand what the PR changed
   - Figure out why there is a conflict and resolve it
   - Create a summary of the conflict resolution:
     * Which files had conflicts
     * Nature of each conflict (parameter changes, code removal, etc.)
     * How you resolved it
     * Whether any manual adjustments were needed beyond accepting one side
   - Ask the user to review your conflict resolution summary before continuing
   - After conflicts are resolved, continue with:
     ```bash
     git add <resolved-files>
     git cherry-pick --continue
     ```

### Step 4: Push the backport branch

Push to your fork (typically the remote that you have write access to):

```bash
git push <your-fork-remote> backport-<pr-number>
```

Example: `git push origin backport-26193`

Note: If you're pushing to the official PowerShell repository and have permissions, you may push to `upstream` or the appropriate remote.

### Step 5: Create the backport PR

Create a new PR with the following format:

**Title:**
```
[<target-release-branch>] <original-pr-title>
```

Example: `[release/v7.4] GitHub Workflow cleanup`

**Body:**
```
Backport of #<original-pr-number> to <target-release-branch>

<!--
DO NOT MODIFY THIS COMMENT. IT IS AUTO-GENERATED.
$$$originalprnumber:<original-pr-number>$$$
-->

Triggered by @<current-user> on behalf of @<original-author>

Original CL Label: <original-cl-label>

/cc @PowerShell/powershell-maintainers

## Impact

Choose either tooling or Customer impact.
### Tooling Impact

- [ ] Required tooling change
- [ ] Optional tooling change (include reasoning)

### Customer Impact

- [ ] Customer reported
- [ ] Found internally

[Select one or both of the boxes. Describe how this issue impacts customers, citing the expected and actual behaviors and scope of the issue. If customer-reported, provide the issue number.]

## Regression

- [ ] Yes
- [ ] No

[If yes, specify when the regression was introduced. Provide the PR or commit if known.]

## Testing

[How was the fix verified? How was the issue missed previously? What tests were added?]

## Risk

- [ ] High
- [ ] Medium
- [ ] Low

[High/Medium/Low. Justify the indication by mentioning how risks were measured and addressed.]
```

**Base branch:** `<target-release-branch>` (e.g., `release/v7.4`)

**Head branch:** `backport-<pr-number>` (e.g., `backport-26193`)

#### Guidelines for Filling Out the PR Body

**For Impact Section**:
- If the original PR changed build/tooling/packaging, select "Tooling Impact"
- If it fixes a user-facing bug or changes user-visible behavior, select "Customer Impact"
- Copy relevant context from the original PR description
- Be specific about what changed and why

**For Regression Section**:
- Mark "Yes" only if the original PR fixed a regression
- Include when the regression was introduced if known

**For Testing Section**:
- Reference the original PR's testing approach
- Note any additional backport-specific testing needed
- Mention if manual testing was done to verify the backport

**For Risk Assessment**:
- **High**: Changes core functionality, packaging, build systems, or security-related code
- **Medium**: Changes non-critical features, adds new functionality, or modifies existing behavior
- **Low**: Documentation, test-only changes, minor refactoring, or fixes with narrow scope
- Justify your assessment based on the scope of changes and potential impact
- **For CI/CD changes**: When backporting CI/CD infrastructure changes (workflows, build scripts, packaging), note in your justification that not taking these changes may create technical debt and make it difficult to apply future CI/CD changes that build on top of them. This doesn't change the risk level itself, but provides important context for why the change should be taken despite potentially higher risk

**If there were merge conflicts**:
Add a note in the PR description after the Risk section describing what conflicts occurred and how they were resolved.

### Step 6: Add the CL label to the backport PR

After creating the backport PR, add the same changelog label (CL-*) from the original PR to the backport PR:

```bash
gh pr edit <backport-pr-number> --repo PowerShell/PowerShell --add-label "<original-cl-label>"
```

Example: `gh pr edit 26389 --repo PowerShell/PowerShell --add-label "CL-BuildPackaging"`

This ensures the backport is properly categorized in the changelog for the release branch.

### Step 7: Update the original PR's backport labels

After successfully creating the backport PR, update the original PR to reflect that it has been backported:

```bash
gh pr edit <original-pr-number> --repo PowerShell/PowerShell --add-label "Backport-<version>.x-Migrated" --remove-label "Backport-<version>.x-Consider"
```

Example: `gh pr edit 26193 --repo PowerShell/PowerShell --add-label "Backport-7.5.x-Migrated" --remove-label "Backport-7.5.x-Consider"`

Notes:
- If the original PR had `Backport-<version>.x-Approved` instead of `Consider`, remove that label
- This step helps track which PRs have been successfully backported
- The `Migrated` label indicates the backport PR has been created (not necessarily merged)
- The `Done` label should only be added once the backport PR is merged

### Step 8: Clean up temporary files

After successful PR creation and labeling, clean up any temporary files created during the process:

```powershell
Remove-Item pr*.diff -ErrorAction SilentlyContinue
```

## 5 — Definition of Done (self-check list)

- [ ] Original PR is verified as merged
- [ ] Checked for existing backport PRs
- [ ] Reviewed backport labels to understand status
- [ ] Backport branch created from correct release branch
- [ ] Merge commit cherry-picked successfully (or conflicts resolved)
- [ ] If conflicts occurred, provided resolution summary to user
- [ ] Branch pushed to origin
- [ ] PR created with correct title format: `[<release-branch>] <original-title>`
- [ ] CL label added to backport PR (matching original PR's CL label)
- [ ] Original PR labels updated (added Migrated, removed Consider/Approved)
- [ ] Temporary files cleaned up (pr*.diff)
- [ ] PR body includes:
  - [ ] Backport reference: `Backport of (PR-number) to <release-branch>`
  - [ ] Auto-generated comment with original PR number
  - [ ] Triggered by and original author attribution
  - [ ] Original CL label (if available)
  - [ ] CC to PowerShell maintainers
  - [ ] Impact section filled out
  - [ ] Regression section filled out
  - [ ] Testing section filled out
  - [ ] Risk section filled out
- [ ] Base branch set to target release branch
- [ ] No unrelated changes included

## 6 — Branch naming convention

**Format:** `backport/release/<version>/pr/<pr-number>`

Examples:
- `backport/release/v7.5/pr/26193`
- `backport/release/v7.4.1/pr/26334`

Note: Automated bot uses format `backport/release/v<version>/<pr-number>-<commit-hash>`, but manual backports should use the format `backport/release/<version>/pr/<pr-number>` as shown above.

## 7 — Example backport PR

Reference PR 26334 as the canonical example of a correct backport:

**Original PR**: PR 26193 "GitHub Workflow cleanup"

**Backport PR**: PR 26334 "[release/v7.4] GitHub Workflow cleanup"
- **Title**: `[release/v7.4] GitHub Workflow cleanup`
- **Body**: Started with backport reference to original PR and release branch
- **Branch**: `backport/release/v7.4/26193-4aff02475` (bot-created)
- **Base**: `release/v7.4`
- **Includes**: Auto-generated metadata, impact assessment, regression info, testing details, and risk level

## 8 — Backport label system (for context)

Backport labels follow pattern: `Backport-<version>.x-<state>`

**Triage states:**
- `Consider` - Under review for backporting
- `Approved` - Approved and ready to be backported
- `Done` - Backport completed

**Examples:** `Backport-7.4.x-Approved`, `Backport-7.5.x-Consider`, `Backport-7.3.x-Done`

Note: The PowerShell repository has an automated bot (pwshBot) that creates backport PRs automatically when a merged PR has a backport approval label. Manual backports follow the same format.

## Manual Backport Using PowerShell Tools

For situations where automated backports fail or manual intervention is needed, use the `Invoke-PRBackport` function from `tools/releaseTools.psm1`.

### Prerequisites

1. **GitHub CLI**: Install from https://cli.github.com/
   - Required version: 2.17 or later
   - Authenticate with `gh auth login`

2. **Upstream Remote**: Configure a Git remote named `upstream` pointing to `PowerShell/PowerShell`:
   ```powershell
   git remote add upstream https://github.com/PowerShell/PowerShell.git
   ```

### Using Invoke-PRBackport

```powershell
# Import the release tools module
Import-Module ./tools/releaseTools.psm1

# Backport a single PR
Invoke-PRBackport -PrNumber 26193 -Target release/v7.4.1

# Backport with custom branch postfix
Invoke-PRBackport -PrNumber 26193 -Target release/v7.4.1 -BranchPostFix "retry"

# Overwrite existing local branch if it exists
Invoke-PRBackport -PrNumber 26193 -Target release/v7.4.1 -Overwrite
```

### Parameters

- **PrNumber** (Required): The PR number to backport
- **Target** (Required): Target release branch (must match pattern `release/v\d+\.\d+(\.\d+)?`)
- **Overwrite**: Switch to overwrite local branch if it already exists
- **BranchPostFix**: Add a postfix to the branch name (e.g., for retry attempts)
- **UpstreamRemote**: Name of the upstream remote (default: `upstream`)

### How It Works

1. Verifies the PR is merged
2. Fetches the target release branch from upstream
3. Creates a new branch: `backport-<pr-number>[-<postfix>]`
4. Cherry-picks the merge commit
5. If conflicts occur, prompts you to resolve them
6. Creates the backport PR using GitHub CLI

## Handling Merge Conflicts

When cherry-picking fails due to conflicts:

1. The script will pause and prompt you to fix conflicts
2. Resolve conflicts in your editor:
   ```powershell
   # Check which files have conflicts
   git status

   # Edit files to resolve conflicts
   # After resolving, stage the changes
   git add <resolved-files>

   # Continue the cherry-pick
   git cherry-pick --continue
   ```
3. Type 'Yes<enter>' when prompted to continue the script
4. The script will create the PR

### Understanding Conflict Patterns

When resolving conflicts during backports, follow this approach:

1. **Analyze the diff first**: Before resolving conflicts, fetch and review the original PR's diff to understand what changed:
   ```powershell
   gh pr diff <pr-number> --repo PowerShell/PowerShell | Out-File pr-diff.txt
   ```

2. **Identify conflict types**:
   - **Parameter additions**: New parameters added to functions (e.g., ValidateSet values)
   - **Code removal**: Features removed in main but still exist in release branch
   - **Code additions**: New code blocks that don't exist in release branch
   - **Refactoring conflicts**: Code structure changes between branches

3. **Resolution priorities**:
   - Preserve the intent of the backported change
   - Keep release branch-specific code that doesn't conflict with the fix
   - When in doubt, favor the incoming change from the backport
   - Document significant manual changes in the PR description

4. **Verification**:
   - After resolving conflicts, verify the file compiles/runs
   - Check that the resolved code matches the original PR's intent
   - Look for orphaned code that references removed functions

5. **Create a conflict resolution summary**:
   - List which files had conflicts
   - Briefly explain the nature of each conflict
   - Describe how you resolved it
   - Ask user to review the resolution before continuing

### Context-Aware Conflict Resolution

**Key Principle**: The release branch may have different code than main. Your goal is to apply the *change* from the PR, not necessarily make the code identical to main.

**Common Scenarios**:
1. **Function parameters differ**: If the release branch has fewer parameters than main, and the backport adds functionality unrelated to new parameters, keep the release branch parameters unless the new parameters are part of the fix
2. **Dependencies removed in main**: If main removed a dependency but the release branch still has it, and the backport is unrelated to that dependency, keep the release branch code
3. **New features in main**: If main has new features not in the release, focus on backporting only the specific fix, not the new features

## Bulk Backporting Approved PRs

To backport all PRs labeled as approved for a specific version:

```powershell
Import-Module ./tools/releaseTools.psm1

# Backport all approved PRs for version 7.2.12
Invoke-PRBackportApproved -Version 7.2.12
```

This function:
1. Queries all merged PRs with the `Backport-<version>.x-Approved` label
2. Attempts to backport each PR in order of merge date
3. Creates individual backport PRs for each

## Viewing Backport Reports

Get a list of PRs that need backporting:

```powershell
Import-Module ./tools/releaseTools.psm1

# List all approved backports for 7.4
Get-PRBackportReport -Version 7.4 -TriageState Approved

# Open all approved backports in browser
Get-PRBackportReport -Version 7.4 -TriageState Approved -Web

# Check which backports are done
Get-PRBackportReport -Version 7.4 -TriageState Done
```

## Branch Naming Conventions

### Automated Bot Branches
Format: `backport/release/v<version>/<pr-number>-<short-commit-hash>`

Example: `backport/release/v7.4/26193-4aff02475`

### Manual Backport Branches
Format: `backport-<pr-number>[-<postfix>]`

Examples:
- `backport-26193`
- `backport-26193-retry`

## PR Title and Description Format

### Title
Format: `[release/v<version>] <original-title>`

Example: `[release/v7.4] GitHub Workflow cleanup`

### Description
The backport PR description includes:
- Reference to original PR number
- Target release branch
- Auto-generated comment with original PR metadata
- Maintainer information
- Original CL label
- CC to PowerShell maintainers team

Example description structure:
```text
Backport of (original-pr-number) to release/v<version>

<!--
DO NOT MODIFY THIS COMMENT. IT IS AUTO-GENERATED.
$$$originalprnumber:<original-pr-number>$$$
-->

Triggered by @<maintainer> on behalf of @<original-author>

Original CL Label: <original-label>

/cc @PowerShell/powershell-maintainers
```

## Best Practices

1. **Verify PR is merged**: Only backport merged PRs
2. **Test backports**: Ensure backported changes work in the target release context
3. **Check for conflicts early**: Large PRs are more likely to have conflicts
4. **Use appropriate labels**: Apply correct version and triage state labels
5. **Document special cases**: If manual changes were needed, note them in the PR description
6. **Follow up on CI failures**: Backports should pass all CI checks before merging

## Troubleshooting

### "PR is not merged" Error
**Cause**: Attempting to backport a PR that hasn't been merged yet
**Solution**: Wait for the PR to be merged to the main branch first

### "Please create an upstream remote" Error
**Cause**: No upstream remote configured
**Solution**:
```powershell
git remote add upstream https://github.com/PowerShell/PowerShell.git
git fetch upstream
```

### "GitHub CLI is not installed" Error
**Cause**: gh CLI not found in PATH
**Solution**: Install from https://cli.github.com/ and restart terminal

### Cherry-pick Conflicts
**Cause**: Changes conflict with the target branch
**Solution**: Manually resolve conflicts, stage files, and continue cherry-pick

### "Commit does not exist" Error
**Cause**: Local Git doesn't have the commit
**Solution**:
```powershell
git fetch upstream
```

## Related Resources

- **Release Process**: See `docs/maintainers/releasing.md`
- **Release Tools**: See `tools/releaseTools.psm1`
- **Issue Management**: See `docs/maintainers/issue-management.md`
