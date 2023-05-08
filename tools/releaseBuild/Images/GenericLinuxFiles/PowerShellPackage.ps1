# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# PowerShell Script to build and package PowerShell from specified form and branch
# Script is intented to use in Docker containers
# Ensure PowerShell is available in the provided image

param (
    [string] $location = "/powershell",

    # Destination location of the package on docker host
    [string] $destination = '/mnt',

    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,

    [switch]$TarX64,
    [switch]$TarArm,
    [switch]$TarArm64,
    [switch]$TarMinSize,
    [switch]$FxDependent,
    [switch]$Alpine
)

$releaseTagParam = @{}
if ($ReleaseTag)
{
    $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }
}

#Remove the initial 'v' from the ReleaseTag
$version = $ReleaseTag -replace '^v'
$semVersion = [System.Management.Automation.SemanticVersion] $version

$metadata = Get-Content "$location/tools/metadata.json" -Raw | ConvertFrom-Json

$LTS = $metadata.LTSRelease.Package

Write-Verbose -Verbose -Message "LTS is set to: $LTS"

function BuildPackages {
    param(
        [switch] $LTS
    )

    Push-Location
    try {
        Set-Location $location
        Import-Module "$location/build.psm1"
        Import-Module "$location/tools/packaging"

        Start-PSBootstrap -Package -NoSudo

        $buildParams = @{ Configuration = 'Release'; PSModuleRestore = $true; Restore = $true }

        if ($FxDependent.IsPresent) {
            $projectAssetsZipName = 'linuxFxDependantProjectAssetssymbols.zip'
            $buildParams.Add("Runtime", "fxdependent")
        } elseif ($Alpine.IsPresent) {
            $projectAssetsZipName = 'linuxAlpineProjectAssetssymbols.zip'
            $buildParams.Add("Runtime", 'alpine-x64')
        } else {
            # make the artifact name unique
            $projectAssetsZipName = "linuxProjectAssets-$((Get-Date).Ticks)-symbols.zip"
        }

        Start-PSBuild @buildParams @releaseTagParam
        $options = Get-PSOptions

        if ($FxDependent) {
            Start-PSPackage -Type 'fxdependent' @releaseTagParam -LTS:$LTS
        } elseif ($Alpine) {
            Start-PSPackage -Type 'tar-alpine' @releaseTagParam -LTS:$LTS
        } else {
            Start-PSPackage @releaseTagParam -LTS:$LTS
        }

        if ($TarX64) { Start-PSPackage -Type tar @releaseTagParam -LTS:$LTS }

        if ($TarMinSize) {
            Write-Verbose -Verbose "---- Min-Size ----"
            Write-Verbose -Verbose "options.Output: $($options.Output)"
            Write-Verbose -Verbose "options.Top $($options.Top)"

            $binDir = Join-Path -Path $options.Top -ChildPath 'bin'
            Write-Verbose -Verbose "Remove $binDir, to get a clean build for min-size package"
            Remove-Item -Path $binDir -Recurse -Force

            ## Build 'min-size' and create 'tar.gz' package for it.
            $buildParams['ForMinimalSize'] = $true
            Start-PSBuild @buildParams @releaseTagParam
            Start-PSPackage -Type min-size @releaseTagParam -LTS:$LTS
        }

        if ($TarArm) {
            ## Build 'linux-arm' and create 'tar.gz' package for it.
            ## Note that 'linux-arm' can only be built on Ubuntu environment.
            Start-PSBuild -Configuration Release -Restore -Runtime linux-arm -PSModuleRestore @releaseTagParam
            Start-PSPackage -Type tar-arm @releaseTagParam -LTS:$LTS
        }

        if ($TarArm64) {
            Start-PSBuild -Configuration Release -Restore -Runtime linux-arm64 -PSModuleRestore @releaseTagParam
            Start-PSPackage -Type tar-arm64 @releaseTagParam -LTS:$LTS
        }
    } finally {
        Pop-Location
    }
}

BuildPackages

if ($LTS) {
    Write-Verbose -Verbose "Packaging LTS"
    BuildPackages -LTS
}

$linuxPackages = Get-ChildItem "$location/powershell*" -Include *.deb,*.rpm,*.tar.gz

foreach ($linuxPackage in $linuxPackages)
{
    $filePath = $linuxPackage.FullName
    Write-Verbose "Copying $filePath to $destination" -Verbose
    Copy-Item -Path $filePath -Destination $destination -Force
}

Write-Verbose "Exporting project.assets files ..." -Verbose

$projectAssetsCounter = 1
$projectAssetsFolder = Join-Path -Path $destination -ChildPath 'projectAssets'
$projectAssetsZip = Join-Path -Path $destination -ChildPath $projectAssetsZipName
Get-ChildItem $location\project.assets.json -Recurse | ForEach-Object {
    $subfolder = $_.FullName.Replace($location,'')
    $subfolder.Replace('project.assets.json','')
    $itemDestination = Join-Path -Path $projectAssetsFolder -ChildPath $subfolder
    New-Item -Path $itemDestination -ItemType Directory -Force
    $file = $_.FullName
    Write-Verbose "Copying $file to $itemDestination" -Verbose
    Copy-Item -Path $file -Destination "$itemDestination\" -Force
    $projectAssetsCounter++
}

Compress-Archive -Path $projectAssetsFolder -DestinationPath $projectAssetsZip
Remove-Item -Path $projectAssetsFolder -Recurse -Force -ErrorAction SilentlyContinue
