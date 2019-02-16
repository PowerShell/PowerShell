param(
    [Parameter(HelpMessage='ReleaseTag from the job.  Set to "fromBranch" or $null to update using the branch name')]
    [string]$ReleaseTag,

    [Parameter(HelpMessage='The branch name used to update the release tag.')]
    [string]$Branch=$env:BUILD_SOURCEBRANCH,

    [Parameter(HelpMessage='The variable name to put the new release tagin.')]
    [string]$Variable='ReleaseTag'
)

# Script to set the release tag based on the branch name if it is not set or it is "fromBranch"
# the branch name is expected to be release-<semver> or <previewname>
# VSTS passes it as 'refs/heads/release-v6.0.2'

$branchOnly = $Branch -replace '^refs/heads/';
$branchOnly = $branchOnly -replace '[_\-]'

if($ReleaseTag -eq 'fromBranch' -or !$ReleaseTag)
{
    # Branch is named release-<semver>
    if($Branch -match '^.*(release[-/])')
    {
        Write-verbose "release branch:" -verbose
        $releaseTag = $Branch -replace '^.*(release[-/])'
        $vstsCommandString = "vso[task.setvariable variable=$Variable]$releaseTag"
        Write-Verbose -Message "setting $Variable to $releaseTag" -Verbose
        Write-Host -Object "##$vstsCommandString"
    }
    else
    {
        Write-verbose "non-release branch" -verbose
        # Branch is named <previewname>
        # Get version from metadata and append -<previewname>
        $metaDataJsonPath = Join-Path $PSScriptRoot -ChildPath '..\metadata.json'
        $metadata = Get-content $metaDataJsonPath | ConvertFrom-Json
        $versionPart = $metadata.PreviewReleaseTag
        if($versionPart -match '-.*$')
        {
            $versionPart = $versionPart -replace '-.*$'
        }

        $releaseTag = "$versionPart-$branchOnly"
        $vstsCommandString = "vso[task.setvariable variable=$Variable]$releaseTag"
        Write-Verbose -Message "setting $Variable to $releaseTag" -Verbose
        Write-Host -Object "##$vstsCommandString"
    }
}

Write-Output $releaseTag
