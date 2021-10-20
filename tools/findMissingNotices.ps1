# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This script is used to completely rebuild the
# Requires the module dotnet.project.assets from the PowerShell Gallery authored by @TravisEz13

Import-Module dotnet.project.assets

$existingRegistrationTable = @{}
$existingRegistrationsJson = Get-Content $PSScriptRoot\..\cgmanifest.json | ConvertFrom-Json -AsHashtable
$existingRegistrationsJson.Registrations | ForEach-Object {
    $registration = [Registration]$_
    $existingRegistrationTable.Add($registration.Component.Name(), $registration)
}

# this function wraps native command Execution
# for more information, read https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
function script:Start-NativeExecution {
    param(
        [scriptblock]$sb,
        [switch]$IgnoreExitcode,
        [switch]$VerboseOutputOnError
    )
    $backupEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if ($VerboseOutputOnError.IsPresent) {
            $output = & $sb 2>&1
        } else {
            & $sb
        }

        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode) {
            if ($VerboseOutputOnError.IsPresent -and $output) {
                $output | Out-String | Write-Verbose -Verbose
            }

            # Get caller location for easier debugging
            $caller = Get-PSCallStack -ErrorAction SilentlyContinue
            if ($caller) {
                $callerLocationParts = $caller[1].Location -split ":\s*line\s*"
                $callerFile = $callerLocationParts[0]
                $callerLine = $callerLocationParts[1]

                $errorMessage = "Execution of {$sb} by ${callerFile}: line $callerLine failed with exit code $LASTEXITCODE"
                throw $errorMessage
            }
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    } finally {
        $ErrorActionPreference = $backupEAP
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

function New-NugetComponent {
    param(
        [string]$name,
        [string]$version
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
        DevelopmentDependency = $false
    }

    return $registration
}

$winDesktopSdk = 'Microsoft.NET.Sdk.WindowsDesktop'
if (!$IsWindows) {
    $winDesktopSdk = 'Microsoft.NET.Sdk'
    Write-Warning "Always using $winDesktopSdk since this is not windows!!!"
}

Function Get-CGRegistrations {
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

    $newRegistrations = $Registrations

    $dotnetTargetName = 'net6.0'
    $dotnetTargetNameWin7 = 'net6.0-windows7.0'
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
        script:Start-NativeExecution -VerboseOutputOnError -sb {
            dotnet restore --runtime $actualRuntime  "/property:SDKToUse=$sdkToUse"
        }
        $null = New-PADrive -Path $PSScriptRoot\..\src\$folder\obj\project.assets.json -Name $folder
        try {
            $targets = Get-ChildItem -Path "${folder}:/targets/$target" -ErrorAction Stop | Where-Object {
                $_.Type -eq 'package' -and
                $_.Name -notlike 'DotNetAnalyzers.DocumentationAnalyzers*' -and
                $_.Name -notlike 'StyleCop*' -and
                $_.Name -notlike 'Microsoft.CodeAnalysis.Analyzers*' -and
                $_.Name -notlike 'Microsoft.CodeAnalysis.NetAnalyzers*'
            }  | select-object -ExpandProperty name
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
        $pattern = [regex]::Escape($name) + " "
        $tpnMatch = select-string -Path $PSScriptRoot\..\ThirdPartyNotices.txt -Pattern $pattern

        # Add the registration to the cgmanifest if the TPN does not contain the name of the target OR
        # the exisitng CG contains the registration, because if the existing CG contains the registration,
        # that might be the only reason it is in the TPN.
        if (!$tpnMatch -or $existingRegistrationTable.ContainsKey($name)) {
            if (!$RegistrationTable.ContainsKey($target)) {
                $registration = New-NugetComponent -Name $name -Version $targetVersion
                $RegistrationTable.Add($target, $registration)
            }
        }
    }
}

[System.Collections.Generic.Dictionary[string, Registration]]$registrations = @{}
$lastCount = 0
foreach ($runtime in @("win7-x64", "linux-x64", "osx-x64", "alpine-x64", "win-arm", "linux-arm", "linux-arm64", "osx-arm64", "win-arm64", "win7-x86")) {
    Get-CGRegistrations -Runtime $runtime -RegistrationTable $registrations
    $count = $registrations.Count
    $newCount = $count - $lastCount
    $lastCount = $count
    Write-Verbose "$newCount new registrations, $count total..." -Verbose
}

$newRegistrations = @()
foreach ($target in ($registrations.Keys | Sort-Object)) {
    $newRegistrations += $registrations[$target]
}

$count = $newRegistrations.Count
@{Registrations = $newRegistrations } | ConvertTo-Json -depth 99 | Set-Content $PSScriptRoot\..\cgmanifest.json
Write-Verbose "$count registrations created!" -Verbose
