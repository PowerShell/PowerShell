# Cherry-Pick Commits Between Branches

Cherry-pick recent commits from a source branch to a target branch without switching branches.

## Instructions for Copilot

1. **Confirm branches with the user**
   - Ask the user to confirm the source and target branches
   - If different branches are needed, update the configuration

2. **Identify unique commits**
   - Run: `git log <target-branch>..<source-branch> --oneline --reverse`
   - **IMPORTANT**: The commit count may be misleading if branches diverged from different base commits
   - Compare the LAST few commits from each branch to identify actual missing commits:
     - `git log <source-branch> --oneline -10`
     - `git log <target-branch> --oneline -10`
   - Look for commits with the same message but different SHAs (rebased commits)
   - Show the user ONLY the truly missing commits (usually just the most recent ones)

3. **Confirm with user before proceeding**
   - If the commit count seems unusually high (e.g., 400+), STOP and verify semantically
   - Ask: "I found X commits to cherry-pick. Shall I proceed?"
   - If there are many commits, warn that this may take time

4. **Execute the cherry-pick**
   - Ensure the target branch is checked out first
   - Run: `git cherry-pick <sha1>` for single commits
   - Or: `git cherry-pick <sha1> <sha2> <sha3>` for multiple commits
   - Apply commits in chronological order (oldest first)

5. **Handle any issues**
   - If conflicts occur, pause and ask user for guidance
   - If empty commits occur, automatically skip with `git cherry-pick --skip`

6. **Verify and report results**
   - Run: `git log <target-branch> -<number-of-commits> --oneline`
   - Show the user the newly applied commits
   - Confirm the branch is now ahead by X commits

## Key Git Commands

```bash
# Find unique commits (may show full divergence if branches were rebased)
git log <target>..<source> --oneline --reverse

# Compare recent commits on each branch (more reliable for rebased branches)
git log <source-branch> --oneline -10
git log <target-branch> --oneline -10

# Cherry-pick specific commits (when target is checked out)
git cherry-pick <sha1>
git cherry-pick <sha1> <sha2> <sha3>

# Skip empty commits
git cherry-pick --skip

# Verify result
git log <target-branch> -<count> --oneline
```

## Common Scenarios

- **Empty commits**: Automatically skip with `git cherry-pick --skip`
- **Conflicts**: Stop, show files with conflicts, ask user to resolve
- **Many commits**: Warn user and confirm before proceeding
- **Already applied**: These will result in empty commits that should be skipped
- **Diverged branches**: If branches diverged (rebased), `git log` may show the entire history difference
  - The actual missing commits are usually only the most recent ones
  - Compare commit messages from recent history on both branches
  - Cherry-pick only commits that are semantically missing

## Workflow Style

Use an interactive, step-by-step approach:
- Show output from each command
- Ask for confirmation before major actions
- Provide clear status updates
- Handle errors gracefully with user guidance
