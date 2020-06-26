# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Import-Module "$PSScriptRoot\dockerInstall.psm1"

# Install using Wix Zip because the MSI requires an older version of dotnet
# which was large and unstable in docker
function Install-WixZip
{
    param($zipPath)

    $targetRoot = "${env:ProgramFiles(x86)}\WiX Toolset xcopy"
    $binPath = Join-Path -Path $targetRoot -ChildPath 'bin'
    Write-Verbose "Expanding $zipPath to $binPath ..." -Verbose
    Expand-Archive -Path $zipPath -DestinationPath $binPath -Force
    $docExpandPath = Join-Path -Path $binPath -ChildPath 'doc'
    $sdkExpandPath = Join-Path -Path $binPath -ChildPath 'sdk'
    $docTargetPath = Join-Path -Path $targetRoot -ChildPath 'doc'
    $sdkTargetPath = Join-Path -Path $targetRoot -ChildPath 'sdk'
    Write-Verbose "Fixing folder structure ..." -Verbose
    Move-Item -Path $docExpandPath -Destination $docTargetPath
    Move-Item -Path $sdkExpandPath -Destination $sdkTargetPath
    Append-Path -path $binPath
    Write-Verbose "Done installing WIX!"
}
