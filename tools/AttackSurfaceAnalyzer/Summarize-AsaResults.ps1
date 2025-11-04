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

.PARAMETER IncludeInformationalEvent
    Includes informational events in the analysis. By default, only WARNING and ERROR events are processed.

.PARAMETER IncludeDebugEvent
    Includes debug events in the analysis. By default, only WARNING and ERROR events are processed.

.EXAMPLE
    .\Summarize-AsaResults.ps1

    Summarizes the ASA results with basic statistics, showing only WARNING and ERROR events.

.EXAMPLE
    .\Summarize-AsaResults.ps1 -ShowDetails

    Shows detailed breakdown of findings by category, filtering out informational and debug events.

.EXAMPLE
    .\Summarize-AsaResults.ps1 -IncludeInformationalEvent

    Includes informational events along with WARNING and ERROR events in the analysis..NOTES
    Author: GitHub Copilot
    Version: 1.0
    Created for PowerShell ASA Analysis
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Path = "asa-results\asa-results.json",

    [Parameter()]
    [switch]$ShowDetails,

    [Parameter()]
    [switch]$IncludeInformationalEvent,

    [Parameter()]
    [switch]$IncludeDebugEvent
)

function Get-AsaSummary {
    param(
        [Parameter(Mandatory)]
        $AsaData,

        [Parameter()]
        [switch]$IncludeInformationalEvent,

        [Parameter()]
        [switch]$IncludeDebugEvent
    )

    # Extract metadata
    $metadata = $AsaData["Metadata"]
    $results = $AsaData["Results"]

    # Initialize counters
    $summary = @{
        Metadata = @{
            Version = $metadata["compare-version"]
            OS = $metadata["compare-os"]
            OSVersion = $metadata["compare-osversion"]
            BaseRunId = ""
            CompareRunId = ""
        }
        Categories = @{}
        TotalFindings = 0
        AnalysisLevels = @{
            WARNING = 0
            ERROR = 0
            INFORMATION = 0
            DEBUG = 0
        }
        RuleTypes = @{}
        FileIssuesByRule = @{}
        FileExtensionSummary = @{}
        TimeSpan = $null
    }

    # Process each category
    foreach ($categoryName in $results.Keys) {
        $categoryData = $results[$categoryName]

        $summary.Categories[$categoryName] = @{
            Count = 0
            Items = @()
        }

        # Process items in category with filtering
        foreach ($item in $categoryData) {
            # Filter events based on analysis level
            $analysisLevel = $item["Analysis"]
            if ($analysisLevel) {
                # Skip informational events unless explicitly included
                if ($analysisLevel -eq "INFORMATION" -and -not $IncludeInformationalEvent) {
                    continue
                }
                # Skip debug events unless explicitly included
                if ($analysisLevel -eq "DEBUG" -and -not $IncludeDebugEvent) {
                    continue
                }

                $summary.AnalysisLevels[$analysisLevel]++
            }            # If we reach here, the item passed the filter
            $summary.Categories[$categoryName].Count++
            $summary.TotalFindings++

            # Extract run IDs and calculate timespan
            if ($item["BaseRunId"]) {
                $summary.Metadata.BaseRunId = $item["BaseRunId"]
            }
            if ($item["CompareRunId"]) {
                $summary.Metadata.CompareRunId = $item["CompareRunId"]
            }

            # Process rules
            foreach ($rule in $item["Rules"]) {
                $ruleName = $rule["Name"]
                if (-not $summary.RuleTypes.ContainsKey($ruleName)) {
                    $summary.RuleTypes[$ruleName] = @{
                        Count = 0
                        Description = $rule["Description"]
                        Flag = $rule["Flag"]
                        Platforms = $rule["Platforms"]
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
                if ($categoryName -like "*FILE*" -and $item["Identity"]) {
                    $fileExtension = [System.IO.Path]::GetExtension($item["Identity"]).ToLower()
                    if (-not $fileExtension) { $fileExtension = "(no extension)" }

                    # Track by rule and extension
                    if (-not $summary.FileIssuesByRule.ContainsKey($ruleName)) {
                        $summary.FileIssuesByRule[$ruleName] = @{}
                    }
                    if (-not $summary.FileIssuesByRule[$ruleName].ContainsKey($fileExtension)) {
                        $summary.FileIssuesByRule[$ruleName][$fileExtension] = 0
                    }
                    $summary.FileIssuesByRule[$ruleName][$fileExtension]++

                    # Track overall file extension summary
                    if (-not $summary.FileExtensionSummary.ContainsKey($fileExtension)) {
                        $summary.FileExtensionSummary[$fileExtension] = @{
                            Count = 0
                            Rules = @{}
                            Categories = @{}
                        }
                    }
                    $summary.FileExtensionSummary[$fileExtension].Count++

                    # Track which rules affect this extension
                    if (-not $summary.FileExtensionSummary[$fileExtension].Rules.ContainsKey($ruleName)) {
                        $summary.FileExtensionSummary[$fileExtension].Rules[$ruleName] = 0
                    }
                    $summary.FileExtensionSummary[$fileExtension].Rules[$ruleName]++

                    # Track which categories this extension appears in
                    if (-not $summary.FileExtensionSummary[$fileExtension].Categories.ContainsKey($categoryName)) {
                        $summary.FileExtensionSummary[$fileExtension].Categories[$categoryName] = 0
                    }
                    $summary.FileExtensionSummary[$fileExtension].Categories[$categoryName]++
                }
            }

            # Store item details for detailed view
            $summary.Categories[$categoryName].Items += @{
                Identity = $item["Identity"]
                Analysis = $item["Analysis"]
                Rules = $item["Rules"]
                Compare = $item["Compare"]
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
        [switch]$ShowDetails,

        [Parameter()]
        [switch]$IncludeInformationalEvent,

        [Parameter()]
        [switch]$IncludeDebugEvent
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

    # Show filtering information
    $filterInfo = @()
    if (-not $IncludeInformationalEvent) { $filterInfo += "INFORMATION events excluded" }
    if (-not $IncludeDebugEvent) { $filterInfo += "DEBUG events excluded" }
    if ($filterInfo.Count -gt 0) {
        Write-Host "  Filtering: $($filterInfo -join ', ')" -ForegroundColor DarkYellow
    }

    # Analysis Levels
    Write-Host "  Analysis Levels:" -ForegroundColor White
    foreach ($level in $Summary.AnalysisLevels.Keys | Sort-Object) {
        $count = $Summary.AnalysisLevels[$level]
        $color = switch ($level) {
            'ERROR' { 'Red' }
            'WARNING' { 'Yellow' }
            'INFORMATION' { 'Green' }
            'DEBUG' { 'DarkGray' }
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
            'INFORMATION' { 'Green' }
            'DEBUG' { 'DarkGray' }
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

    # File Extension Summary
    if ($Summary.FileExtensionSummary.Count -gt 0) {
        Write-Host ""
        Write-Host "File Extension Analysis:" -ForegroundColor Yellow

        $sortedExtensions = $Summary.FileExtensionSummary.GetEnumerator() |
                           Sort-Object { $_.Value.Count } -Descending |
                           Select-Object -First 15

        foreach ($extEntry in $sortedExtensions) {
            $extension = $extEntry.Key
            $count = $extEntry.Value.Count
            $displayExt = if ($extension -eq "(no extension)") { $extension } else { "*$extension" }

            Write-Host "  $displayExt`: $count files" -ForegroundColor Cyan

            if ($ShowDetails) {
                # Show top rules for this extension
                $topRulesForExt = $extEntry.Value.Rules.GetEnumerator() |
                                 Sort-Object { $_.Value } -Descending |
                                 Select-Object -First 3

                foreach ($ruleEntry in $topRulesForExt) {
                    $ruleName = $ruleEntry.Key
                    $ruleCount = $ruleEntry.Value
                    Write-Host "    $ruleName`: $ruleCount files" -ForegroundColor Gray
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
                    'INFORMATION' { 'Green' }
                    'DEBUG' { 'DarkGray' }
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
                    'INFORMATION' { 'Green' }
                    'DEBUG' { 'DarkGray' }
                    default { 'White' }
                }

                Write-Host "    $level`: $count items" -ForegroundColor $color
            }

            # Show individual file details for file-related categories
            if ($categoryName -like "*FILE*" -and $items.Count -gt 0) {
                # Check if this category contains files with expired signatures
                $expiredSigItems = $items | Where-Object {
                    $_.Rules -and ($_.Rules | Where-Object { $_.Name -eq 'Binaries with expired signatures' })
                }

                if ($expiredSigItems.Count -gt 0) {
                    Write-Host ""
                    Write-Host "    Files with Expired Signatures (grouped by Issuer):" -ForegroundColor DarkCyan

                    # Group by issuer only
                    $groupedByIssuer = @{}
                    foreach ($item in $expiredSigItems) {
                        if ($item.Compare -and $item.Compare.SignatureStatus -and $item.Compare.SignatureStatus.SigningCertificate) {
                            $cert = $item.Compare.SignatureStatus.SigningCertificate
                            $issuer = $cert.Issuer
                            $notAfter = $cert.NotAfter
                            $identity = $item.Identity

                            if (-not $groupedByIssuer.ContainsKey($issuer)) {
                                $groupedByIssuer[$issuer] = @()
                            }
                            $groupedByIssuer[$issuer] += [PSCustomObject]@{
                                Identity = $identity
                                NotAfter = $notAfter
                            }
                        }
                    }

                    # Display grouped results
                    $sortedIssuers = $groupedByIssuer.GetEnumerator() | Sort-Object Name

                    foreach ($issuerGroup in $sortedIssuers) {
                        $issuer = $issuerGroup.Name
                        $files = $issuerGroup.Value
                        $fileCount = $files.Count

                        Write-Host ""
                        Write-Host "      Issuer: $issuer" -ForegroundColor Yellow
                        Write-Host "      Files ($fileCount):" -ForegroundColor White

                        # Sort files by expiration date (handle nulls safely)
                        $sortedFiles = $files | Sort-Object {
                            if ($_.NotAfter) {
                                try { [DateTime]::Parse($_.NotAfter) }
                                catch { [DateTime]::MaxValue }
                            } else {
                                [DateTime]::MaxValue
                            }
                        }

                        # Show first 20 files per issuer
                        $displayLimit = [Math]::Min(20, $fileCount)
                        for ($i = 0; $i -lt $displayLimit; $i++) {
                            $file = $sortedFiles[$i]

                            # Get identity - handle both hashtable and PSCustomObject
                            $filePath = if ($file -is [hashtable]) { $file['Identity'] } else { $file.Identity }

                            # Format date without time
                            $expirationDate = 'Unknown'
                            $notAfterValue = if ($file -is [hashtable]) { $file['NotAfter'] } else { $file.NotAfter }
                            if ($notAfterValue) {
                                try {
                                    $expirationDate = ([DateTime]::Parse($notAfterValue)).ToString('yyyy-MM-dd')
                                }
                                catch {
                                    $expirationDate = 'Unknown'
                                }
                            }
                            Write-Host "        [Expired: $expirationDate] $filePath" -ForegroundColor Gray
                        }

                        if ($fileCount -gt $displayLimit) {
                            Write-Host "        ... and $($fileCount - $displayLimit) more files" -ForegroundColor DarkGray
                        }
                    }

                    # Show other files (non-expired signature issues)
                    $otherFiles = $items | Where-Object {
                        -not ($_.Rules -and ($_.Rules | Where-Object { $_.Name -eq 'Binaries with expired signatures' }))
                    }

                    if ($otherFiles.Count -gt 0) {
                        Write-Host ""
                        Write-Host "    Other Files:" -ForegroundColor DarkCyan

                        $displayLimit = [Math]::Min(20, $otherFiles.Count)
                        for ($i = 0; $i -lt $displayLimit; $i++) {
                            $item = $otherFiles[$i]
                            $identity = $item.Identity
                            $analysis = $item.Analysis

                            $color = switch ($analysis) {
                                'ERROR' { 'Red' }
                                'WARNING' { 'Yellow' }
                                'INFORMATION' { 'Green' }
                                'DEBUG' { 'DarkGray' }
                                default { 'Gray' }
                            }

                            if ($item.Rules -and $item.Rules.Count -gt 0) {
                                $ruleNames = $item.Rules | ForEach-Object { $_.Name }
                                Write-Host "      [$analysis] $identity" -ForegroundColor $color
                                Write-Host "          Rules: $($ruleNames -join ', ')" -ForegroundColor DarkGray
                            }
                            else {
                                Write-Host "      [$analysis] $identity" -ForegroundColor $color
                            }
                        }

                        if ($otherFiles.Count -gt $displayLimit) {
                            Write-Host "      ... and $($otherFiles.Count - $displayLimit) more files" -ForegroundColor DarkGray
                        }
                    }
                }
                else {
                    # No expired signatures, show standard file listing
                    Write-Host ""
                    Write-Host "    Files:" -ForegroundColor DarkCyan

                    $displayLimit = [Math]::Min(50, $items.Count)
                    for ($i = 0; $i -lt $displayLimit; $i++) {
                        $item = $items[$i]
                        $identity = $item.Identity
                        $analysis = $item.Analysis

                        $color = switch ($analysis) {
                            'ERROR' { 'Red' }
                            'WARNING' { 'Yellow' }
                            'INFORMATION' { 'Green' }
                            'DEBUG' { 'DarkGray' }
                            default { 'Gray' }
                        }

                        # Show triggered rules for this file
                        if ($item.Rules -and $item.Rules.Count -gt 0) {
                            $ruleNames = $item.Rules | ForEach-Object { $_.Name }
                            Write-Host "      [$analysis] $identity" -ForegroundColor $color
                            Write-Host "          Rules: $($ruleNames -join ', ')" -ForegroundColor DarkGray
                        }
                        else {
                            Write-Host "      [$analysis] $identity" -ForegroundColor $color
                        }
                    }

                    if ($items.Count -gt $displayLimit) {
                        Write-Host "      ... and $($items.Count - $displayLimit) more files" -ForegroundColor DarkGray
                    }
                }
            }
            # Show details for non-file categories (users, groups, etc.)
            elseif ($items.Count -gt 0) {
                Write-Host ""
                Write-Host "    Items:" -ForegroundColor DarkCyan

                $displayLimit = [Math]::Min(20, $items.Count)
                for ($i = 0; $i -lt $displayLimit; $i++) {
                    $item = $items[$i]
                    $identity = $item.Identity
                    $analysis = $item.Analysis

                    $color = switch ($analysis) {
                        'ERROR' { 'Red' }
                        'WARNING' { 'Yellow' }
                        'INFORMATION' { 'Green' }
                        'DEBUG' { 'DarkGray' }
                        default { 'Gray' }
                    }

                    Write-Host "      [$analysis] $identity" -ForegroundColor $color
                }

                if ($items.Count -gt $displayLimit) {
                    Write-Host "      ... and $($items.Count - $displayLimit) more items" -ForegroundColor DarkGray
                }
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
    $asaData = $jsonContent | ConvertFrom-Json -AsHashtable

    # Generate summary
    Write-Verbose "Analyzing ASA results..."
    $summary = Get-AsaSummary -AsaData $asaData -IncludeInformationalEvent:$IncludeInformationalEvent -IncludeDebugEvent:$IncludeDebugEvent

    # Output results to console
    Write-ConsoleSummary -Summary $summary -ShowDetails:$ShowDetails -IncludeInformationalEvent:$IncludeInformationalEvent -IncludeDebugEvent:$IncludeDebugEvent
}
catch {
    Write-Error "Error processing ASA results: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace
    exit 1
}
