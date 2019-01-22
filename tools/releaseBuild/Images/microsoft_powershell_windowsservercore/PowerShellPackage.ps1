# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
[cmdletbinding(DefaultParameterSetName='default')]
# PowerShell Script to clone, build and package PowerShell from specified fork and branch
param (
    [string] $fork = 'powershell',

    [string] $branch = 'master',

    [string] $location = "$pwd\powershell",

    [string] $destination = "$env:WORKSPACE",

    [ValidateSet("win7-x64", "win7-x86", "win-arm", "win-arm64", "fxdependent")]
    [string]$Runtime = 'win7-x64',

    [switch] $Wait,

    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d+)?)?$")]
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseTag,

    [Parameter(Mandatory,ParameterSetName='IncludeSymbols')]
    [switch] $Symbols,

    [Parameter(Mandatory,ParameterSetName='packageSigned')]
    [ValidatePattern("-signed.zip$")]
    [string]$BuildZip,

    [Parameter(Mandatory,ParameterSetName='ComponentRegistration')]
    [switch] $ComponentRegistration
)

$releaseTagParam = @{}
if ($ReleaseTag)
{
    $releaseTagParam = @{ 'ReleaseTag' = $ReleaseTag }
}

if (-not $env:homedrive)
{
    Write-Verbose "fixing empty home paths..." -Verbose
    $profileParts = $env:userprofile -split ':'
    $env:homedrive = $profileParts[0]+':'
    $env:homepath = $profileParts[1]
}

if (! (Test-Path $destination))
{
    Write-Verbose "Creating destination $destination" -Verbose
    $null = New-Item -Path $destination -ItemType Directory
}

Write-Verbose "homedrive : ${env:homedrive}"
Write-Verbose "homepath : ${env:homepath}"

# Don't use CIM_PhysicalMemory, docker containers may cache old values
$memoryMB = (Get-CimInstance win32_computersystem).TotalPhysicalMemory /1MB
$requiredMemoryMB = 2048
if ($memoryMB -lt $requiredMemoryMB)
{
    throw "Building powershell requires at least $requiredMemoryMB MiB of memory and only $memoryMB MiB is present."
}
Write-Verbose "Running with $memoryMB MB memory." -Verbose

try{
    Set-Location $location

    Import-Module "$location\build.psm1" -Force
    Import-Module "$location\tools\packaging" -Force
    $env:platform = $null

    Write-Verbose "Sync'ing Tags..." -verbose
    Sync-PSTags -AddRemoteIfMissing

    Write-Verbose "Bootstrapping powershell build..." -verbose
    Start-PSBootstrap -Force -Package

    if ($PSCmdlet.ParameterSetName -eq 'packageSigned')
    {
        Write-Verbose "Expanding signed build..." -verbose
        if($Runtime -eq 'fxdependent')
        {
            Expand-PSSignedBuild -BuildZip $BuildZip -SkipPwshExeCheck
        }
        else
        {
            Expand-PSSignedBuild -BuildZip $BuildZip
        }

        Remove-Item -Path $BuildZip
    }
    else
    {
        Write-Verbose "Starting powershell build for RID: $Runtime and ReleaseTag: $ReleaseTag ..." -verbose
        $buildParams = @{'CrossGen'= $Runtime -notmatch "arm" -and $Runtime -ne "fxdependent"}

        if($Symbols.IsPresent)
        {
            $buildParams['NoPSModuleRestore'] = $true
        }
        else
        {
            $buildParams['PSModuleRestore'] = $true
        }

        Start-PSBuild -Clean -Runtime $Runtime -Configuration Release @releaseTagParam @buildParams
    }

    if ($Runtime -eq 'fxdependent')
    {
        $pspackageParams = @{'Type'='fxdependent'}
    }
    else
    {
        $pspackageParams = @{'Type'='msi'; 'WindowsRuntime'=$Runtime}
    }

    if (!$ComponentRegistration.IsPresent -and !$Symbols.IsPresent -and $Runtime -notmatch "arm" -and $Runtime -ne 'fxdependent')
    {
        Write-Verbose "Starting powershell packaging(msi)..." -verbose
        Start-PSPackage @pspackageParams @releaseTagParam
    }

    if (!$ComponentRegistration.IsPresent -and $Runtime -ne 'fxdependent')
    {
        $pspackageParams['Type']='zip'
        $pspackageParams['IncludeSymbols']=$Symbols.IsPresent
        Write-Verbose "Starting powershell packaging(zip)..." -verbose
        Start-PSPackage @pspackageParams @releaseTagParam

        Write-Verbose "Exporting packages ..." -verbose

        Get-ChildItem $location\*.msi,$location\*.zip,$location\*.wixpdb | ForEach-Object {
            $file = $_.FullName
            Write-Verbose "Copying $file to $destination" -verbose
            Copy-Item -Path $file -Destination "$destination\" -Force
        }
    }
    elseif (!$ComponentRegistration.IsPresent -and $Runtime -eq 'fxdependent')
    {
        ## Add symbols for just like zip package.
        $pspackageParams['IncludeSymbols']=$Symbols.IsPresent
        Start-PSPackage @pspackageParams @releaseTagParam

        ## Copy the fxdependent Zip package to destination.
        Get-ChildItem $location\PowerShell-*.zip | ForEach-Object {
            $file = $_.FullName
            Write-Verbose "Copying $file to $destination" -verbose
            Copy-Item -Path $file -Destination "$destination\" -Force
        }
    }
    else
    {
        Write-Verbose "Exporting project.assets files ..." -verbose

        $projectAssetsCounter = 1
        $projectAssetsFolder = Join-Path -Path $destination -ChildPath 'projectAssets'
        $projectAssetsZip = Join-Path -Path $destination -ChildPath 'windowsProjectAssetssymbols.zip'
        Get-ChildItem $location\project.assets.json -Recurse | ForEach-Object {
            $subfolder = $_.FullName.Replace($location,'')
            $subfolder.Replace('project.assets.json','')
            $itemDestination = Join-Path -Path $projectAssetsFolder -ChildPath $subfolder
                    New-Item -Path $itemDestination -ItemType Directory -Force
            $file = $_.FullName
            Write-Verbose "Copying $file to $itemDestination" -verbose
            Copy-Item -Path $file -Destination "$itemDestination\" -Force
            $projectAssetsCounter++
        }

        Compress-Archive -Path $projectAssetsFolder -DestinationPath $projectAssetsZip
        Remove-Item -Path $projectAssetsFolder -Recurse -Force -ErrorAction SilentlyContinue
    }

}
finally
{
    Write-Verbose "Beginning build clean-up..." -verbose
    if ($Wait.IsPresent)
    {
        $path = Join-Path $PSScriptRoot -ChildPath 'delete-to-continue.txt'
        $null = New-Item -Path $path -ItemType File
        Write-Verbose "Computer name: $env:COMPUTERNAME" -Verbose
        Write-Verbose "Delete $path to exit." -Verbose
        while(Test-Path -LiteralPath $path)
        {
            Start-Sleep -Seconds 60
        }
    }
}
