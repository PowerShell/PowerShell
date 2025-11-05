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
        return ([ordered] @{
            profileType = $this.profileType
            content    = $this.content
        }) | ConvertTo-Json -Compress -EnumsAsStrings
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
    $resource.content = $fileExists ? (Get-Content -Path $profilePath) : $null
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

    # Cannot use the ToJson() method here as we are adding a note property
    $allUserCurrentHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'AllUsersCurrentHost' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    $allUsersAllHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'AllUsersAllHosts' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    $currentUserAllHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'CurrentUserAllHosts' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    $currentUserCurrentHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'CurrentUserCurrentHost' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
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

    $InputResource.ToJson()
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
        else  {
            ## Do nothing if content is not specified
        }
    }
    elseif (-not $InputResource._exist -and $profileExists) {
        Remove-Item -Path $profilePath -Force
    }
    elseif (-not $InputResource._exist -and -not $profileExists) {
        # Do nothing
    }
}

$inputJson = $input | ConvertFrom-Json

if ($inputJson) {
    $InputResource = [PwshResource]::new()
    $InputResource.profileType = $inputJson.profileType
    $InputResource.content = $inputJson.content
    $InputResource._exist = $inputJson._exist
}

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
