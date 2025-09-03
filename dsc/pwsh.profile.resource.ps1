## Copyright (c) Microsoft Corporation. All rights reserved.
## Licensed under the MIT License.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('get', 'set', 'export')]
    [string]$Operation,
    [Parameter(ValueFromPipeline)]
    $stdinput
)

enum ProfileType {
    AllUsersCurrentHost
    AllUsersAllHosts
    CurrentUserAllHosts
    CurrentUserCurrentHost
}

class PwshResource {
    [ProfileType] $profileType
    [string] $content
    [bool] $_exist

    [string] ToJson() {
        return @{
            ProfileType = $this.ProfileType
            Content    = $this.Content
        } | ConvertTo-Json -Compress
    }
}

function PopulatePwshResource {
    param (
        [ProfileType] $profileType
    )

    $profilePath = GetProfilePath -profileType $profileType
    $fileExists = Test-Path $profilePath

    $resource = [PwshResource]::new()
    $resource.profileType = $profileType
    $resource.content = $fileExists ? (Get-Content -Path $profilePath -Raw) : $null
    $resource._exist = $fileExists

    return $resource
}

function GetProfilePath {
    param (
        [ProfileType] $profileType
    )

    $path = switch ($profileType) {
        'AllUsersCurrentHost' { $PROFILE.AllUsersCurrentHost}
        'AllUsersAllHosts' { $PROFILE.AllUsersAllHosts}
        'CurrentUserAllHosts' { $PROFILE.CurrentUserAllHosts}
        'CurrentUserCurrentHost' { $PROFILE.CurrentUserCurrentHost}
    }

    return $path
}

function ExportOperation {
    $allUserCurrentHost = PopulatePwshResource -profileType 'AllUsersCurrentHost'
    $allUsersAllHost = PopulatePwshResource -profileType 'AllUsersAllHosts'
    $currentUserAllHost = PopulatePwshResource -profileType 'CurrentUserAllHosts'
    $currentUserCurrentHost = PopulatePwshResource -profileType 'CurrentUserCurrentHost'

    $allUserCurrentHost | ConvertTo-Json -Compress
    $allUsersAllHost | ConvertTo-Json -Compress
    $currentUserAllHost | ConvertTo-Json -Compress
    $currentUserCurrentHost | ConvertTo-Json -Compress
}

function GetOperation {
    param (
        [PwshResource] $InputResource
    )

    $profilePath = GetProfilePath -profileType $InputResource.profileType.ToString()

    $exists = Test-Path $profilePath

    if ($InputResource._exist -and $exists) {
        $content = Get-Content -Path $profilePath
        $InputResource.Content = $content
    }
    elseif ($InputResource._exist -and -not $exists) {
        $InputResource.Content = $null
        $InputResource._exist = $false
    }
    elseif (-not $InputResource._exist -and $exists) {
        $InputResource.Content = Get-Content -Path $profilePath
        $InputResource._exist = $true
    }
    else {
        $InputResource.Content = $null
        $InputResource._exist = $false
    }

    $InputResource | ConvertTo-Json -Compress
}

function SetOperation {
    param (
        [PwshResource] $InputResource
    )

    $profilePath = GetProfilePath -profileType $InputResource.profileType.ToString()
    $profileExists = Test-Path $profilePath

    if ($InputResource._exist) {
        if ($InputResource.content) {
            Set-Content -Path $profilePath -Value $InputResource.content
        }
        else {
            Remove-Item -Path $profilePath -Force
        }
    }
    elseif (-not $InputResource._exist -and $profileExists) {
        Remove-Item -Path $profilePath -Force
    }
}

$inputJson = $input | ConvertFrom-Json

$InputResource = [PwshResource]::new()
$InputResource.profileType = $inputJson.profileType
$InputResource.content = $inputJson.content
$InputResource._exist = $inputJson._exist

switch ($Operation) {
    'get' {
        GetOperation -InputResource $InputResource
    }
    'set' {
        SetOperation -InputResource $InputResource
    }
    'export' {
        ExportOperation
    }
}
