# PowerShell Script to build and package PowerShell from specified form and branch
# Script is intented to use in Docker containers
# Ensure PowerShell is available in the provided image

param (
    [string] $location = "/powershell",

    # Destination location of the package on docker host
    [string] $destination = '/mnt',

    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+\.\d+)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,
    [switch]$AppImage
)

$releaseTagParam = @{}
if($ReleaseTag)
{
    $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }
}

Push-Location
try {
    Set-Location $location
    Import-Module "$location/build.psm1"
    Import-Module "$location/tools/packaging"
    
    Start-PSBootstrap -Package -NoSudo
    Start-PSBuild -Crossgen -PSModuleRestore @releaseTagParam

    Start-PSPackage @releaseTagParam
    if($AppImage.IsPresent)
    {
        Start-PSPackage -Type AppImage @releaseTagParam
    }
}
finally
{
    Pop-Location
}

$linuxPackages = Get-ChildItem "$location/powershell*" -Include *.deb,*.rpm
    
foreach($linuxPackage in $linuxPackages) 
{ 
    Copy-Item -Path $linuxPackage.FullName -Destination $destination -force
}

if($AppImage.IsPresent)
{
    $appImages = Get-ChildItem -Path $location -Filter '*.AppImage'
    foreach($appImageFile in $appImages) 
    { 
        Copy-Item -Path $appImageFile.FullName -Destination $destination -force
    }
}
