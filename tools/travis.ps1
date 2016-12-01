Import-Module $PSScriptRoot/../build.psm1 -Force

# https://docs.travis-ci.com/user/environment-variables/
# TRAVIS_EVENT_TYPE: Indicates how the build was triggered.
# One of push, pull_request, api, cron.
$isPR = $env:TRAVIS_EVENT_TYPE -eq 'pull_request'
$isFullBuild = $env:TRAVIS_EVENT_TYPE -eq 'cron' -or $env:TRAVIS_EVENT_TYPE -eq 'api'

Write-Host -Foreground Green "Executing travis.ps1 `$isPR='$isPr' `$isFullBuild='$isFullBuild'"

Start-PSBootstrap -Package:(-not $isPr)
$output = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Publish))
Start-PSBuild -CrossGen -PSModuleRestore

$pesterParam = @{ 'binDir' = $output }

if ($isFullBuild) {
    $pesterParam['Tag'] = @('CI','Feature','Scenario')
    $pesterParam['ExcludeTag'] = @()
    # cron jobs create log files which include the stdout of Pester
    # which creates too much data for travis to put in the log file
    # and the job is then cancelled. Add Quiet to reduce the log size
    # the xunit log created by pester is what is important
    if ( $env:TRAVIS_EVENT_TYPE -eq 'cron' ) {
        $pesterParam['Quiet'] = $true
    }
} else {
    $pesterParam['Tag'] = @('CI')
    $pesterParam['ThrowOnFailure'] = $true
}

Start-PSPester @pesterParam
if (-not $isPr) {
    # Only build packages for branches, not pull requests
    Start-PSPackage
    Test-PSPesterResults
}

Start-PSxUnit
