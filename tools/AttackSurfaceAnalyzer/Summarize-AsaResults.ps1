#Requires -Version 5.1
<#
.SYNOPSIS
    Summarizes Attack Surface Analyzer (ASA) results from a JSON file.

.DESCRIPTION
    This script analyzes ASA JSON results and provides a comprehensive summary of security findings,
    including counts by category, analysis levels, and detailed breakdowns of security issues.

.PARAMETER Path
    Path to the ASA results JSON file. Defaults to 'asa-results\asa-results.json' in the current directory.

.PARAMETER ShowDetails
    Shows detailed information about each finding category.

.EXAMPLE
    .\Summarize-AsaResults.ps1

    Summarizes the ASA results with basic statistics.

.EXAMPLE
    .\Summarize-AsaResults.ps1 -ShowDetails

    Shows detailed breakdown of findings by category..NOTES
    Author: GitHub Copilot
    Version: 1.0
    Created for PowerShell ASA Analysis
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Path = "asa-results\asa-results.json",

    [Parameter()]
    [switch]$ShowDetails
)

function Get-AsaSummary {
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$AsaData
    )

    # Extract metadata
    $metadata = $AsaData.Metadata
    $results = $AsaData.Results

    # Initialize counters
    $summary = @{
        Metadata = @{
            Version = $metadata.'compare-version'
            OS = $metadata.'compare-os'
            OSVersion = $metadata.'compare-osversion'
            BaseRunId = ""
            CompareRunId = ""
        }
        Categories = @{}
        TotalFindings = 0
        AnalysisLevels = @{
            WARNING = 0
            ERROR = 0
            INFO = 0
        }
        RuleTypes = @{}
        FileIssuesByRule = @{}
        TimeSpan = $null
    }

    # Process each category
    foreach ($categoryName in $results.PSObject.Properties.Name) {
        $categoryData = $results.$categoryName
        $categoryCount = $categoryData.Count

        $summary.Categories[$categoryName] = @{
            Count = $categoryCount
            Items = @()
        }

        $summary.TotalFindings += $categoryCount

        # Process items in category
        foreach ($item in $categoryData) {
            # Count analysis levels
            if ($item.Analysis) {
                $summary.AnalysisLevels[$item.Analysis]++
            }

            # Extract run IDs and calculate timespan
            if ($item.BaseRunId) {
                $summary.Metadata.BaseRunId = $item.BaseRunId
            }
            if ($item.CompareRunId) {
                $summary.Metadata.CompareRunId = $item.CompareRunId
            }

            # Process rules
            foreach ($rule in $item.Rules) {
                $ruleName = $rule.Name
                if (-not $summary.RuleTypes.ContainsKey($ruleName)) {
                    $summary.RuleTypes[$ruleName] = @{
                        Count = 0
                        Description = $rule.Description
                        Flag = $rule.Flag
                        Platforms = $rule.Platforms
                        Categories = @{}
                    }
                }
                $summary.RuleTypes[$ruleName].Count++

                # Track which categories this rule appears in
                if (-not $summary.RuleTypes[$ruleName].Categories.ContainsKey($categoryName)) {
                    $summary.RuleTypes[$ruleName].Categories[$categoryName] = 0
                }
                $summary.RuleTypes[$ruleName].Categories[$categoryName]++

                # For file-related categories, track file extension if available
                if ($categoryName -like "*FILE*" -and $item.Identity) {
                    $fileExtension = [System.IO.Path]::GetExtension($item.Identity).ToLower()
                    if (-not $fileExtension) { $fileExtension = "(no extension)" }

                    if (-not $summary.FileIssuesByRule.ContainsKey($ruleName)) {
                        $summary.FileIssuesByRule[$ruleName] = @{}
                    }
                    if (-not $summary.FileIssuesByRule[$ruleName].ContainsKey($fileExtension)) {
                        $summary.FileIssuesByRule[$ruleName][$fileExtension] = 0
                    }
                    $summary.FileIssuesByRule[$ruleName][$fileExtension]++
                }
            }

            # Store item details for detailed view
            $summary.Categories[$categoryName].Items += @{
                Identity = $item.Identity
                Analysis = $item.Analysis
                Rules = $item.Rules
            }
        }
    }

    # Calculate timespan if we have both run IDs
    if ($summary.Metadata.BaseRunId -and $summary.Metadata.CompareRunId) {
        try {
            $baseTime = [DateTime]::Parse($summary.Metadata.BaseRunId)
            $compareTime = [DateTime]::Parse($summary.Metadata.CompareRunId)
            $summary.TimeSpan = $compareTime - $baseTime
        }
        catch {
            $summary.TimeSpan = "Unable to calculate"
        }
    }

    return $summary
}

function Write-ConsoleSummary {
    param(
        [Parameter(Mandatory)]
        [hashtable]$Summary,

        [Parameter()]
        [switch]$ShowDetails
    )

    # Header
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host "Attack Surface Analyzer Results Summary" -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host ""

    # Metadata
    Write-Host "Analysis Metadata:" -ForegroundColor Yellow
    Write-Host "  ASA Version:    $($Summary.Metadata.Version)" -ForegroundColor White
    Write-Host "  Operating System: $($Summary.Metadata.OS) ($($Summary.Metadata.OSVersion))" -ForegroundColor White
    if ($Summary.TimeSpan -and $Summary.TimeSpan -ne "Unable to calculate") {
        Write-Host "  Analysis Duration: $($Summary.TimeSpan.ToString())" -ForegroundColor White
    }
    Write-Host ""

    # Overall Statistics
    Write-Host "Overall Statistics:" -ForegroundColor Yellow
    Write-Host "  Total Findings: $($Summary.TotalFindings)" -ForegroundColor White

    # Analysis Levels
    Write-Host "  Analysis Levels:" -ForegroundColor White
    foreach ($level in $Summary.AnalysisLevels.Keys | Sort-Object) {
        $count = $Summary.AnalysisLevels[$level]
        $color = switch ($level) {
            'ERROR' { 'Red' }
            'WARNING' { 'Yellow' }
            'INFO' { 'Green' }
            default { 'White' }
        }
        Write-Host "    $level`: $count" -ForegroundColor $color
    }
    Write-Host ""

    # Category Breakdown
    Write-Host "Findings by Category:" -ForegroundColor Yellow
    $sortedCategories = $Summary.Categories.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending

    foreach ($category in $sortedCategories) {
        $categoryName = $category.Key
        $count = $category.Value.Count

        if ($count -gt 0) {
            Write-Host "  $categoryName`: $count items" -ForegroundColor Cyan
        }
        else {
            Write-Host "  $categoryName`: $count items" -ForegroundColor DarkGray
        }
    }
    Write-Host ""

    # Rule Types Summary
    Write-Host "Top Security Rules Triggered:" -ForegroundColor Yellow
    $topRules = $Summary.RuleTypes.GetEnumerator() |
                Sort-Object { $_.Value.Count } -Descending |
                Select-Object -First 10

    foreach ($rule in $topRules) {
        $ruleName = $rule.Key
        $count = $rule.Value.Count
        $flag = $rule.Value.Flag

        $color = switch ($flag) {
            'ERROR' { 'Red' }
            'WARNING' { 'Yellow' }
            'INFO' { 'Green' }
            default { 'White' }
        }

        Write-Host "  [$flag] $ruleName`: $count occurrences" -ForegroundColor $color
        if ($ShowDetails) {
            Write-Host "    Description: $($rule.Value.Description)" -ForegroundColor DarkGray
            Write-Host "    Platforms: $($rule.Value.Platforms -join ', ')" -ForegroundColor DarkGray

            # Show breakdown by category for this rule
            if ($rule.Value.Categories.Count -gt 0) {
                Write-Host "    Categories:" -ForegroundColor DarkGray
                foreach ($cat in $rule.Value.Categories.GetEnumerator() | Sort-Object { $_.Value } -Descending) {
                    Write-Host "      $($cat.Key): $($cat.Value) occurrences" -ForegroundColor Gray
                }
            }
        }
    }

    # Detailed Rule Analysis by Category
    if ($ShowDetails) {
        Write-Host ""
        Write-Host "Detailed Rule Analysis by Category:" -ForegroundColor Yellow

        # Focus on file-related categories
        $fileCategories = $Summary.Categories.GetEnumerator() | Where-Object { $_.Key -like "*FILE*" -and $_.Value.Count -gt 0 }

        foreach ($category in $fileCategories) {
            $categoryName = $category.Key
            Write-Host ""
            Write-Host "  $categoryName Rules Breakdown:" -ForegroundColor Cyan

            # Get rules that appear in this category
            $categoryRules = $Summary.RuleTypes.GetEnumerator() |
                           Where-Object { $_.Value.Categories.ContainsKey($categoryName) } |
                           Sort-Object { $_.Value.Categories[$categoryName] } -Descending

            foreach ($ruleEntry in $categoryRules) {
                $ruleName = $ruleEntry.Key
                $count = $ruleEntry.Value.Categories[$categoryName]
                $flag = $ruleEntry.Value.Flag

                $color = switch ($flag) {
                    'ERROR' { 'Red' }
                    'WARNING' { 'Yellow' }
                    'INFO' { 'Green' }
                    default { 'White' }
                }

                Write-Host "    [$flag] $ruleName`: $count files" -ForegroundColor $color
            }
        }

        # Show file extension breakdown if available
        if ($Summary.FileIssuesByRule.Count -gt 0) {
            Write-Host ""
            Write-Host "File Issues by Rule and Extension:" -ForegroundColor Yellow

            foreach ($ruleEntry in $Summary.FileIssuesByRule.GetEnumerator()) {
                $ruleName = $ruleEntry.Key
                Write-Host ""
                Write-Host "  $ruleName`:" -ForegroundColor Cyan

                $sortedExtensions = $ruleEntry.Value.GetEnumerator() | Sort-Object { $_.Value } -Descending
                foreach ($extEntry in $sortedExtensions) {
                    $extension = $extEntry.Key
                    $count = $extEntry.Value
                    Write-Host "    $extension`: $count files" -ForegroundColor White
                }
            }
        }
    }

    # Detailed Category Information
    if ($ShowDetails) {
        Write-Host ""
        Write-Host "Detailed Category Breakdown:" -ForegroundColor Yellow

        foreach ($category in $sortedCategories | Where-Object { $_.Value.Count -gt 0 }) {
            $categoryName = $category.Key
            $items = $category.Value.Items

            Write-Host ""
            Write-Host "  $categoryName ($($items.Count) items):" -ForegroundColor Cyan

            # Group by analysis level
            $groupedByAnalysis = $items | Group-Object Analysis
            foreach ($group in $groupedByAnalysis) {
                $level = $group.Name
                $count = $group.Count

                $color = switch ($level) {
                    'ERROR' { 'Red' }
                    'WARNING' { 'Yellow' }
                    'INFO' { 'Green' }
                    default { 'White' }
                }

                Write-Host "    $level`: $count items" -ForegroundColor $color
            }
        }
    }

    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

# Main execution
try {
    # Validate input file
    if (-not (Test-Path $Path)) {
        Write-Error "ASA results file not found: $Path"
        exit 1
    }

    Write-Verbose "Reading ASA results from: $Path"

    # Load and parse JSON
    $jsonContent = Get-Content -Path $Path -Raw -Encoding UTF8
    $asaData = $jsonContent | ConvertFrom-Json

    # Generate summary
    Write-Verbose "Analyzing ASA results..."
    $summary = Get-AsaSummary -AsaData $asaData

    # Output results to console
    Write-ConsoleSummary -Summary $summary -ShowDetails:$ShowDetails
}
catch {
    Write-Error "Error processing ASA results: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace
    exit 1
}
