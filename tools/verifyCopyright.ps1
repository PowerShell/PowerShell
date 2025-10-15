# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Verify that changed files have the proper copyright header.

.DESCRIPTION
    This script checks that all changed source code files have the Microsoft copyright header.
    It supports C# (.cs) and PowerShell (.ps1, .psm1, .psd1) files as specified in the contributing guide.

.PARAMETER ChangedFiles
    Array of file paths to check. If not provided, uses GitHub API to find changed files in a PR.

.EXAMPLE
    ./verifyCopyright.ps1
    Check all files changed in the current PR.

.EXAMPLE
    ./verifyCopyright.ps1 -ChangedFiles @('src/test.cs', 'tools/test.ps1')
    Check specific files.
#>

param(
    [string[]]$ChangedFiles
)

$ErrorActionPreference = 'Stop'

# Define copyright patterns for different file types (only those in CONTRIBUTING.md)
$copyrightPatterns = @{
    # C# files
    'cs' = @{
        Pattern = '(?m)^//\s*Copyright\s*\(c\)\s*Microsoft\s+Corporation\.\s*\r?\n//\s*Licensed\s+under\s+the\s+MIT\s+License\.'
        Extensions = @('.cs')
    }
    # PowerShell files
    'powershell' = @{
        Pattern = '(?m)^#\s*Copyright\s*\(c\)\s*Microsoft\s+Corporation\.\s*\r?\n#\s*Licensed\s+under\s+the\s+MIT\s+License\.'
        Extensions = @('.ps1', '.psm1', '.psd1')
    }
}

# File types that don't require copyright headers
$excludePatterns = @(
    '\.md$',           # Markdown files
    '\.txt$',          # Text files
    '\.json$',         # JSON files
    '\.gitignore$',    # Git ignore files
    '\.gitattributes$',# Git attributes files
    '^LICENSE',        # License files
    '^CHANGELOG',      # Changelog files
    '^README',         # Readme files
    '\.sln$',          # Solution files
    '\.config$',       # Config files
    '\.editorconfig$', # Editor config files
    '\.png$',          # Image files
    '\.jpg$',
    '\.gif$',
    '\.svg$',
    '\.ico$',
    '\.pdf$',          # PDF files
    '\.zip$',          # Archive files
    '\.tar$',
    '\.gz$',
    '^\.vscode/',      # VS Code settings
    '^\.github/ISSUE_TEMPLATE/', # Issue templates
    '^\.github/PULL_REQUEST_TEMPLATE/', # PR templates
    '/testdata/',      # Test data directories
    '/test/assets/',   # Test assets
    '\.man$',          # Manual files
    '\.sample$',       # Sample files
    '\.resx$',         # Resource files
    '\.csproj$',       # Project files
    '\.props$',
    '\.targets$'
)

function Test-ShouldCheckCopyright {
    param([string]$FilePath)
    
    # Skip if file doesn't exist (deleted files)
    if (-not (Test-Path $FilePath)) {
        return $false
    }
    
    # Skip if it's a directory
    if ((Get-Item $FilePath) -is [System.IO.DirectoryInfo]) {
        return $false
    }
    
    # Check if file matches exclude patterns
    foreach ($pattern in $excludePatterns) {
        if ($FilePath -match $pattern) {
            Write-Verbose "Skipping $FilePath (matches exclude pattern: $pattern)"
            return $false
        }
    }
    
    return $true
}

function Get-CopyrightPattern {
    param([string]$FilePath)
    
    $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
    
    foreach ($styleKey in $copyrightPatterns.Keys) {
        $style = $copyrightPatterns[$styleKey]
        if ($style.Extensions -contains $extension) {
            return $style.Pattern
        }
    }
    
    return $null
}

function Test-FileCopyright {
    param([string]$FilePath)
    
    if (-not (Test-ShouldCheckCopyright -FilePath $FilePath)) {
        return @{ HasCopyright = $true; Reason = 'Excluded' }
    }
    
    $pattern = Get-CopyrightPattern -FilePath $FilePath
    if (-not $pattern) {
        # Unknown file type, skip
        Write-Verbose "Skipping $FilePath (unknown file type or not in contributing guide)"
        return @{ HasCopyright = $true; Reason = 'Unknown file type' }
    }
    
    # Read first few lines of the file (copyright should be at the top)
    $content = Get-Content -Path $FilePath -Raw -ErrorAction SilentlyContinue
    if (-not $content) {
        return @{ HasCopyright = $false; Reason = 'Empty file or cannot read' }
    }
    
    # Get first 500 characters (should be enough for copyright header)
    $header = $content.Substring(0, [Math]::Min(500, $content.Length))
    
    if ($header -match $pattern) {
        return @{ HasCopyright = $true; Reason = 'Found' }
    } else {
        return @{ HasCopyright = $false; Reason = 'Missing copyright header' }
    }
}

# Get list of changed files
if (-not $ChangedFiles) {
    Write-Host "Getting changed files from PR..."
    
    # Check if we're in a GitHub Actions PR context
    if ($env:GITHUB_EVENT_NAME -eq 'pull_request') {
        # Use GitHub API to get changed files
        $prNumber = $env:GITHUB_REF -replace 'refs/pull/(\d+)/merge', '$1'
        
        # Parse event file to get PR number
        if (Test-Path $env:GITHUB_EVENT_PATH) {
            $event = Get-Content $env:GITHUB_EVENT_PATH | ConvertFrom-Json
            $prNumber = $event.pull_request.number
            
            Write-Host "Fetching files from PR #$prNumber..."
            
            # Use gh CLI if available, otherwise use REST API
            if (Get-Command gh -ErrorAction SilentlyContinue) {
                $ChangedFiles = gh pr view $prNumber --json files --jq '.files[].path' 2>$null
            } else {
                # Use GitHub REST API via Invoke-RestMethod
                $token = $env:GITHUB_TOKEN
                $repo = $env:GITHUB_REPOSITORY
                
                if ($token -and $repo) {
                    $headers = @{
                        Authorization = "token $token"
                        Accept = "application/vnd.github.v3+json"
                    }
                    
                    $uri = "https://api.github.com/repos/$repo/pulls/$prNumber/files"
                    try {
                        $response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get
                        $ChangedFiles = $response | Where-Object { $_.status -ne 'removed' } | Select-Object -ExpandProperty filename
                    } catch {
                        Write-Warning "Could not fetch files from GitHub API: $_"
                    }
                }
            }
        }
    }
    
    # Fallback: if we still don't have files, use git diff
    if (-not $ChangedFiles) {
        Write-Host "Using git diff to find changed files..."
        $baseRef = if ($env:GITHUB_BASE_REF) { "origin/$env:GITHUB_BASE_REF" } else { "HEAD^" }
        
        # Check if we're in a git repository
        $isGitRepo = git rev-parse --git-dir 2>$null
        if ($isGitRepo) {
            try {
                $ChangedFiles = git diff --name-only --diff-filter=AM $baseRef 2>&1
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Could not get diff from $baseRef"
                    $ChangedFiles = @()
                }
            } catch {
                Write-Warning "Could not get diff: $_"
                $ChangedFiles = @()
            }
        }
    }
}

if (-not $ChangedFiles -or $ChangedFiles.Count -eq 0) {
    Write-Host "No changed files to check."
    exit 0
}

Write-Host "Checking $($ChangedFiles.Count) changed files for copyright headers..."
Write-Host ""

$filesWithoutCopyright = @()
$filesChecked = 0

foreach ($file in $ChangedFiles) {
    if ([string]::IsNullOrWhiteSpace($file)) {
        continue
    }
    
    $result = Test-FileCopyright -FilePath $file
    
    if ($result.Reason -eq 'Found' -or $result.Reason -eq 'Excluded' -or $result.Reason -eq 'Unknown file type') {
        Write-Verbose "✓ $file - $($result.Reason)"
    } elseif (-not $result.HasCopyright) {
        Write-Host "✗ $file - $($result.Reason)" -ForegroundColor Red
        $filesWithoutCopyright += $file
        $filesChecked++
    }
}

Write-Host ""
Write-Host "Copyright check complete!"
Write-Host "Files checked: $filesChecked"
Write-Host "Files without copyright: $($filesWithoutCopyright.Count)"

if ($filesWithoutCopyright.Count -gt 0) {
    Write-Host ""
    Write-Host "The following files are missing copyright headers:" -ForegroundColor Red
    $filesWithoutCopyright | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Please add the appropriate copyright header to the beginning of each file:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For C# files (.cs):" -ForegroundColor Yellow
    Write-Host "  // Copyright (c) Microsoft Corporation." -ForegroundColor Yellow
    Write-Host "  // Licensed under the MIT License." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For PowerShell files (.ps1, .psm1, .psd1):" -ForegroundColor Yellow
    Write-Host "  # Copyright (c) Microsoft Corporation." -ForegroundColor Yellow
    Write-Host "  # Licensed under the MIT License." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "See .github/CONTRIBUTING.md for more details." -ForegroundColor Yellow
    
    exit 1
}

Write-Host "All checked files have proper copyright headers! ✓" -ForegroundColor Green
exit 0

# File types that don't require copyright headers
$excludePatterns = @(
    '\.md$',           # Markdown files
    '\.txt$',          # Text files
    '\.json$',         # JSON files
    '\.gitignore$',    # Git ignore files
    '\.gitattributes$',# Git attributes files
    '^LICENSE',        # License files
    '^CHANGELOG',      # Changelog files
    '^README',         # Readme files
    '\.sln$',          # Solution files
    '\.config$',       # Config files
    '\.editorconfig$', # Editor config files
    '\.png$',          # Image files
    '\.jpg$',
    '\.gif$',
    '\.svg$',
    '\.ico$',
    '\.pdf$',          # PDF files
    '\.zip$',          # Archive files
    '\.tar$',
    '\.gz$',
    '^\.vscode/',      # VS Code settings
    '^\.github/ISSUE_TEMPLATE/', # Issue templates
    '^\.github/PULL_REQUEST_TEMPLATE/', # PR templates
    '/testdata/',      # Test data directories
    '/test/assets/',   # Test assets
    '\.man$',          # Manual files
    '\.sample$'        # Sample files
)

function Test-ShouldCheckCopyright {
    param([string]$FilePath)
    
    # Skip if file doesn't exist (deleted files)
    if (-not (Test-Path $FilePath)) {
        return $false
    }
    
    # Skip if it's a directory
    if ((Get-Item $FilePath) -is [System.IO.DirectoryInfo]) {
        return $false
    }
    
    # Check if file matches exclude patterns
    foreach ($pattern in $excludePatterns) {
        if ($FilePath -match $pattern) {
            Write-Verbose "Skipping $FilePath (matches exclude pattern: $pattern)"
            return $false
        }
    }
    
    return $true
}

function Get-CopyrightPattern {
    param([string]$FilePath)
    
    $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
    
    foreach ($styleKey in $copyrightPatterns.Keys) {
        $style = $copyrightPatterns[$styleKey]
        if ($style.Extensions -contains $extension) {
            return $style.Pattern
        }
    }
    
    return $null
}

function Test-FileCopyright {
    param([string]$FilePath)
    
    if (-not (Test-ShouldCheckCopyright -FilePath $FilePath)) {
        return @{ HasCopyright = $true; Reason = 'Excluded' }
    }
    
    $pattern = Get-CopyrightPattern -FilePath $FilePath
    if (-not $pattern) {
        # Unknown file type, skip
        Write-Verbose "Skipping $FilePath (unknown file type)"
        return @{ HasCopyright = $true; Reason = 'Unknown file type' }
    }
    
    # Read first few lines of the file (copyright should be at the top)
    $content = Get-Content -Path $FilePath -Raw -ErrorAction SilentlyContinue
    if (-not $content) {
        return @{ HasCopyright = $false; Reason = 'Empty file or cannot read' }
    }
    
    # Get first 500 characters (should be enough for copyright header)
    $header = $content.Substring(0, [Math]::Min(500, $content.Length))
    
    if ($header -match $pattern) {
        return @{ HasCopyright = $true; Reason = 'Found' }
    } else {
        return @{ HasCopyright = $false; Reason = 'Missing copyright header' }
    }
}

# Get list of changed files
if (-not $ChangedFiles) {
    Write-Host "Finding changed files..."
    
    # Check if we're in a git repository
    $isGitRepo = git rev-parse --git-dir 2>$null
    if (-not $isGitRepo) {
        Write-Error "Not in a git repository"
        exit 1
    }
    
    # For pull requests, compare against the base branch
    if ($env:GITHUB_BASE_REF) {
        $baseRef = "origin/$env:GITHUB_BASE_REF"
        Write-Host "Comparing against base branch: $baseRef"
    } elseif ($env:GITHUB_EVENT_NAME -eq 'push') {
        # For pushes, use the before commit
        if ($env:GITHUB_EVENT_BEFORE) {
            $baseRef = $env:GITHUB_EVENT_BEFORE
            Write-Host "Comparing against before commit: $baseRef"
        }
    }
    
    # Get changed files
    try {
        $ChangedFiles = git diff --name-only --diff-filter=AM $baseRef 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not get diff from $baseRef, using HEAD^"
            $ChangedFiles = git diff --name-only --diff-filter=AM HEAD^
        }
    } catch {
        Write-Warning "Could not get diff, using HEAD^"
        $ChangedFiles = git diff --name-only --diff-filter=AM HEAD^
    }
}

if (-not $ChangedFiles) {
    Write-Host "No changed files to check."
    exit 0
}

Write-Host "Checking $($ChangedFiles.Count) changed files for copyright headers..."
Write-Host ""

$filesWithoutCopyright = @()
$filesChecked = 0

foreach ($file in $ChangedFiles) {
    if ([string]::IsNullOrWhiteSpace($file)) {
        continue
    }
    
    $result = Test-FileCopyright -FilePath $file
    
    if ($result.Reason -eq 'Found' -or $result.Reason -eq 'Excluded' -or $result.Reason -eq 'Unknown file type') {
        Write-Verbose "✓ $file - $($result.Reason)"
    } elseif (-not $result.HasCopyright) {
        Write-Host "✗ $file - $($result.Reason)" -ForegroundColor Red
        $filesWithoutCopyright += $file
        $filesChecked++
    }
}

Write-Host ""
Write-Host "Copyright check complete!"
Write-Host "Files checked: $filesChecked"
Write-Host "Files without copyright: $($filesWithoutCopyright.Count)"

if ($filesWithoutCopyright.Count -gt 0) {
    Write-Host ""
    Write-Host "The following files are missing copyright headers:" -ForegroundColor Red
    $filesWithoutCopyright | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Please add the following copyright header to the beginning of each file:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For C#, C, C++, Java, JavaScript, TypeScript files:" -ForegroundColor Yellow
    Write-Host "  // Copyright (c) Microsoft Corporation." -ForegroundColor Yellow
    Write-Host "  // Licensed under the MIT License." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For PowerShell, Python, Ruby, YAML, Shell files:" -ForegroundColor Yellow
    Write-Host "  # Copyright (c) Microsoft Corporation." -ForegroundColor Yellow
    Write-Host "  # Licensed under the MIT License." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For XML, CSPROJ, RESX files:" -ForegroundColor Yellow
    Write-Host "  <!-- Copyright (c) Microsoft Corporation." -ForegroundColor Yellow
    Write-Host "       Licensed under the MIT License. -->" -ForegroundColor Yellow
    
    exit 1
}

Write-Host "All checked files have proper copyright headers! ✓" -ForegroundColor Green
exit 0
