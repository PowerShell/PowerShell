# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Class CgData {
    [pscredential]
    $Pat
    [string]$Organization
    [string]$Project
}
function Get-CgData {
    if($Script:cgData){return $Script:cgData}
    throw "First call Set-CgCredentials"
}

function Get-Uri {
    param(
        [Parameter(Mandatory=$true)]
        [string]$PathPortion
    )
    $cgData = Get-CgData
    $baseUri = "https://governance.dev.azure.com/$($cgData.Organization)/$($cgData.Project)/_apis/componentgovernance/$PathPortion"
    Write-Verbose "uri: $baseUri" -Verbose
    return $baseUri
}

class CgRepository {
    #Json formatted summary information about this repository. Currently contains number of active nuget.config alerts.
    [Object] $additionalInformation

    #The associations for this governed repository. For example, all service tree related entries.
    [Object] $associations

    #Creator of the governed repository.
    [Object] $createdBy

    [string] $createdDate

    [int] $id

    [string] $modifiedBy

    [string] $modifiedDate

    [string] $name

    #The policies that are configured in the governed repository.
    [object[]] $policies

    [object] $projectReference

    [string] $repositoryMoniker

    [object] $repositoryOptions

    [object] $type

    [string] $url

    [object] $userRole
}

function Get-CgRepositories {
    $uri = Get-Uri -PathPortion "governedrepositories?api-version=6.1-preview.1"
    $cgData = Get-CgData
    [CgRepository[]] (Invoke-RestMethod -Uri $uri -Authentication Basic -Credential $cgData.Pat).value
}

Function Get-CgPsRepository {
    @(Get-CgRepositories | Where-Object {$_.name -eq 'PowerShell' -and $_.repositoryMoniker -notlike '*/*'})
}

function Get-CgSnapshotType {
    param(
        [CgRepository]
        $CgRepository
    )

    $id = $CgRepository.Id
    $uri = Get-Uri -PathPortion "GovernedRepositories/$id/snapshottypes?api-version=6.1-preview.1"
    $cgData = Get-CgData
    (Invoke-RestMethod -Authentication Basic -Credential $cgData.Pat -Uri $uri).Value
}

function Get-CgNotice {
    param(
        [CgRepository]
        $CgRepository,
        [int]
        $SnapshotTypeId
    )

    $id = $CgRepository.Id
    $uri = Get-Uri -PathPortion "GovernedRepositories/${id}/notice?snapshotTypeId=${SnapshotTypeId}&api-version=6.1-preview.1"
    Write-Verbose "uri: $uri" -Verbose
    $cgData = Get-CgData
    (Invoke-RestMethod -Authentication Basic -Credential $cgData.Pat -Uri $uri).content
}

function Set-CgCredentials {
    param(
        [Parameter(Mandatory=$true)]
        [securestring] $Pat,

        [Parameter(Mandatory=$true)]
        [string] $Organization,

        [Parameter(Mandatory=$true)]
        [string] $Project
    )

    $pscred = [PSCredential]::new("PAT",$Pat)
    $script:cgData = [CgData]@{
        "PAT" = $pscred
        "Organization" = $Organization
        "Project" = $Project
    }
}

Export-ModuleMember -Function @(
    'Get-CgRepositories'
    'Set-CgCredentials'
    'Get-CgRepository'
    'Get-CgPsRepository'
    'Get-CgSnapshotType'
    'Get-CgNotice'
)
