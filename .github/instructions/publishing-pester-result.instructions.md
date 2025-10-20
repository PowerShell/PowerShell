# Publishing Pester Test Results Instructions

This document describes how the PowerShell repository uses GitHub Actions to publish Pester test results.

## Overview

The PowerShell repository uses a custom composite GitHub Action located at `.github/actions/test/process-pester-results` to process and publish Pester test results in CI/CD workflows.
This action aggregates test results from NUnitXml formatted files, creates a summary in the GitHub Actions job summary, and uploads the results as artifacts.

## How It Works

### Action Location and Structure

**Path**: `.github/actions/test/process-pester-results/`

The action consists of two main files:

1. **action.yml** - The composite action definition
1. **process-pester-results.ps1** - PowerShell script that processes test results

### Action Inputs

The action accepts the following inputs:

- **name** (required): A descriptive name for the test run (e.g., "UnelevatedPesterTests-CI")
    - Used for naming the uploaded artifact and in the summary
    - Format: `junit-pester-{name}`

- **testResultsFolder** (optional): Path to the folder containing test result XML files
    - Default: `${{ runner.workspace }}/testResults`
    - The script searches for all `*.xml` files in this folder recursively

### Action Workflow

The action performs the following steps:

1. **Process Test Results**
    - Runs `process-pester-results.ps1` with the provided name and test results folder
    - Parses all NUnitXml formatted test result files (`*.xml`)
    - Aggregates test statistics across all files:
        - Total test cases
        - Errors
        - Failures
        - Not run tests
        - Inconclusive tests
        - Ignored tests
        - Skipped tests
        - Invalid tests

1. **Generate Summary**
    - Creates a markdown summary using the `$GITHUB_STEP_SUMMARY` environment variable
    - Uses `Write-Log` and `Write-LogGroupStart`/`Write-LogGroupEnd` functions from `build.psm1`
    - Outputs a formatted summary with all test statistics
    - Example format:

    ```markdown
    # Summary of {Name}

    - Total Tests: X
    - Total Errors: X
    - Total Failures: X
    - Total Not Run: X
    - Total Inconclusive: X
    - Total Ignored: X
    - Total Skipped: X
    - Total Invalid: X
    ```

1. **Upload Artifacts**
    - Uses `actions/upload-artifact@v4` to upload test results
    - Artifact name: `junit-pester-{name}`
    - Always runs (even if previous steps fail) via `if: always()`
    - Uploads the entire test results folder

1. **Exit Status**
    - Fails the job (exit 1) if:
        - Any test errors occurred (`$testErrorCount -gt 0`)
        - Any test failures occurred (`$testFailureCount -gt 0`)
        - No test cases were run (`$testCaseCount -eq 0`)

## Usage in Test Actions

The `process-pester-results` action is called by two platform-specific composite test actions:

### Linux/macOS Tests: `.github/actions/test/nix`

Used in:

- `.github/workflows/linux-ci.yml`
- `.github/workflows/macos-ci.yml`

Example usage (line 107-112 in `nix/action.yml`):

```yaml
- name: Convert, Publish, and Upload Pester Test Results
  uses: "./.github/actions/test/process-pester-results"
  with:
    name: "${{ inputs.purpose }}-${{ inputs.tagSet }}"
    testResultsFolder: "${{ runner.workspace }}/testResults"
```

### Windows Tests: `.github/actions/test/windows`

Used in:

- `.github/workflows/windows-ci.yml`

Example usage (line 78-83 in `windows/action.yml`):

```yaml
- name: Convert, Publish, and Upload Pester Test Results
  uses: "./.github/actions/test/process-pester-results"
  with:
    name: "${{ inputs.purpose }}-${{ inputs.tagSet }}"
    testResultsFolder: ${{ runner.workspace }}\testResults
```

## Workflow Integration

The process-pester-results action is integrated into the CI workflows through a multi-level hierarchy:

### Level 1: Main CI Workflows

- `linux-ci.yml`
- `macos-ci.yml`
- `windows-ci.yml`

### Level 2: Test Jobs

Each workflow contains multiple test jobs with different purposes and tag sets:

- `UnelevatedPesterTests` with tagSet `CI`
- `ElevatedPesterTests` with tagSet `CI`
- `UnelevatedPesterTests` with tagSet `Others`
- `ElevatedPesterTests` with tagSet `Others`

### Level 3: Platform Test Actions

Test jobs use platform-specific actions:

- `nix` for Linux and macOS
- `windows` for Windows

### Level 4: Process Results Action

Platform actions call `process-pester-results` to publish results

## Test Execution Flow

1. **Build Phase**: Source code is built (e.g., in `ci_build` job)
1. **Test Preparation**:
    - Build artifacts are downloaded
    - PowerShell is bootstrapped
    - Test binaries are extracted
1. **Test Execution**:
    - `Invoke-CITest` is called with:
        - `-Purpose`: Test purpose (e.g., "UnelevatedPesterTests")
        - `-TagSet`: Test category (e.g., "CI", "Others")
        - `-OutputFormat NUnitXml`: Results format
    - Results are written to `${{ runner.workspace }}/testResults`
1. **Results Processing**:
    - `process-pester-results` action runs
    - Results are aggregated and summarized
    - Artifacts are uploaded
    - Job fails if any tests failed or errored

## Key Dependencies

### PowerShell Modules

- **build.psm1**: Provides utility functions
    - `Write-Log`: Logging function with GitHub Actions support
    - `Write-LogGroupStart`: Creates collapsible log groups
    - `Write-LogGroupEnd`: Closes collapsible log groups

### GitHub Actions Features

- **GITHUB_STEP_SUMMARY**: Environment variable for job summary
- **actions/upload-artifact@v4**: For uploading test results
- **Composite Actions**: For reusable workflow steps

### Test Result Format

- **NUnitXml**: XML format for test results
- Expected XML structure with `test-results` root element containing:
    - `total`: Total number of tests
    - `errors`: Number of errors
    - `failures`: Number of failures
    - `not-run`: Number of tests not run
    - `inconclusive`: Number of inconclusive tests
    - `ignored`: Number of ignored tests
    - `skipped`: Number of skipped tests
    - `invalid`: Number of invalid tests

## Best Practices

1. **Naming Convention**: Use descriptive names that include both purpose and tagSet:
    - Format: `{purpose}-{tagSet}`
    - Example: `UnelevatedPesterTests-CI`

1. **Test Results Location**:
    - Default location: `${{ runner.workspace }}/testResults`
    - Use platform-appropriate path separators (Windows: `\`, Unix: `/`)

1. **Always Upload**: The artifact upload step uses `if: always()` to ensure results are uploaded even when tests fail

1. **Error Handling**: The action will fail the job if:
    - Tests have errors or failures (intentional fail-fast behavior)
    - No tests were executed (potential configuration issue)
    - `GITHUB_STEP_SUMMARY` is not set (environment issue)

## Customizing for Your Repository

To use this pattern in another repository:

1. **Copy the Action Files**:
    - Copy `.github/actions/test/process-pester-results/` directory
    - Ensure the PowerShell script has proper permissions

1. **Adjust Dependencies**:
    - Modify or remove the `Import-Module "$PSScriptRoot/../../../../build.psm1"` line
    - Implement equivalent `Write-Log` and `Write-LogGroup*` functions if needed

1. **Customize Summary Format**:
    - Modify the here-string in `process-pester-results.ps1` to change summary format
    - Add additional metrics or formatting as needed

1. **Call from Your Workflows**:

    ```yaml
    - name: Process Test Results
      uses: "./.github/actions/test/process-pester-results"
      with:
        name: "my-test-run"
        testResultsFolder: "path/to/results"
    ```

## Related Documentation

- [GitHub Actions: Creating composite actions](https://docs.github.com/en/actions/creating-actions/creating-a-composite-action)
- [GitHub Actions: Job summaries](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#adding-a-job-summary)
- [GitHub Actions: Uploading artifacts](https://docs.github.com/en/actions/using-workflows/storing-workflow-data-as-artifacts)
- [Pester: PowerShell testing framework](https://pester.dev/)
- [NUnit XML Format](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html)

## Troubleshooting

### No Test Results Found

- Verify `testResultsFolder` path is correct
- Ensure tests are generating NUnitXml formatted output
- Check that `*.xml` files exist in the specified folder

### Action Fails with "GITHUB_STEP_SUMMARY is not set"

- Ensure the action runs within a GitHub Actions environment
- Cannot be run locally without mocking this environment variable

### All Tests Pass but Job Fails

- Check if any tests are marked as errors (different from failures)
- Verify that at least some tests executed (`$testCaseCount -eq 0`)

### Artifact Upload Fails

- Check artifact name for invalid characters
- Ensure the test results folder exists
- Verify actions/upload-artifact version compatibility
