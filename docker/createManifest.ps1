# Used to create a container manifest.
# Prereq: you must login to $ContainerRegistery before running this script
# default scenarios is to build a `latest` tag which will point to the `ubuntu-16.04` tag for linux
# and the `windowsservercore` tag for windows
param(
    [parameter(Mandatory)]
    [string]
    $ContainerRegistry,

    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[abcdefghijklmnopqrstuvwxyz\-0123456789\.]+$')]
    [string]
    $ManifestTag = 'latest',

    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[abcdefghijklmnopqrstuvwxyz\-0123456789\.]+$')]
    [string]
    $Image = 'powershell',

    [ValidateNotNullOrEmpty()]
    [ValidatePattern('^[abcdefghijklmnopqrstuvwxyz\-0123456789\.]+$')]
    [string[]]
    $TagList = ('ubuntu-16.04', 'windowsservercore')
)

$manifestList = @()
foreach($tag in $TagList)
{
    $manifestList += "$ContainerRegistry/${Image}:$tag"
}

# Create the manifest
docker manifest create $ContainerRegistry/${Image}:$ManifestTag $manifestList

# Inspect (print) the manifest
docker manifest inspect $ContainerRegistry/${Image}:$ManifestTag

# push the manifest
docker manifest push --purge $ContainerRegistry/${Image}:$ManifestTag
