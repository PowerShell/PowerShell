# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [Parameter(Mandatory)]
    [semver]
    $ReleaseVersion,

    [Parameter()]
    [string]
    $WingetRepoPath = "$PSScriptRoot/../../winget-pkgs",

    [Parameter()]
    [string]
    $FromRepository = 'rjmholt',

    [Parameter()]
    [string]
    $GitHubToken
)

function GetMsiHash
{
    param(
        [Parameter(Mandatory)]
        [string]
        $ReleaseVersion,

        [Parameter(Mandatory)]
        $MsiName
    )

    $releaseParams = @{
        Tag = "v$ReleaseVersion"
        OwnerName = 'PowerShell'
        RepositoryName = 'PowerShell'
    }

    if ($GitHubToken) { $releaseParams.AccessToken = $GitHubToken }

    $releaseDescription = (Get-GitHubRelease @releaseParams).body

    $regex = [regex]::new("powershell-$ReleaseVersion-win-x64.msi.*?([0-9A-F]{64})", 'SingleLine,IgnoreCase')

    return $regex.Match($releaseDescription).Groups[1].Value
}

function GetThisScriptRepoUrl
{
    # Find the root of the repo
    $prefix = $PSScriptRoot
    while ($prefix)
    {
        if (Test-Path "$prefix/LICENSE.txt")
        {
            break
        }

        $prefix = Split-Path $prefix
    }

    $stem = $PSCommandPath.Substring($prefix.Length + 1).Replace('\', '/')

    return "https://github.com/PowerShell/PowerShell/blob/master/$stem"
}

$ErrorActionPreference = 'Stop'

$wingetPath = (Resolve-Path $WingetRepoPath).Path

# Ensure we have PowerShellForGitHub installed
Import-Module -Name PowerShellForGitHub

# Get the MSI hash from the release body
$msiName = "PowerShell-$ReleaseVersion-win-x64.msi"
$msiHash = GetMsiHash -ReleaseVersion $ReleaseVersion -MsiName $msiName

# Create the manifest
$productName = if ($ReleaseVersion.PreReleaseLabel)
{
    "PowerShell-Preview"
}
else
{
    "PowerShell"
}

$manifestPath = Join-Path $wingetPath "manifests" "Microsoft" $productName "$ReleaseVersion.yaml"

$manifestContent = @"
Id: Microsoft.$productName
Version: $ReleaseVersion
Name: $productName
Publisher: Microsoft
License: MIT
LicenseUrl: https://github.com/PowerShell/PowerShell/blob/master/LICENSE.txt
AppMoniker: $($productName.ToLower())
Tags: powershell, pwsh
Description: PowerShell is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with your existing tools and is optimized for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models. It includes a command-line shell, an associated scripting language and a framework for processing cmdlets.
Homepage: https://github.com/PowerShell/PowerShell
Installers:
  - Arch: x64
    Url: https://github.com/PowerShell/PowerShell/releases/download/v$ReleaseVersion/$msiName
    Sha256: $msiHash
    InstallerType: msi

"@

# Get the path to this script in the PS repo so we can put a link in the PR body
$scriptPath = $MyInvocation.MyCommand.Source


Push-Location $wingetPath
try
{
    $branch = "pwsh-$ReleaseVersion"

    git checkout master
    git checkout -b $branch

    Set-Content -Path $manifestPath -Value $manifestContent -Encoding utf8NoBOM

    git add $manifestPath
    git commit -m "Add $productName $ReleaseVersion"
    git push origin $branch

    $prParams = @{
        Title = "Add $productName $ReleaseVersion"
        Body = "This pull request is automatically generated. See $(GetThisScriptRepoUrl)."
        Head = $branch
        HeadOwner = $FromRepository
        Base = 'master'
        Owner = 'Microsoft'
        RepositoryName = 'winget-pkgs'
        MaintainerCanModify = $true
    }

    if ($GitHubToken) { $prParams.AccessToken = $GitHubToken }

    New-GitHubPullRequest @prParams
}
finally
{
    git checkout master
    Pop-Location
}
