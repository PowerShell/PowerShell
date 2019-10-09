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

    [switch]$TarX64,
    [switch]$TarArm,
    [switch]$TarArm64,
    [switch]$FxDependent,
    [switch]$Alpine
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
        $projectAssetsZipName = 'linuxFxDependantProjectAssetssymbols.zip'
        $buildParams.Add("Runtime", "fxdependent")
    } elseif ($Alpine.IsPresent) {
        $projectAssetsZipName = 'linuxAlpineProjectAssetssymbols.zip'
        $buildParams.Add("Runtime", 'alpine-x64')
    } else {
        # make the artifact name unique
        $projectAssetsZipName = "linuxProjectAssets-$((get-date).Ticks)-symbols.zip"
        $buildParams.Add("Crossgen", $true)
    }

    Start-PSBuild @buildParams @releaseTagParam

    if($FxDependent) {
        Start-PSPackage -Type 'fxdependent' @releaseTagParam
    } elseif ($Alpine) {
        Start-PSPackage -Type 'tar-alpine' @releaseTagParam
    } else {
        Start-PSPackage @releaseTagParam
    }

    if ($TarX64) { Start-PSPackage -Type tar @releaseTagParam }

    if ($TarArm) {
        ## Build 'linux-arm' and create 'tar.gz' package for it.
        ## Note that 'linux-arm' can only be built on Ubuntu environment.
        Start-PSBuild -Configuration Release -Restore -Runtime linux-arm -PSModuleRestore @releaseTagParam
        Start-PSPackage -Type tar-arm @releaseTagParam
    }

    if ($TarArm64) {
        Start-PSBuild -Configuration Release -Restore -Runtime linux-arm64 -PSModuleRestore @releaseTagParam
        Start-PSPackage -Type tar-arm64 @releaseTagParam
    }
}
finally
{
    Pop-Location
}

$linuxPackages = Get-ChildItem "$location/powershell*" -Include *.deb,*.rpm,*.tar.gz

foreach ($linuxPackage in $linuxPackages)
{
    $filePath = $linuxPackage.FullName
    Write-Verbose "Copying $filePath to $destination" -Verbose
    Copy-Item -Path $filePath -Destination $destination -force
}

Write-Verbose "Exporting project.assets files ..." -verbose

$projectAssetsCounter = 1
$projectAssetsFolder = Join-Path -Path $destination -ChildPath 'projectAssets'
$projectAssetsZip = Join-Path -Path $destination -ChildPath $projectAssetsZipName
Get-ChildItem $location\project.assets.json -Recurse | ForEach-Object {
    $subfolder = $_.FullName.Replace($location,'')
    $subfolder.Replace('project.assets.json','')
    $itemDestination = Join-Path -Path $projectAssetsFolder -ChildPath $subfolder
    New-Item -Path $itemDestination -ItemType Directory -Force
    $file = $_.FullName
    Write-Verbose "Copying $file to $itemDestination" -verbose
    Copy-Item -Path $file -Destination "$itemDestination\" -Force
    $projectAssetsCounter++
}

Compress-Archive -Path $projectAssetsFolder -DestinationPath $projectAssetsZip
Remove-Item -Path $projectAssetsFolder -Recurse -Force -ErrorAction SilentlyContinue
