# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This script is used to completely rebuild the cgmanifgest.json file,
# which is used to generate the notice file.
# Requires the module dotnet.project.assets from the PowerShell Gallery authored by @TravisEz13

param(
    [switch] $Fix,
    [switch] $IsStable,
    [switch] $ForceHarvestedOnly
)

Import-Module dotnet.project.assets
Import-Module "$PSScriptRoot\..\.github\workflows\GHWorkflowHelper" -Force
. "$PSScriptRoot\..\tools\buildCommon\startNativeExecution.ps1"
. "$PSScriptRoot\clearlyDefined\Find-LastHarvestedVersion.ps1"

$packageSourceName = 'findMissingNoticesNugetOrg'
if (!(Get-PackageSource -Name $packageSourceName -ErrorAction SilentlyContinue)) {
    $null = Register-PackageSource -Name $packageSourceName -Location https://www.nuget.org/api/v2 -ProviderName NuGet
}

$existingRegistrationTable = @{}
$cgManifestPath = (Resolve-Path -Path $PSScriptRoot\cgmanifest\main\cgmanifest.json).ProviderPath
$existingRegistrationsJson = Get-Content $cgManifestPath | ConvertFrom-Json -AsHashtable
$existingRegistrationsJson.Registrations | ForEach-Object {
    $registration = [Registration]$_
    if ($registration.Component) {
        $name = $registration.Component.Name()
        if (!$existingRegistrationTable.ContainsKey($name)) {
            $existingRegistrationTable.Add($name, $registration)
        }
    }
}

Class Registration {
    [Component]$Component
    [bool]$DevelopmentDependency
}

Class Component {
    [ValidateSet("nuget")]
    [String] $Type
    [Nuget]$Nuget

    [string]ToString() {
        $message = "Type: $($this.Type)"
        if ($this.Type -eq "nuget") {
            $message += "; $($this.Nuget)"
        }
        return $message
    }

    [string]Name() {
        switch ($this.Type) {
            "nuget" {
                return $($this.Nuget.Name)
            }
            default {
                throw "Unknown component type: $($this.Type)"
            }
        }
        throw "How did we get here?!?"
    }

    [string]Version() {
        switch ($this.Type) {
            "nuget" {
                return $($this.Nuget.Version)
            }
            default {
                throw "Unknown component type: $($this.Type)"
            }
        }
        throw "How did we get here?!?"
    }
}

Class Nuget {
    [string]$Name
    [string]$Version

    [string]ToString() {
        return "$($this.Name) - $($this.Version)"
    }
}

$winDesktopSdk = 'Microsoft.NET.Sdk.WindowsDesktop'
if (!$IsWindows) {
    $winDesktopSdk = 'Microsoft.NET.Sdk'
    Write-Warning "Always using $winDesktopSdk since this is not windows!!!"
}

function ConvertTo-SemVer {
    param(
        [String] $Version
    )

    [System.Management.Automation.SemanticVersion]$desiredVersion = [System.Management.Automation.SemanticVersion]::Empty

    try {
        $desiredVersion = $Version
    } catch {
        <#
            Json.More.Net broke the rules and published 2.0.1.2 as 2.0.1.
            So, I'm making the logic work for that scenario by
            thorwing away any part that doesn't match non-pre-release semver portion
        #>
        $null = $Version -match '^(\d+\.\d+\.\d+).*'
        $desiredVersion = $matches[1]
    }

    return $desiredVersion
}

function New-NugetComponent {
    param(
        [string]$name,
        [string]$version,
        [switch]$DevelopmentDependency
    )

    $nuget = [Nuget]@{
        Name    = $name
        Version = $version
    }
    $Component = [Component]@{
        Type  = "nuget"
        Nuget = $nuget
    }

    $registration = [Registration]@{
        Component             = $Component
        DevelopmentDependency = $DevelopmentDependency
    }

    return $registration
}

$nugetPublicVersionCache = [System.Collections.Generic.Dictionary[string, string]]::new()
function Get-NuGetPublicVersion {
    param(
        [parameter(Mandatory)]
        [string]$Name,
        [parameter(Mandatory)]
        [string]$Version
    )

    if($nugetPublicVersionCache.ContainsKey($Name)) {
        return $nugetPublicVersionCache[$Name]
    }

    [System.Management.Automation.SemanticVersion]$desiredVersion = ConvertTo-SemVer -Version $Version

    $publicVersion = $null
    $publicVersion = Find-Package -Name $Name -AllowPrereleaseVersions -source $packageSourceName -AllVersions -ErrorAction SilentlyContinue | ForEach-Object {
        [System.Management.Automation.SemanticVersion]$packageVersion = ConvertTo-SemVer -Version $_.Version
        $_ | Add-Member -Name SemVer -MemberType NoteProperty -Value $packageVersion -PassThru
    } | Where-Object {
            $_.SemVer -le $desiredVersion
        } | Sort-Object -Property semver -Descending | Select-Object -First 1 -ExpandProperty Version

    if(!$publicVersion) {
        Write-Warning "No public version found for $Name, using $Version"
        $publicVersion = $Version
    }

    if(!$nugetPublicVersionCache.ContainsKey($Name)) {
        $nugetPublicVersionCache.Add($Name, $publicVersion)
    }

    return $publicVersion
}

function Get-CGRegistrations {
    param(
        [Parameter(Mandatory)]
        [ValidateSet(
            "linux-musl-x64",
            "linux-arm",
            "linux-arm64",
            "linux-x64",
            "osx-arm64",
            "osx-x64",
            "win-arm64",
            "win-x64",
            "win-x86",
            "modules")]
        [string]$Runtime,

        [Parameter(Mandatory)]
        [System.Collections.Generic.Dictionary[string, Registration]] $RegistrationTable
    )

    $registrationChanged = $false

    $dotnetTargetName = 'net11.0'
    $dotnetTargetNameWin7 = 'net11.0-windows8.0'
    $unixProjectName = 'powershell-unix'
    $windowsProjectName = 'powershell-win-core'
    $actualRuntime = $Runtime

    switch -regex ($Runtime) {
        "alpine-.*" {
            $folder = $unixProjectName
            $target = "$dotnetTargetName|$Runtime"
            $neutralTarget = "$dotnetTargetName"
        }
        "linux-.*" {
            $folder = $unixProjectName
            $target = "$dotnetTargetName|$Runtime"
            $neutralTarget = "$dotnetTargetName"
        }
        "osx-.*" {
            $folder = $unixProjectName
            $target = "$dotnetTargetName|$Runtime"
            $neutralTarget = "$dotnetTargetName"
        }
        "win-x*" {
            $sdkToUse = $winDesktopSdk
            $folder = $windowsProjectName
            $target = "$dotnetTargetNameWin7|$Runtime"
            $neutralTarget = "$dotnetTargetNameWin7"
        }
        "win-.*" {
            $folder = $windowsProjectName
            $target = "$dotnetTargetNameWin7|$Runtime"
            $neutralTarget = "$dotnetTargetNameWin7"
        }
        "modules" {
            $folder = "modules"
            $actualRuntime = 'linux-x64'
            $target = "$dotnetTargetName|$actualRuntime"
            $neutralTarget = "$dotnetTargetName"
        }
        Default {
            throw "Invalid runtime name: $Runtime"
        }
    }

    Write-Verbose "Getting registrations for $folder - $actualRuntime ..." -Verbose
    Get-PSDrive -Name $folder -ErrorAction Ignore | Remove-PSDrive
    Push-Location $PSScriptRoot\..\src\$folder
    try {
        Start-NativeExecution -VerboseOutputOnError -sb {
            dotnet restore --runtime $actualRuntime  "/property:SDKToUse=$sdkToUse"
        }
        $null = New-PADrive -Path $PSScriptRoot\..\src\$folder\obj\project.assets.json -Name $folder
        try {
            $targets = Get-ChildItem -Path "${folder}:/targets/$target" -ErrorAction Stop | Where-Object { $_.Type -eq 'package' }  | select-object -ExpandProperty name
            $targets += Get-ChildItem -Path "${folder}:/targets/$neutralTarget" -ErrorAction Stop | Where-Object { $_.Type -eq 'project' }  | select-object -ExpandProperty name
        } catch {
            Get-ChildItem -Path "${folder}:/targets" | Out-String | Write-Verbose -Verbose
            throw
        }
    } finally {
        Pop-Location
        Get-PSDrive -Name $folder -ErrorAction Ignore | Remove-PSDrive
    }

    # Name to skip for TPN generation
    $skipNames = @(
        "Microsoft.PowerShell.Native"
        "Microsoft.Management.Infrastructure.Runtime.Unix"
        "Microsoft.Management.Infrastructure"
        "Microsoft.PowerShell.Commands.Diagnostics"
        "Microsoft.PowerShell.Commands.Management"
        "Microsoft.PowerShell.Commands.Utility"
        "Microsoft.PowerShell.ConsoleHost"
        "Microsoft.PowerShell.SDK"
        "Microsoft.PowerShell.Security"
        "Microsoft.Management.Infrastructure.CimCmdlets"
        "Microsoft.WSMan.Management"
        "Microsoft.WSMan.Runtime"
        "System.Management.Automation"
        "Microsoft.PowerShell.GraphicalHost"
        "Microsoft.PowerShell.CoreCLR.Eventing"
    )

    Write-Verbose "Found $($targets.Count) targets to process..." -Verbose
    $targets | ForEach-Object {
        $target = $_
        $parts = ($target -split '\|')
        $name = $parts[0]

        if ($name -in $skipNames) {
            Write-Verbose "Skipping $name..."

        } else {
            $targetVersion = $parts[1]
            $publicVersion = Get-NuGetPublicVersion -Name $name -Version $targetVersion

            # Add the registration to the cgmanifest if the TPN does not contain the name of the target OR
            # the exisitng CG contains the registration, because if the existing CG contains the registration,
            # that might be the only reason it is in the TPN.
            if (!$RegistrationTable.ContainsKey($target)) {
                $DevelopmentDependency = $false
                if (!$existingRegistrationTable.ContainsKey($name) -or $existingRegistrationTable.$name.Component.Version() -ne $publicVersion) {
                    $registrationChanged = $true
                }
                if ($existingRegistrationTable.ContainsKey($name) -and $existingRegistrationTable.$name.DevelopmentDependency) {
                    $DevelopmentDependency = $true
                }

                $registration = New-NugetComponent -Name $name -Version $publicVersion -DevelopmentDependency:$DevelopmentDependency
                $RegistrationTable.Add($target, $registration)
            }
        }
    }

    return $registrationChanged
}

$registrations = [System.Collections.Generic.Dictionary[string, Registration]]::new()
$lastCount = 0
$registrationChanged = $false
foreach ($runtime in "win-x64", "linux-x64", "osx-x64", "linux-musl-x64", "linux-arm", "linux-arm64", "osx-arm64", "win-arm64", "win-x86") {
    $registrationChanged = (Get-CGRegistrations -Runtime $runtime -RegistrationTable $registrations) -or $registrationChanged
    $count = $registrations.Count
    $newCount = $count - $lastCount
    $lastCount = $count
    Write-Verbose "$newCount new registrations, $count total..." -Verbose
}

$newRegistrations = $registrations.Keys | Sort-Object | ForEach-Object { $registrations[$_] }

if ($IsStable) {
    foreach ($registion in $newRegistrations) {
        $name = $registion.Component.Name()
        $version = $registion.Component.Version()
        $developmentDependency = $registion.DevelopmentDependency
        if ($version -match '-' -and !$developmentDependency) {
            throw "Version $version of $name is preview.  This is not allowed."
        }
    }
}

$count = $newRegistrations.Count
$registrationsToSave = $newRegistrations
$tpnRegistrationsToSave = $null

# If -ForceHarvestedOnly is specified with -Fix, only include harvested packages
# and revert non-harvested packages to their previous versions
if ($Fix -and $ForceHarvestedOnly) {
    Write-Verbose "Checking harvest status and filtering to harvested packages with reversion..." -Verbose

    # Import ClearlyDefined module to check harvest status
    Import-Module -Name "$PSScriptRoot/clearlyDefined/src/ClearlyDefined" -Force

    # Import cache from previous runs to speed up lookups
    Import-ClearlyDefinedCache

    # Get harvest data for all registrations
    $fullCgList = $newRegistrations |
        ForEach-Object {
            [PSCustomObject]@{
                type           = $_.Component.Type
                Name           = $_.Component.Nuget.Name
                PackageVersion = $_.Component.Nuget.Version
            }
        }

    $fullList = $fullCgList | Get-ClearlyDefinedData

    # Build a lookup table of harvest status by package name + version
    $harvestStatus = @{}
    foreach ($item in $fullList) {
        $key = "$($item.Name)|$($item.PackageVersion)"
        $harvestStatus[$key] = $item.harvested
    }

    # Build a lookup table of old versions from existing manifest
    $oldVersions = @{}
    foreach ($registration in $existingRegistrationsJson.Registrations) {
        $name = $registration.Component.Nuget.Name
        if (!$oldVersions.ContainsKey($name)) {
            $oldVersions[$name] = $registration
        }
    }

    # Process each new registration: keep harvested, revert non-harvested
    $tpnRegistrationsToSave = @()
    $harvestedCount = 0
    $revertedCount = 0

    foreach ($reg in $newRegistrations) {
        $name = $reg.Component.Nuget.Name
        $version = $reg.Component.Nuget.Version
        $key = "$name|$version"

        if ($harvestStatus.ContainsKey($key) -and $harvestStatus[$key]) {
            # Package is harvested, include it
            $tpnRegistrationsToSave += $reg
            $harvestedCount++
        } else {
            # Package not harvested, find last harvested version
            $lastHarvestedVersion = Find-LastHarvestedVersion -Name $name -CurrentVersion $version

            # Use last harvested version if found, otherwise use old version as fallback
            if ($lastHarvestedVersion) {
                if ($lastHarvestedVersion -ne $version) {
                    $revertedReg = New-NugetComponent -Name $name -Version $lastHarvestedVersion -DevelopmentDependency:$reg.DevelopmentDependency
                    $tpnRegistrationsToSave += $revertedReg
                    $revertedCount++
                    Write-Verbose "Reverted $name from v$version to last harvested v$lastHarvestedVersion" -Verbose
                } else {
                    $tpnRegistrationsToSave += $reg
                }
            } elseif ($oldVersions.ContainsKey($name)) {
                $tpnRegistrationsToSave += $oldVersions[$name]
                $revertedCount++
                Write-Verbose "Reverted $name to previous version (no harvested version found)" -Verbose
            } else {
                Write-Warning "$name v$version not harvested and no previous version found. Excluding from manifest."
            }
        }
    }

    Write-Verbose "Completed filtering for TPN: $harvestedCount harvested + $revertedCount reverted = $($tpnRegistrationsToSave.Count) total" -Verbose
}

$newJson = @{
    Registrations = $registrationsToSave
     '$schema' = "https://json.schemastore.org/component-detection-manifest.json"
} | ConvertTo-Json -depth 99

if ($Fix -and $registrationChanged) {
    $newJson | Set-Content $cgManifestPath
    Set-GWVariable -Name CGMANIFEST_PATH -Value $cgManifestPath
}

# If -ForceHarvestedOnly was used, write the TPN manifest with filtered registrations
if ($Fix -and $ForceHarvestedOnly -and $tpnRegistrationsToSave.Count -gt 0) {
    $tpnManifestDir = Join-Path -Path $PSScriptRoot -ChildPath "cgmanifest\tpn"
    New-Item -ItemType Directory -Path $tpnManifestDir -Force | Out-Null
    $tpnManifestPath = Join-Path -Path $tpnManifestDir -ChildPath "cgmanifest.json"

    $tpnManifest = @{
        Registrations = @($tpnRegistrationsToSave)
        '$schema'     = "https://json.schemastore.org/component-detection-manifest.json"
    }

    $tpnJson = $tpnManifest | ConvertTo-Json -depth 99
    $tpnJson | Set-Content $tpnManifestPath -Encoding utf8NoBOM
    Write-Verbose "TPN manifest created/updated with $($tpnRegistrationsToSave.Count) registrations (filtered for harvested packages)" -Verbose
}

# Skip legacy TPN update when -ForceHarvestedOnly already produced a filtered manifest
if ($Fix -and $registrationChanged -and -not $ForceHarvestedOnly) {
    # Import ClearlyDefined module to check harvest status
    Write-Verbose "Checking harvest status for newly added packages..." -Verbose
    Import-Module -Name "$PSScriptRoot/clearlyDefined/src/ClearlyDefined" -Force

    # Get harvest data for all registrations
    $fullCgList = $newRegistrations |
        ForEach-Object {
            [PSCustomObject]@{
                type           = $_.Component.Type
                Name           = $_.Component.Nuget.Name
                PackageVersion = $_.Component.Nuget.Version
            }
        }

    $fullList = $fullCgList | Get-ClearlyDefinedData
    $needHarvest = $fullList | Where-Object { !$_.harvested }

    if ($needHarvest.Count -gt 0) {
        Write-Verbose "Found $($needHarvest.Count) packages that need harvesting. Starting harvest..." -Verbose
        $needHarvest | Select-Object -ExpandProperty coordinates | ConvertFrom-ClearlyDefinedCoordinates | Start-ClearlyDefinedHarvest
    } else {
        Write-Verbose "All packages are already harvested." -Verbose
    }

    # After manifest update and harvest, update TPN manifest with individual package status
    Write-Verbose "Updating TPN manifest with individual package harvest status..." -Verbose
    $tpnManifestDir = Join-Path -Path $PSScriptRoot -ChildPath "cgmanifest\tpn"
    $tpnManifestPath = Join-Path -Path $tpnManifestDir -ChildPath "cgmanifest.json"

    # Load current TPN manifest to get previous versions
    $currentTpnManifest = @()
    if (Test-Path $tpnManifestPath) {
        $currentTpnJson = Get-Content $tpnManifestPath | ConvertFrom-Json -AsHashtable
        $currentTpnManifest = $currentTpnJson.Registrations
    }

    # Build a lookup table of old versions
    $oldVersions = @{}
    foreach ($registration in $currentTpnManifest) {
        $name = $registration.Component.Nuget.Name
        if (!$oldVersions.ContainsKey($name)) {
            $oldVersions[$name] = $registration
        }
    }

    # Note: Do not recheck harvest status here. Harvesting is an async process that takes a significant amount of time.
    # Use the harvest data from the initial check. Newly triggered harvests will be captured
    # on the next run of this script after harvesting completes.
    $finalHarvestData = $fullList

    # Update packages individually based on harvest status
    $tpnRegistrations = @()
    $harvestedCount = 0
    $restoredCount = 0

    foreach ($item in $finalHarvestData) {
        $matchingNewRegistration = $newRegistrations | Where-Object {
            $_.Component.Nuget.Name -eq $item.Name -and
            $_.Component.Nuget.Version -eq $item.PackageVersion
        }

        if ($matchingNewRegistration) {
            if ($item.harvested) {
                # Use new harvested version
                $tpnRegistrations += $matchingNewRegistration
                $harvestedCount++
            } else {
                # Package not harvested - find the last harvested version from ClearlyDefined API
                Write-Verbose "Finding last harvested version for $($item.Name)..." -Verbose

                $lastHarvestedVersion = $null
                try {
                    # Search through all versions of this package to find the last harvested one
                    # Create a list of versions we know about from all runtimes
                    $packageVersionsToCheck = $newRegistrations | Where-Object {
                        $_.Component.Nuget.Name -eq $item.Name
                    } | ForEach-Object { $_.Component.Nuget.Version } | Sort-Object -Unique -Descending

                    foreach ($versionToCheck in $packageVersionsToCheck) {
                        $versionCheckList = [PSCustomObject]@{
                            type           = "nuget"
                            Name           = $item.Name
                            PackageVersion = $versionToCheck
                        }

                        $versionStatus = $versionCheckList | Get-ClearlyDefinedData
                        if ($versionStatus -and $versionStatus.harvested) {
                            $lastHarvestedVersion = $versionToCheck
                            break  # Found the most recent harvested version
                        }
                    }
                } catch {
                    Write-Verbose "Error checking harvested versions for $($item.Name): $_" -Verbose
                }

                # Use last harvested version if found, otherwise use old version as fallback
                if ($lastHarvestedVersion) {
                    $revertedReg = New-NugetComponent -Name $item.Name -Version $lastHarvestedVersion -DevelopmentDependency:$matchingNewRegistration.DevelopmentDependency
                    $tpnRegistrations += $revertedReg
                    $restoredCount++
                    Write-Verbose "Reverted $($item.Name) from v$($item.PackageVersion) to last harvested v$lastHarvestedVersion" -Verbose
                } elseif ($oldVersions.ContainsKey($item.Name)) {
                    $tpnRegistrations += $oldVersions[$item.Name]
                    $restoredCount++
                    Write-Verbose "Reverted $($item.Name) to previous version in TPN (no harvested version found)" -Verbose
                } else {
                    Write-Warning "$($item.Name) v$($item.PackageVersion) not harvested and no harvested version found. Excluding from TPN manifest."
                }
            }
        }
    }

    # Save updated TPN manifest
    if ($tpnRegistrations.Count -gt 0) {
        $tpnManifest = @{
            Registrations = @($tpnRegistrations)
            '$schema'     = "https://json.schemastore.org/component-detection-manifest.json"
        }

        $tpnJson = $tpnManifest | ConvertTo-Json -depth 99
        $tpnJson | Set-Content $tpnManifestPath -Encoding utf8NoBOM
        Write-Verbose "TPN manifest updated: $harvestedCount new harvested + $restoredCount reverted to last harvested versions" -Verbose
    }
}

if (!$Fix -and $registrationChanged) {
    $temp = Get-GWTempPath

    $tempJson = Join-Path -Path $temp -ChildPath "cgmanifest$((Get-Date).ToString('yyyMMddHHmm')).json"
    $newJson | Set-Content $tempJson -Encoding utf8NoBOM
    Set-GWVariable -Name CGMANIFEST_PATH -Value $tempJson
    throw "cgmanifest is out of date.  run ./tools/findMissingNotices.ps1 -Fix.  Generated cgmanifest is here: $tempJson"
}

Write-Verbose "$count registrations created!" -Verbose
