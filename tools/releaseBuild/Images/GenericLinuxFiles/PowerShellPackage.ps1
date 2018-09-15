# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# PowerShell Script to build and package PowerShell from specified form and branch
# Script is intented to use in Docker containers
# Ensure PowerShell is available in the provided image

param (
    [string] $location = "/powershell",

    # Destination location of the package on docker host
    [string] $destination = '/mnt',

    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d+)?)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,

    [switch]$AppImage,
    [switch]$TarX64,
    [switch]$TarArm,
    [switch]$FxDependent
)

$releaseTagParam = @{}
if ($ReleaseTag)
{
    $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }
}

Push-Location
try {
    Set-Location $location
    Import-Module "$location/build.psm1"
    Import-Module "$location/tools/packaging"

    Start-PSBootstrap -Package -NoSudo

    $buildParams = @{ Configuration = 'Release'; PSModuleRestore = $true}

    if($FxDependent.IsPresent) {
        $buildParams.Add("Runtime", "fxdependent")
    } else {
        $buildParams.Add("Crossgen", $true)
    }

    Start-PSBuild @buildParams @releaseTagParam

    if($FxDependent) {
        Start-PSPackage -Type 'fxdependent' @releaseTagParam
    } else {
        Start-PSPackage @releaseTagParam
    }

    if ($AppImage) { Start-PSPackage -Type AppImage @releaseTagParam }
    if ($TarX64) { Start-PSPackage -Type tar @releaseTagParam }

    if ($TarArm) {
        ## Build 'linux-arm' and create 'tar.gz' package for it.
        ## Note that 'linux-arm' can only be built on Ubuntu environment.
        Start-PSBuild -Configuration Release -Restore -Runtime linux-arm -PSModuleRestore @releaseTagParam
        Start-PSPackage -Type tar-arm @releaseTagParam
    }
}
finally
{
    Pop-Location
}

$linuxPackages = Get-ChildItem "$location/powershell*" -Include *.deb,*.rpm,*.AppImage,*.tar.gz

foreach ($linuxPackage in $linuxPackages)
{
    $filePath = $linuxPackage.FullName
    Write-Verbose "Copying $filePath to $destination" -Verbose
    Copy-Item -Path $filePath -Destination $destination -force
}

Write-Verbose "Exporting project.assets files ..." -verbose

$projectAssetsCounter = 1
$projectAssetsFolder = Join-Path -Path $destination -ChildPath 'projectAssets'
$projectAssetsZip = Join-Path -Path $destination -ChildPath 'projectAssetssymbols.zip'
Get-ChildItem $location\project.assets.json -Recurse | ForEach-Object {
    $itemDestination = Join-Path -Path $projectAssetsFolder -ChildPath $projectAssetsCounter
    New-Item -Path $itemDestination -ItemType Directory -Force
    $file = $_.FullName
    Write-Verbose "Copying $file to $itemDestination" -verbose
    Copy-Item -Path $file -Destination "$itemDestination\" -Force
    $projectAssetsCounter++
}

Compress-Archive -Path $projectAssetsFolder -DestinationPath $projectAssetsZip
Remove-Item -Path $projectAssetsFolder -Recurse -Force -ErrorAction SilentlyContinue
