#requires -Version 6.0
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

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
    [string] $ThankYouMessage
    [bool] $IsBreakingChange

    CommitNode($hash, $parents, $name, $email, $subject, $body) {
        $this.Hash = $hash
        $this.Parents = $parents
        $this.AuthorName = $name
        $this.AuthorEmail = $email
        $this.Subject = $subject
        $this.Body = $body
        $this.IsBreakingChange = $body -match "\[breaking change\]"

        if ($subject -match "\(#(\d+)\)$") {
            $this.PullRequest = $Matches[1]
        }
    }
}

# These powershell team members don't use 'microsoft.com' for Github email or choose to not show their emails.
# We have their names in this array so that we don't need to query GitHub to find out if they are powershell team members.
$Script:powershell_team = @(
    "Travis Plunk"
    "dependabot-preview[bot]"
    "dependabot[bot]"
    "github-actions[bot]"
    "Anam Navied"
    "Andrew Schwartzmeyer"
    "Jason Helmick"
    "Patrick Meinecke"
    "Steven Bucher"
    "PowerShell Team Bot"
)

# They are very active contributors, so we keep their email-login mappings here to save a few queries to Github.
$Script:community_login_map = @{
    "darpa@yandex.ru" = "iSazonov"
    "c.bergmeister@gmail.com" = "bergmeister"
    "github@markekraus.com" = "markekraus"
    "info@powercode-consulting.se" = "powercode"
}

# Ignore dependency bumping bot (Dependabot):
$Script:attribution_ignore_list = @(
    'dependabot[bot]@users.noreply.github.com'
)

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
    if ($PSCmdlet.ParameterSetName -eq "TagName") { $tag_hash = git rev-parse "$LastReleaseTag^0" }

    ## Get the merge commits that are reachable from 'HEAD' but not from the release tag
    $merge_commits_not_in_release_branch = git --no-pager log --merges "$tag_hash..HEAD" --format='%H||%P'
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
        $hash, $parents, $name, $email, $subject = $CommitMetadata.Split("||")
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
        [Parameter(Mandatory = $true)]
        [string]$LastReleaseTag,

        [Parameter(Mandatory = $true)]
        [string]$ThisReleaseTag,

        [Parameter(Mandatory)]
        [string]$Token,

        [Parameter()]
        [switch]$HasCherryPick
    )

    $tag_hash = git rev-parse "$LastReleaseTag^0"
    $format = '%H||%P||%aN||%aE||%s'
    $header = @{"Authorization"="token $Token"}

    # Find the merge commit that merged the release branch to master.
    $child_merge_commit = Get-ChildMergeCommit -CommitHash $tag_hash
    if($child_merge_commit)
    {
        $commit_hash, $parent_hashes = $child_merge_commit.Split("||")
    }
    # Find the other parent of the merge commit, which represents the original head of master right before merging.
    $other_parent_hash = ($parent_hashes -replace $tag_hash).Trim()

    if ($HasCherryPick) {
        ## Sometimes we need to cherry-pick some commits from the master branch to the release branch during the release,
        ## and eventually merge the release branch back to the master branch. This will result in different commit nodes
        ## in master branch that actually represent same set of changes.
        ##
        ## In this case, we cannot simply use the revision range "$tag_hash..HEAD" because it will include the original
        ## commits in the master branch that were cherry-picked to the release branch -- they are reachable from 'HEAD'
        ## but not reachable from the last release tag. Instead, we need to exclude the commits that were cherry-picked,
        ## and only include the commits that are not in the last release into the change log.

        # Find the commits that were only in the original master, excluding those that were cherry-picked to release branch.
        $new_commits_from_other_parent = git --no-pager log --first-parent --cherry-pick --right-only "$tag_hash...$other_parent_hash" --format=$format | New-CommitNode
        # Find the commits that were only in the release branch, excluding those that were cherry-picked from master branch.
        $new_commits_from_last_release = git --no-pager log --first-parent --cherry-pick --left-only "$tag_hash...$other_parent_hash" --format=$format | New-CommitNode
        # Find the commits that are actually duplicate but having different patch-ids due to resolving conflicts during the cherry-pick.
        $duplicate_commits = $null
        if($new_commits_from_last_release -and $new_commits_from_other_parent)
        {
            $duplicate_commits = Compare-Object $new_commits_from_last_release $new_commits_from_other_parent -Property PullRequest -ExcludeDifferent -IncludeEqual -PassThru
        }
        if ($duplicate_commits) {
            $duplicate_pr_numbers = @($duplicate_commits | ForEach-Object -MemberName PullRequest)
            $new_commits_from_other_parent = $new_commits_from_other_parent | Where-Object PullRequest -NotIn $duplicate_pr_numbers
        }

        # Find the commits that were made after the merge commit.
        $new_commits_after_merge_commit = @(git --no-pager log --first-parent "$commit_hash..HEAD" --format=$format | New-CommitNode)
        $new_commits = $new_commits_after_merge_commit + $new_commits_from_other_parent
    } else {
        ## No cherry-pick was involved in the last release branch.
        ## Using a ref rang like "$tag_hash..HEAD" with 'git log' means getting the commits that are reachable from 'HEAD' but not reachable from the last release tag.

        ## We use '--first-parent' for 'git log'. It means for any merge node, only follow the parent node on the master branch side.
        ## In case we merge a branch to master for a PR, only the merge node will show up in this way, the individual commits from that branch will be ignored.
        ## This is what we want because the merge commit itself already represents the PR.

        ## First, we want to get all new commits merged during the last release
        $new_commits_during_last_release = @(git --no-pager log --first-parent "$tag_hash..$other_parent_hash" --format=$format | New-CommitNode)
        ## Then, we want to get all new commits merged after the last release
        $new_commits_after_last_release  = @(git --no-pager log --first-parent "$commit_hash..HEAD" --format=$format | New-CommitNode)
        ## Last, we get the full list of new commits
        $new_commits = $new_commits_during_last_release + $new_commits_after_last_release
    }

    # Array of unlabled PRs.
    $unlabeledPRs = @()

    # Array of PRs with multiple labels. The label "CL-BreakingChange" is allowed with some other "CL-*" label.
    $multipleLabelsPRs = @()

    # Array of PRs tagged with 'CL-BreakingChange' label.
    $clBreakingChange = @()

    # Array of PRs tagged with 'CL-BuildPackaging' label.
    $clBuildPackage = @()

    # Array of PRs tagged with 'CL-CodeCleanup' label.
    $clCodeCleanup = @()

    # Array of PRs tagged with 'CL-Docs' label.
    $clDocs = @()

    # Array of PRs tagged with 'CL-Engine' label.
    $clEngine = @()

    # Array of PRs with general cmdlet changes.
    $clGeneral = @()

    # Array of PRs tagged with 'CL-Performance' label.
    $clPerformance = @()

    # Array of PRs tagged with 'CL-Test' label.
    $clTest = @()

    # Array of PRs tagged with 'CL-Tools' label.
    $clTools = @()

    # Array of PRs tagged with 'CL-Untagged' label.
    $clUntagged = @()

    # Array of PRs tagged with 'CL-Experimental' label.
    $clExperimental = @()

    foreach ($commit in $new_commits) {
        Write-Verbose "authorname: $($commit.AuthorName)"
        if ($commit.AuthorEmail.EndsWith("@microsoft.com") -or $powershell_team -contains $commit.AuthorName -or $Script:attribution_ignore_list -contains $commit.AuthorEmail) {
            $commit.ChangeLogMessage = "- {0}" -f (Get-ChangeLogMessage $commit.Subject)
        } else {
            if ($community_login_map.ContainsKey($commit.AuthorEmail)) {
                $commit.AuthorGitHubLogin = $community_login_map[$commit.AuthorEmail]
            } else {
                $uri = "https://api.github.com/repos/PowerShell/PowerShell/commits/$($commit.Hash)"
                try{
                    $response = Invoke-WebRequest -Uri $uri -Method Get -Headers $header -ErrorAction Ignore
                } catch{}
                if($response)
                {
                    $content = ConvertFrom-Json -InputObject $response.Content
                    $commit.AuthorGitHubLogin = $content.author.login
                    $community_login_map[$commit.AuthorEmail] = $commit.AuthorGitHubLogin
                }
            }
            $commit.ChangeLogMessage = ("- {0} (Thanks @{1}!)" -f (Get-ChangeLogMessage $commit.Subject), $commit.AuthorGitHubLogin)
            $commit.ThankYouMessage = ("@{0}" -f ($commit.AuthorGitHubLogin))
        }

        if ($commit.IsBreakingChange) {
            $commit.ChangeLogMessage = "{0} [Breaking Change]" -f $commit.ChangeLogMessage
        }

        ## Get the labels for the PR
        try {
            $pr = Invoke-RestMethod -Uri "https://api.github.com/repos/PowerShell/PowerShell/pulls/$($commit.PullRequest)" -Headers $header -ErrorAction SilentlyContinue
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq '404') {
                $pr = $null
                #continue
            }
        }

        if($pr)
        {
            $clLabel = $pr.labels | Where-Object { $_.Name -match "^CL-"}
        }
        else {
            Write-Warning -Message "Tagging $($commit.Hash) by $($commit.AuthorName), as CL-BuildPackaging as it does not have a PR."
            $clLabel = [PSCustomObject]@{Name ='CL-BuildPackaging'}
        }

        if ($clLabel.count -gt 1 -and $clLabel.Name -notcontains 'CL-BreakingChange') {
            $multipleLabelsPRs += $pr
        }
        elseif ($clLabel.count -eq 0) {
            $unlabeledPRs += $pr
        }
        else {
            switch ($clLabel.Name) {
                "CL-BreakingChange" { $clBreakingChange += $commit }
                "CL-BuildPackaging" { $clBuildPackage += $commit }
                "CL-CodeCleanup" { $clCodeCleanup += $commit }
                "CL-Docs" { $clDocs += $commit }
                "CL-Engine" { $clEngine += $commit }
                "CL-Experimental" { $clExperimental += $commit }
                "CL-General" { $clGeneral += $commit }
                "CL-Performance" { $clPerformance += $commit }
                "CL-Test" { $clTest += $commit }
                "CL-Tools" { $clTools += $commit }
                "CL-Untagged" { $clUntagged += $commit }
                "CL-NotInBuild" { continue }
                Default { throw "unknown tag '$cLabel' for PR: '$($commit.PullRequest)'" }
            }
        }
    }

    if ($multipleLabelsPRs.count -gt 0) {
        Write-Error "PRs should not be tagged with multiple CL labels. PRs with multiple labels: $($multipleLabelsPRs.number -join ' ')"
        $shouldThrow = $true
    }

    if ($unlabeledPRs.count -gt 0) {
        Write-Error "PRs should have at least one CL label. PRs missing labels: $($unlabeledPRs.number -join ' ')"
        $shouldThrow = $true
    }

    if ($shouldThrow) {
        throw "Some PRs are tagged multiple times or have no tags."
    }

    # Write output

    $version = $ThisReleaseTag.TrimStart('v')

    Write-Output "## [${version}] - $(Get-Date -Format yyyy-MM-dd)`n"

    PrintChangeLog -clSection $clUntagged -sectionTitle 'UNTAGGED - Please classify'
    PrintChangeLog -clSection $clBreakingChange -sectionTitle 'Breaking Changes'
    PrintChangeLog -clSection $clEngine -sectionTitle 'Engine Updates and Fixes'
    PrintChangeLog -clSection $clExperimental -sectionTitle 'Experimental Features'
    PrintChangeLog -clSection $clPerformance -sectionTitle 'Performance'
    PrintChangeLog -clSection $clGeneral -sectionTitle 'General Cmdlet Updates and Fixes'
    PrintChangeLog -clSection $clCodeCleanup -sectionTitle 'Code Cleanup' -Compress
    PrintChangeLog -clSection $clTools -sectionTitle 'Tools'
    PrintChangeLog -clSection $clTest -sectionTitle 'Tests'
    PrintChangeLog -clSection $clBuildPackage -sectionTitle 'Build and Packaging Improvements' -Compress
    PrintChangeLog -clSection $clDocs -sectionTitle 'Documentation and Help Content'

    Write-Output "[${version}]: https://github.com/PowerShell/PowerShell/compare/${LastReleaseTag}...${ThisReleaseTag}`n"
}

function PrintChangeLog($clSection, $sectionTitle, [switch] $Compress) {
    if ($clSection.Count -gt 0) {
        "### $sectionTitle`n"

        if ($Compress) {
            $items = $clSection.ChangeLogMessage -join "`n"
            $thankYou = "We thank the following contributors!`n`n"
            $thankYou += ($clSection.ThankYouMessage | Select-Object -Unique | Where-Object { if($_) { return $true} return $false}) -join ", "

            "<details>`n"
            "<summary>`n"
            $thankYou | ConvertFrom-Markdown | Select-Object -ExpandProperty Html
            "</summary>`n"
            $items | ConvertFrom-Markdown | Select-Object -ExpandProperty Html
            "</details>"
        }
        else {
            $clSection | ForEach-Object -MemberName ChangeLogMessage
        }
        ""
    }
}

function Get-ChangeLogMessage
{
    param($OriginalMessage)

    switch -regEx ($OriginalMessage)
    {
        '^Merged PR (\d*): ' {
            return $OriginalMessage.replace($Matches.0,'') + " (Internal $($Matches.1))"
        }
        '^Build\(deps\): ' {
            return $OriginalMessage.replace($Matches.0,'')
        }
        default {
            return $OriginalMessage
        }
    }
}

##############################
#.SYNOPSIS
#Gets packages which have newer packages in nuget.org
#
#.PARAMETER Path
#The path to check for csproj files with packagse
#
#.PARAMETER IncludeAll
#Include packages that don't need to be updated
#
#.OUTPUTS
#Objects which represet the csproj package ref, with the current and new version
##############################
function Get-NewOfficalPackage
{
    param(
        [String]
        $Path = (Join-Path -Path $PSScriptRoot -ChildPath '..\src'),
        [Switch]
        $IncludeAll
    )
    # Calculate the filter to find the CSProj files
    $filter = Join-Path -Path $Path -ChildPath '*.csproj'
    $csproj = Get-ChildItem $filter -Recurse -Exclude 'PSGalleryModules.csproj'

    $csproj | ForEach-Object{
        $file = $_

        # parse the csproj
        [xml] $csprojXml = (Get-Content -Raw -Path $_)

        # get the package references
        $packages=$csprojXml.Project.ItemGroup.PackageReference

        # check to see if there is a newer package for each refernce
        foreach ($package in $packages)
        {
            # Get the name of the package
            $name = $package.Include

            if ($name)
            {
                # Get the current package from nuget
                $versions = Find-Package -Name $name -Source https://nuget.org/api/v2/  -ErrorAction SilentlyContinue -AllVersions |
                    Add-Member -Type ScriptProperty -Name Published -Value { $this.Metadata['published']} -PassThru |
                        Where-Object { Test-IncludePackageVersion -NewVersion $_.Version -Version $package.version}

                $revsionRegEx = Get-MatchingMajorMinorRegEx -Version $package.version
                $newPackage = $versions |
                    Sort-Object -Descending |
                        Select-Object -First 1

                # Get the newest matching revision
                $newRevision = $versions |
                    Where-Object {$_.Version -match $revsionRegEx } |
                        Sort-Object -Descending |
                            Select-Object -First 1

                # If the current package has a different version from the version in the csproj, print the details
                if ($newRevision -and $newRevision.Version.ToString() -ne $package.version -or $newPackage -and $newPackage.Version.ToString() -ne $package.version -or $IncludeAll.IsPresent)
                {
                    if ($newRevision)
                    {
                        $newRevisionString = $newRevision.Version
                    }
                    else
                    {
                        # We don't have a new Revision, report the current version
                        $newRevisionString = $package.Version
                    }

                    if ($newPackage)
                    {
                        $newVersionString = $newPackage.Version
                    }
                    else
                    {
                        # We don't have a new Version, report the current version
                        $newVersionString = $package.Version
                    }

                    [pscustomobject]@{
                        Csproj = (Split-Path -Path $file -Leaf)
                        PackageName = $name
                        CsProjVersion = $Package.Version
                        NuGetRevision = $newRevisionString
                        NuGetVersion = $newVersionString
                    }
                }
            }
        }
    }
}

##############################
#.SYNOPSIS
# Returns True if NewVersion is newer than Version
# Pre release are ignored if the current version is not pre-release
# If the current version is pre-release, this function only determines if the version portion is NewReleaseTag
# The calling function is responsible for sorting prelease version by publish date (as find-package gives them to you)
# and returning the newest.
#
#.PARAMETER Version
# The current Version
#
#.PARAMETER NewVersion
# The potention replacement version
#
#.OUTPUTS
# True if NewVersion should be considere as a replacement
##############################
function Test-IncludePackageVersion
{
    param(
        [string]
        $NewVersion,
        [string]
        $Version
    )

    $simpleCompare = $Version -notlike '*-*'

    if($simpleCompare -and $NewVersion -like '*-*')
    {
        # We are using a stable and the new version is pre-release
        return $false
    }
    elseif($simpleCompare -and [Version]$NewVersion -ge [Version] $Version)
    {
        # Simple comparison says the new version is newer
        return $true
    }
    elseif($simpleCompare)
    {
        # Simple comparison was done, but it was not newer
        return $false
    }
    elseif($NewVersion -notlike '*-*')
    {
        # Our current version is a pre-release but the new is not
        # make sure the new version is newer than the version part of the current version
        $versionOnly = ($Version -Split '\-')[0]
        if([Version]$NewVersion -ge [Version] $versionOnly)
        {
            return $true
        }
        else
        {
            return $false
        }
    }
    else
    {
        # Not sure, include it
        return $true
    }
}

##############################
#.SYNOPSIS
# Get a RegEx based on a version that will match the major and minor
#
#.PARAMETER Version
# The version to match
#
##############################
function Get-MatchingMajorMinorRegEx
{
    param(
        [Parameter(Mandatory)]
        $Version
    )

    $parts = $Version -split '\.'

    $regEx = "^$($parts[0])\.$($parts[1])\..*"
    return $regEx
}

##############################
#.SYNOPSIS
# Update the version number in code
#
#.PARAMETER NewReleaseTag
# The new Release Tag
#
#.PARAMETER NextReleaseTag
# The next Release Tag
#
#.PARAMETER Path
# The path to the root of where you want to update
#
##############################
function Update-PsVersionInCode
{
    param(
        [Parameter(Mandatory)]
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
        [String]
        $NewReleaseTag,

        [Parameter(Mandatory)]
        [ValidatePattern("^v\d+\.\d+\.\d+(-\w+(\.\d{1,2})?)?$")]
        [String]
        $NextReleaseTag,

        [String]
        $Path = (Join-Path -Path $PSScriptRoot -ChildPath '..')
    )

    $metaDataPath = (Join-Path -Path $PSScriptRoot -ChildPath 'metadata.json')
    $metaData = Get-Content -Path $metaDataPath | ConvertFrom-Json
    $currentTag = $metaData.StableReleaseTag

    $currentVersion = $currentTag -replace '^v'
    $newVersion = $NewReleaseTag -replace '^v'
    $metaData.NextReleaseTag = $NextReleaseTag
    Set-Content -Path $metaDataPath -Encoding ascii -Force -Value ($metaData | ConvertTo-Json)

    Get-ChildItem -Path $Path -Recurse -File |
        Where-Object {$_.Extension -notin '.icns','.svg' -and $_.NAME -ne 'CHANGELOG.md' -and $_.DirectoryName -notmatch '[\\/]docs|demos[\\/]'} |
            Where-Object {$_ | Select-String -SimpleMatch $currentVersion -List} |
                ForEach-Object {
                    $content = Get-Content -Path $_.FullName -Raw -ReadCount 0
                    $newContent = $content.Replace($currentVersion,$newVersion)
                    Set-Content -Path $_.FullName -Encoding ascii -Force -Value $newContent -NoNewline
                }
}


##############################
#.SYNOPSIS
# Test if the GithubCli is in the path
##############################
function Test-GitHubCli {
    $gitHubCli = Get-Command -Name 'gh' -ErrorAction SilentlyContinue

    if ($gitHubCli) {
        return $true
    } else {
        return $false
    }
}

##############################
#.SYNOPSIS
# Test if the GithubCli is the required version
##############################
function Test-GitHubCliVersion {
    param(
        [Parameter(Mandatory)]
        [System.Management.Automation.SemanticVersion]
        $RequiredVersion
    )
    [System.Management.Automation.SemanticVersion] $version = gh --version | ForEach-Object {
        if ($_ -match ' (\d+\.\d+\.\d+) ') {
            $matches[1]
        }
    }

    if ($version -ge $RequiredVersion) {
        return $true
    } else {
        return $false
    }
}

##############################
#.SYNOPSIS
# Gets a report of Backport PRs
#
#.PARAMETER Triage state
# The triage states of the PR.  Consider, Approved or Done
#
#.PARAMETER Version
# The version of PowerShell the backport is targeting.  7.0, 7.2, 7.3, etc
#
#.PARAMETER Web
# A switch to open all the PRs in the browser
#
##############################
function Get-PRBackportReport {
    param(
        [ValidateSet('Consider', 'Approved', 'Done')]
        [String] $TriageState = 'Approved',
        [ValidatePattern('^\d+\.\d+$')]
        [string] $Version,
        [switch] $Web
    )

    if (!(Test-GitHubCli)) {
        throw "GitHub CLI is not installed. Please install it from https://cli.github.com/"
    }

    $requiredVersion = '2.17'
    if (!(Test-GitHubCliVersion -RequiredVersion $requiredVersion)) {
        throw "Please upgrade the GitHub CLI to version $requiredVersion. Please install it from https://cli.github.com/"
    }

    if (!(gh auth status 2>&1  | Select-String 'logged in')){
        throw "Please login to GitHub CLI using 'gh auth login'"
    }

    $prs = gh pr list --state merged --label "Backport-$Version.x-$TriageState" --json title,number,mergeCommit,mergedAt |
        ConvertFrom-Json |
        ForEach-Object {
            [PScustomObject]@{
                CommitId = $_.mergeCommit.oid
                Number   = $_.number
                Title    = $_.title
                MergedAt = $_.mergedAt
            }
        } | Sort-Object -Property MergedAt

    if ($Web) {
        $prs | ForEach-Object {
            gh pr view $_.Number --web
        }
    } else {
        $prs
    }
}
enum RemoteType {
    GitHub
    AzureRepo
}

function Get-UpstreamInfo {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$Upstream,

        [Parameter(Mandatory=$true)]
        [string]$UpstreamRemote
    )

    $upstreamName = '(powershell(core)?)(/_git)?/(powershell)'
    $pattern = "^$UpstreamRemote\s*(.*)\:(.*/([-\w.]+)/)?$upstreamName(\.git)?.*fetch"
    Write-Verbose -Verbose "searching for an upstream with regex: '$pattern'"
    $Upstream = $Upstream | Where-Object { $_ -match $pattern }

    Write-Verbose -Verbose "found $Upstream"

    if (!$Upstream) {
        throw "Please create an upstream remote that points to $upstreamName"
    }

    $matches | Format-Table | Out-String -Stream -Width 9999 | Write-Verbose
    $org = $matches[3]
    if ($org -ne 'github.com' -and $matches[1] -ne 'git@github.com') {
        Write-Verbose 'parsing Azure repo remote' -Verbose
        # Azure Repo remote
        $project = $matches[4]
        $repo = $matches[7]
        $upstreamHost = $matches[1]

        if ($upstreamHost -eq 'https') {
            $upstreamHost = $org
        }
        # matches everything but `.` ending in a `.`
        # in other word, matching the first part of a hostname.
        # like `www.microsoft.com` it would match `www.` with `www` in a capture group.
        if ($org -match '([^\..]*)\.') {
            $org = $Matches[1]
        }
    } else {
        Write-Verbose 'parsing github remote' -Verbose
        # GitHub Repo remote
        $org = $matches[4]
        $repo = $matches[7]
        $upstreamHost = 'github.com'
        $project = $upstreamHost
    }

    $remoteType = [RemoteType]::GitHub

    if ($upstreamHost -match '.*azure.com$' -or $upstreamHost -match '.*visualstudio.com$') {
        [RemoteType] $remoteType = [RemoteType]::AzureRepo
    }

    $upstreamMatchInfo = @{
        org     = $org
        project = $project
        repo    = $repo
        host    = $upstreamHost
        remoteType = $remoteType
    }

    return $upstreamMatchInfo
}

# Backports a PR
# requires:
#   * a remote called upstream pointing to powershell/powershell
#   * the github cli installed and authenticated
# Usage:
#     Invoke-PRBackport -PRNumber 1234 -Target release/v7.0.1
# To overwrite a local branch add -Overwrite
# To add an postfix to the branch name use -BranchPostFix <postfix>
function Invoke-PRBackport {
    [cmdletbinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory)]
        [string]
        $PrNumber,

        [Parameter(Mandatory)]
        [ValidateScript({$_ -match '^release/v\d+\.\d+\.\d+'})]
        [string]
        $Target,

        [switch]
        $Overwrite,

        [string]
        $BranchPostFix,

        [string]
        $UpstreamRemote = 'upstream'
    )
    function script:Invoke-NativeCommand {
        param(
            [scriptblock] $ScriptBlock
        )
        &$ScriptBlock
        if ($LASTEXITCODE -ne 0) {
            throw "$ScriptBlock fail with $LASTEXITCODE"
        }
    }
    function script:Test-ShouldContinue {
        param (
            $Message
        )
        $continue = $false
        while(!$continue) {
            $input= Read-Host -Prompt ($Message + "`nType 'Yes<enter>' to continue 'No<enter>' to exit")
            switch($input) {
                'yes' {
                    $continue= $true
                }
                'no' {
                    throw "User abort"
                }
            }
        }
    }
    $ErrorActionPreference = 'stop'

    $pr = gh pr view $PrNumber --json 'mergeCommit,state,title' | ConvertFrom-Json

    $commitId = $pr.mergeCommit.oid
    $state = $pr.state
    $originaltitle = $pr.title
    $backportTitle = "[$Target]$originalTitle"

    Write-Verbose -Verbose "commitId: $commitId; state: $state"
    Write-Verbose -Verbose "title:$backportTitle"

    if ($state -ne 'MERGED') {
        throw "PR is not merged ($state)"
    }

    $upstream = Invoke-NativeCommand { git remote -v }
    $upstreamMatchInfo = Get-UpstreamInfo -Upstream $upstream -UpstreamRemote $UpstreamRemote
    $remoteType = $upstreamMatchInfo.remoteType

    Write-Verbose -Verbose "remotetype: $remoteType"
    $upstreamMatchInfo | Format-Table | Out-String -Stream -Width 9999 | Write-Verbose -Verbose

    Invoke-NativeCommand { git fetch $UpstreamRemote $Target }

    $switch = '-c'
    if ($Overwrite) {
        $switch = '-C'
    }

    $branchName = "backport-$PrNumber"
    if ($BranchPostFix) {
        $branchName += "-$BranchPostFix"
    }

    if ($PSCmdlet.ShouldProcess("Create branch $branchName from $UpstreamRemote/$Target")) {
        Invoke-NativeCommand { git switch $UpstreamRemote/$Target $switch $branchName }
    }

    try {
        $revParseParams = @(
            '--verify'
            "$commitId^{commit}"
        )
        Invoke-NativeCommand { git rev-parse --quiet $revParseParams }
    }
    catch {
        throw "Commit does not exist.  Try fetching the upstream. (git rev-parse $revParseParams)"
    }


    try {
        Invoke-NativeCommand { git cherry-pick $commitId }
    }
    catch {
        Test-ShouldContinue -Message "Fix any conflicts with the cherry-pick."
    }

    if ($PSCmdlet.ShouldProcess("Create the PR")) {
        $body = "Backport #$PrNumber"
        switch($remoteType) {
            "AzureRepo" {
                Write-Verbose -Verbose "Pushing branch to $UpstreamRemote"
                git push --set-upstream $UpstreamRemote HEAD
                $parameters = @(
                    'repos'
                    'pr'
                    'create'
                )
                # Open in the browser
                $parameters += @(
                    '--open'
                )
                $parameters += @(
                    '--target-branch'
                    $Target
                )
                $parameters += @(
                    '--title'
                    $backportTitle
                )
                $parameters += @(
                    '--description'
                $body
                )
                $parameters += @(
                    '--squash'
                    'true'
                )
                $parameters += @(
                    '--auto-complete'
                    'true'
                )
                $parameters += @(
                    '--delete-source-branch'
                    'true'
                )
                $parameters += @(
                    '--org'
                    "https://dev.azure.com/$($upstreamMatchInfo.org)"
                )
                $parameters += @(
                    '--project'
                    $upstreamMatchInfo.project
                )
                $parameters += @(
                    '--source-branch'
                    $branchName
                )
                $parameters += @(
                    '--repository'
                    $upstreamMatchInfo.repo
                )

                Write-Verbose -Verbose "az $parameters"
                $null = Invoke-NativeCommand { az $parameters }
            }
            "GitHub" {
                Write-Verbose -Verbose "Creating PR using gh CLI"
                gh pr create --base $Target --title $backportTitle --body $body --web
            }
            default {
                throw "unknown remoteType: $remoteType"
            }
        }
    }
}

# Backport all approved backports
# Usage:
#      Invoke-PRBackportApproved -Version 7.2.12
function Invoke-PRBackportApproved {
    param(
        [Parameter(Mandatory)]
        [semver]
        $Version
    )

    $tagVersion = "$($Version.Major).$($Version.Minor)"
    $target = "release/$ReleaseTag"

    Get-PRBackportReport -Version $tagVersion |
        ForEach-Object {
            $prNumber = $_.Number
            Invoke-PRBackport -ErrorAction Stop -PrNumber $prNumber -Target $target
        }
}

Export-ModuleMember -Function Get-ChangeLog, Get-NewOfficalPackage, Update-PsVersionInCode, Get-PRBackportReport, Invoke-PRBackport, Invoke-PRBackportApproved, Get-UpstreamInfo
