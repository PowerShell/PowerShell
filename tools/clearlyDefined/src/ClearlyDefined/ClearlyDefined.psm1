# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Start the collection (known as harvest) of ClearlyDefined data for a package
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
        (Invoke-WebRequest -Method Post  -Uri 'https://api.clearlydefined.io/harvest' -Body $body -ContentType 'application/json' -MaximumRetryCount 5 -RetryIntervalSec 60 -Verbose).Content
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
        $cacheMinutes = 60
        $cacheCutoff = (get-date).AddMinutes(-$cacheMinutes)
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
            if ($containsKey -and $cdCache[$coordinates].cachedTime -gt $cacheCutoff) {
                Write-Verbose "Returning cached data for $coordinates"
                Write-Output $cdCache[$coordinates]
                continue
            }

            Invoke-RestMethod  -Uri "https://api.clearlydefined.io/definitions/$coordinates" -MaximumRetryCount 5 -RetryIntervalSec 60 | ForEach-Object {
                [bool] $harvested = if ($_.licensed.declared) { $true } else { $false }
                Add-Member -NotePropertyName cachedTime -NotePropertyValue (get-date) -InputObject $_ -PassThru | Add-Member -NotePropertyName harvested -NotePropertyValue $harvested -PassThru
                if ($_.harvested) {
                    Write-Verbose "Caching data for $coordinates"
                    $cdCache[$coordinates] = $_
                }
            }
            $completed++
        }
    }
}

Export-ModuleMember -Function @(
    'Start-ClearlyDefinedHarvest'
    'Get-ClearlyDefinedData'
    'ConvertFrom-ClearlyDefinedCoordinates'
)
