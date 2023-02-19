# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This script is used to completely rebuild the cgmanifest.json file,
# which is used to generate the notice file.
# Requires the module dotnet.project.assets from the PowerShell Gallery authored by @TravisEz13

param(
    [switch] $Fix
)

Import-Module dotnet.project.assets
Import-Module "$PSScriptRoot\..\.github\workflows\GHWorkflowHelper" -Force
. "$PSScriptRoot\..\tools\buildCommon\startNativeExecution.ps1"

$packageSourceName = 'findMissingNoticesNugetOrg'
if (!(Get-PackageSource -Name $packageSourceName -ErrorAction SilentlyContinue)) {
    $null = Register-PackageSource -Name $packageSourceName -Location https://www.nuget.org/api/v2 -ProviderName NuGet
}

$existingRegistrationTable = @{}
$cgManifestPath = (Resolve-Path -Path $PSScriptRoot\..\tools\cgmanifest.json).ProviderPath
$existingRegistrationsJson = Get-Content $cgManifestPath | ConvertFrom-Json -AsHashtable
$existingRegistrationsJson.Registrations | ForEach-Object {
    $registration = [Registration]$_
    if ($registration.Component) {
        $name = $registration.Component.Name()
        $existingRegistrationTable.Add($name, $registration)
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

    try {
        [System.Management.Automation.SemanticVersion]$desiredVersion = $Version
    } catch {
        [Version]$desiredVersion = $Version
    }

    $publicVersion = $null
    $publicVersion = Find-Package -Name $Name -AllowPrereleaseVersions -source $packageSourceName -AllVersions -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $packageVersion = [System.Management.Automation.SemanticVersion]$_.Version
        } catch {
            # Fall back to using [version] if it is not a semantic version
            $packageVersion = $_.Version
        }

        $_ | Add-Member -Name SemVer -MemberType NoteProperty -Value $packageVersion -PassThru
    } | Where-Object { $_.SemVer -le $desiredVersion } | Sort-Object -Property semver -Descending | Select-Object -First 1 -ExpandProperty Version

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
            "alpine-x64",
            "linux-arm",
            "linux-arm64",
            "linux-x64",
            "osx-arm64",
            "osx-x64",
            "win-arm",
            "win-arm64",
            "win7-x64",
            "win7-x86",
            "modules")]
        [string]$Runtime,

        [Parameter(Mandatory)]
        [System.Collections.Generic.Dictionary[string, Registration]] $RegistrationTable
    )

    $registrationChanged = $false

    $dotnetTargetName = 'net7.0'
    $dotnetTargetNameWin7 = 'net7.0-windows7.0'
    $unixProjectName = 'powershell-unix'
    $windowsProjectName = 'powershell-win-core'
    $actualRuntime = $Runtime

    switch -regex ($Runtime) {
        "alpine-.*" {
            $folder = $unixProjectName
            $target = "$dotnetTargetName|$Runtime"
        }
        "linux-.*" {
            $folder = $unixProjectName
            $target = "$dotnetTargetName|$Runtime"
        }
        "osx-.*" {
            $folder = $unixProjectName
            $target = "$dotnetTargetName|$Runtime"
        }
        "win7-.*" {
            $sdkToUse = $winDesktopSdk
            $folder = $windowsProjectName
            $target = "$dotnetTargetNameWin7|$Runtime"
        }
        "win-.*" {
            $folder = $windowsProjectName
            $target = "$dotnetTargetNameWin7|$Runtime"
        }
        "modules" {
            $folder = "modules"
            $actualRuntime = 'linux-x64'
            $target = "$dotnetTargetName|$actualRuntime"
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
        } catch {
            Get-ChildItem -Path "${folder}:/targets" | Out-String | Write-Verbose -Verbose
            throw
        }
    } finally {
        Pop-Location
        Get-PSDrive -Name $folder -ErrorAction Ignore | Remove-PSDrive
    }

    $targets | ForEach-Object {
        $target = $_
        $parts = ($target -split '\|')
        $name = $parts[0]
        $targetVersion = $parts[1]
        $publicVersion = Get-NuGetPublicVersion -Name $name -Version $targetVersion

        # Add the registration to the cgmanifest if the TPN does not contain the name of the target OR
        # the existing CG contains the registration, because if the existing CG contains the registration,
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

    return $registrationChanged
}

$registrations = [System.Collections.Generic.Dictionary[string, Registration]]::new()
$lastCount = 0
$registrationChanged = $false
foreach ($runtime in "win7-x64", "linux-x64", "osx-x64", "alpine-x64", "win-arm", "linux-arm", "linux-arm64", "osx-arm64", "win-arm64", "win7-x86") {
    $registrationChanged = (Get-CGRegistrations -Runtime $runtime -RegistrationTable $registrations) -or $registrationChanged
    $count = $registrations.Count
    $newCount = $count - $lastCount
    $lastCount = $count
    Write-Verbose "$newCount new registrations, $count total..." -Verbose
}

$newRegistrations = $registrations.Keys | Sort-Object | ForEach-Object { $registrations[$_] }

$count = $newRegistrations.Count
$newJson = @{
    Registrations = $newRegistrations 
     '$schema' = "https://json.schemastore.org/component-detection-manifest.json"
} | ConvertTo-Json -depth 99

if ($Fix -and $registrationChanged) {
    $newJson | Set-Content $cgManifestPath
    Set-GWVariable -Name CGMANIFEST_PATH -Value $cgManifestPath
}

if (!$Fix -and $registrationChanged) {
    $temp = Get-GWTempPath

    $tempJson = Join-Path -Path $temp -ChildPath "cgmanifest$((Get-Date).ToString('yyyMMddHHmm')).json"
    $newJson | Set-Content $tempJson -Encoding utf8NoBOM
    Set-GWVariable -Name CGMANIFEST_PATH -Value $tempJson
    throw "cgmanifest is out of date.  run ./tools/findMissingNotices.ps1 -Fix.  Generated cgmanifest is here: $tempJson"
}

Write-Verbose "$count registrations created!" -Verbose
