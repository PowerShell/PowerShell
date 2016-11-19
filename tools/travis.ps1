Import-Module $PSScriptRoot/../build.psm1 -Force

$isPR = $env:TRAVIS_PULL_REQUEST -eq 'true'
$isCron = $env:TRAVIS_EVENT_TYPE -eq 'cron'

Start-PSBootstrap -Package:(-not $isPr)
$output = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Publish))
Start-PSBuild -CrossGen -PSModuleRestore

$pesterParam = @{ 'binDir' = $output }

if ($cron) {
    # daily builds
    $pesterParam['Tag'] = @('CI','Feature','Scenario')
    $pesterParam['ExcludeTag'] = @()
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
