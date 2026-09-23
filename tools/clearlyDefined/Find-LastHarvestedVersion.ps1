# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Find the last harvested version of a NuGet package from ClearlyDefined.

.DESCRIPTION
    Searches for the last harvested version of a package by checking versions
    backwards from the specified current version. This is useful for reverting
    to a known-good harvested version when a newer version hasn't been harvested yet.

.PARAMETER Name
    The NuGet package name to search for.

.PARAMETER CurrentVersion
    The version to start searching backwards from. Version comparison uses semantic versioning.

.PARAMETER PackageSourceName
    The NuGet package source name to use when searching for available versions.
    Default is 'findMissingNoticesNugetOrg' if not specified.

.EXAMPLE
    Find-LastHarvestedVersion -Name "Microsoft.Windows.Compatibility" -CurrentVersion "8.0.24"

    # This will return "8.0.22" if that's the last harvested version

.NOTES
    Requires the ClearlyDefined module to be imported:
    Import-Module ".\clearlyDefined\src\ClearlyDefined" -Force
#>

function Find-LastHarvestedVersion {
    [CmdletBinding()]
    param(
        [parameter(Mandatory)]
        [string]$Name,

        [parameter(Mandatory)]
        [string]$CurrentVersion,

        [string]$PackageSourceName = 'findMissingNoticesNugetOrg'
    )

    try {
        Write-Verbose "Finding last harvested version for $Name starting from v$CurrentVersion..."

        # Parse the current version
        try {
            [System.Management.Automation.SemanticVersion]$currentSemVer = $CurrentVersion
        } catch {
            [Version]$currentSemVer = $CurrentVersion
        }

        # First try the ClearlyDefined search API (more efficient)
        try {
            Write-Verbose "Searching ClearlyDefined API for versions of $Name (sorted by release date)..."
            # Get versions sorted by release date descending (newest first) for efficiency
            $versions = Get-ClearlyDefinedPackageVersions -PackageName $Name

            if ($versions -and $versions.Count -gt 0) {
                # Results are already sorted by release date newest first
                # Filter to versions <= current version
                foreach ($versionInfo in $versions) {
                    try {
                        $versionObj = [System.Management.Automation.SemanticVersion]$versionInfo.Version
                        if ($versionObj -le $currentSemVer) {
                            # Check harvest status
                            if ($versionInfo.Harvested) {
                                Write-Verbose "Found harvested version: v$($versionInfo.Version)"
                                return $versionInfo.Version
                            } else {
                                Write-Verbose "v$($versionInfo.Version) - Not harvested, continuing..."
                            }
                        }
                    } catch {
                        # Skip versions that can't be parsed
                    }
                }

                Write-Verbose "No harvested version found in ClearlyDefined results"
                return $null
            }
        } catch {
            Write-Verbose "ClearlyDefined search API failed ($_), falling back to NuGet search..."
        }

        # Fallback: Get all available versions from NuGet and check individually
        Write-Verbose "Falling back to NuGet source search..."

        # Ensure package source exists
        if (!(Get-PackageSource -Name $PackageSourceName -ErrorAction SilentlyContinue)) {
            Write-Verbose "Registering package source: $PackageSourceName"
            $null = Register-PackageSource -Name $PackageSourceName -Location https://www.nuget.org/api/v2 -ProviderName NuGet
        }

        # Get all available versions from NuGet
        try {
            $allVersions = Find-Package -Name $Name -AllowPrereleaseVersions -source $PackageSourceName -AllVersions -ErrorAction SilentlyContinue | ForEach-Object {
                try {
                    $packageVersion = [System.Management.Automation.SemanticVersion]$_.Version
                } catch {
                    $packageVersion = [Version]$_.Version
                }
                $_ | Add-Member -Name SemVer -MemberType NoteProperty -Value $packageVersion -PassThru
            } | Where-Object { $_.SemVer -le $currentSemVer } | Sort-Object -Property SemVer -Descending | ForEach-Object { $_.Version }
        } catch {
            Write-Warning "Failed to get versions for $Name : $_"
            return $null
        }

        if (!$allVersions) {
            Write-Verbose "No versions found for $Name"
            return $null
        }

        # Check each version backwards until we find one that's harvested
        foreach ($version in $allVersions) {
            $pkg = [PSCustomObject]@{
                type           = "nuget"
                Name           = $Name
                PackageVersion = $version
            }

            try {
                $result = $pkg | Get-ClearlyDefinedData
                if ($result -and $result.harvested) {
                    Write-Verbose "Found harvested version: v$version"
                    return $version
                } else {
                    Write-Verbose "v$version - Not harvested, continuing..."
                }
            } catch {
                Write-Verbose "Error checking v$version : $_" -Verbose
            }
        }

        Write-Verbose "No harvested version found for $Name"
        return $null
    } finally {
        Save-ClearlyDefinedCache
    }
}

# If this script is called directly (not sourced), run a test
if ($MyInvocation.InvocationName -eq '.' -or $MyInvocation.Line -like '. "*Find-LastHarvestedVersion*') {
    # Script was sourced, just load the function
} else {
    # Script was called directly
    Write-Host "Testing Find-LastHarvestedVersion function..."
    Write-Host "Ensure ClearlyDefined module is loaded first:"
    Write-Host '  Import-Module ".\clearlydefined\src\ClearlyDefined" -Force'
    Write-Host ""
    Write-Host "Example usage:"
    Write-Host '  Find-LastHarvestedVersion -Name "Microsoft.Windows.Compatibility" -CurrentVersion "8.0.24"'
}
