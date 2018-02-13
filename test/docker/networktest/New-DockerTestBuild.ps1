# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
param ( [switch]$Force, [switch]$UseExistingMsi )

$script:Constants =  @{
    AccountName   = 'PowerShell'
    UrlBase       = 'https://ci.appveyor.com'
    ApiUrl        = 'https://ci.appveyor.com/api'
    ProjectName   = 'powershell-f975h'
    TestImageName = "remotetestimage"
    MsiName       = "PSCore.msi"
    Token         = "" # in this particular use we don't need a token
}

function Get-AppVeyorBuildArtifact {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$build,
        [string]$downloadFolder = '.',
        [string]$artifactFilter = '*.msi',
        [string]$artifactLocalFileName = $Constants.MsiName
    )

    $headers = @{
      'Authorization' = "Bearer {0}" -f $Constants.Token
      'Content-type' = 'application/json'
    }

    # get project with last build details
    $URL = "{0}/projects/{1}/{2}/build/$build" -f $Constants.ApiUrl,$Constants.AccountName,$Constants.ProjectName,$build
    $project = Invoke-RestMethod -Method Get -Uri $URL -Headers $headers

    # we assume here that build has a single job
    # get this job id
    $jobId = $project.build.jobs[0].jobId

    # get job artifacts (just to see what we've got)
    $URL = "{0}/buildjobs/{1}/artifacts" -f $Constants.ApiUrl,$jobid
    $artifacts = Invoke-RestMethod -Method Get -Uri $URL -Headers $headers

    # here we just take the first artifact, but you could specify its file name
    # $artifactFileName = 'MyWebApp.zip'
    $artifact = $artifacts | Where-Object {$_.fileName -like $artifactFilter} | Select-Object -First 1
    $artifactFileName = $artifact.fileName

    # artifact will be downloaded as
    $localArtifactPath = "$downloadFolder\$artifactLocalFileName"

    # download artifact
    # -OutFile - is local file name where artifact will be downloaded into
    $URI = "{0}/buildjobs/{1}/artifacts/{2}" -f $Constants.ApiUrl,$jobId,$artifactFileName
    Invoke-RestMethod -Method Get -Uri $URI -OutFile $localArtifactPath -Headers $headers

    return $localArtifactPath
}

# Get a collection of appveyor objects representing the builds for the last day
function Get-AppVeyorBuilds
{
    [CmdletBinding()]
    param(
        [ValidateNotNullOrEmpty()]
        [string]$ExtensionBranch = 'master',
        [ValidateRange(1,7)][int]$Days = 7,
        [int]$Last = 1
    )

    [datetime]$start = ((get-date).AddDays(-${Days}))
    $results = Get-AppVeyorBuildRange -Start $start -LastBuildId $null -ExtensionBranch $ExtensionBranch
    $results
}

function Get-AppVeyorBuildRange
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true, Position=0)]
        [string]
        $ExtensionBranch = 'dev',

        [Parameter(Mandatory=$true, Position=1)]
        [datetime]
        $Start,

        [Parameter(Mandatory=$true, Position=2)]
        [AllowNull()]
        [Object]
        $LastBuildId,

        [Parameter(Mandatory=$false, Position=3)]
        [int]
        $Records = 20,

        [switch]
        $IncludeRunning
    )

    $result = @{
            builds = @()
            LastBuildId = ''
            FoundLast = $false
        }

    if($LastBuildId)
    {
        $startBuildIdString = "&startBuildId=$LastBuildId"
    }

    $URI = "{0}/projects/{1}/{2}/history?recordsNumber={3}{4}&branch{5}" -f $Constants.ApiUrl,$Constants.AccountName,
        $Constants.ProjectName,$Records,$startBuildIdString,$ExtensionBranch
    $project = Invoke-RestMethod -Method Get -Uri $URI

    foreach($build in $project.builds)
    {
        if($build.Status -ne 'running' -or $IncludeRunning)
        {
            $createdString = $build.created
            $created = [datetime]::Parse($createdString)

            if($created -gt $Start)
            {
                $version = $build.version
                $result.lastBuildId = $build.buildId
                $URI = "{0}/projects/{1}/{2}/build/{3}"  -f $Constants.ApiUrl, $Constants.AccountName,
                        $Constants.ProjectName,$version
                $buildProject = Invoke-RestMethod -Method Get -Uri $URI -Headers $headers -verbose:$false

                $result.builds += Convert-AppVeyorBuildJson -build $buildProject.Build
            }
            else
            {
                $result.foundLast = $true
            }
        }
    }

    return $result
}

# Convert AppVeyor Build Json into a more usable PSObject
function Convert-AppVeyorBuildJson
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true, Position=0)]
        [Object]
        $build
    )

    $Job = $build.jobs[0]
    $status = $build.status
    [datetime] $started = [datetime]::MinValue
    [datetime] $finished = [datetime]::MaxValue

    if($build.started)
    {
        [datetime] $started = [datetime]::Parse($build.started)
    }
    if($build.finished)
    {
        [datetime] $finished = [datetime]::Parse($build.finished)
    }

    $duration = $null

    if($status -ne 'running')
    {
        $duration = $finished.Subtract($started)
    }

    $version = $build.version

    $link = '<a href="{0}/project/{0}/{1}/build/{2}">Results</a>' -f $Constants.BaseUrl,$Constants.AccountName, $Constants.ProjectName, $version
    $tests = @()
    $tag = [string]::Empty

    if($build.message.StartsWith('[') -and $build.message.Contains(']'))
    {
        $tag = $build.message.Substring(1,$build.message.IndexOf(']')).Replace(']','')
        if($tag.Contains('('))
        {
            $tagParts = $tag.Split('(')
            $tag = $tagParts[0]
            for($i=1;$i -lt $tagParts.Count; $i++)
            {
                $tests += $tagParts[$i].Replace(')','')
            }
        }
        $tag = $tag
    }

    $testString = $tests -join ','

    $result = [PsCustomObject]@{
        version = $version
        Id = $build.BuildId
        author = $build.authorName
        branch = $Build.branch
        status = $status
        started = $started
        StartedDay = [datetime]$started.Date
        finished = $finished
        duration = $duration
        message = $build.message
        testsCount = $Job.testsCount
        passedTestsCount  = $Job.passedTestsCount
        failedTestsCount  = $Job.failedTestsCount
        link = $link
        JobId = $Job.JobId
        Tests = $testString
        Tag = $tag
    }

    $result.pstypenames.Clear()
    $result.pstypenames.Add('AppVeyorBuildSummary')
    $result
}

############
### MAIN ###
############

#### DOCKER OPS #####
# is docker installed?
$dockerExe = get-command docker -ea silentlycontinue
if ( $dockerExe.name -ne "docker.exe" ) {
    throw "Cannot find docker, is it installed?"
}
# Check to see if we already have an image, and if so
# delete it if -Force was used, otherwise throw and exit
$TestImage = docker images $Constants.TestImageName --format '{{.Repository}}'
if ( $TestImage -eq $Constants.TestImageName) 
{
    if ( $Force ) 
    {
        docker rmi $Constants.TestImageName
    }
    else
    {
        throw ("{0} already exists, use '-Force' to remove" -f $Constants.TestImageName)
    }
}
# check again - there could be some permission problems
$TestImage = docker images $Constants.TestImageName --format '{{.Repository}}'
if ( $TestImage -eq $Constants.TestImageName) 
{
    throw ("'{0}' still exists, giving up" -f $Constants.TestImageName)
}

#### MSI CHECKS ####
# check to see if the MSI is present
$MsiExists = test-path $Constants.MsiName
$msg = "{0} exists, use -Force to remove or -UseExistingMsi to use" -f $Constants.MsiName
if ( $MsiExists -and ! ($force -or $useExistingMsi)) 
{
    throw $msg
}

# remove the msi
if ( $MsiExists -and $Force -and ! $UseExistingMsi ) 
{
    Remove-Item -force $Constants.MsiName
    $MsiExists = $false
}

# a couple of checks before downloading or using the existing one
# if the msi exists and -UseExistingMsi is present, we'll use the
# one we found
if ( ! $MsiExists -and $UseExistingMsi )
{
    throw ("{0} does not exist" -f $Constants.MsiName)
}
elseif ( $MsiExists -and ! $UseExistingMsi ) 
{
    throw $msg
}
elseif ( ! ($MsiExists -and $UseExistingMsi) )  # download the msi
{
    $builds = Get-AppVeyorBuilds -ExtensionBranch master
    $build = $builds.builds | sort-object finished | select-object -last 1
    Get-AppVeyorBuildArtifact -build $build.version
}

# last check before bulding the image
if ( ! (test-path $Constants.MsiName) )
{
    throw ("{0} does not exist, giving up" -f $Constants.MsiName)
}

# collect the builds and select the last one
Docker build --tag $Constants.TestImageName .
