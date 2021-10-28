# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Requires the module dotnet.project.assets from the PowerShell Gallery authored by @TravisEz13

import-module dotnet.project.assets
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

$existingRegistrationTable = @{}
$newRegistrations = @()
$existingRegistrationsJson = Get-Content $PSScriptRoot\..\cgmanifest.json | ConvertFrom-Json -AsHashtable
$existingRegistrationsJson.Registrations | ForEach-Object {
    $registration = [Registration]$_
    $existingRegistrationTable.Add($registration.Component.Name(), $registration)
    $newRegistrations += $registration
}

Get-PSDrive -Name pwsh-win-core -ErrorAction Ignore | Remove-PSDrive
Push-Location $PSScriptRoot\..\src\powershell-win-core
$null = dotnet restore
$null = New-PADrive -Path $PSScriptRoot\..\src\powershell-win-core\obj\project.assets.json -Name pwsh-win-core
$targets = Get-ChildItem -Path 'pwsh-win-core:/targets/net6.0-windows7.0|win7-x64' | Where-Object {
    $_.Type -eq 'package' -and
    $_.Name -notlike 'DotNetAnalyzers.DocumentationAnalyzers*' -and
    $_.Name -notlike 'StyleCop*' -and
    $_.Name -notlike 'Microsoft.CodeAnalysis.Analyzers*' -and
    $_.Name -notlike 'Microsoft.CodeAnalysis.NetAnalyzers*'
}  | select-object -ExpandProperty name
Pop-Location
Get-PSDrive -Name pwsh-win-core | Remove-PSDrive

$updateRegistrations = @()
$targets | ForEach-Object {
    $target = $_
    $parts = ($target -split '\|')
    $name = $parts[0]
    $targetVersion = $parts[1]
    $pattern = [regex]::Escape($name) + " "
    $tpnMatch = Select-String -Path $PSScriptRoot\..\ThirdPartyNotices.txt -Pattern $pattern
    if (!$tpnMatch) {
        if ($existingRegistrationTable.ContainsKey($name)) {
            $registrationVersion = $existingRegistrationTable.$name.Component.Version()
            if ($registrationVersion -ne $targetVersion) {
                $registration = New-NugetComponent -Name $name -Version $targetVersion
                $updateRegistrations += $registration
            } else {
                Write-Verbose "$target already registered: $registrationVersion" -Verbose
            }
        } else {
            $registration = New-NugetComponent -Name $name -Version $targetVersion
            $newRegistrations += $registration
        }
    }
}

if ($updateRegistrations.count -gt 0) {
    #TODO delete existing and add new registration
    throw "updating registrations is not implemented"
}

$newCount = $newRegistrations.count - $existingRegistrationTable.count
@{Registrations = $newRegistrations } | ConvertTo-Json -depth 99 | Set-Content $PSScriptRoot\..\cgmanifest.json
Write-Verbose "$newCount registrations added" -Verbose
