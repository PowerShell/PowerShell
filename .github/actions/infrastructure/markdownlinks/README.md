# Verify Markdown Links Action

A GitHub composite action that verifies all links in markdown files using PowerShell and Markdig.

## Features

- ✅ Parses markdown files using Markdig (built into PowerShell 7)
- ✅ Extracts all link types: inline links, reference links, and autolinks
- ✅ Verifies HTTP/HTTPS links with configurable timeouts and retries
- ✅ Validates local file references
- ✅ Supports excluding specific URL patterns
- ✅ Provides detailed error reporting with file locations
- ✅ Outputs metrics for CI/CD integration

## Usage

### Basic Usage

```yaml
- name: Verify Markdown Links
  uses: ./.github/actions/infrastructure/markdownlinks
  with:
    path: './CHANGELOG'
```

### Advanced Usage

```yaml
- name: Verify Markdown Links
  uses: ./.github/actions/infrastructure/markdownlinks
  with:
    path: './docs'
    fail-on-error: 'true'
    timeout: 30
    max-retries: 2
    exclude-patterns: '*.example.com/*,*://localhost/*'
```

### With Outputs

```yaml
- name: Verify Markdown Links
  id: verify-links
  uses: ./.github/actions/infrastructure/markdownlinks
  with:
    path: './CHANGELOG'
    fail-on-error: 'false'

- name: Display Results
  run: |
    echo "Total links: ${{ steps.verify-links.outputs.total-links }}"
    echo "Passed: ${{ steps.verify-links.outputs.passed-links }}"
    echo "Failed: ${{ steps.verify-links.outputs.failed-links }}"
    echo "Skipped: ${{ steps.verify-links.outputs.skipped-links }}"
```

## Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `path` | Path to the directory containing markdown files to verify | No | `./CHANGELOG` |
| `exclude-patterns` | Comma-separated list of URL patterns to exclude from verification | No | `''` |
| `fail-on-error` | Whether to fail the action if any links are broken | No | `true` |
| `timeout` | Timeout in seconds for HTTP requests | No | `30` |
| `max-retries` | Maximum number of retries for failed requests | No | `2` |

## Outputs

| Output | Description |
|--------|-------------|
| `total-links` | Total number of unique links checked |
| `passed-links` | Number of links that passed verification |
| `failed-links` | Number of links that failed verification |
| `skipped-links` | Number of links that were skipped |

## Excluded Link Types

The action automatically skips the following link types:

- **Anchor links** (`#section-name`) - Would require full markdown parsing
- **Email links** (`mailto:user@example.com`) - Cannot be verified without sending email

## GitHub Workflow Test

This section provides a workflow example and instructions for testing the link verification action.

### Testing the Workflow

To test that the workflow properly detects broken links:

1. Make change to this file (e.g., this README.md file already contains one in the [Broken Link Test](#broken-link-test) section)
1. The workflow will run and should fail, reporting the broken link(s)
1. Revert your change to this file
1. Push again to verify the workflow passes

### Example Workflow Configuration

```yaml
name: Verify Links

on:
  push:
    branches: [ main ]
    paths:
      - '**/*.md'
  pull_request:
    branches: [ main ]
    paths:
      - '**/*.md'
  schedule:
    # Run weekly to catch external link rot
    - cron: '0 0 * * 0'

jobs:
  verify-links:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Verify CHANGELOG Links
        uses: ./.github/actions/infrastructure/markdownlinks
        with:
          path: './CHANGELOG'
          fail-on-error: 'true'

      - name: Verify Documentation Links
        uses: ./.github/actions/infrastructure/markdownlinks
        with:
          path: './docs'
          fail-on-error: 'false'
          exclude-patterns: '*.internal.example.com/*'
```

## How It Works

1. **Parse Markdown**: Uses `Parse-MarkdownLink.ps1` to extract all links from markdown files using Markdig
2. **Deduplicate**: Groups links by URL to avoid checking the same link multiple times
3. **Verify Links**:
   - HTTP/HTTPS links: Makes HEAD/GET requests with configurable timeout and retries
   - Local file references: Checks if the file exists relative to the markdown file
   - Excluded patterns: Skips links matching the exclude patterns
4. **Report Results**: Displays detailed results with file locations for failed links
5. **Set Outputs**: Provides metrics for downstream steps

## Error Output Example

```
✗ FAILED: https://example.com/broken-link - HTTP 404
    Found in: /path/to/file.md:42:15
    Found in: /path/to/other.md:100:20

Link Verification Summary
============================================================
Total URLs checked: 150
Passed: 145
Failed: 2
Skipped: 3

Failed Links:
  • https://example.com/broken-link
    Error: HTTP 404
    Occurrences: 2
```

## Requirements

- PowerShell 7+ (includes Markdig)
- Runs on: `ubuntu-latest`, `windows-latest`, `macos-latest`

## Broken Link Test

- [Broken Link](https://github.com/PowerShell/PowerShell/wiki/NonExistentPage404)

## License

Same as the PowerShell repository.
