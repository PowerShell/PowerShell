# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Verify that changed files have the proper copyright header.

.DESCRIPTION
    This script checks that all changed source code files have the Microsoft copyright header.
    It supports C#, PowerShell, YAML, and other common file types.

.PARAMETER ChangedFiles
    Array of file paths to check. If not provided, uses git diff to find changed files.

.PARAMETER BaseRef
    Git reference to compare against (e.g., 'origin/master'). Default is 'HEAD^'.

.EXAMPLE
    ./verifyCopyright.ps1
    Check all files changed in the last commit.

.EXAMPLE
    ./verifyCopyright.ps1 -BaseRef origin/master
    Check all files changed compared to origin/master.
#>

param(
    [string[]]$ChangedFiles,
    [string]$BaseRef = 'HEAD^'
)

$ErrorActionPreference = 'Stop'

# Define copyright patterns for different file types
$copyrightPatterns = @{
    # C-style comments (C#, C, C++, Java, JavaScript, TypeScript, etc.)
    'c-style' = @{
        Pattern = '(?m)^//\s*Copyright\s*\(c\)\s*Microsoft\s+Corporation\.\s*\r?\n//\s*Licensed\s+under\s+the\s+MIT\s+License\.'
        Extensions = @('.cs', '.c', '.cpp', '.h', '.hpp', '.java', '.js', '.ts', '.tsx', '.jsx')
    }
    # Hash comments (PowerShell, Python, Ruby, YAML, Shell, etc.)
    'hash' = @{
        Pattern = '(?m)^#\s*Copyright\s*\(c\)\s*Microsoft\s+Corporation\.\s*\r?\n#\s*Licensed\s+under\s+the\s+MIT\s+License\.'
        Extensions = @('.ps1', '.psm1', '.psd1', '.py', '.rb', '.sh', '.yml', '.yaml', '.bashrc', '.bash_profile')
    }
    # XML comments
    'xml' = @{
        Pattern = '<!--\s*Copyright\s*\(c\)\s*Microsoft\s+Corporation\.\s*\r?\n\s*Licensed\s+under\s+the\s+MIT\s+License\.\s*-->'
        Extensions = @('.xml', '.csproj', '.proj', '.props', '.targets', '.resx', '.xaml')
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
