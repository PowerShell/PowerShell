# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Start the collection (known as harvest) of ClearlyDefined data for a package

$retryIntervalSec = 90
$maxRetryCount = 5
function Start-ClearlyDefinedHarvest {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [Alias('Type')]
        [validateset('nuget')]
        [string]
        $PackageType = 'nuget',

        [parameter(mandatory = $true, ValueFromPipelineByPropertyName=$true)]
        [Alias('Name')]
        [string]
        $PackageName,

        [parameter(mandatory = $true, ValueFromPipelineByPropertyName=$true)]
        [Alias('Version')]
        [Alias('Revision')]
        [string]
        $PackageVersion
    )

    Process {
        $coordinates = Get-ClearlyDefinedCoordinates @PSBoundParameters
        $body = @{tool='package';coordinates=$coordinates} | convertto-json
        Write-Verbose $body -Verbose
        Start-job -ScriptBlock {
            Invoke-WebRequest -Method Post  -Uri 'https://api.clearlydefined.io/harvest' -Body $using:body -ContentType 'application/json' -MaximumRetryCount $using:maxRetryCount -RetryIntervalSec $using:retryIntervalSec
        }
    }
}

function ConvertFrom-ClearlyDefinedCoordinates {
    [CmdletBinding()]
    param(
        [parameter(mandatory = $true, ValueFromPipeline = $true)]
        [string]
        $Coordinates
    )

    Begin {}
    Process {
        $parts = $Coordinates.Split('/')
        [PSCustomObject]@{
            type = $parts[0]
            provider = $parts[1]
            namespace = $parts[2]
            name = $parts[3]
            revision = $parts[4]
        }
    }
    End {}
}

# Get the coordinate string for a package
Function Get-ClearlyDefinedCoordinates {
    [CmdletBinding()]
    param(
        [validateset('nuget')]
        [string]
        $PackageType = 'nuget',
        [parameter(mandatory = $true)]
        [string]
        $PackageName,
        [parameter(mandatory = $true)]
        [string]
        $PackageVersion
    )

    return "$PackageType/$PackageType/-/$PackageName/$PackageVersion"
}

# Cache of ClearlyDefined data
$cdCache = @{}

function Test-ClearlyDefinedCachePersistenceAllowed {
    [CmdletBinding()]
    param()

    if ($env:TF_BUILD -or $env:ADO_BUILD_ID -or $env:BUILD_BUILDID) {
        return $false
    }

    if ($env:GITHUB_ACTIONS -or $env:GITHUB_RUN_ID) {
        return $false
    }

    return $true
}

function Get-ClearlyDefinedCachePath {
    [CmdletBinding()]
    param()

    $tempPath = [System.IO.Path]::GetTempPath()
    return (Join-Path -Path $tempPath -ChildPath 'clearlydefined-cache.json')
}

function Save-ClearlyDefinedCache {
    [CmdletBinding()]
    param()

    if (-not (Test-ClearlyDefinedCachePersistenceAllowed)) {
        Write-Verbose 'Skipping cache persistence for CI environment.'
        return
    }

    if ($cdCache.Count -eq 0) {
        Write-Verbose 'No cache entries to persist.'
        return
    }

    $cachePath = Get-ClearlyDefinedCachePath
    $entries = foreach ($key in $cdCache.Keys) {
        [PSCustomObject]@{
            coordinates = $key
            data = $cdCache[$key]
        }
    }

    $cachePayload = @{
        savedAtUtc = (Get-Date).ToUniversalTime()
        entries = $entries
    } | ConvertTo-Json -Depth 20

    $cachePayload | Set-Content -Path $cachePath -Encoding UTF8
    Write-Verbose "Persisted cache to $cachePath"
}

function Import-ClearlyDefinedCache {
    [CmdletBinding()]
    param()

    if (-not (Test-ClearlyDefinedCachePersistenceAllowed)) {
        Write-Verbose 'Skipping cache import for CI environment.'
        return
    }

    $cachePath = Get-ClearlyDefinedCachePath
    if (-not (Test-Path -Path $cachePath)) {
        Write-Verbose 'No persisted cache found.'
        return
    }

    try {
        $payload = Get-Content -Path $cachePath -Raw | ConvertFrom-Json
    } catch {
        Write-Verbose "Failed to read cache file: $cachePath"
        return
    }

    if (-not $payload.entries) {
        Write-Verbose 'Cache file did not contain entries.'
        return
    }

    foreach ($entry in $payload.entries) {
        if (-not $entry.coordinates -or -not $entry.data) {
            continue
        }

        try {
            $entry.data.cachedTime = [datetime]$entry.data.cachedTime
        } catch {
            continue
        }

        $cdCache[$entry.coordinates] = $entry.data
    }

    Write-Verbose "Imported $($cdCache.Count) cache entries from $cachePath"
}

# Search for packages in ClearlyDefined
Function Search-ClearlyDefined {
    [CmdletBinding()]
    param(
        [string]$Type = 'nuget',
        [string]$Provider = 'nuget',
        [string]$Namespace,
        [string]$Name,
        [string]$Pattern,
        [datetime]$ReleasedAfter,
        [datetime]$ReleasedBefore,
        [ValidateSet('releaseDate', 'name')]
        [string]$Sort,
        [switch]$SortDesc
    )

    $queryParams = @()
    if ($Type) { $queryParams += "type=$([System.Uri]::EscapeDataString($Type))" }
    if ($Provider) { $queryParams += "provider=$([System.Uri]::EscapeDataString($Provider))" }
    if ($Namespace) { $queryParams += "namespace=$([System.Uri]::EscapeDataString($Namespace))" }
    if ($Name) { $queryParams += "name=$([System.Uri]::EscapeDataString($Name))" }
    if ($Pattern) { $queryParams += "pattern=$([System.Uri]::EscapeDataString($Pattern))" }
    if ($ReleasedAfter) { $queryParams += "releasedAfter=$($ReleasedAfter.ToString('o'))" }
    if ($ReleasedBefore) { $queryParams += "releasedBefore=$($ReleasedBefore.ToString('o'))" }
    if ($Sort) { $queryParams += "sort=$([System.Uri]::EscapeDataString($Sort))" }
    if ($SortDesc) { $queryParams += "sortDesc=true" }

    $searchUri = "https://api.clearlydefined.io/definitions?" + ($queryParams -join '&')
    Write-Verbose "Searching ClearlyDefined: $searchUri"

    try {
        $results = Invoke-RestMethod -Uri $searchUri -MaximumRetryCount $maxRetryCount -RetryIntervalSec $retryIntervalSec
        return $results
    } catch {
        if ($retryIntervalSec -lt 300) {
            $retryIntervalSec++
        }

        Write-Warning "Failed to search ClearlyDefined: $_"
        return $null
    }
}

# Get available versions for a NuGet package with harvest status
Function Get-ClearlyDefinedPackageVersions {
    [CmdletBinding()]
    param(
        [parameter(mandatory = $true)]
        [string]
        $PackageName,

        [validateset('nuget')]
        [string]
        $PackageType = 'nuget'
    )

    # Search for all definitions of this package, sorted by release date (newest first)
    Write-Verbose "Fetching versions of $PackageName from ClearlyDefined..."

    $results = Search-ClearlyDefined -Type $PackageType -Provider nuget -Name $PackageName -Sort releaseDate -SortDesc

    if (!$results) {
        Write-Verbose "No results found for $PackageName"
        return @()
    }

    # Convert results to version info objects
    $versions = @()

    # API returns results in different formats depending on the query
    $dataArray = $null
    if ($results.data) {
        $dataArray = $results.data
    } elseif ($results -is [array]) {
        $dataArray = $results
    } elseif ($results.PSObject.Properties.Count -gt 0) {
        # If it's an object with properties, try to extract the actual results
        foreach ($prop in $results.PSObject.Properties) {
            if ($prop.Value -is [object] -and $prop.Value.revision) {
                $dataArray += $prop.Value
            }
        }
    }

    if ($dataArray) {
        foreach ($item in $dataArray) {
            if ($item.revision) {
                $harvested = if ($item.licensed -and $item.licensed.declared) { $true } else { $false }

                $versions += [PSCustomObject]@{
                    Name      = $item.name
                    Version   = $item.revision
                    Harvested = $harvested
                    Licensed  = $item.licensed.declared
                }
            }
        }
    }

    # Results are already sorted by API, no need to re-sort
    return $versions
}

# Get the ClearlyDefined data for a package
Function Get-ClearlyDefinedData {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [Alias('Type')]
        [validateset('nuget')]
        [string]
        $PackageType = 'nuget',

        [parameter(mandatory = $true, ValueFromPipelineByPropertyName=$true)]
        [Alias('Name')]
        [string]
        $PackageName,

        [parameter(mandatory = $true, ValueFromPipelineByPropertyName=$true)]
        [Alias('Revision')]
        [string]
        $PackageVersion
    )

    Begin {
        # Different TTLs for different cache types
        $harvestedCacheMinutes = 60      # Cache positive results for 60 minutes
        $nonHarvestedCacheMinutes = 30   # Cache negative results for 30 minutes (less aggressive)
        $coordinateList = @()
    }

    Process {
        $coordinateList += Get-ClearlyDefinedCoordinates @PSBoundParameters
    }

    end {
        $total = $coordinateList.Count
        $completed = 0
        foreach($coordinates in $coordinateList) {
            Write-Progress -Activity "Getting ClearlyDefined data" -Status "Getting data for $coordinates" -PercentComplete (($completed / $total) * 100)
            $containsKey = $cdCache.ContainsKey($coordinates)

            if ($containsKey) {
                $cached = $cdCache[$coordinates]
                # Check if cache entry is still valid based on its type
                $cacheCutoff = if ($cached.harvestedResult) {
                    (get-date).AddMinutes(-$harvestedCacheMinutes)
                } else {
                    (get-date).AddMinutes(-$nonHarvestedCacheMinutes)
                }

                if ($cached.cachedTime -gt $cacheCutoff) {
                    Write-Progress -Activity "Getting ClearlyDefined data" -Status "Getting data for $coordinates - cache hit" -PercentComplete (($completed / $total) * 100)
                    Write-Verbose "Returning cached data for $coordinates (harvested: $($cached.harvestedResult))"
                    Write-Output $cached
                    $completed++
                    continue
                }
            }

            Write-Progress -Activity "Getting ClearlyDefined data" -Status "Getting data for $coordinates - cache miss" -PercentComplete (($completed / $total) * 100)

            try {
                Invoke-RestMethod -Uri "https://api.clearlydefined.io/definitions/$coordinates" -MaximumRetryCount $maxRetryCount -RetryIntervalSec $retryIntervalSec | ForEach-Object {
                    [bool] $harvested = if ($_.licensed.declared) { $true } else { $false }
                    # Always cache, with harvestedResult property to distinguish for TTL purposes
                    Add-Member -NotePropertyName cachedTime -NotePropertyValue (get-date) -InputObject $_ -PassThru |
                        Add-Member -NotePropertyName harvested -NotePropertyValue $harvested -PassThru |
                        Add-Member -NotePropertyName harvestedResult -NotePropertyValue $harvested -PassThru |
                        ForEach-Object {
                            Write-Verbose "Caching data for $coordinates (harvested: $($_.harvested))"
                            $cdCache[$coordinates] = $_
                            Write-Output $_
                        }
                }
            } catch {
                if ($retryIntervalSec -lt 300) {
                    $retryIntervalSec++
                }

                Write-Warning "Failed to get ClearlyDefined data for $coordinates : $_"
                # Return a minimal object indicating failure/not harvested so the pipeline continues
                $failedResult = [PSCustomObject]@{
                    coordinates = $coordinates
                    harvested = $false
                    harvestedResult = $false
                    cachedTime = (get-date)
                    licensed = @{ declared = $null }
                }
                Write-Output $failedResult
            }
            $completed++
        }
    }
}

Export-ModuleMember -Function @(
    'Start-ClearlyDefinedHarvest'
    'Get-ClearlyDefinedData'
    'ConvertFrom-ClearlyDefinedCoordinates'
    'Search-ClearlyDefined'
    'Get-ClearlyDefinedPackageVersions'
    'Save-ClearlyDefinedCache'
    'Import-ClearlyDefinedCache'
    'Test-ClearlyDefinedCachePersistenceAllowed'
    'Get-ClearlyDefinedCachePath'
)
