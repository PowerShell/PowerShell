param(
    [Parameter(HelpMessage='ReleaseTag from the job.  Set to "fromBranch" or $null to update using the branch name')]
    [string]$ReleaseTag,

    [Parameter(HelpMessage='The branch name used to update the release tag.')]
    [string]$Branch=$env:BUILD_SOURCEBRANCH,

    [Parameter(HelpMessage='The variable name to put the new release tagin.')]
    [string]$Variable='ReleaseTag'
)

# Script to set the release tag based on the branch name if it is not set or it is "fromBranch"
# the branch name is expected to be release-<semver>
# VSTS passes it as 'refs/heads/release-v6.0.2'

if($ReleaseTag -eq 'fromBranch' -or !$ReleaseTag)
{
   $releaseTag = $Branch -replace '^.*(release-)'
   $vstsCommandString = "vso[task.setvariable variable=$Variable]$releaseTag"
   Write-Verbose -Message "setting $Variable to $releaseTag" -Verbose
   Write-Host -Object "##$vstsCommandString"
}
