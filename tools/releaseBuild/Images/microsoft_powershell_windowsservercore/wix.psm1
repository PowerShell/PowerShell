Import-Module "$PSScriptRoot\dockerInstall.psm1"

function Install-WixZip
{
    param($zipPath)

    $targetRoot = "${env:ProgramFiles(x86)}\WiX Toolset xcopy"
    $binPath = Join-Path -Path $targetRoot -ChildPath 'bin'
    if(-not (Test-Path $targetRoot))
    {
        $null = New-Item -Path $targetRoot -ItemType Directory
    }
    Write-Verbose "Expanding $zipPath to $binPath ..." -Verbose
    Expand-Archive -Path $zipPath -DestinationPath $binPath
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
