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
#and a merge commit will be created as the child of the release tag commit.
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
        ## Sometimes we need to cherry-pick some commits from the master branch to the release branch during the release,
        ## and eventually merge the release branch back to the master branch. This will result in different commit nodes
        ## in master branch that actually represent same set of changes.
        ##
        ## In this case, we cannot simply use the revision range "$tag_hash..HEAD" becuase it will include the original
        ## commits in the master branch that were cherry-picked to the release branch -- they are reachable from 'HEAD'
        ## but not reachable from the last release tag. Instead, we need to exclude the commits that were cherry-picked,
        ## and only include the commits that are not in the last release into the change log.

        # Find the merge commit that merged the release branch to master.
        $child_merge_commit = Get-ChildMergeCommit -CommitHash $tag_hash
        $commit_hash, $parent_hashes = $child_merge_commit.Split("|")
        # Find the other parent of the merge commit, which represents the original head of master right before merging.
        $other_parent_hash = ($parent_hashes -replace $tag_hash).Trim()

        # Find the commits that were only in the orginal master, excluding those that were cherry-picked to release branch.
        $new_commits_from_other_parent = git --no-pager log --no-merges --cherry-pick --right-only "$tag_hash...$other_parent_hash" --format=$format | New-CommitNode
        # Find the commits that were only in the release branch, excluding those that were cherry-picked from master branch.
        $new_commits_from_last_release = git --no-pager log --no-merges --cherry-pick  --left-only "$tag_hash...$other_parent_hash" --format=$format | New-CommitNode
        # Find the commits that are actually duplicate but having different patch-ids due to resolving conflicts during the cherry-pick.
        $duplicate_commits = Compare-Object $new_commits_from_last_release $new_commits_from_other_parent -Property PullRequest -ExcludeDifferent -IncludeEqual -PassThru
        if ($duplicate_commits) {
            $duplicate_pr_numbers = @($duplicate_commits | ForEach-Object -MemberName PullRequest)
            $new_commits_from_other_parent = $new_commits_from_other_parent | Where-Object PullRequest -NotIn $duplicate_pr_numbers
        }

        # Find the commits that were made after the merge commit.
        $new_commits_after_merge_commit = git --no-pager log --no-merges "$commit_hash..HEAD" --format=$format | New-CommitNode
        $new_commits = $new_commits_after_merge_commit + $new_commits_from_other_parent
    } else {
        ## No cherry-pick was involved in the last release branch.
        ## We can get all new commits using the revision range "$tag_hash..HEAD", meaning the commits that are
        ## reachable from 'HEAD' but not reachable from the last release tag.
        $new_commits = git --no-pager log --no-merges "$tag_hash..HEAD" --format=$format | New-CommitNode
    }

    $community_login_map = @{}
    foreach ($commit in $new_commits) {
        if ($commit.AuthorEmail.EndsWith("@microsoft.com")) {
            $commit.ChangeLogMessage = "- {0}" -f $commit.Subject
        } else {
            if ($community_login_map.ContainsKey($commit.AuthorEmail)) {
                $commit.AuthorGitHubLogin = $community_login_map[$commit.AuthorEmail]
            } else {
                $uri = "https://api.github.com/repos/PowerShell/PowerShell/commits/$($commit.Hash)"
                $response = Invoke-WebRequest -Uri $uri -Method Get -Headers $header
                $content = ConvertFrom-Json -InputObject $response.Content
                $commit.AuthorGitHubLogin = $content.author.login
                $community_login_map[$commit.AuthorEmail] = $commit.AuthorGitHubLogin
            }
            $commit.ChangeLogMessage = "- {0} (Thanks @{1}!)" -f $commit.Subject, $commit.AuthorGitHubLogin
        }

        if ($commit.IsBreakingChange) {
            $commit.ChangeLogMessage = "{0} [Breaking Change]" -f $commit.ChangeLogMessage
        }
    }

    $new_commits | Sort-Object -Descending -Property IsBreakingChange | ForEach-Object -MemberName ChangeLogMessage
}

##############################
#.SYNOPSIS
#Gets packages which have newer packages in nuget.org
#
#.PARAMETER Path
#The path to check for csproj files with packagse
#
#.OUTPUTS
#Objects which represet the csproj package ref, with the current and new version
##############################
function Get-NewPackage
{
    param(
        [String]
        $Path = (Join-path -Path $PSScriptRoot -ChildPath '..')
    )
    # Calculate the filter to find the CSProj files
    $filter = Join-Path -Path $Path -ChildPath '*.csproj'
    $csproj = Get-ChildItem $filter -Recurse

    $csproj | ForEach-Object{
        $file = $_

        # parse the csproj
        [xml] $csprojXml = (Get-content -Raw -Path $_)

        # get the package references
        $packages=$csprojXml.Project.ItemGroup.PackageReference

        # check to see if there is a newer package for each refernce
        foreach($package in $packages)
        {
            # Get the name of the package
            $name = $package.Include

            # don't pull 'Microsoft.Management.Infrastructure' from nuget
            if($name -and $name -ne 'Microsoft.Management.Infrastructure')
            {
                # Get the current package from nuget
                $newPackage = find-package -Name $name -Source https://nuget.org/api/v2/  -ErrorAction SilentlyContinue

                # If the current package has a different version from the version in the csproj, print the details
                if($newPackage -and $newPackage.Version.ToString() -ne $package.version)
                {
                    [pscustomobject]@{
                        Csproj = $file
                        PackageName = $name
                        CsProjVersion = $Package.Version
                        NuGetVersion = $newPackage.Version
                    }
                }
            }
        }
    }
}


Export-ModuleMember -Function Get-ChangeLog, Get-NewPackage
