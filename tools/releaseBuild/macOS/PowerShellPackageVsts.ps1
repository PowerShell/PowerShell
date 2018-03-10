# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# PowerShell Script to build and package PowerShell from specified form and branch
# Script is intented to use in Docker containers
# Ensure PowerShell is available in the provided image

param (
    # Set default location to where VSTS cloned the repository locally.
    [string] $location = $env:BUILD_REPOSITORY_LOCALPATH,

    # Destination location of the package on docker host
    [Parameter(Mandatory,ParameterSetName='Build')]
    [string] $destination = '/mnt',

    [Parameter(Mandatory,ParameterSetName='Build')]
    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d+)?)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,

    [Parameter(ParameterSetName='Build')]
    [ValidateSet("zip", "tar")]
    [string[]]$ExtraPackage,

    [Parameter(Mandatory,ParameterSetName='Bootstrap')]
    [switch] $BootStrap,

    [Parameter(Mandatory,ParameterSetName='Build')]
    [switch] $Build
)

# We must build in /PowerShell
$repoRoot = '/PowerShell'
if($BootStrap.IsPresent)
{
    $repoRoot = $location
}

if($Build.IsPresent)
{
    # cleanup the folder but don't delete it or the build agent will loose ownership of the folder
    Write-Verbose -Message "cleaning /PowerShell" -Verbose
    Get-ChildItem -Path /PowerShell/* -Attributes Hidden,Normal,Directory | Remove-Item -Recurse -Force

    dir env:
    # clone the repositor to the location we must build from
    Write-Verbose -Message "cloaning to /PowerShell" -Verbose
    git clone $location /PowerShell
    $releaseTagParam = @{}
    if ($ReleaseTag)
    {
        $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }
    }
}


Push-Location
try {
    Write-Verbose -Message "Init..." -Verbose
    Set-Location $repoRoot
    git submodule update --init --recursive --quiet
    Import-Module "$repoRoot/build.psm1"
    Import-Module "$repoRoot/tools/packaging"
    Sync-PSTags -AddRemoteIfMissing

    if($BootStrap.IsPresent)
    {
        Start-PSBootstrap -Package
        Write-Verbose -Message "Installing fpm..." -Verbose
        Start-NativeExecution { sudo gem install fpm -v 1.8.1 }
        Write-Verbose -Message "Installing ronn..." -Verbose
        Start-NativeExecution { sudo gem install ronn }
    }

    if($Build.IsPresent)
    {
        Start-PSBuild -Crossgen -PSModuleRestore @releaseTagParam

        Start-PSPackage @releaseTagParam
        switch ($ExtraPackage)
        {
            "zip" { Start-PSPackage -Type zip @releaseTagParam }
            "tar" { Start-PSPackage -Type tar @releaseTagParam }
        }
    }
}
finally
{
    Pop-Location
}

if($Build.IsPresent)
{
    $linuxPackages = Get-ChildItem "$repoRoot/powershell*" -Include *.pkg,*.zip,*.tar.gz
    foreach ($linuxPackage in $linuxPackages)
    {
        $filePath = $linuxPackage.FullName
        $name = split-path -Leaf -Path $filePath
        $extension = (Split-Path -Extension -Path $filePath).Replace('.','')
        Write-Verbose "Copying $filePath to $destination" -Verbose
        Write-Host "##vso[artifact.upload containerfolder=results;artifactname=$name]$filePath"
        Write-Host "##vso[task.setvariable variable=Package-$extension]$filePath"
        Copy-Item -Path $filePath -Destination $destination -force
    }
}
