# Get Changed Files Action

A reusable composite action that retrieves the list of files changed in a pull request or push event.

## Features

- Supports both `pull_request` and `push` events
- Optional filtering by file pattern
- Returns files as JSON array for easy consumption
- Filters out deleted files (only returns added, modified, or renamed files)
- Handles up to 100 changed files per request

## Usage

### Basic Usage (Pull Requests Only)

```yaml
- name: Get changed files
  id: changed-files
  uses: "./.github/actions/infrastructure/get-changed-files"

- name: Process files
  run: |
    echo "Changed files: ${{ steps.changed-files.outputs.files }}"
    echo "Count: ${{ steps.changed-files.outputs.count }}"
```

### With Filtering

```yaml
# Get only markdown files
- name: Get changed markdown files
  id: changed-md
  uses: "./.github/actions/infrastructure/get-changed-files"
  with:
    filter: '*.md'

# Get only GitHub workflow/action files
- name: Get changed GitHub files
  id: changed-github
  uses: "./.github/actions/infrastructure/get-changed-files"
  with:
    filter: '.github/'
```

### Support Both PR and Push Events

```yaml
- name: Get changed files
  id: changed-files
  uses: "./.github/actions/infrastructure/get-changed-files"
  with:
    event-types: 'pull_request,push'
```

## Inputs

| Name | Description | Required | Default |
|------|-------------|----------|---------|
| `filter` | Optional filter pattern (e.g., `*.md` for markdown files, `.github/` for GitHub files) | No | `''` |
| `event-types` | Comma-separated list of event types to support (`pull_request`, `push`) | No | `pull_request` |

## Outputs

| Name | Description |
|------|-------------|
| `files` | JSON array of changed file paths |
| `count` | Number of changed files |

## Filter Patterns

The action supports simple filter patterns:

- **Extension matching**: Use `*.ext` to match files with a specific extension
  - Example: `*.md` matches all markdown files
  - Example: `*.yml` matches all YAML files

- **Path prefix matching**: Use a path prefix to match files in a directory
  - Example: `.github/` matches all files in the `.github` directory
  - Example: `tools/` matches all files in the `tools` directory

## Example: Processing Changed Files

```yaml
- name: Get changed files
  id: changed-files
  uses: "./.github/actions/infrastructure/get-changed-files"

- name: Process each file
  shell: pwsh
  env:
    CHANGED_FILES: ${{ steps.changed-files.outputs.files }}
  run: |
    $changedFilesJson = $env:CHANGED_FILES
    $changedFiles = $changedFilesJson | ConvertFrom-Json
    
    foreach ($file in $changedFiles) {
      Write-Host "Processing: $file"
      # Your processing logic here
    }
```

## Limitations

- Simple filter patterns only (no complex glob or regex patterns)

## Pagination

The action automatically handles pagination to fetch **all** changed files in a PR, regardless of how many files were changed:

- Fetches files in batches of 100 per page
- Continues fetching until all files are retrieved
- Logs a note when pagination occurs, showing the total file count
- **No file limit** - all changed files will be processed, even in very large PRs

This ensures that critical workflows (such as merge conflict checking, link validation, etc.) don't miss files due to pagination limits.

## Related Actions

- **markdownlinks**: Uses this pattern to get changed markdown files
- **merge-conflict-checker**: Uses this pattern to get changed files for conflict detection
- **path-filters**: Similar functionality but with more complex filtering logic
