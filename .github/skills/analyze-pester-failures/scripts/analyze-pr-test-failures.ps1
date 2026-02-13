#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated Pester test failure analysis workflow for GitHub PRs.

.DESCRIPTION
    This script automates the complete analysis workflow defined in the analyze-pester-failures
    skill. It performs all steps in order:
    1. Identify failing test jobs in the PR
    2. Download test artifacts and logs
    3. Extract specific test failures
    4. Parse error messages
    5. Search logs for error markers and generate recommendations

    By automating the workflow, this ensures analysis steps are followed in order
    and nothing is skipped.

.PARAMETER PR
    The GitHub PR number to analyze (e.g., 26800)

.PARAMETER Owner
    Repository owner (default: PowerShell)

.PARAMETER Repo
    Repository name (default: PowerShell)

.PARAMETER OutputDir
    Directory to store analysis results (default: ./pester-analysis-PR<number>)

.PARAMETER Interactive
    Prompt for recommendations after analysis (default: non-interactive)

.PARAMETER ForceDownload
    Force re-download of artifacts and logs, even if they already exist

.EXAMPLE
    .\tools\analyze-pr-test-failures.ps1 -PR 26800
    Analyzes PR #26800 and saves results to ./pester-analysis-PR26800

.EXAMPLE
    .\tools\analyze-pr-test-failures.ps1 -PR 26800 -Interactive
    Interactive mode: shows failures and prompts for next steps

.EXAMPLE
    .\tools\analyze-pr-test-failures.ps1 -PR 26800 -ForceDownload
    Re-download all logs and artifacts, skipping the cache

.NOTES
    Requires: GitHub CLI (gh) configured and authenticated
    This script enforces the workflow defined in .github/skills/analyze-pester-failures/SKILL.md
#>

param(
    [Parameter(Mandatory)]
    [int]$PR,

    [string]$Owner = 'PowerShell',
    [string]$Repo = 'PowerShell',
    [string]$OutputDir,
    [switch]$Interactive,
    [switch]$ForceDownload
)

$ErrorActionPreference = 'Stop'

if (-not $OutputDir) {
    $OutputDir = "./pester-analysis-PR$PR"
}

# Colors for output
$colors = @{
    Step    = [ConsoleColor]::Cyan
    Success = [ConsoleColor]::Green
    Warning = [ConsoleColor]::Yellow
    Error   = [ConsoleColor]::Red
    Info    = [ConsoleColor]::Gray
}

function Write-Step {
    param([string]$text, [int]$number)
    Write-Host "`n[$number/5] $text" -ForegroundColor $colors.Step -BackgroundColor Black
}

function Write-Result {
    param([string]$text, [ValidateSet('Success','Warning','Error','Info')]$type = 'Info')
    Write-Host $text -ForegroundColor $colors[$type]
}

Write-Host "`n=== Pester Test Failure Analysis ===" -ForegroundColor $colors.Step
Write-Host "PR: $Owner/$Repo#$PR" -ForegroundColor $colors.Info
Write-Host "Output Directory: $OutputDir" -ForegroundColor $colors.Info

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# STEP 1: Identify the Failing Test Job
Write-Step "Identify failing test jobs" 1

Write-Result "Fetching PR status checks..." Info
$prResponse = gh pr view $PR --repo "$Owner/$Repo" --json 'statusCheckRollup' | ConvertFrom-Json
$allChecks = $prResponse.statusCheckRollup

$failedJobs = $allChecks | Where-Object { $_.conclusion -eq 'FAILURE' }

if (-not $failedJobs) {
    Write-Result "✓ No failed jobs found" Success
    Write-Host "  Total checks: $($allChecks.Count)"
    $allChecks | Where-Object { $_ } | ForEach-Object {
        Write-Host "  - $($_.name): $($_.conclusion)" -ForegroundColor $colors.Info
    }
    exit 0
}

Write-Result "✓ Found $($failedJobs.Count) failing job(s)" Warning

$failedJobs | Where-Object { $_.conclusion -eq 'FAILURE' } | ForEach-Object {
    Write-Host "  ✗ $($_.name) - $($_.conclusion)" -ForegroundColor $colors.Error
    if ($_.detailsUrl) {
        Write-Host "    URL: $($_.detailsUrl)" -ForegroundColor $colors.Info
    }
}

if ($Interactive) {
    Write-Host "`nPress Enter to continue to Step 2..."
    Read-Host | Out-Null
}

# STEP 2: Get Test Results
Write-Step "Download test artifacts and logs" 2

# Extract unique run IDs from failing jobs
$uniqueRuns = @()
foreach ($failedJob in $failedJobs) {
    if ($failedJob.detailsUrl -match 'runs/(\d+)') {
        $runId = $matches[1]
        if ($runId -notin $uniqueRuns) {
            $uniqueRuns += $runId
        }
    }
}

if ($uniqueRuns.Count -eq 0) {
    Write-Result "✗ Could not extract run IDs from failing jobs" Error
    exit 1
}

Write-Result "Found $($uniqueRuns.Count) run(s): $($uniqueRuns -join ', ')" Info

$artifactDir = Join-Path $OutputDir artifacts

# Check if artifacts already exist
$existingArtifacts = Get-ChildItem $artifactDir -Recurse -File -ErrorAction SilentlyContinue

if ($existingArtifacts -and -not $ForceDownload) {
    Write-Result "✓ Artifacts already downloaded" Success
    $existingArtifacts | ForEach-Object {
        Write-Host "  - $($_.FullName)" -ForegroundColor $colors.Info
    }
} else {
    Write-Result "Downloading artifacts from run $($uniqueRuns[0])..." Info
    gh run download $uniqueRuns[0] --dir $artifactDir --repo "$Owner/$Repo" 2>&1 | Out-Null

    if (Test-Path $artifactDir) {
        Write-Result "✓ Artifacts downloaded" Success
        Get-ChildItem $artifactDir -Recurse -File | ForEach-Object {
            Write-Host "  - $($_.FullName)" -ForegroundColor $colors.Info
        }
    } else {
        Write-Result "✗ Failed to download artifacts" Error
        exit 1
    }
}

# Download individual job logs for failing jobs
Write-Result "Downloading individual job logs..." Info

$logsDir = Join-Path $OutputDir "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
}

# Check if logs already exist
$existingLogs = Get-ChildItem $logsDir -Filter "*.txt" -ErrorAction SilentlyContinue

if ($existingLogs -and -not $ForceDownload) {
    Write-Result "✓ Job logs already downloaded" Success
    $existingLogs | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor $colors.Info
    }
} else {
    # Process each run and get its jobs
    $failedJobIds = @()
    foreach ($runId in $uniqueRuns) {
        $runJobs = gh run view $runId --repo "$Owner/$Repo" --json jobs | ConvertFrom-Json

        foreach ($failedJob in $failedJobs) {
            # Check if this failed job belongs to this run
            if ($failedJob.detailsUrl -match "runs/$runId/") {
                $jobMatch = $runJobs.jobs | Where-Object { $_.name -eq $failedJob.name } | Select-Object -First 1
                if ($jobMatch) {
                    $failedJobIds += @{
                        name  = $failedJob.name
                        id    = $jobMatch.databaseId
                        runId = $runId
                    }
                }
            }
        }
    }

    # Download logs for all failed jobs
    foreach ($jobInfo in $failedJobIds) {
        $logFile = Join-Path $logsDir ("log-{0}.txt" -f ($jobInfo.name -replace '[^a-zA-Z0-9-]', '_'))
        Write-Result "  Downloading: $($jobInfo.name) (Run $($jobInfo.runId))" Info
        gh run view $jobInfo.runId --log --job $jobInfo.id --repo "$Owner/$Repo" > $logFile 2>&1
    }

    Write-Result "✓ Job logs downloaded" Success
    Get-ChildItem $logsDir -Filter "*.txt" | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor $colors.Info
    }
}

if ($Interactive) {
    Write-Host "`nPress Enter to continue to Step 3..."
    Read-Host | Out-Null
}

# STEP 3: Extract Specific Failures
Write-Step "Extract test failures from XML" 3

$xmlFiles = Get-ChildItem $artifactDir -Filter "*.xml" -Recurse
if (-not $xmlFiles) {
    Write-Result "✗ No test result XML files found" Error
    exit 1
}

Write-Result "✓ Found $($xmlFiles.Count) test result file(s)" Success

$allFailures = @()

foreach ($xmlFile in $xmlFiles) {
    Write-Result "`nParsing: $($xmlFile.Name)" Info

    try {
        [xml]$xml = Get-Content $xmlFile
        $testResults = $xml.'test-results'

        Write-Host "  Total: $($testResults.total)" -ForegroundColor $colors.Info
        Write-Host "  Passed: $($testResults.passed)" -ForegroundColor $colors.Success
        if ($testResults.failures -gt 0) {
            Write-Host "  Failed: $($testResults.failures)" -ForegroundColor $colors.Error
        }
        if ($testResults.errors -gt 0) {
            Write-Host "  Errors: $($testResults.errors)" -ForegroundColor $colors.Error
        }
        if ($testResults.skipped -gt 0) {
            Write-Host "  Skipped: $($testResults.skipped)" -ForegroundColor $colors.Warning
        }
        if ($testResults.ignored -gt 0) {
            Write-Host "  Ignored: $($testResults.ignored)" -ForegroundColor $colors.Warning
        }

        # Extract failures
        $failures = $xml.SelectNodes('.//test-case[@result = "Failure"]')

        foreach ($failure in $failures) {
            $allFailures += @{
                Name       = $failure.name
                File       = $xmlFile.Name
                Message    = $failure.failure.message
                StackTrace = $failure.failure.'stack-trace'
            }
        }
    } catch {
        Write-Result "✗ Error parsing XML: $_" Error
    }
}

Write-Result "`n✓ Extracted $($allFailures.Count) failures total" Success

# Save failures to JSON for later analysis
$allFailures | ConvertTo-Json -Depth 10 | Out-File (Join-Path $OutputDir "failures.json")

if ($Interactive) {
    Write-Host "`nPress Enter to continue to Step 4..."
    Read-Host | Out-Null
}

# STEP 4: Read Error Messages
Write-Step "Analyze error messages" 4

$failuresByType = @{}

foreach ($failure in $allFailures) {
    $message = $failure.Message -split "`n" | Select-Object -First 1

    # Categorize failure
    $type = 'Other'
    if ($message -match 'Expected .* but got') { $type = 'Assertion' }
    elseif ($message -match 'Cannot (find|bind)') { $type = 'Exception' }
    elseif ($message -match 'timed out') { $type = 'Timeout' }

    if (-not $failuresByType[$type]) {
        $failuresByType[$type] = @()
    }
    $failuresByType[$type] += $failure
}

Write-Result "Failure breakdown:" Info
$failuresByType.GetEnumerator() | ForEach-Object {
    Write-Host "  $($_.Key): $($_.Value.Count)" -ForegroundColor $colors.Warning
}

Write-Result "`nTop failure messages:" Info
$allFailures | Group-Object Message | Sort-Object Count -Descending | Select-Object -First 3 | ForEach-Object {
    Write-Host "  [$($_.Count)x] $($_.Name -split "`n" | Select-Object -First 1)" -ForegroundColor $colors.Info
}

# Save analysis
$analysis = @{
    FailuresByType = @{}
    TopMessages    = @()
}

$failuresByType.GetEnumerator() | ForEach-Object {
    $analysis.FailuresByType[$_.Key] = $_.Value.Count
}

$allFailures | Group-Object Message | Sort-Object Count -Descending | Select-Object -First 5 | ForEach-Object {
    $analysis.TopMessages += @{
        Count   = $_.Count
        Message = ($_.Name -split "`n" | Select-Object -First 1)
    }
}

$analysis | ConvertTo-Json | Out-File (Join-Path $OutputDir "analysis.json")

if ($Interactive) {
    Write-Host "`nPress Enter to continue to Step 5..."
    Read-Host | Out-Null
}

# STEP 5: Search Logs for Error Markers
Write-Step "Search logs for error markers" 5

$logsDir = Join-Path $OutputDir "logs"
if (-not (Test-Path $logsDir)) {
    Write-Result "⚠ Logs directory not found" Warning
} else {
    $logFiles = Get-ChildItem $logsDir -Filter "*.txt" -ErrorAction SilentlyContinue
    if (-not $logFiles) {
        Write-Result "⚠ No log files found in logs directory" Warning
    } else {
        Write-Result "Searching $($logFiles.Count) job log(s) for error markers ([-])" Info
        Write-Result "Format: [JobName] [LineNumber] Content" Info
        Write-Host ""

        $allErrorLines = @()

        foreach ($logFile in $logFiles) {
            $jobName = $logFile.BaseName -replace '^log-', ''
            $logLines = @(Get-Content $logFile)

            for ($i = 0; $i -lt $logLines.Count; $i++) {
                $line = $logLines[$i]
                if ($line -match '\s\[-\]\s') {
                    $allErrorLines += @{
                        JobName    = $jobName
                        LineNumber = $i + 1
                        Content    = $line
                    }
                }
            }
        }

        if ($allErrorLines.Count -gt 0) {
            Write-Result "✓ Found $($allErrorLines.Count) error marker line(s)" Warning

            $allErrorLines | ForEach-Object {
                Write-Host "  [$($_.JobName)] [$($_.LineNumber)] $($_.Content)" -ForegroundColor $colors.Error
            }

            # Save to file
            $allErrorLines | ConvertTo-Json | Out-File (Join-Path $OutputDir "error-markers.json")
            Write-Result "✓ Error markers saved to error-markers.json" Success
        } else {
            Write-Result "✓ No error markers found in logs" Success
        }
    }
}

if ($Interactive) {
    Write-Host "`nPress Enter to continue to Step 5..."
    Read-Host | Out-Null
}

# STEP 5: Generate Recommendations
Write-Step "Generate recommendations" 5

$recommendations = @()

# Analyze patterns
if ($failuresByType['Assertion']) {
    $recommendations += "Multiple assertion failures detected. These indicate test expectations don't match actual behavior."
}

if ($failuresByType['Exception']) {
    $recommendations += "Exception errors found. Check test setup and prerequisites - may indicate missing files, modules, or permissions."
}

if ($failuresByType['Timeout']) {
    $recommendations += "Timeout failures suggest slow or hanging operations. Consider network issues or resource constraints on CI."
}

# Check for patterns in failure messages
$failureMessages = $allFailures.Message -join "`n"
if ($failureMessages -match 'PackageManagement') {
    $recommendations += "PackageManagement module issues detected. Verify module availability and help repository access."
}

if ($failureMessages -match 'Update-Help') {
    $recommendations += "Update-Help failures detected. Check network connectivity to help repository and help installation paths."
}

Write-Result "`n📋 Recommendations:" Info
if ($recommendations) {
    $recommendations | ForEach-Object { Write-Host "  • $_" -ForegroundColor $colors.Info }
} else {
    Write-Host "  • Review failures in detail" -ForegroundColor $colors.Info
    Write-Host "  • Check if test changes are needed" -ForegroundColor $colors.Info
    Write-Host "  • Consider environment-specific issues" -ForegroundColor $colors.Info
}

$recommendations | Out-File (Join-Path $OutputDir "recommendations.txt")

# Summary
Write-Host "`n=== Analysis Complete ===" -ForegroundColor $colors.Step
Write-Host "Results saved to: $OutputDir" -ForegroundColor $colors.Info
Write-Host "  - failures.json (detailed failure data)" -ForegroundColor $colors.Info
Write-Host "  - analysis.json (summary analysis)" -ForegroundColor $colors.Info
Write-Host "  - recommendations.txt (suggested fixes)" -ForegroundColor $colors.Info
Write-Host "  - error-markers.json (error markers from logs)" -ForegroundColor $colors.Info
Write-Host "  - logs/ (individual job log files)" -ForegroundColor $colors.Info
Write-Host "  - artifacts/ (downloaded test artifacts)" -ForegroundColor $colors.Info

Write-Host "`nNext steps:" -ForegroundColor $colors.Step
Write-Host "1. Review recommendations.txt for analysis" -ForegroundColor $colors.Info
Write-Host "2. Examine failures.json for detailed error messages" -ForegroundColor $colors.Info
Write-Host "3. Check error-markers.json for specific test failures in logs" -ForegroundColor $colors.Info
Write-Host "4. Review individual job logs in logs/ directory for contextual details" -ForegroundColor $colors.Info
Write-Host "`n"
