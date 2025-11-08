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
    [string] $profilePath
    [bool] $_exist

    [string] ToJson() {
        return ([ordered] @{
            profileType = $this.profileType
            content    = $this.content
            profilePath = $this.profilePath
            _exist     = $this._exist
        }) | ConvertTo-Json -Compress -EnumsAsStrings
    }

    PwshResource([ProfileType] $profileType, [string] $content, [bool] $_exist) {
        $this.profileType = $profileType
        $this.content = $content
        $this.profilePath = GetProfilePath -profileType $profileType
        $this._exist = $_exist
    }

    PwshResource([ProfileType] $profileType) {
        $this.profileType = $profileType
        $this.profilePath = GetProfilePath -profileType $profileType

        $fileExists = Test-Path $this.profilePath
        if ($fileExists) {
            $this.content = Get-Content -Path $this.profilePath
        }
        else {
            $this.content = $null
        }

        $this._exist = $fileExists
    }
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
    $allUserCurrentHost = [PwshResource]::new('AllUsersCurrentHost')
    $allUsersAllHost = [PwshResource]::new('AllUsersAllHosts')
    $currentUserAllHost = [PwshResource]::new('CurrentUserAllHosts')
    $currentUserCurrentHost = [PwshResource]::new('CurrentUserCurrentHost')

    # Cannot use the ToJson() method here as we are adding a note property
    $allUserCurrentHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'AllUsersCurrentHost' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    $allUsersAllHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'AllUsersAllHosts' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    $currentUserAllHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'CurrentUserAllHosts' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    $currentUserCurrentHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'CurrentUserCurrentHost' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
}

function GetOperation {
    param (
        [Parameter(Mandatory = $true)]
        [PwshResource] $InputResource,
        [Parameter()]
        [switch] $AsJson
    )

    $profilePath = GetProfilePath -profileType $InputResource.profileType.ToString()

    $actualState = [PwshResource]::new($InputResource.profileType)

    $actualState.profilePath = $profilePath

    $exists = Test-Path $profilePath

    if ($InputResource._exist -and $exists) {
        $content = Get-Content -Path $profilePath
        $actualState.Content = $content
    }
    elseif ($InputResource._exist -and -not $exists) {
        $actualState.Content = $null
        $actualState._exist = $false
    }
    elseif (-not $InputResource._exist -and $exists) {
        $actualState.Content = Get-Content -Path $profilePath
        $actualState._exist = $true
    }
    else {
        $actualState.Content = $null
        $actualState._exist = $false
    }

    if ($AsJson) {
        return $actualState.ToJson()
    }
    else {
        return $actualState
    }
}

function SetOperation {
    param (
        [PwshResource] $InputResource
    )

    $actualState = GetOperation -InputResource $InputResource

    if ($InputResource._exist) {
        if (-not $actualState._exist) {
            $null = New-Item -Path $actualState.profilePath -ItemType File -Force
        }

        if ($null -ne $InputResource.content) {
            Set-Content -Path $actualState.profilePath -Value $InputResource.content
        }
    }
    elseif ($actualState._exist) {
        Remove-Item -Path $actualState.profilePath -Force
    }
}

$inputJson = $input | ConvertFrom-Json

if ($inputJson) {
    $InputResource = [PwshResource]::new( $inputJson.profileType, $inputJson.content, $inputJson._exist )
}

switch ($Operation) {
    'get' {
        GetOperation -InputResource $InputResource -AsJson
    }
    'set' {
        SetOperation -InputResource $InputResource
    }
    'export' {
        if ($inputJson) {
            throw "Input is not expected for export operation."
        }

        ExportOperation
    }
}
