# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Import-Module "$PSScriptRoot\dockerInstall.psm1"

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

    $psresourceGet = Get-Module -ListAvailable -Name 'Microsoft.PowerShell.PSResourceGet' -ErrorAction SilentlyContinue

    if (-not $psresourceGet) {
        Install-Module -Name 'Microsoft.PowerShell.PSResourceGet' -Force -AllowClobber -Scope CurrentUser
    }

    $respository = Get-PSResourceRepository -Name 'dotnet-eng' -ErrorAction SilentlyContinue

    if (-not $respository) {
        Write-Verbose -Verbose "Registering dotnet-eng repository..."
        Register-PSResourceRepository -Name 'dotnet-eng' -Uri 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json' -Trusted
    }

    # keep version in sync with Microsoft.PowerShell.Packaging.csproj

    if (-not (Test-Path $binPath)) {
        $null = New-Item -ItemType Directory -Path $binPath
        Write-Verbose -Verbose "Created bin directory for WIX at $binPath"
    }

    try {
        Save-PSResource -Name 'Microsoft.Signed.Wix' -Repository 'dotnet-eng' -path "$binPath/" -Prerelease
    }
    finally {
        Write-Verbose -Verbose "Unregistering dotnet-eng repository..."
        Unregister-PSResourceRepository -Name 'dotnet-eng'
    }

    $docExpandPath = Join-Path -Path "$binPath\Microsoft.Signed.Wix\3.14.1\tools\" -ChildPath 'doc'
    $sdkExpandPath = Join-Path -Path "$binPath\Microsoft.Signed.Wix\3.14.1\tools\" -ChildPath 'sdk'
    $x86ExpandPath = Join-Path -Path "$binPath\Microsoft.Signed.Wix\3.14.1\tools\" -ChildPath 'x86'

    $docTargetPath = Join-Path -Path $targetRoot -ChildPath 'doc'
    $sdkTargetPath = Join-Path -Path $targetRoot -ChildPath 'sdk'
    Write-Verbose "Fixing folder structure ..." -Verbose
    Copy-Item -Path $docExpandPath -Destination $docTargetPath -Force
    Copy-Item -Path $sdkExpandPath -Destination $sdkTargetPath -Force
    Copy-Item -Path "$binPath\Microsoft.Signed.Wix\3.14.1\tools\*" -Destination $binTargetPath -Force
    Copy-Item -Path $x86ExpandPath -Destination $x86TargetPath -Force -Recurse -Verbose
    Copy-Item -Path "$x86ExpandPath\burn.exe" -Destination "$x86TargetPath\burn.exe" -Force -Verbose

    Append-Path -path $binPath
    Write-Verbose "Done installing WIX!"
}
