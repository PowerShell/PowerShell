# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param(
    [Parameter(HelpMessage="Artifact folder to find compliance files in.")]
    [string[]]
    $ArtifactFolder,
    [Parameter(HelpMessage="VSTS Variable to set path to compliance Files.")]
    [string]
    $VSTSVariableName
)

$compliancePath = $null
foreach($folder in $ArtifactFolder)
{
    # Find Symbols zip which contains compliance files
    Write-Host "ArtifactFolder: $folder"
    $filename = Join-Path -Path $folder -ChildPath 'symbols.zip'

    $parentName = Split-Path -Path $folder -Leaf

    # Use simplified names because some of the compliance tools didn't like the full names
    # decided not to use hashes because the names need to be consistent otherwise the tool also has issues
    # which is another problem with the full name, it includes version.
    if ($parentName -match 'x64' -or $parentName -match 'amd64')
    {
        $name = 'x64'
    }
    elseif ($parentName -match 'x86') {
        $name = 'x86'
    }
    elseif ($parentName -match 'fxdependent') {
        $name = 'fxd'
    }
    else
    {
        throw "$parentName could not be classified as x86 or x64"
    }

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

    # Extract compliance files to individual folder to avoid overwriting files.
    $unzipPath = Join-Path -Path $compliancePath -ChildPath $name
    Write-Host "Symbols-zip: $filename ; unzipPath: $unzipPath"
    Expand-Archive -Path $fileName -DestinationPath $unzipPath
}

# set VSTS variable with path to compliance files
Write-Host "##vso[task.setvariable variable=$VSTSVariableName]$unzipPath"
