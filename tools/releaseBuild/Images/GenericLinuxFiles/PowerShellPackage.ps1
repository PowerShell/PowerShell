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

    [ValidateSet("AppImage", "tar")]
    [string[]]$ExtraPackage
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
    Start-PSBuild -Crossgen -PSModuleRestore @releaseTagParam

    Start-PSPackage @releaseTagParam
    switch ($ExtraPackage)
    {
        "AppImage" { Start-PSPackage -Type AppImage @releaseTagParam }
        "tar"      { Start-PSPackage -Type tar @releaseTagParam }
    }
}
finally
{
    Pop-Location
}

$linuxPackages = Get-ChildItem "$location/powershell*" -Include *.deb,*.rpm

foreach ($linuxPackage in $linuxPackages)
{
    Copy-Item -Path $linuxPackage.FullName -Destination $destination -force
}

$extraPackages = @()
switch ($ExtraPackage)
{
    "AppImage" { $extraPackages += @(Get-ChildItem -Path $location -Filter '*.AppImage') }
    "tar"      { $extraPackages += @(Get-ChildItem -Path $location -Filter '*.tar.gz') }
}

foreach ($extraPkgFile in $extraPackages)
{
    Copy-Item -Path $extraPkgFile.FullName -Destination $destination -force
}
