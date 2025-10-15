# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Verify all links in markdown files.

.DESCRIPTION
    This script parses markdown files to extract links and verifies their accessibility.
    It supports HTTP/HTTPS links and local file references.

.PARAMETER Path
    Path to the directory containing markdown files. Defaults to current directory.

.PARAMETER Exclude
    Array of URL patterns to exclude from verification (e.g., for known temporary issues).

.PARAMETER FailOnError
    If set, the script will exit with non-zero code if any links fail.

.PARAMETER Timeout
    Timeout in seconds for HTTP requests. Defaults to 30.

.PARAMETER MaxRetries
    Maximum number of retries for failed requests. Defaults to 2.

.EXAMPLE
    .\Verify-MarkdownLinks.ps1 -Path ./CHANGELOG

.EXAMPLE
    .\Verify-MarkdownLinks.ps1 -Path ./docs -FailOnError
#>

param(
    [string]$Path = "Q:\src\git\powershell\docs\git",
    [string[]]$Exclude = @(),
    [switch]$FailOnError,
    [int]$Timeout = 30,
    [int]$MaxRetries = 2
)

$ErrorActionPreference = 'Stop'

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Extracting links from markdown files in: $Path" -ForegroundColor Cyan

# Get all links from markdown files using the Parse-ChangelogLinks script
$parseScriptPath = Join-Path $scriptDir "Parse-ChangelogLinks.ps1"
$allLinks = & $parseScriptPath -ChangelogPath $Path

if ($allLinks.Count -eq 0) {
    Write-Host "No links found in markdown files." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($allLinks.Count) links to verify" -ForegroundColor Green

# Group links by URL to avoid duplicate checks
$uniqueLinks = $allLinks | Group-Object -Property Url

Write-Host "Unique URLs to verify: $($uniqueLinks.Count)" -ForegroundColor Cyan

$results = @{
    Total = $uniqueLinks.Count
    Passed = 0
    Failed = 0
    Skipped = 0
    Errors = [System.Collections.ArrayList]::new()
}

# Create a web client for HTTP requests with proper headers
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.Timeout = [TimeSpan]::FromSeconds($Timeout)
$httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; GitHubActions/1.0; +https://github.com)")

function Test-HttpLink {
    param(
        [string]$Url,
        [int]$Retries = 0
    )

    try {
        $response = $httpClient.GetAsync($Url).GetAwaiter().GetResult()

        if ($response.IsSuccessStatusCode) {
            return @{ Success = $true; StatusCode = [int]$response.StatusCode }
        }
        elseif ($response.StatusCode -eq [System.Net.HttpStatusCode]::TooManyRequests -and $Retries -lt $MaxRetries) {
            Write-Verbose "Rate limited, retrying... ($($Retries + 1)/$MaxRetries)"
            Start-Sleep -Seconds (5 * ($Retries + 1))
            return Test-HttpLink -Url $Url -Retries ($Retries + 1)
        }
        else {
            return @{ Success = $false; StatusCode = [int]$response.StatusCode; Error = "HTTP $([int]$response.StatusCode)" }
        }
    }
    catch {
        if ($Retries -lt $MaxRetries) {
            Write-Verbose "Request failed, retrying... ($($Retries + 1)/$MaxRetries)"
            Start-Sleep -Seconds 2
            return Test-HttpLink -Url $Url -Retries ($Retries + 1)
        }
        return @{ Success = $false; StatusCode = 0; Error = $_.Exception.Message }
    }
}

function Test-LocalLink {
    param(
        [string]$Url,
        [string]$BasePath
    )

    # Handle relative paths
    $targetPath = Join-Path $BasePath $Url

    if (Test-Path $targetPath) {
        return @{ Success = $true }
    }
    else {
        return @{ Success = $false; Error = "File not found: $targetPath" }
    }
}

# Verify each unique link
$progressCount = 0
foreach ($linkGroup in $uniqueLinks) {
    $progressCount++
    $url = $linkGroup.Name
    $occurrences = $linkGroup.Group
    Write-Verbose "[$progressCount/$($uniqueLinks.Count)] Checking: $url"

    # Check if URL should be excluded
        $shouldExclude = $false
        foreach ($excludePattern in $Exclude) {
            if ($url -like $excludePattern) {
                $shouldExclude = $true
                break
            }
        }
        if ($shouldExclude) {
            Write-Host "⊘ SKIPPED: $url (excluded)" -ForegroundColor Gray
            $results.Skipped++
            continue
        }
        # Determine link type and verify
        $verifyResult = $null
        if ($url -match '^https?://') {
            $verifyResult = Test-HttpLink -Url $url
        }
        elseif ($url -match '^#') {
            Write-Verbose "Skipping anchor link: $url"
            $results.Skipped++
            continue
        }
        elseif ($url -match '^mailto:') {
            Write-Verbose "Skipping mailto link: $url"
            $results.Skipped++
            continue
        }
        else {
            $basePath = Split-Path -Parent $occurrences[0].Path
            $verifyResult = Test-LocalLink -Url $url -BasePath $basePath
        }
        if ($verifyResult.Success) {
            Write-Host "✓ OK: $url" -ForegroundColor Green
            $results.Passed++
        }
        else {
            $errorMsg = if ($verifyResult.StatusCode) {
                "HTTP $($verifyResult.StatusCode)"
            }
            else {
                $verifyResult.Error
            }
            Write-Host "✗ FAILED: $url - $errorMsg" -ForegroundColor Red
            foreach ($occurrence in $occurrences) {
                Write-Host "    Found in: $($occurrence.Path):$($occurrence.Line):$($occurrence.Column)" -ForegroundColor DarkGray
            }
            $results.Failed++
            [void]$results.Errors.Add(@{
                Url = $url
                Error = $errorMsg
                Occurrences = $occurrences
            })
        }
    }

# Print summary
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "Link Verification Summary" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "Total URLs checked: $($results.Total)" -ForegroundColor White
Write-Host "Passed: $($results.Passed)" -ForegroundColor Green
Write-Host "Failed: $($results.Failed)" -ForegroundColor $(if ($results.Failed -gt 0) { "Red" } else { "Green" })
Write-Host "Skipped: $($results.Skipped)" -ForegroundColor Gray

if ($results.Failed -gt 0) {
    Write-Host "`nFailed Links:" -ForegroundColor Red
    foreach ($failedLink in $results.Errors) {
        Write-Host "  • $($failedLink.Url)" -ForegroundColor Red
        Write-Host "    Error: $($failedLink.Error)" -ForegroundColor DarkGray
        Write-Host "    Occurrences: $($failedLink.Occurrences.Count)" -ForegroundColor DarkGray
    }

    if ($FailOnError) {
        Write-Host "`n❌ Link verification failed!" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "`n✅ All links verified successfully!" -ForegroundColor Green
}

exit 0
