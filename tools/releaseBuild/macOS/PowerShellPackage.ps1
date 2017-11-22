# PowerShell Script to build and package PowerShell from specified form and branch
# Script is intented to use in Docker containers
# Ensure PowerShell is available in the provided image

param (
    # Set default location to where VSTS cloned the repository locally.
    [string] $location = $env:BUILD_REPOSITORY_LOCALPATH,

    # Destination location of the package on docker host
    [string] $destination = '/mnt',

    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d+)?)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,

    [ValidateSet("zip", "tar")]
    [string[]]$ExtraPackage
)

# We must build in /PowerShell

# cleanup the folder but don't delete it or the build agent will loose ownership of the folder
Get-ChildItem -Path /PowerShell/* -Attributes Hidden,Normal,Directory | Remove-Item -Recurse -Force

# clone the repositor to the location we must build from
git clone $location /PowerShell

$releaseTagParam = @{}
if ($ReleaseTag)
{
    $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }
}

Push-Location
try {
    Set-Location /PowerShell
    Import-Module "/PowerShell/build.psm1"
    Import-Module "/PowerShell/tools/packaging"

    Start-PSBootstrap -Package -NoSudo
    Start-PSBuild -Crossgen -PSModuleRestore @releaseTagParam

    Start-PSPackage @releaseTagParam
    switch ($ExtraPackage)
    {
        "zip" { Start-PSPackage -Type zip @releaseTagParam }
        "tar" { Start-PSPackage -Type tar @releaseTagParam }
    }
}
finally
{
    Pop-Location
}

$linuxPackages = Get-ChildItem "/PowerShell/powershell*" -Include *.pkg,*.zip,*.tar.gz
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
