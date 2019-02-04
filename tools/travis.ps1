param(
    [ValidateSet('Bootstrap','Build','Failure','Success')]
    [String]$Stage = 'Build',
    [String]$NugetKey
)

$commitMessage = [string]::Empty

# Run a full build if the build was trigger via cron, api or the commit message contains `[Feature]`
# or the environment variable `FORCE_FEATURE` equals `True`
$hasFeatureTag = $commitMessage -match '\[feature\]' -or $env:FORCE_FEATURE -eq 'True'

# Run a packaging if the commit message contains `[Package]`
# or the environment variable `FORCE_PACKAGE` equals `True`
$hasPackageTag = $commitMessage -match '\[package\]' -or $env:FORCE_PACKAGE -eq 'True'
$createPackages = -not $isPr -or $hasPackageTag
$hasRunFailingTestTag = $commitMessage -match '\[includeFailingTest\]'
$isDailyBuild = $env:TRAVIS_EVENT_TYPE -eq 'cron' -or $env:TRAVIS_EVENT_TYPE -eq 'api' -or $env:BUILD_REASON -eq 'Schedule'
# only update the build badge for the cron job
$cronBuild = $env:TRAVIS_EVENT_TYPE -eq 'cron' -or $env:BUILD_REASON -eq 'Schedule'
$isFullBuild = $isDailyBuild -or $hasFeatureTag

if($Stage -eq 'Bootstrap')
{
    if($cronBuild -and $env:TF_BUILD)
    {
        Write-Host "##vso[build.updatebuildnumber]Daily-$env:BUILD_SOURCEBRANCHNAME-$env:BUILD_SOURCEVERSION-$((get-date).ToString("yyyyMMddhhmmss"))"
    }

    Write-Host -Foreground Green "Executing travis.ps1 -BootStrap `$isPR='$isPr' - $commitMessage"
    # Make sure we have all the tags
    Sync-PSTags -AddRemoteIfMissing
    Start-PSBootstrap -Package:$createPackages
}
elseif($Stage -eq 'Build')
{
    $releaseTag = Get-ReleaseTag

    Write-Host -Foreground Green "Executing travis.ps1 `$isPR='$isPr' `$isFullBuild='$isFullBuild' - $commitMessage"

    $originalProgressPreference = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try {
        ## We use CrossGen build to run tests only if it's the daily build.
        Start-PSBuild -CrossGen -PSModuleRestore -CI -ReleaseTag $releaseTag -Configuration 'Release'
    }
    finally{
        $ProgressPreference = $originalProgressPreference
    }

    $output = Split-Path -Parent (Get-PSOutput -Options (Get-PSOptions))

    $testResultsNoSudo = "$pwd/TestResultsNoSudo.xml"
    $testResultsSudo = "$pwd/TestResultsSudo.xml"

    $excludeTag = @('RequireSudoOnUnix')

    $noSudoPesterParam = @{
        'BinDir'     = $output
        'PassThru'   = $true
        'Terse'      = $true
        'Tag'        = @()
        'ExcludeTag' = $excludeTag
        'OutputFile' = $testResultsNoSudo
    }

    if ($isFullBuild) {
        $noSudoPesterParam['Tag'] = @('CI','Feature','Scenario')
    } else {
        $noSudoPesterParam['Tag'] = @('CI')
        $noSudoPesterParam['ThrowOnFailure'] = $true
    }

    if ($hasRunFailingTestTag) {
        $noSudoPesterParam['IncludeFailingTest'] = $true
    }

    # Get the experimental feature names and the tests associated with them
    $ExperimentalFeatureTests = Get-ExperimentalFeatureTests

    # Running tests which do not require sudo.
    $pesterPassThruNoSudoObject = Start-PSPester @noSudoPesterParam -Title 'Pester No Sudo'

    # Running tests that do not require sudo, with specified experimental features enabled
    $noSudoResultsWithExpFeatures = @()
    foreach ($entry in $ExperimentalFeatureTests.GetEnumerator()) {
        $featureName = $entry.Key
        $testFiles = $entry.Value

        $expFeatureTestResultFile = "$pwd\TestResultsNoSudo.$featureName.xml"
        $noSudoPesterParam['OutputFile'] = $expFeatureTestResultFile
        $noSudoPesterParam['ExperimentalFeatureName'] = $featureName
        if ($testFiles.Count -eq 0) {
            # If an empty array is specified for the feature name, we run all tests with the feature enabled.
            # This allows us to prevent regressions to a critical engine experimental feature.
            $noSudoPesterParam.Remove('Path')
        } else {
            # If a non-empty string or array is specified for the feature name, we only run those test files.
            $noSudoPesterParam['Path'] = $testFiles
        }
        $passThruResult = Start-PSPester @noSudoPesterParam -Title "Pester Experimental No Sudo - $featureName"
        $noSudoResultsWithExpFeatures += $passThruResult
    }

    # Running tests, which require sudo.
    $sudoPesterParam = $noSudoPesterParam.Clone()
    $sudoPesterParam.Remove('Path')
    $sudoPesterParam['Tag'] = @('RequireSudoOnUnix')
    $sudoPesterParam['ExcludeTag'] = @()
    $sudoPesterParam['Sudo'] = $true
    $sudoPesterParam['OutputFile'] = $testResultsSudo
    $pesterPassThruSudoObject = Start-PSPester @sudoPesterParam -Title 'Pester Sudo'

    # Running tests that require sudo, with specified experimental features enabled
    $sudoResultsWithExpFeatures = @()
    foreach ($entry in $ExperimentalFeatureTests.GetEnumerator()) {
        $featureName = $entry.Key
        $testFiles = $entry.Value

        $expFeatureTestResultFile = "$pwd\TestResultsSudo.$featureName.xml"
        $sudoPesterParam['OutputFile'] = $expFeatureTestResultFile
        $sudoPesterParam['ExperimentalFeatureName'] = $featureName
        if ($testFiles.Count -eq 0) {
            # If an empty array is specified for the feature name, we run all tests with the feature enabled.
            # This allows us to prevent regressions to a critical engine experimental feature.
            $sudoPesterParam.Remove('Path')
        } else {
            # If a non-empty string or array is specified for the feature name, we only run those test files.
            $sudoPesterParam['Path'] = $testFiles
        }
        $passThruResult = Start-PSPester @sudoPesterParam -Title "Pester Experimental Sudo - $featureName"
        $sudoResultsWithExpFeatures += $passThruResult
    }

    # Determine whether the build passed
    try {
        $allTestResultsWithNoExpFeature = @($pesterPassThruNoSudoObject, $pesterPassThruSudoObject)
        $allTestResultsWithExpFeatures = $noSudoResultsWithExpFeatures + $sudoResultsWithExpFeatures
        # this throws if there was an error
        $allTestResultsWithNoExpFeature | ForEach-Object { Test-PSPesterResults -ResultObject $_ }
        $allTestResultsWithExpFeatures  | ForEach-Object { Test-PSPesterResults -ResultObject $_ -CanHaveNoResult }
        $result = "PASS"
    }
    catch {
        $resultError = $_
        $result = "FAIL"
    }

    try {
        $ParallelXUnitTestResultsFile = "$pwd/ParallelXUnitTestResults.xml"

        Start-PSxUnit -ParallelTestResultsFile $ParallelXUnitTestResultsFile
        # If there are failures, Test-XUnitTestResults throws
        Test-XUnitTestResults -TestResultsFile $ParallelXUnitTestResultsFile
    }
    catch {
        $result = "FAIL"
        if (!$resultError)
        {
            $resultError = $_
        }
    }

    if ($createPackages) {

        $packageParams = @{}
        $packageParams += @{ReleaseTag=$releaseTag}

        # Only build packages for branches, not pull requests
        $packages = @(Start-PSPackage @packageParams -SkipReleaseChecks)
        foreach($package in $packages)
        {
            # Publish the packages to the nuget feed if:
            # 1 - It's a Daily build (already checked, for not a PR)
            # 2 - We have the info to publish (NUGET_KEY and NUGET_URL)
            # 3 - it's a nupkg file
            if($isDailyBuild -and $NugetKey -and $env:NUGET_URL -and [system.io.path]::GetExtension($package) -ieq '.nupkg')
            {
                Write-Log "pushing $package to $env:NUGET_URL"
                Start-NativeExecution -sb {dotnet nuget push $package --api-key $NugetKey --source "$env:NUGET_URL/api/v2/package"} -IgnoreExitcode
            }
        }
        if ($IsLinux)
        {
            # Create and package Raspbian .tgz
            Start-PSBuild -PSModuleRestore -Clean -Runtime linux-arm -Configuration 'Release'
            Start-PSPackage @packageParams -Type tar-arm -SkipReleaseChecks
        }

        if ($isDailyBuild)
        {
            New-TestPackage -Destination $pwd
        }
    }

    # if the tests did not pass, throw the reason why
    if ( $result -eq "FAIL" ) {
        Throw $resultError
    }
}
elseif($Stage -in 'Failure', 'Success')
{
    $result = 'PASS'
    if($Stage -eq 'Failure')
    {
        $result = 'FAIL'
    }

    if ($cronBuild) {
        # update the badge if you've done a cron build, these are not fatal issues
        try {
            $svgData = Get-DailyBadge -result $result
            if ( ! $svgData ) {
                write-warning "Could not retrieve $result badge"
            }
            else {
                Write-Log "Setting status badge to '$result'"
                Set-DailyBuildBadge -content $svgData
            }
        }
        catch {
            Write-Warning "Could not update status badge: $_"
        }

        try {
            Send-DailyWebHook -result $result
        }
        catch {
            Write-Warning "Could not send webhook: $_"
        }
    }
    else {
        Write-Log 'We only send bagde or webhook update for Cron builds'
    }

}
