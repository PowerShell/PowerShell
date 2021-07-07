# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param (
    [Parameter()]
    [string]$SDKVersionOverride,

    [Parameter()]
    [switch]$UseNuGetOrg,

    [Parameter()]
    [switch]$UpdateMSIPackaging,

    [Parameter()]
    [string]$RuntimeSourceFeed,

    [Parameter()]
    [string]$RuntimeSourceFeedKey,

    [Parameter()]
    [switch]$InteractiveAuth,

    [Parameter()]
    [switch]$UseInternalFeed
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
        "Microsoft.PowerShell.MarkdownRender"
    )

    $packages = [System.Collections.Generic.Dictionary[[string], [PkgVer[]] ]]::new()

    $paths = @(
        "$PSScriptRoot/packaging/projects/reference/Microsoft.PowerShell.Commands.Utility/Microsoft.PowerShell.Commands.Utility.csproj"
        "$PSScriptRoot/packaging/projects/reference/System.Management.Automation/System.Management.Automation.csproj"
        "$PSScriptRoot/packaging/projects/reference/Microsoft.PowerShell.ConsoleHost/Microsoft.PowerShell.ConsoleHost.csproj"
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

    $source = if ($UseNuGetOrg) { 'nuget.org' } elseif ($UseInternalFeed) { 'dotnet-internal' } else { 'dotnet' }

    # Always add nuget.org as some packages are only found there.
    $source = if ($source -ne 'nuget.org') {
        @($source, "nuget.org")
    }

    $packages.GetEnumerator() | ForEach-Object {
        $pkgs = Find-Package -Name $_.Key -AllVersions -AllowPrereleaseVersions -Source $source

        foreach ($v in $_.Value) {
            $version = $v.Version

            foreach ($p in $pkgs) {
                # some packages are directly updated on nuget.org so need to check that too.
                if ($p.Version -like "$versionPattern*" -or $p.Source -eq 'nuget.org') {
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
    param (
        $channel,
        $quality,
        $qualityFallback,
        $feedUrl,
        $sdkImageVersion
    )

    if ($SDKVersionOverride) {
        return @{
            ShouldUpdate = $true
            NewVersion   = $SDKVersionOverride
            Message      = $null
            FeedUrl      = $feedUrl
        }
    }

    try {

        $latestSDKVersionString = Invoke-RestMethod -Uri "http://aka.ms/dotnet/$channel/$quality/sdk-productVersion.txt" -ErrorAction Stop | ForEach-Object { $_.Trim() }
        $selectedQuality = $quality

        if (-not $latestSDKVersionString.StartsWith($sdkImageVersion))
        {
            # we did not get a version number so fall back to daily
            $latestSDKVersionString = Invoke-RestMethod -Uri "http://aka.ms/dotnet/$channel/$qualityFallback/sdk-productVersion.txt" -ErrorAction Stop | ForEach-Object { $_.Trim() }
            $selectedQuality = $qualityFallback

            if (-not $latestSDKVersionString.StartsWith($sdkImageVersion))
            {
                throw "No build found!"
            }
        }

        $latestSDKversion = [System.Management.Automation.SemanticVersion] $latestSDKVersionString

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
        FeedUrl      = $feedUrl
        Quality      = $selectedQuality
    }
}

function Update-DevContainer {
    $dockerFilePath = "$PSScriptRoot/../.devcontainer/Dockerfile"
    $sdkImageVersion = (Get-Content -Raw "$PSScriptRoot/../DotnetRuntimeMetadata.json" | ConvertFrom-Json).sdk.sdkImageVersion

    $devContainerDocker = (Get-Content $dockerFilePath) -replace 'FROM mcr\.microsoft\.com/dotnet.*', "FROM mcr.microsoft.com/dotnet/nightly/sdk:$sdkImageVersion"

    $devContainerDocker | Out-File -FilePath $dockerFilePath -Force
}

$dotnetMetadataPath = "$PSScriptRoot/../DotnetRuntimeMetadata.json"
$dotnetMetadataJson = Get-Content $dotnetMetadataPath -Raw | ConvertFrom-Json
$channel = $dotnetMetadataJson.sdk.channel
$nextChannel = $dotnetMetadataJson.sdk.nextChannel
$quality = $dotnetMetadataJson.sdk.quality
$qualityFallback = $dotnetMetadataJson.sdk.qualityFallback
$sdkImageVersion = $dotnetMetadataJson.sdk.sdkImageVersion
$internalfeed = $dotnetMetadataJson.internalfeed.url

$dotnetUpdate = Get-DotnetUpdate -channel $nextChannel -quality $quality -feedUrl $internalfeed -qualityFallback $qualityFallback -sdkImageVersion $sdkImageVersion

if ($dotnetUpdate.ShouldUpdate) {

    Import-Module "$PSScriptRoot/../build.psm1" -Force

    Find-Dotnet

    $feedname = if ($UseNuGetOrg) { 'nuget.org' } elseif ($UseInternalFeed) { 'dotnet-internal' } else { 'dotnet' }

    $addDotnetSource = (-not (Get-PackageSource -Name $feedname -ErrorAction SilentlyContinue))

    if (!$UseNuGetOrg -and $addDotnetSource) {
        $nugetFileContent = Get-Content "$PSScriptRoot/../nuget.config" -Raw
        $nugetFileSources = ([xml]($nugetFileContent)).Configuration.packagesources.add

        if ($feedname -ne 'dotnet-internal') {
            $dotnetFeed = $nugetFileSources | Where-Object { $_.Key -eq $feedname } | Select-Object -ExpandProperty Value
            Register-PackageSource -Name $feedname -Location $dotnetFeed -ProviderName NuGet
            Write-Verbose -Message "Register new package source $feedname" -verbose
        }

        if ($feedname -eq 'dotnet-internal') {
            # This NuGet feed is for internal to Microsoft use only.
            $dotnetInternalFeed = $dotnetMetadataJson.internalfeed.url
            $updatedNugetFile = $nugetFileContent -replace "</packageSources>", "  <add key=`"dotnet-internal`" value=`"$dotnetInternalFeed`" />`r`n  </packageSources>"
            $updatedNugetFile | Out-File "$PSScriptRoot/../nuget.config" -Force
            Register-PackageSource -Name 'dotnet-internal' -Location $dotnetInternalFeed -ProviderName NuGet
            Write-Verbose -Message "Register new package source 'dotnet-internal'" -verbose
        }
    }

    ## Install latest version from the channel

    $sdkQuality = $dotnetUpdate.Quality
    $sdkVersion = if ($SDKVersionOverride) { $SDKVersionOverride } else { $dotnetUpdate.NewVersion }

    if (-not $RuntimeSourceFeed) {
        Install-Dotnet -Version $sdkVersion -Quality $sdkQuality -Channel $null
    }
    else {
        Install-Dotnet -Version $sdkVersion -Quality $sdkQuality -AzureFeed $RuntimeSourceFeed -FeedCredential $RuntimeSourceFeedKey -Channel $null
    }

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
        Start-PSBuild -Clean -Configuration Release -CrossGen -InteractiveAuth:$InteractiveAuth

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

    Update-DevContainer
}
