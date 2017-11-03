param(
    [Parameter(HelpMessage="Artifact folder to find compliance files in.")]
    [string[]]
    $ArtifactFolder,
    [Parameter(HelpMessage="VSTS Variable to set path to complinance Files.")]
    [string]
    $VSTSVariableName
)

$compliancePath = $null
foreach($folder in $ArtifactFolder)
{
    # Find Symbols zip which contains compliance files
    Write-Host "ArtifactFolder: $folder"
    $filename = Join-Path -Path $folder -ChildPath 'symbols.zip'
    $name = Split-Path -Path $folder -Leaf

    # Throw is compliance zip does not exist
    if (!(Test-Path $filename))
    {
        throw "symbols.zip for $VSTSVariableName does not exist"
    }

    # make sure we have a single parent for everything
    if (!$compliancePath)
    {
        $parent = Split-Path -Path $folder
        $compliancePath = Join-Path -Path $parent -ChildPath 'compliance'
    }

    # Extract complance files to individual folder to avoid overwriting files.
    $unzipPath = Join-Path -Path $compliancePath -ChildPath $name
    Write-Host "Symbols-zip: $filename ; unzipPath: $unzipPath"
    Expand-Archive -Path $fileName -DestinationPath $unzipPath
}

# set VSTS variable with path to compliance files
Write-Host "##vso[task.setvariable variable=$VSTSVariableName]$unzipPath"