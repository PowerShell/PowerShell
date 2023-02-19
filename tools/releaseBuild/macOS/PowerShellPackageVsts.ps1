# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# PowerShell Script to build and package PowerShell from specified form and branch
# Script is intended to use in Docker containers
# Ensure PowerShell is available in the provided image

param (
    # Set default location to where VSTS cloned the repository locally.
    [string] $location = $env:BUILD_REPOSITORY_LOCALPATH,

    # Destination location of the package on docker host
    [Parameter(Mandatory, ParameterSetName = 'packageSigned')]
    [Parameter(Mandatory, ParameterSetName = 'IncludeSymbols')]
    [Parameter(Mandatory, ParameterSetName = 'Build')]
    [string] $destination = '/mnt',

    [Parameter(Mandatory, ParameterSetName = 'packageSigned')]
    [Parameter(Mandatory, ParameterSetName = 'IncludeSymbols')]
    [Parameter(Mandatory, ParameterSetName = 'Build')]
    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,

    [Parameter(ParameterSetName = 'packageSigned')]
    [Parameter(ParameterSetName = 'IncludeSymbols')]
    [Parameter(ParameterSetName = 'Build')]
    [ValidateSet("zip", "tar")]
    [string[]]$ExtraPackage,

    [Parameter(Mandatory, ParameterSetName = 'Bootstrap')]
    [switch] $BootStrap,

    [Parameter(Mandatory, ParameterSetName = 'IncludeSymbols')]
    [Parameter(Mandatory, ParameterSetName = 'Build')]
    [switch] $Build,

    [Parameter(Mandatory, ParameterSetName = 'IncludeSymbols')]
    [switch] $Symbols,

    [Parameter(Mandatory, ParameterSetName = 'packageSigned')]
    [ValidatePattern("-signed.zip$")]
    [string]$BuildZip,

    [Parameter(Mandatory, ParameterSetName = 'packageSigned')]
    [Parameter(Mandatory, ParameterSetName = 'IncludeSymbols')]
    [Parameter(Mandatory, ParameterSetName = 'Build')]
    [ValidateSet('osx-x64', 'osx-arm64')]
    [string]$Runtime,

    [string]$ArtifactName = 'result',

    [switch]$SkipReleaseChecks
)

$repoRoot = $location

if ($Build -or $PSCmdlet.ParameterSetName -eq 'packageSigned') {
    $releaseTagParam = @{}
    if ($ReleaseTag) {
        $releaseTagParam['ReleaseTag'] = $ReleaseTag

        #Remove the initial 'v' from the ReleaseTag
        $version = $ReleaseTag -replace '^v'
        $semVersion = [System.Management.Automation.SemanticVersion] $version

        $metadata = Get-Content "$location/tools/metadata.json" -Raw | ConvertFrom-Json

        $LTS = $metadata.LTSRelease.Package

        Write-Verbose -Verbose -Message "LTS is set to: $LTS"
    }
}

Push-Location
try {
    $pspackageParams = @{ SkipReleaseChecks = $SkipReleaseChecks; MacOSRuntime = $Runtime }
    Write-Verbose -Message "Init..." -Verbose
    Set-Location $repoRoot
    Import-Module "$repoRoot/build.psm1"
    Import-Module "$repoRoot/tools/packaging"
    Sync-PSTags -AddRemoteIfMissing

    if ($BootStrap) {
        Start-PSBootstrap -Package
    }

    if ($PSCmdlet.ParameterSetName -eq 'packageSigned') {
        Write-Verbose "Expanding signed build $BuildZip ..." -Verbose
        Expand-PSSignedBuild -BuildZip $BuildZip

        Remove-Item -Path $BuildZip

        Start-PSPackage @pspackageParams @releaseTagParam
        switch ($ExtraPackage) {
            "tar" { Start-PSPackage -Type tar @pspackageParams @releaseTagParam }
        }

        if ($LTS) {
            Start-PSPackage @pspackageParams @releaseTagParam -LTS
            switch ($ExtraPackage) {
                "tar" { Start-PSPackage -Type tar @pspackageParams @releaseTagParam -LTS }
            }
        }
    }

    if ($Build) {
        if ($Symbols) {
            Start-PSBuild -Clean -Configuration 'Release' -NoPSModuleRestore @releaseTagParam -Runtime $Runtime
            $pspackageParams['Type']='zip'
            $pspackageParams['IncludeSymbols']=$Symbols.IsPresent
            Write-Verbose "Starting powershell packaging(zip)..." -Verbose
            Start-PSPackage @pspackageParams @releaseTagParam
        } else {
            Start-PSBuild -Configuration 'Release' -PSModuleRestore @releaseTagParam -Runtime $Runtime
            Start-PSPackage @pspackageParams @releaseTagParam
            switch ($ExtraPackage) {
                "tar" { Start-PSPackage -Type tar @pspackageParams @releaseTagParam }
            }

            if ($LTS) {
                Start-PSPackage @releaseTagParam -LTS
                switch ($ExtraPackage) {
                    "tar" { Start-PSPackage -Type tar @pspackageParams @releaseTagParam -LTS }
                }
            }
        }
    }
} finally {
    Pop-Location
}

if ($Build -or $PSCmdlet.ParameterSetName -eq 'packageSigned') {
    $macPackages = Get-ChildItem "$repoRoot/powershell*" -Include *.pkg, *.tar.gz, *.zip
    foreach ($macPackage in $macPackages) {
        $filePath = $macPackage.FullName
        $extension = (Split-Path -Extension -Path $filePath).Replace('.', '')
        Write-Verbose "Copying $filePath to $destination" -Verbose
        Write-Host "##vso[artifact.upload containerfolder=$ArtifactName;artifactname=$ArtifactName]$filePath"
        Write-Host "##vso[task.setvariable variable=Package-$extension]$filePath"
        Copy-Item -Path $filePath -Destination $destination -Force
    }
}
