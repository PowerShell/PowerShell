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

function Exec
{
    param([scriptblock]$sb)

    & $sb

    if ($LASTEXITCODE -ne 0)
    {
        throw "Invocation failed for '$sb'. See above errors for details"
    }
}

$ErrorActionPreference = 'Stop'

$wingetPath = (Resolve-Path $WingetRepoPath).Path

# Ensure we have git and PowerShellForGitHub installed
Import-Module -Name PowerShellForGitHub
$null = Get-Command git

# Get the MSI hash from the release body
$msiName = "PowerShell-$ReleaseVersion-win-x64.msi"
$msiHash = GetMsiHash -ReleaseVersion $ReleaseVersion -MsiName $msiName

$publisherName = 'Microsoft'

# Create the manifest
$productName = if ($ReleaseVersion.PreReleaseLabel)
{
    'PowerShell-Preview'
}
else
{
    'PowerShell'
}

$manifestDir = Join-Path $wingetPath 'manifests' 'm' $publisherName $productName $ReleaseVersion
$manifestPath = Join-Path $manifestDir "$publisherName.$productName.yaml"

$manifestContent = @"
PackageIdentifier: $publisherName.$productName
PackageVersion: $ReleaseVersion
PackageName: $productName
Publisher: $publisherName
PackageUrl: https://microsoft.com/PowerShell
License: MIT
LicenseUrl: https://github.com/PowerShell/PowerShell/blob/master/LICENSE.txt
Moniker: $($productName.ToLower())
ShortDescription: $publisherName.$productName
Description: PowerShell is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with your existing tools and is optimized for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models. It includes a command-line shell, an associated scripting language and a framework for processing cmdlets.
Tags:
- powershell
- pwsh
Homepage: https://github.com/PowerShell/PowerShell
Installers:
- Architecture: x64
  InstallerUrl: https://github.com/PowerShell/PowerShell/releases/download/v$ReleaseVersion/$msiName
  InstallerSha256: $msiHash
  InstallerType: msi
PackageLocale: en-US
ManifestType: singleton
ManifestVersion: 1.0.0

"@

Push-Location $wingetPath
try
{
    $branch = "pwsh-$ReleaseVersion"

    Exec { git checkout master }
    Exec { git checkout -b $branch }

    New-Item -Path $manifestDir -ItemType Directory
    Set-Content -Path $manifestPath -Value $manifestContent -Encoding utf8NoBOM

    Exec { git add $manifestPath }
    Exec { git commit -m "Add $productName $ReleaseVersion" }
    Exec { git push origin $branch }

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
