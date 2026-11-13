## Copyright (c) Microsoft Corporation. All rights reserved.
## Licensed under the MIT License.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('get', 'set', 'export')]
    [string]$Operation,
    [Parameter(ValueFromPipeline)]
    [string[]]$UserInput
)

Begin {
    enum ProfileType {
        AllUsersCurrentHost
        AllUsersAllHosts
        CurrentUserAllHosts
        CurrentUserCurrentHost
    }

    function New-PwshResource {
        param(
            [Parameter(Mandatory = $true)]
            [ProfileType] $ProfileType,

            [Parameter(ParameterSetName = 'WithContent')]
            [string] $Content,

            [Parameter(ParameterSetName = 'WithContent')]
            [bool] $Exist
        )

        # Create the PSCustomObject with properties
        $resource = [PSCustomObject]@{
            profileType = $ProfileType
            content     = $null
            profilePath = GetProfilePath -profileType $ProfileType
            _exist      = $false
        }

        # Add ToJson method
        $resource | Add-Member -MemberType ScriptMethod -Name 'ToJson' -Value {
            return ([ordered] @{
                    profileType = $this.profileType
                    content     = $this.content
                    profilePath = $this.profilePath
                    _exist      = $this._exist
                }) | ConvertTo-Json -Compress -EnumsAsStrings
        }

        # Constructor logic - if Content and Exist parameters are provided (WithContent parameter set)
        if ($PSCmdlet.ParameterSetName -eq 'WithContent') {
            $resource.content = $Content
            $resource._exist = $Exist
        } else {
            # Default constructor logic - read from file system
            $fileExists = Test-Path $resource.profilePath
            if ($fileExists) {
                $resource.content = Get-Content -Path $resource.profilePath
            } else {
                $resource.content = $null
            }
            $resource._exist = $fileExists
        }

        return $resource
    }

    function GetProfilePath {
        param (
            [ProfileType] $profileType
        )

        $path = switch ($profileType) {
            'AllUsersCurrentHost' { $PROFILE.AllUsersCurrentHost }
            'AllUsersAllHosts' { $PROFILE.AllUsersAllHosts }
            'CurrentUserAllHosts' { $PROFILE.CurrentUserAllHosts }
            'CurrentUserCurrentHost' { $PROFILE.CurrentUserCurrentHost }
        }

        return $path
    }

    function ExportOperation {
        $allUserCurrentHost = New-PwshResource -ProfileType 'AllUsersCurrentHost'
        $allUsersAllHost = New-PwshResource -ProfileType 'AllUsersAllHosts'
        $currentUserAllHost = New-PwshResource -ProfileType 'CurrentUserAllHosts'
        $currentUserCurrentHost = New-PwshResource -ProfileType 'CurrentUserCurrentHost'

        # Cannot use the ToJson() method here as we are adding a note property
        $allUserCurrentHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'AllUsersCurrentHost' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
        $allUsersAllHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'AllUsersAllHosts' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
        $currentUserAllHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'CurrentUserAllHosts' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
        $currentUserCurrentHost | Add-Member -NotePropertyName '_name' -NotePropertyValue 'CurrentUserCurrentHost' -PassThru | ConvertTo-Json -Compress -EnumsAsStrings
    }

    function GetOperation {
        param (
            [Parameter(Mandatory = $true)]
            $InputResource,
            [Parameter()]
            [switch] $AsJson
        )

        $profilePath = GetProfilePath -profileType $InputResource.profileType.ToString()

        $actualState = New-PwshResource -ProfileType $InputResource.profileType

        $actualState.profilePath = $profilePath

        $exists = Test-Path $profilePath

        if ($InputResource._exist -and $exists) {
            $content = Get-Content -Path $profilePath
            $actualState.Content = $content
        } elseif ($InputResource._exist -and -not $exists) {
            $actualState.Content = $null
            $actualState._exist = $false
        } elseif (-not $InputResource._exist -and $exists) {
            $actualState.Content = Get-Content -Path $profilePath
            $actualState._exist = $true
        } else {
            $actualState.Content = $null
            $actualState._exist = $false
        }

        if ($AsJson) {
            return $actualState.ToJson()
        } else {
            return $actualState
        }
    }

    function SetOperation {
        param (
            $InputResource
        )

        $actualState = GetOperation -InputResource $InputResource

        if ($InputResource._exist) {
            if (-not $actualState._exist) {
                $null = New-Item -Path $actualState.profilePath -ItemType File -Force
            }

            if ($null -ne $InputResource.content) {
                Set-Content -Path $actualState.profilePath -Value $InputResource.content
            }
        } elseif ($actualState._exist) {
            Remove-Item -Path $actualState.profilePath -Force
        }
    }
}
End {
    $inputJson = $input | ConvertFrom-Json

    if ($inputJson) {
        $InputResource = New-PwshResource -ProfileType $inputJson.profileType -Content $inputJson.content -Exist $inputJson._exist
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
                Write-Error "Input not supported for export operation"
                exit 2
            }

            ExportOperation
        }
    }

    exit 0
}
