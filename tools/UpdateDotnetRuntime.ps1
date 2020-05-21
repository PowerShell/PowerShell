# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param (
    [Parameter()]
    [string]$SDKVersionOverride,

    [Parameter()]
    [switch]$UpdateMSIPackaging
)

<#
 .DESCRIPTION Update the global.json with the new SDK version to be used.
#>
function Update-GlobalJson([string] $Version) {
    $psGlobalJsonPath = Resolve-Path "$PSScriptRoot/../global.json"
    $psGlobalJson = Get-Content -Path $psGlobalJsonPath -Raw | ConvertFrom-Json

    if ($psGlobalJson.sdk.version -eq $Version) {
        throw '.NET SDK version is not updated'
    }

    $psGlobalJson.sdk.version = $Version
    $psGlobalJson | ConvertTo-Json | Out-File -FilePath $psGlobalJsonPath -Force
}

<#
 .DESCRIPTION Iterate through all the csproj to find all the packages that need to be updated
#>
function Update-PackageVersion {

    class PkgVer {
        [string] $Name
        [string] $Version
        [string] $NewVersion
        [string] $Path

        PkgVer($n, $v, $nv, $p) {
            $this.Name = $n
            $this.Version = $v
            $this.NewVersion = $nv
            $this.Path = $p
        }
    }

    $skipModules = @(
        "NJsonSchema"
        "Markdig.Signed"
        "PowerShellHelpFiles"
        "Newtonsoft.Json"
        "Microsoft.ApplicationInsights"
        "Microsoft.Management.Infrastructure"
        "Microsoft.PowerShell.Native"
        "Microsoft.NETCore.Windows.ApiSets"
    )

    $packages = [System.Collections.Generic.Dictionary[[string], [PkgVer[]] ]]::new()

    $paths = @(
        "$PSScriptRoot/packaging/projects/reference/Microsoft.PowerShell.Commands.Utility/Microsoft.PowerShell.Commands.Utility.csproj"
        "$PSScriptRoot/packaging/projects/reference/System.Management.Automation/System.Management.Automation.csproj"
        "$PSScriptRoot/../src/"
        "$PSScriptRoot/../test/tools/"
    )

    Get-ChildItem -Path $paths -Recurse -Filter "*.csproj" -Exclude 'PSGalleryModules.csproj', 'PSGalleryTestModules.csproj' | ForEach-Object {
        Write-Verbose -Message "Reading - $($_.FullName)" -Verbose
        $prj = [xml] (Get-Content $_.FullName -Raw)
        $pkgRef = $prj.Project.ItemGroup.PackageReference

        foreach ($p in $pkgRef) {
            if ($null -ne $p -and -not $skipModules.Contains($p.Include)) {
                if (-not $packages.ContainsKey($p.Include)) {
                    $packages.Add($p.Include, @([PkgVer]::new($p.Include, $p.Version, $null, $_.FullName)))
                } else {
                    $packages[$p.Include] += [PkgVer]::new($p.Include, $p.Version, $null, $_.FullName)
                }
            }
        }
    }

    $versionPattern = (Get-Content "$PSScriptRoot/../DotnetRuntimeMetadata.json" | ConvertFrom-Json).sdk.packageVersionPattern

    $packages.GetEnumerator() | ForEach-Object {
        $pkgs = Find-Package -Name $_.Key -AllVersions -AllowPrereleaseVersions -Source 'dotnet5'

        foreach ($v in $_.Value) {
            $version = $v.Version

            foreach ($p in $pkgs) {
                if ($p.Version -like "$versionPattern*") {
                    if ([System.Management.Automation.SemanticVersion] ($version) -lt [System.Management.Automation.SemanticVersion] ($p.Version)) {
                        $v.NewVersion = $p.Version
                        break
                    }
                }
            }
        }
    }

    # we need a ForEach-Object below to unravel each of the items in 'Values' which is an array of PkgVer
    $pkgsByPath = $packages.Values | ForEach-Object { $_ } | Group-Object -Property Path

    $pkgsByPath | ForEach-Object {
        Update-CsprojFile -Path $_.Name -Values $_.Group
    }
}

<#
 .DESCRIPTION Update package versions to the latest as per the pattern mentioned in DotnetRuntimeMetadata.json
#>
function Update-CsprojFile([string] $path, $values) {
    $fileContent = Get-Content $path -Raw
    $updated = $false

    foreach ($v in $values) {
        if ($v.NewVersion) {
            $stringToReplace = "<PackageReference Include=`"$($v.Name)`" Version=`"$($v.Version)`" />"
            $newString = "<PackageReference Include=`"$($v.Name)`" Version=`"$($v.NewVersion)`" />"

            $fileContent = $fileContent -replace $stringToReplace, $newString
            $updated = $true
        }
    }

    if ($updated) {
        ($fileContent).TrimEnd() | Out-File -FilePath $path -Force
    }
}

function Get-DotnetUpdate {
    try {
        $dotnetMetadataPath = "$PSScriptRoot/../DotnetRuntimeMetadata.json"
        $nextChannel = (Get-Content $dotnetMetadataPath -Raw | ConvertFrom-Json).sdk.nextChannel
        $latestSDKversion = [System.Management.Automation.SemanticVersion] (Invoke-RestMethod -Uri "http://aka.ms/dotnet/$nextChannel/Sdk/productVersion.txt" -ErrorAction Stop | ForEach-Object { $_.Trim() })
        $currentVersion = [System.Management.Automation.SemanticVersion] (( Get-Content -Path "$PSScriptRoot/../global.json" -Raw | ConvertFrom-Json).sdk.version)

        if ($latestSDKversion -gt $currentVersion) {
            $shouldUpdate = $true
            $newVersion = $latestSDKversion
        } else {
            $shouldUpdate = $false
            $newVersion = $null
        }
    } catch {
        Write-Verbose -Verbose "Error occured: $_.message"
        $shouldUpdate = $false
        $newVersion = $null
        Write-Error "Error while checking .NET SDK update: $($_.message)"
    }

    return @{
        ShouldUpdate = $shouldUpdate
        NewVersion   = $newVersion
        Message      = $Message
    }
}

$dotnetUpdate = Get-DotnetUpdate

if ($dotnetUpdate.ShouldUpdate) {

    $dotnetMetadataPath = "$PSScriptRoot/../DotnetRuntimeMetadata.json"
    $dotnetMetadataJson = Get-Content $dotnetMetadataPath -Raw | ConvertFrom-Json

    # Channel is like: $Channel = "5.0.1xx-preview2"
    $Channel = $dotnetMetadataJson.sdk.channel

    Import-Module "$PSScriptRoot/../build.psm1" -Force

    Find-Dotnet

    if (-not (Get-PackageSource -Name 'dotnet5' -ErrorAction SilentlyContinue)) {
        $nugetFeed = ([xml](Get-Content .\nuget.config -Raw)).Configuration.packagesources.add | Where-Object { $_.Key -eq 'dotnet5' } | Select-Object -ExpandProperty Value
        Register-PackageSource -Name 'dotnet5' -Location $nugetFeed -ProviderName NuGet
        Write-Verbose -Message "Register new package source 'dotnet5'" -verbose
    }

    ## Install latest version from the channel

    $sdkVersion = if ($SDKVersionOverride) { $SDKVersionOverride } else { $dotnetUpdate.NewVersion }

    Install-Dotnet -Channel "$Channel" -Version $sdkVersion

    Write-Verbose -Message "Installing .NET SDK completed." -Verbose

    $environment = Get-EnvironmentInformation

    $dotnetPath = if ($environment.IsWindows) { "$env:LocalAppData\Microsoft\dotnet" } else { "$env:HOME/.dotnet" }

    $pathSep = [System.IO.Path]::PathSeparator

    if (-not (($ENV:PATH -split $pathSep) -contains "$dotnetPath")) {
        $env:PATH = "$dotnetPath" + $pathSep + "$ENV:PATH"
    }

    $latestSdkVersion = (dotnet --list-sdks | Select-Object -Last 1 ).Split() | Select-Object -First 1

    Write-Verbose -Message "Installing .NET SDK completed, version - $latestSdkVersion" -Verbose

    Update-GlobalJson -Version $latestSdkVersion

    Write-Verbose -Message "Updating global.json completed." -Verbose

    Update-PackageVersion

    Write-Verbose -Message "Updating project files completed." -Verbose

    if ($UpdateMSIPackaging) {
        if (-not $environment.IsWindows) {
            throw "UpdateMSIPackaging can only be done on Windows"
        }

        Import-Module "$PSScriptRoot/../build.psm1" -Force
        Import-Module "$PSScriptRoot/packaging" -Force
        Start-PSBootstrap -Package
        Start-PSBuild -Clean -Configuration Release -CrossGen

        try {
            Start-PSPackage -Type msi -SkipReleaseChecks -InformationVariable wxsData
        } catch {
            if ($_.Exception.Message -like "Current files to not match *") {
                Copy-Item -Path $($wxsData.MessageData.NewFile) -Destination ($wxsData.MessageData.FilesWxsPath)
                Write-Verbose -Message "Updating files.wxs file completed." -Verbose
            } else {
                throw $_
            }
        }
    }
}
