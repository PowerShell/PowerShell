param(
    [Parameter(ParameterSetName='Build')]
    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+\.\d+)?$")]
    [string]$ReleaseTag,

    [ValidateSet('win7-x86','win7-x64','ubuntu.14.04','ubuntu.16.04','centos.7')]
    [String]
    $Name
)
$ErrorActionPreference = 'Stop'


$psReleaseBranch = 'master'
$psReleaseFork = 'PowerShell'
$location = Join-Path -Path $PSScriptRoot -ChildPath 'PSRelease'
if(Test-Path $location)
{
    Remove-Item -Path $location -Recurse -Force
}

$gitBinFullPath = (Get-Command -Name git).Source
if (-not $gitBinFullPath)
{
    throw "Git is required to proceed. Install from 'https://git-scm.com/download/win'"
}

Write-Verbose "cloning -b $psReleaseBranch --quiet https://github.com/$psReleaseFork/PSRelease.git" -verbose
& $gitBinFullPath clone -b $psReleaseBranch --quiet https://github.com/$psReleaseFork/PSRelease.git $location

Push-Location -Path $PWD.Path
try{
    Set-Location $location
    & $gitBinFullPath  submodule update --init --recursive --quiet
}
finally
{
    Pop-Location
}

$unresolvedRepoRoot = Join-Path -Path $PSScriptRoot '../..'
$resolvedRepoRoot = (Resolve-Path -Path $unresolvedRepoRoot).ProviderPath

try 
{
    Write-Verbose "Starting build at $resolvedRepoRoot  ..." -Verbose
    Import-Module "$location/vstsBuild" -Force
    Import-Module "$location/dockerBasedBuild" -Force
    Clear-VstsTaskState

    Invoke-Build -RepoPath $resolvedRepoRoot  -BuildJsonPath './tools/releaseBuild/build.json' -Name $Name -Parameters $PSBoundParameters 
}
catch
{
    Write-VstsError -Error $_
}
finally{
    Write-VstsTaskState
    exit 0
}
