class CommitNode {
    [string] $Hash
    [string[]] $Parents
    [string] $AuthorName
    [string] $AuthorGitHubLogin
    [string] $AuthorEmail
    [string] $Subject
    [string] $Body
    [string] $PullRequest
    [string] $ChangeLogMessage
    [bool] $IsBreakingChange

    CommitNode($hash, $parents, $name, $email, $subject, $body) {
        $this.Hash = $hash
        $this.Parents = $parents
        $this.AuthorName = $name
        $this.AuthorEmail = $email
        $this.Subject = $subject
        $this.Body = $body
        $this.IsBreakingChange = $body -match "\[breaking change\]"

        if ($subject -match "\(#(\d+)\)") {
            $this.PullRequest = $Matches[1]
        } else {
            throw "PR number is missing from the commit subject. (commit: $hash)"
        }
    }
}

##############################
#.SYNOPSIS
#In the release workflow, the release branch will be merged back to master after the release is done,
#and a merge commit will be greated as the child of the release tag commit.
#This cmdlet takes a release tag or the corresponding commit hash, find its child merge commit, and
#return its metadata in this format: <merge-commit-hash>|<parent-commit-hashes>
#
#.PARAMETER LastReleaseTag
#The last release tag
#
#.PARAMETER CommitHash
#The commit hash of the last release tag
#
#.OUTPUTS
#Return the metadata of the child merge commit, in this format: <merge-commit-hash>|<parent-commit-hashes>
##############################
function Get-ChildMergeCommit
{
    [CmdletBinding(DefaultParameterSetName="TagName")]
    param(
        [Parameter(Mandatory, ParameterSetName="TagName")]
        [string]$LastReleaseTag,

        [Parameter(Mandatory, ParameterSetName="CommitHash")]
        [string]$CommitHash
    )

    $tag_hash = $CommitHash
    if ($PSCmdlet.ParameterSetName -eq "TagName") { git rev-parse "$LastReleaseTag^0" }

    ## Get the merge commits that are reachable from 'HEAD' but not from the release tag
    $merge_commits_not_in_release_branch = git --no-pager log --merges "$tag_hash..HEAD" --format='%H|%P'
    ## Find the child merge commit, whose parent-commit-hashes contains the release tag hash
    $child_merge_commit = $merge_commits_not_in_release_branch | Select-String -SimpleMatch $tag_hash
    return $child_merge_commit.Line
}

##############################
#.SYNOPSIS
#Create a CommitNode instance to represent a commit.
#
#.PARAMETER CommitMetadata
#The commit metadata. It's in this format:
#<commit-hash>|<parent-hashes>|<author-name>|<author-email>|<commit-subject>
#
#.PARAMETER CommitMetadata
#The commit metadata, in this format:
#<commit-hash>|<parent-hashes>|<author-name>|<author-email>|<commit-subject>
#
#.OUTPUTS
#Return the 'CommitNode' object
##############################
function New-CommitNode
{
    param(
        [Parameter(ValueFromPipeline)]
        [ValidatePattern("^.+\|.+\|.+\|.+\|.+$")]
        [string]$CommitMetadata
    )

    Process {
        $hash, $parents, $name, $email, $subject = $CommitMetadata.Split("|")
        $body = (git --no-pager show $hash -s --format=%b) -join "`n"
        return [CommitNode]::new($hash, $parents, $name, $email, $subject, $body)
    }
}

##############################
#.SYNOPSIS
#Generate the draft change log.
#
#.PARAMETER LastReleaseTag
#The last release tag
#
#.PARAMETER HasCherryPick
#Indicate whether there are any commits in the last release branch that were cherry-picked from the master branch
#
#.PARAMETER Token
#The authentication token to use for retrieving the GitHub user log-in names for external contributors
#
#.OUTPUTS
#The generated change log draft.
##############################
function Get-ChangeLog
{
    param(
        [Parameter(Mandatory)]
        [string]$LastReleaseTag,

        [Parameter(Mandatory)]
        [string]$Token,

        [Parameter()]
        [switch]$HasCherryPick
    )

    $tag_hash = git rev-parse "$LastReleaseTag^0"
    $format = '%H|%P|%aN|%aE|%s'
    $header = @{"Authorization"="token $Token"}

    if ($HasCherryPick) {
        $child_merge_commit = Get-ChildMergeCommit -CommitHash $tag_hash
        $commit_hash, $parent_hashes = $child_merge_commit.Split("|")
        $other_parent_hash = ($parent_hashes -replace $tag_hash).Trim()

        $new_commits_from_other_parent = git --no-pager log --no-merges --cherry-pick --right-only "$tag_hash...$other_parent_hash" --format=$format | New-CommitNode
        $new_commits_from_last_release = git --no-pager log --no-merges --cherry-pick  --left-only "$tag_hash...$other_parent_hash" --format=$format | New-CommitNode
        $duplicate_commits = Compare-Object $new_commits_from_last_release $new_commits_from_other_parent -Property PullRequest -ExcludeDifferent -IncludeEqual -PassThru
        if ($duplicate_commits) {
            $duplicate_pr_numbers = @($duplicate_commits | ForEach-Object -MemberName PullRequest)
            $new_commits_from_other_parent = $new_commits_from_other_parent | Where-Object PullRequest -NotIn $duplicate_pr_numbers
        }

        $new_commits_after_merge_commit = git --no-pager log --no-merges "$commit_hash..HEAD" --format=$format | New-CommitNode
        $new_commits = $new_commits_after_merge_commit + $new_commits_from_other_parent
    } else {
        $new_commits = git --no-pager log --no-merges "$tag_hash..HEAD" --format=$format | New-CommitNode
    }

    foreach ($commit in $new_commits) {
        if ($commit.AuthorEmail.EndsWith("@microsoft.com")) {
            $commit.ChangeLogMessage = $commit.Subject
        } else {
            $uri = "https://api.github.com/repos/PowerShell/PowerShell/commits/$($commit.Hash)"
            $response = Invoke-WebRequest -Uri $uri -Method Get -Headers $header
            $content = ConvertFrom-Json -InputObject $response.Content
            $commit.AuthorGitHubLogin = $content.author.login
            $commit.ChangeLogMessage = "{0} (Thanks @$($commit.AuthorGitHubLogin)!)" -f $commit.Subject
        }

        if ($commit.IsBreakingChange) {
            $commit.ChangeLogMessage = "[Breaking Change] {0}" -f $commit.ChangeLogMessage
        }
    }

    $new_commits | Sort-Object -Descending -Property IsBreakingChange | ForEach-Object -MemberName ChangeLogMessage
}
