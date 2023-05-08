# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Set-GWVariable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    Write-Verbose "Setting CI variable $Name to $Value" -Verbose

    if ($env:GITHUB_ENV) {
        "$Name=$Value" | Out-File $env:GITHUB_ENV -Append
    }
}

function Get-GWTempPath {
    $temp = [System.IO.Path]::GetTempPath()
    if ($env:RUNNER_TEMP) {
        $temp = $env:RUNNER_TEMP
    }

    Write-Verbose "Get CI Temp path: $temp" -Verbose
    return $temp
}
