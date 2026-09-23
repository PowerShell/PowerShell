# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [parameter(Mandatory)]
    [string]$Name,
    [parameter(Mandatory)]
    [string]$TestResultsFolder
)

Import-Module "$PSScriptRoot/../../../../build.psm1"

if (-not $env:GITHUB_STEP_SUMMARY) {
    Write-Error "GITHUB_STEP_SUMMARY is not set. Ensure this workflow is running in a GitHub Actions environment."
    exit 1
}

$testCaseCount = 0
$testErrorCount = 0
$testFailureCount = 0
$testNotRunCount = 0
$testInconclusiveCount = 0
$testIgnoredCount = 0
$testSkippedCount = 0
$testInvalidCount = 0

# Process test results and generate annotations for failures
Get-ChildItem -Path "${TestResultsFolder}/*.xml" -Recurse | ForEach-Object {
    $results = [xml] (get-content $_.FullName)

    $testCaseCount += [int]$results.'test-results'.total
    $testErrorCount += [int]$results.'test-results'.errors
    $testFailureCount += [int]$results.'test-results'.failures
    $testNotRunCount += [int]$results.'test-results'.'not-run'
    $testInconclusiveCount += [int]$results.'test-results'.inconclusive
    $testIgnoredCount += [int]$results.'test-results'.ignored
    $testSkippedCount += [int]$results.'test-results'.skipped
    $testInvalidCount += [int]$results.'test-results'.invalid

    # Generate GitHub Actions annotations for test failures
    # Select failed test cases
    if ("System.Xml.XmlDocumentXPathExtensions" -as [Type]) {
        $failures = [System.Xml.XmlDocumentXPathExtensions]::SelectNodes($results.'test-results', './/test-case[@result = "Failure"]')
    }
    else {
        $failures = $results.SelectNodes('.//test-case[@result = "Failure"]')
    }

    foreach ($testfail in $failures) {
        $description = $testfail.description
        $testName = $testfail.name
        $message = $testfail.failure.message
        $stack_trace = $testfail.failure.'stack-trace'

        # Parse stack trace to get file and line info
        $fileInfo = Get-PesterFailureFileInfo -StackTraceString $stack_trace

        if ($fileInfo.File) {
            # Convert absolute path to relative path for GitHub Actions
            $filePath = $fileInfo.File

            # GitHub Actions expects paths relative to the workspace root
            if ($env:GITHUB_WORKSPACE) {
                $workspacePath = $env:GITHUB_WORKSPACE
                if ($filePath.StartsWith($workspacePath)) {
                    $filePath = $filePath.Substring($workspacePath.Length).TrimStart('/', '\')
                    # Normalize to forward slashes for consistency
                    $filePath = $filePath -replace '\\', '/'
                }
            }

            # Create annotation title
            $annotationTitle = "Test Failure: $description / $testName"

            # Build the annotation message
            $annotationMessage = $message -replace "`n", "%0A" -replace "`r"

            # Build and output the workflow command
            $workflowCommand = "::error file=$filePath"
            if ($fileInfo.Line) {
                $workflowCommand += ",line=$($fileInfo.Line)"
            }
            $workflowCommand += ",title=$annotationTitle::$annotationMessage"

            Write-Host $workflowCommand

            # Output a link to the test run
            if ($env:GITHUB_SERVER_URL -and $env:GITHUB_REPOSITORY -and $env:GITHUB_RUN_ID) {
                $logUrl = "$($env:GITHUB_SERVER_URL)/$($env:GITHUB_REPOSITORY)/actions/runs/$($env:GITHUB_RUN_ID)"
                Write-Host "Test logs: $logUrl"
            }
        }
    }
}

@"

# Summary of $Name

- Total Tests: $testCaseCount
- Total Errors: $testErrorCount
- Total Failures: $testFailureCount
- Total Not Run: $testNotRunCount
- Total Inconclusive: $testInconclusiveCount
- Total Ignored: $testIgnoredCount
- Total Skipped: $testSkippedCount
- Total Invalid: $testInvalidCount

"@ | Out-File -FilePath $ENV:GITHUB_STEP_SUMMARY -Append

Write-Log "Summary written to $ENV:GITHUB_STEP_SUMMARY"

Write-LogGroupStart -Title 'Test Results'
Get-Content $ENV:GITHUB_STEP_SUMMARY
Write-LogGroupEnd -Title 'Test Results'

if ($testErrorCount -gt 0 -or $testFailureCount -gt 0) {
    Write-Error "There were $testErrorCount/$testFailureCount errors/failures in the test results."
    exit 1
}
if ($testCaseCount -eq 0) {
    Write-Error "No test cases were run."
    exit 1
}
