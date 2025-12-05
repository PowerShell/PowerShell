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
