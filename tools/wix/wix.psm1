# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Append-Path
{
    param
    (
        $path
    )
    $machinePathString = [System.Environment]::GetEnvironmentVariable('path',[System.EnvironmentVariableTarget]::Machine)
    $machinePath = $machinePathString -split ';'

    if($machinePath -inotcontains $path)
    {
        $newPath = "$machinePathString;$path"
        Write-Verbose "Adding $path to path..." -Verbose
        [System.Environment]::SetEnvironmentVariable('path',$newPath,[System.EnvironmentVariableTarget]::Machine)
        Write-Verbose "Added $path to path." -Verbose
    }
    else
    {
        Write-Verbose "$path already in path." -Verbose
    }
}

# Install using Wix Zip because the MSI requires an older version of dotnet
# which was large and unstable in docker
function Install-Wix
{
    param($arm64 = $false)

    $targetRoot = $arm64 ? "${env:ProgramFiles(x86)}\Arm Support WiX Toolset xcopy" : "${env:ProgramFiles(x86)}\WiX Toolset xcopy"
    # cleanup previous install
    if(Test-Path $targetRoot) {
        Remove-Item $targetRoot -Recurse -Force
    }
    $binPath = Join-Path -Path $targetRoot -ChildPath 'bin'

    $respository = Get-PSRepository -Name NuGetGallery -ErrorAction SilentlyContinue

    if (-not $respository) {
        Register-PSRepository -Name NuGetGallery -SourceLocation https://api.nuget.org/v3/index.json
    }

    # keep version in sync with Microsoft.PowerShell.Packaging.csproj

    if (-not (Test-Path $binPath)) {
        $null = New-Item -ItemType Directory -Path $binPath
        Write-Verbose -Verbose "Created bin directory for WIX at $binPath"
    }

    Save-Module -name wix -RequiredVersion 3.14.1 -path "$binPath/"

    $docExpandPath = Join-Path -Path "$binPath\wix\3.14.1\tools\" -ChildPath 'doc'
    $sdkExpandPath = Join-Path -Path "$binPath\wix\3.14.1\tools\" -ChildPath 'sdk'
    $x86ExpandPath = Join-Path -Path "$binPath\wix\3.14.1\tools\" -ChildPath 'x86'

    $docTargetPath = Join-Path -Path $targetRoot -ChildPath 'doc'

    if (-not (Test-Path $docTargetPath)) {
        $null = New-Item -ItemType Directory -Path $docTargetPath
        Write-Verbose -Verbose "Created doc directory for WIX at $docTargetPath"
    }

    $sdkTargetPath = Join-Path -Path $targetRoot -ChildPath 'sdk'

    if (-not (Test-Path $sdkTargetPath)) {
        $null = New-Item -ItemType Directory -Path $sdkTargetPath
        Write-Verbose -Verbose "Created sdk directory for WIX at $sdkTargetPath"
    }

    $binTargetPath = Join-Path -Path $targetRoot -ChildPath 'bin'

    if (-not (Test-Path $binTargetPath)) {
        $null = New-Item -ItemType Directory -Path $binTargetPath
        Write-Verbose -Verbose "Created bin directory for WIX at $binTargetPath"
    }

    $x86TargetPath = Join-Path -Path $binPath -ChildPath 'x86'

    if (-not (Test-Path $x86TargetPath)) {
        $null = New-Item -ItemType Directory -Path $x86TargetPath
        Write-Verbose -Verbose "Created x86 directory for WIX at $x86TargetPath"
    }

    Write-Verbose "Fixing folder structure ..." -Verbose
    Copy-Item -Path $docExpandPath -Destination $docTargetPath
    Copy-Item -Path $sdkExpandPath -Destination $sdkTargetPath
    Copy-Item -Path "$binPath\wix\3.14.1\tools\*" -Destination $binTargetPath
    Copy-Item -Path $x86ExpandPath -Destination $x86TargetPath

    Append-Path -path "$binPath\wix\3.14.1\tools"
    Write-Verbose "Done installing WIX!"
}
