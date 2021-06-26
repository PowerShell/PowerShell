# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
[cmdletbinding(DefaultParameterSetName='default')]
# PowerShell Script to clone, build and package PowerShell from specified fork and branch
param (
    [string] $fork = 'powershell',

    [string] $branch = 'master',

    [string] $location = "$PWD\powershell",

    [string] $destination = "$env:WORKSPACE",

    [ValidateSet("win7-x64", "win7-x86", "win-arm", "win-arm64", "fxdependent", "fxdependent-win-desktop")]
    [string] $Runtime = 'win7-x64',

    [switch] $ForMinimalSize,

    [switch] $Wait,

    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
    [ValidateNotNullOrEmpty()]
    [string] $ReleaseTag,

    [Parameter(Mandatory,ParameterSetName='IncludeSymbols')]
    [switch] $Symbols,

    [Parameter(Mandatory,ParameterSetName='packageSigned')]
    [ValidatePattern("-signed.zip$")]
    [string] $BuildZip,

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

try
{
    Set-Location $location

    Import-Module "$location\build.psm1" -Force
    Import-Module "$location\tools\packaging" -Force
    $env:platform = $null

    Write-Verbose "Sync'ing Tags..." -Verbose
    Sync-PSTags -AddRemoteIfMissing

    Write-Verbose "Bootstrapping powershell build..." -Verbose
    Start-PSBootstrap -Force -Package

    if ($PSCmdlet.ParameterSetName -eq 'packageSigned')
    {
        Write-Verbose "Expanding signed build..." -Verbose
        if($Runtime -like 'fxdependent*')
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
        Write-Verbose "Starting powershell build for RID: $Runtime and ReleaseTag: $ReleaseTag ..." -Verbose
        $buildParams = @{
            CrossGen = !$ForMinimalSize -and $Runtime -notmatch "arm" -and $Runtime -notlike "fxdependent*"
            ForMinimalSize = $ForMinimalSize
        }

        if($Symbols)
        {
            $buildParams['NoPSModuleRestore'] = $true
        }
        else
        {
            $buildParams['PSModuleRestore'] = $true
        }

        Start-PSBuild -Clean -Runtime $Runtime -Configuration Release @releaseTagParam @buildParams
    }

    if ($ComponentRegistration)
    {
        Write-Verbose "Exporting project.assets files ..." -Verbose

        $projectAssetsCounter = 1
        $projectAssetsFolder = Join-Path -Path $destination -ChildPath 'projectAssets'
        $projectAssetsZip = Join-Path -Path $destination -ChildPath 'windowsProjectAssetssymbols.zip'
        Get-ChildItem $location\project.assets.json -Recurse | ForEach-Object {
            $subfolder = $_.FullName.Replace($location,'')
            $subfolder.Replace('project.assets.json','')
            $itemDestination = Join-Path -Path $projectAssetsFolder -ChildPath $subfolder
            New-Item -Path $itemDestination -ItemType Directory -Force > $null
            $file = $_.FullName
            Write-Verbose "Copying $file to $itemDestination" -Verbose
            Copy-Item -Path $file -Destination "$itemDestination\" -Force
            $projectAssetsCounter++
        }

        Compress-Archive -Path $projectAssetsFolder -DestinationPath $projectAssetsZip
        Remove-Item -Path $projectAssetsFolder -Recurse -Force -ErrorAction SilentlyContinue

        return
    }

    if ($Runtime -like 'fxdependent*')
    {
        $pspackageParams = @{'Type' = $Runtime}
    }
    else
    {
        ## Set the default package type.
        $pspackageParams = @{'Type' = 'msi'; 'WindowsRuntime' = $Runtime}
        if ($ForMinimalSize)
        {
            ## Special case for the minimal size self-contained package.
            $pspackageParams['Type'] = 'min-size'
        }
    }

    if (!$Symbols -and $Runtime -notlike 'fxdependent*' -and !$ForMinimalSize)
    {
        if ($Runtime -notmatch 'arm')
        {
            Write-Verbose "Starting powershell packaging(msi)..." -Verbose
            Start-PSPackage @pspackageParams @releaseTagParam
        }

        $pspackageParams['Type']='msix'
        Write-Verbose "Starting powershell packaging(msix)..." -Verbose
        Start-PSPackage @pspackageParams @releaseTagParam
    }

    if ($Runtime -like 'fxdependent*' -or $ForMinimalSize)
    {
        ## Add symbols for just like zip package.
        $pspackageParams['IncludeSymbols']=$Symbols
        Start-PSPackage @pspackageParams @releaseTagParam

        ## Copy the fxdependent Zip package to destination.
        Get-ChildItem $location\PowerShell-*.zip | ForEach-Object {
            $file = $_.FullName
            Write-Verbose "Copying $file to $destination" -Verbose
            Copy-Item -Path $file -Destination "$destination\" -Force
        }
    }
    else
    {
        if (!$Symbols) {
            $pspackageParams['Type'] = 'zip-pdb'
            Write-Verbose "Starting powershell symbols packaging(zip)..." -Verbose
            Start-PSPackage @pspackageParams @releaseTagParam
        }

        $pspackageParams['Type']='zip'
        $pspackageParams['IncludeSymbols']=$Symbols
        Write-Verbose "Starting powershell packaging(zip)..." -Verbose
        Start-PSPackage @pspackageParams @releaseTagParam

        Write-Verbose "Exporting packages ..." -Verbose

        Get-ChildItem $location\*.msi,$location\*.zip,$location\*.wixpdb,$location\*.msix,$location\*.exe | ForEach-Object {
            $file = $_.FullName
            Write-Verbose "Copying $file to $destination" -Verbose
            Copy-Item -Path $file -Destination "$destination\" -Force
        }
    }
}
finally
{
    Write-Verbose "Beginning build clean-up..." -Verbose
    if ($Wait)
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
