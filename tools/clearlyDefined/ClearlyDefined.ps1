# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param(
    [parameter(Mandatory = $true, ParameterSetName='Harvest')]
    [switch]
    $Harvest,
    [parameter(Mandatory = $true, ParameterSetName='Test')]
    [switch]
    $Test,
    [switch]
    $ForceModuleReload
)

$extraParams = @{}
if ($ForceModuleReload) {
    $extraParams['Force'] = $true
}

Import-Module -Name "$PSScriptRoot/src/ClearlyDefined" @extraParams

$cgManifest = Get-Content "$PSScriptRoot/../cgmanifest.json" | ConvertFrom-Json
$fullCgList = $cgManifest.Registrations.Component |
    ForEach-Object {
        [Pscustomobject]@{
            type = $_.Type
            Name = $_.Nuget.Name
            PackageVersion = $_.Nuget.Version
        }
    }

$fullList = $fullCgList | Get-ClearlyDefinedData

$needHarvest = $fullList | Where-Object { !$_.harvested }

Write-Verbose "Full List count: $($fullList.Count)" -Verbose
Write-Verbose "Need harvest: $($needHarvest.Count)" -Verbose

if ($Harvest) {
    $needHarvest | select-object -ExpandProperty coordinates | Start-ClearlyDefinedHarvest
} elseif ($Test) {
    if($needHarvest.Count -gt 0) {
        $needHarvest | Format-List | Out-String -Width 9999 | Write-Verbose -Verbose
        throw "There are $($needHarvest.Count) packages that need to be harvested"
    } else {
        Write-Verbose "All packages have been harvested" -Verbose
    }
}
