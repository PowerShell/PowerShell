# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'
$repoRoot = Join-Path $PSScriptRoot '..'
$script:administratorsGroupSID = "S-1-5-32-544"
$script:usersGroupSID = "S-1-5-32-545"

$dotNetPath = "$env:USERPROFILE\Appdata\Local\Microsoft\dotnet"
if(Test-Path $dotNetPath)
{
    $env:PATH = $dotNetPath + ';' + $env:PATH
}

#import build into the global scope so it can be used by packaging
Import-Module (Join-Path $repoRoot 'build.psm1') -Scope Global
Import-Module (Join-Path $repoRoot 'tools\packaging')

function New-LocalUser
{
  <#
    .SYNOPSIS
        Creates a local user with the specified username and password
    .DESCRIPTION
    .EXAMPLE
    .PARAMETER
        username Username of the user which will be created
    .PARAMETER
        password Password of the user which will be created
    .OUTPUTS
    .NOTES
  #>
  param(
    [Parameter(Mandatory=$true)]
    [string] $username,

    [Parameter(Mandatory=$true)]
    [string] $password

  )

  $LocalComputer = [ADSI] "WinNT://$env:computername";
  $user = $LocalComputer.Create('user', $username);
  $user.SetPassword($password) | out-null;
  $user.SetInfo() | out-null;
}

<#
  Converts SID to NT Account Name
#>
function ConvertTo-NtAccount
{
  param(
    [Parameter(Mandatory=$true)]
    [string] $sid
  )
	(new-object System.Security.Principal.SecurityIdentifier($sid)).translate([System.Security.Principal.NTAccount]).Value
}

<#
  Add a user to a local security group
#>
function Add-UserToGroup
{
  param(
    [Parameter(Mandatory=$true)]
    [string] $username,

    [Parameter(Mandatory=$true, ParameterSetName = "SID")]
    [string] $groupSid,

    [Parameter(Mandatory=$true, ParameterSetName = "Name")]
    [string] $group
  )

  $userAD = [ADSI] "WinNT://$env:computername/${username},user"

  if($PsCmdlet.ParameterSetName -eq "SID")
  {
    $ntAccount=ConvertTo-NtAccount $groupSid
    $group =$ntAccount.Split("\\")[1]
  }

  $groupAD = [ADSI] "WinNT://$env:computername/${group},group"

  $groupAD.Add($userAD.AdsPath);
}

# tests if we should run a daily build
# returns true if the build is scheduled
# or is a pushed tag
Function Test-DailyBuild
{
    $trueString = 'True'
    # PS_DAILY_BUILD says that we have previously determined that this is a daily build
    # APPVEYOR_SCHEDULED_BUILD is True means that we are in an AppVeyor Scheduled build
    # APPVEYOR_REPO_TAG_NAME means we are building a tag in AppVeyor
    # BUILD_REASON is Schedule means we are in a VSTS Scheduled build
    if(($env:PS_DAILY_BUILD -eq $trueString) -or $env:BUILD_REASON -eq 'Schedule')
    {
        return $true
    }

    # if [Feature] is in the commit message,
    # Run Daily tests
    $commitMessage = Get-CommitMessage
    Write-Verbose "commitMessage: $commitMessage" -verbose

    if($commitMessage -match '\[feature\]' -or $env:FORCE_FEATURE -eq 'True')
    {
        Set-BuildVariable -Name PS_DAILY_BUILD -Value $trueString
        return $true
    }

    return $false
}

# Returns the commit message for the current build
function Get-CommitMessage
{
    if ($env:BUILD_SOURCEVERSIONMESSAGE -match 'Merge\s*([0-9A-F]*)')
    {
        # We are in VSTS and have a commit ID in the Source Version Message
        $commitId = $Matches[1]
        return &git log --format=%B -n 1 $commitId
    }
    else
    {
        Write-Log "Unknown BUILD_SOURCEVERSIONMESSAGE format '$env:BUILD_SOURCEVERSIONMESSAGE'" -Verbose
    }
}

# Sets a build variable
Function Set-BuildVariable
{
    param(
        [Parameter(Mandatory=$true)]
        [string]
        $Name,

        [Parameter(Mandatory=$true)]
        [string]
        $Value
    )

    if($env:TF_BUILD)
    {
        #In VSTS
        Write-Host "##vso[task.setvariable variable=$Name;]$Value"
        # The variable will not show up until the next task.
        # Setting in the current session for the same behavior as AppVeyor
        Set-Item env:/$name -Value $Value
    }
    else
    {
        Set-Item env:/$name -Value $Value
    }
}

# Emulates running all of AppVeyor but locally
function Invoke-AppVeyorFull
{
    param(
        [switch] $APPVEYOR_SCHEDULED_BUILD,
        [switch] $CleanRepo
    )
    if($CleanRepo)
    {
        Clear-PSRepo
    }

    Invoke-AppVeyorInstall
    Invoke-AppVeyorBuild
    Invoke-AppVeyorTest -ErrorAction Continue
    Invoke-AppveyorFinish
}

# Implements the AppVeyor 'build_script' step
function Invoke-AppVeyorBuild
{
    $releaseTag = Get-ReleaseTag
    # check to be sure our test tags are correct
    $result = Get-PesterTag
    if ( $result.Result -ne "Pass" ) {
        $result.Warnings
        throw "Tags must be CI, Feature, Scenario, or Slow"
    }

    if(Test-DailyBuild)
    {
        Start-PSBuild -Configuration 'CodeCoverage' -PSModuleRestore -CI -ReleaseTag $releaseTag
    }

    Start-PSBuild -CrossGen -PSModuleRestore -Configuration 'Release' -CI -ReleaseTag $releaseTag
}

# Implements the AppVeyor 'install' step
function Invoke-AppVeyorInstall
{
    # Make sure we have all the tags
    Sync-PSTags -AddRemoteIfMissing
    $releaseTag = Get-ReleaseTag

    if(Test-DailyBuild){
        if ($env:BUILD_REASON -eq 'Schedule') {
            Write-Host "##vso[build.updatebuildnumber]Daily-$env:BUILD_SOURCEBRANCHNAME-$env:BUILD_SOURCEVERSION-$((get-date).ToString("yyyyMMddhhss"))"
        }
    }

    if ($env:TF_BUILD)
    {
        #
        # Generate new credential for appveyor (only) remoting tests.
        #
        Write-Verbose "Creating account for remoting tests in AppVeyor."

        # Password
        $randomObj = [System.Random]::new()
        $password = ""
        1..(Get-Random -Minimum 15 -Maximum 126) | ForEach-Object { $password = $password + [char]$randomObj.next(45,126) }

        # Account
        $userName = 'ciRemote'
        New-LocalUser -username $userName -password $password
        Add-UserToGroup -username $userName -groupSid $script:administratorsGroupSID

        # Provide credentials globally for remote tests.
        $ss = ConvertTo-SecureString -String $password -AsPlainText -Force
        $appveyorRemoteCredential = [PSCredential]::new("$env:COMPUTERNAME\$userName", $ss)
	    $appveyorRemoteCredential | Export-Clixml -Path "$env:TEMP\AppVeyorRemoteCred.xml" -Force

        # Check that LocalAccountTokenFilterPolicy policy is set, since it is needed for remoting
        # using above local admin account.
        Write-Verbose "Checking for LocalAccountTokenFilterPolicy in AppVeyor."
        $haveLocalAccountTokenFilterPolicy = $false
        try
        {
            $haveLocalAccountTokenFilterPolicy = ((Get-ItemPropertyValue -Path HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System -Name LocalAccountTokenFilterPolicy) -eq 1)
        }
        catch { }
        if (!$haveLocalAccountTokenFilterPolicy)
        {
            Write-Verbose "Setting the LocalAccountTokenFilterPolicy for remoting tests"
            Set-ItemProperty -Path HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System -Name LocalAccountTokenFilterPolicy -Value 1
        }
    }

    Set-BuildVariable -Name TestPassed -Value False
    Start-PSBootstrap -Confirm:$false
}

# A wrapper to ensure that we upload test results
# and that if we are not able to that it does not fail
# the CI build
function Update-AppVeyorTestResults
{
    param(
        [string] $resultsFile
    )
    if(!$pushedResults)
    {
            Write-Warning "Failed to push all artifacts for $resultsFile"
    }
}

# Implement AppVeyor 'Test_script'
function Invoke-AppVeyorTest
{
    [CmdletBinding()]
    param(
        [ValidateSet('UnelevatedPesterTests', 'ElevatedPesterTests_xUnit_Packaging')]
        [string] $Purpose
    )
    #
    # CoreCLR

    $env:CoreOutput = Split-Path -Parent (Get-PSOutput -Options (Get-PSOptions))
    Write-Host -Foreground Green 'Run CoreCLR tests'
    $testResultsNonAdminFile = "$pwd\TestsResultsNonAdmin.xml"
    $testResultsAdminFile = "$pwd\TestsResultsAdmin.xml"
    $ParallelXUnitTestResultsFile = "$pwd\ParallelXUnitTestResults.xml"
    if(!(Test-Path "$env:CoreOutput\pwsh.exe"))
    {
        throw "CoreCLR pwsh.exe was not built"
    }

    # Pester doesn't allow Invoke-Pester -TagAll@('CI', 'RequireAdminOnWindows') currently
    # https://github.com/pester/Pester/issues/608
    # To work-around it, we exlude all categories, but 'CI' from the list
    if (Test-DailyBuild) {
        $ExcludeTag = @()
        Write-Host -Foreground Green 'Running all CoreCLR tests..'
    }
    else {
        $ExcludeTag = @('Slow', 'Feature', 'Scenario')
        Write-Host -Foreground Green 'Running "CI" CoreCLR tests..'
    }

    # Get the experimental feature names and the tests associated with them
    $ExperimentalFeatureTests = Get-ExperimentalFeatureTests

    if ($Purpose -eq 'UnelevatedPesterTests') {
        $arguments = @{
            Bindir = $env:CoreOutput
            OutputFile = $testResultsNonAdminFile
            Unelevate = $true
            Terse = $true
            Tag = @()
            ExcludeTag = $ExcludeTag + 'RequireAdminOnWindows'
        }
        Start-PSPester @arguments -Title 'Pester Unelevated'
        Write-Host -Foreground Green 'Upload CoreCLR Non-Admin test results'
        Update-AppVeyorTestResults -resultsFile $testResultsNonAdminFile
        # Fail the build, if tests failed
        Test-PSPesterResults -TestResultsFile $testResultsNonAdminFile

        # Run tests with specified experimental features enabled
        foreach ($entry in $ExperimentalFeatureTests.GetEnumerator()) {
            $featureName = $entry.Key
            $testFiles = $entry.Value

            $expFeatureTestResultFile = "$pwd\TestsResultsNonAdmin.$featureName.xml"
            $arguments['OutputFile'] = $expFeatureTestResultFile
            $arguments['ExperimentalFeatureName'] = $featureName
            if ($testFiles.Count -eq 0) {
                # If an empty array is specified for the feature name, we run all tests with the feature enabled.
                # This allows us to prevent regressions to a critical engine experimental feature.
                $arguments.Remove('Path')
            } else {
                # If a non-empty string or array is specified for the feature name, we only run those test files.
                $arguments['Path'] = $testFiles
            }
            Start-PSPester @arguments -Title "Pester Experimental Unelevated - $featureName"

            Write-Host -ForegroundColor Green "Upload CoreCLR Non-Admin test results for experimental feature '$featureName'"
            Update-AppVeyorTestResults -resultsFile $expFeatureTestResultFile
            # Fail the build, if tests failed
            Test-PSPesterResults -TestResultsFile $expFeatureTestResultFile
        }
    }

    if ($Purpose -eq 'ElevatedPesterTests_xUnit_Packaging') {
        $arguments = @{
            Terse = $true
            Bindir = $env:CoreOutput
            OutputFile = $testResultsAdminFile
            Tag = @('RequireAdminOnWindows')
            ExcludeTag = $ExcludeTag
        }
        Start-PSPester @arguments -Title 'Pester Elevated'
        Write-Host -Foreground Green 'Upload CoreCLR Admin test results'
        Update-AppVeyorTestResults -resultsFile $testResultsAdminFile

        Start-PSxUnit -ParallelTestResultsFile $ParallelXUnitTestResultsFile
        Write-Host -ForegroundColor Green 'Uploading PSxUnit test results'
        Update-AppVeyorTestResults -resultsFile $ParallelXUnitTestResultsFile

        # Fail the build, if tests failed
        Test-PSPesterResults -TestResultsFile $testResultsAdminFile
        Test-XUnitTestResults -TestResultsFile $ParallelXUnitTestResultsFile

        # Run tests with specified experimental features enabled
        foreach ($entry in $ExperimentalFeatureTests.GetEnumerator()) {
            $featureName = $entry.Key
            $testFiles = $entry.Value

            $expFeatureTestResultFile = "$pwd\TestsResultsAdmin.$featureName.xml"
            $arguments['OutputFile'] = $expFeatureTestResultFile
            $arguments['ExperimentalFeatureName'] = $featureName
            if ($testFiles.Count -eq 0) {
                # If an empty array is specified for the feature name, we run all tests with the feature enabled.
                # This allows us to prevent regressions to a critical engine experimental feature.
                $arguments.Remove('Path')
            } else {
                # If a non-empty string or array is specified for the feature name, we only run those test files.
                $arguments['Path'] = $testFiles
            }
            Start-PSPester @arguments -Title "Pester Experimental Elevated - $featureName"

            Write-Host -ForegroundColor Green "Upload CoreCLR Admin test results for experimental feature '$featureName'"
            Update-AppVeyorTestResults -resultsFile $expFeatureTestResultFile
            # Fail the build, if tests failed
            Test-PSPesterResults -TestResultsFile $expFeatureTestResultFile
        }
    }

    Set-BuildVariable -Name TestPassed -Value True
}

#Implement AppVeyor 'after_test' phase
function Invoke-AppVeyorAfterTest
{
    [CmdletBinding()]
    param()

    if (Test-DailyBuild)
    {
        ## Publish code coverage build, tests and OpenCover module to artifacts, so webhook has the information.
        ## Build webhook is called after 'after_test' phase, hence we need to do this here and not in AppveyorFinish.
        $codeCoverageOutput = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Configuration CodeCoverage))
        $codeCoverageArtifacts = Compress-CoverageArtifacts -CodeCoverageOutput $codeCoverageOutput

        Write-Host -ForegroundColor Green 'Upload CodeCoverage artifacts'
        $codeCoverageArtifacts | ForEach-Object {
            Push-Artifact -Path $_
        }

        New-TestPackage -Destination (Get-Location).Path
        $testPackageFullName = Join-Path $pwd 'TestPackage.zip'
        Write-Verbose "Created TestPackage.zip" -Verbose
        Write-Host -ForegroundColor Green 'Upload test package'
        Push-Artifact $testPackageFullName
    }
}

# Wrapper to push artifact
function Push-Artifact
{
    param(
        [Parameter(Mandatory)]
        [ValidateScript({Test-Path -Path $_})]
        $Path,
        [string]
        $Name
    )

    if(!$Name)
    {
        $artifactName = [system.io.path]::GetFileName($Path)
    }
    else
    {
        $artifactName = $Name
    }

    if ($env:TF_BUILD) {
        # In VSTS
        Write-Host "##vso[artifact.upload containerfolder=$artifactName;artifactname=$artifactName;]$Path"
    }
}

function Compress-CoverageArtifacts
{
    param([string] $CodeCoverageOutput)

    # Create archive for test content, OpenCover module and CodeCoverage build
    $artifacts = New-Object System.Collections.ArrayList

    $zipTestContentPath = Join-Path $pwd 'tests.zip'
    Compress-TestContent -Destination $zipTestContentPath
    $null = $artifacts.Add($zipTestContentPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath((Join-Path $PSScriptRoot '..\test\tools\OpenCover'))
    $zipOpenCoverPath = Join-Path $pwd 'OpenCover.zip'
    [System.IO.Compression.ZipFile]::CreateFromDirectory($resolvedPath, $zipOpenCoverPath)
    $null = $artifacts.Add($zipOpenCoverPath)

    $zipCodeCoveragePath = Join-Path $pwd "CodeCoverage.zip"
    Write-Verbose "Zipping ${CodeCoverageOutput} into $zipCodeCoveragePath" -verbose
    [System.IO.Compression.ZipFile]::CreateFromDirectory($CodeCoverageOutput, $zipCodeCoveragePath)
    $null = $artifacts.Add($zipCodeCoveragePath)

    return $artifacts
}

function Get-ReleaseTag
{
    $metaDataPath = Join-Path -Path $PSScriptRoot -ChildPath 'metadata.json'
    $metaData = Get-Content $metaDataPath | ConvertFrom-Json

    $releaseTag = $metadata.PreviewReleaseTag
    if($env:APPVEYOR_BUILD_NUMBER)
    {
        $releaseTag = $releaseTag.split('.')[0..2] -join '.'
        $releaseTag = $releaseTag + '.' + $env:APPVEYOR_BUILD_NUMBER
    }
    elseif($env:BUILD_BUILID)
    {
        #In VSTS
        $releaseTag = $releaseTag.split('.')[0..2] -join '.'
        $releaseTag = $releaseTag + '.' + $env:BUILD_BUILID
    }

    return $releaseTag
}

# Implements AppVeyor 'on_finish' step
function Invoke-AppveyorFinish
{
    param(
        [string] $NuGetKey
    )

    try {
        $releaseTag = Get-ReleaseTag

        $previewVersion = $releaseTag.Split('-')
        $previewPrefix = $previewVersion[0]
        $previewLabel = $previewVersion[1].replace('.','')

        if(Test-DailyBuild)
        {
            $previewLabel= "daily{0}" -f $previewLabel
        }

        $preReleaseVersion = "$previewPrefix-$previewLabel.$env:BUILD_BUILDID"

        # Build clean before backing to remove files from testing
        Start-PSBuild -CrossGen -PSModuleRestore -Configuration 'Release' -ReleaseTag $preReleaseVersion -Clean

        # Build packages
        $packages = Start-PSPackage -Type msi,nupkg,zip -ReleaseTag $preReleaseVersion -SkipReleaseChecks

        $artifacts = New-Object System.Collections.ArrayList
        foreach ($package in $packages) {
            if($package -is [string])
            {
                $null = $artifacts.Add($package)
            }
            elseif($package -is [pscustomobject] -and $package.msi)
            {
                $null = $artifacts.Add($package.msi)
                $null = $artifacts.Add($package.wixpdb)
            }
        }

        # the packaging tests find the MSI package using env:PSMsiX64Path
        $env:PSMsiX64Path = $artifacts | Where-Object { $_.EndsWith(".msi")}

        # Install the latest Pester and import it
        Install-Module Pester -Force -SkipPublisherCheck
        Import-Module Pester -Force

        # start the packaging tests and get the results
        $packagingTestResult = Invoke-Pester -Script (Join-Path $repoRoot '.\test\packaging\windows\') -PassThru

        # fail the CI job if the tests failed, or nothing passed
        if($packagingTestResult.FailedCount -ne 0 -or !$packagingTestResult.PassedCount)
        {
            throw "Packaging tests failed ($($packagingTestResult.FailedCount) failed/$($packagingTestResult.PassedCount) passed)"
        }

        # only publish assembly nuget packages if it is a daily build and tests passed
        if((Test-DailyBuild) -and $env:TestPassed -eq 'True')
        {
            Publish-NuGetFeed -OutputPath .\nuget-artifacts -ReleaseTag $preReleaseVersion
            $nugetArtifacts = Get-ChildItem .\nuget-artifacts -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }
            if($nugetArtifacts)
            {
                $artifacts.AddRange($nugetArtifacts)
            }
        }

        if (Test-DailyBuild)
        {
            # produce win-arm and win-arm64 packages if it is a daily build
            Start-PSBuild -Restore -Runtime win-arm -PSModuleRestore -Configuration 'Release' -ReleaseTag $releaseTag
            $arm32Package = Start-PSPackage -Type zip -WindowsRuntime win-arm -ReleaseTag $releaseTag -SkipReleaseChecks
            $artifacts.Add($arm32Package)

            Start-PSBuild -Restore -Runtime win-arm64 -PSModuleRestore -Configuration 'Release' -ReleaseTag $releaseTag
            $arm64Package = Start-PSPackage -Type zip -WindowsRuntime win-arm64 -ReleaseTag $releaseTag -SkipReleaseChecks
            $artifacts.Add($arm64Package)
        }

        $pushedAllArtifacts = $true
        $artifacts | ForEach-Object {
            Write-Host "Pushing $_ as Appveyor artifact"
            if(Test-Path $_)
            {
                Push-Artifact -Path $_
            }
            else
            {
                $pushedAllArtifacts = $false
                Write-Warning "Artifact $_ does not exist."
            }

            if($NuGetKey -and $env:NUGET_URL -and [system.io.path]::GetExtension($_) -ieq '.nupkg')
            {
                Write-Log "pushing $_ to $env:NUGET_URL"
                Start-NativeExecution -sb {dotnet nuget push $_ --api-key $NuGetKey --source "$env:NUGET_URL/api/v2/package"} -IgnoreExitcode
            }
        }
        if(!$pushedAllArtifacts)
        {
            throw "Some artifacts did not exist!"
        }
    }
    catch {
        Write-Host -Foreground Red $_
        Write-Host -Foreground Red $_.ScriptStackTrace
        throw $_
    }
}
