# Merge Conflict Checker

This composite GitHub Action checks for Git merge conflict markers in files changed in pull requests.

## Purpose

Automatically detects leftover merge conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`) in pull request files to prevent them from being merged into the codebase.

## Usage

### In a Workflow

```yaml
- name: Check for merge conflict markers
  uses: "./.github/actions/infrastructure/merge-conflict-checker"
```

### Complete Example

```yaml
jobs:
  merge_conflict_check:
    name: Check for Merge Conflict Markers
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    permissions:
      pull-requests: read
      contents: read
    steps:
      - name: checkout
        uses: actions/checkout@v5

      - name: Check for merge conflict markers
        uses: "./.github/actions/infrastructure/merge-conflict-checker"
```

## How It Works

1. **File Detection**: Uses GitHub's API to get the list of files changed in the pull request
2. **Marker Scanning**: Reads each changed file and searches for the following markers:
   - `<<<<<<<` (conflict start marker)
   - `=======` (conflict separator)
   - `>>>>>>>` (conflict end marker)
3. **Result Reporting**: 
   - If markers are found, the action fails and lists all affected files
   - If no markers are found, the action succeeds

## Outputs

- `files-checked`: Number of files that were checked
- `conflicts-found`: Number of files containing merge conflict markers

## Behavior

- **Event Support**: Only works with `pull_request` events
- **File Handling**:
  - Checks only files that were added, modified, or renamed
  - Skips deleted files
  - Skips binary/unreadable files
  - Skips directories

## Example Output

When conflict markers are detected:

```
❌ Merge conflict markers detected in the following files:
  - src/example.cs
    Markers found: <<<<<<<, =======, >>>>>>>
  - README.md
    Markers found: <<<<<<<, =======, >>>>>>>

Please resolve these conflicts before merging.
```

When no markers are found:

```
✅ No merge conflict markers found
```

## Integration

This action is integrated into the `linux-ci.yml` workflow and runs automatically on all pull requests to ensure code quality before merging.
