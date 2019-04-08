# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$script:forcePull = $true
# Get docker Engine OS
function Get-DockerEngineOs
{
    docker info --format '{{ .OperatingSystem }}'
}

# Call Docker with appropriate result checksfunction Invoke-Docker
function Invoke-Docker
{
    param(
        [Parameter(Mandatory=$true)]
        [string[]]
        $Command,
        [ValidateSet("error","warning",'ignore')]
        $FailureAction = 'error',

        [Parameter(Mandatory=$true)]
        [string[]]
        $Params,

        [switch]
        $PassThru,
        [switch]
        $SuppressHostOutput
    )

    $ErrorActionPreference = 'Continue'

    # Log how we are running docker for troubleshooting issues
    Write-Verbose "Running docker $command $params" -Verbose
    if($SuppressHostOutput.IsPresent)
    {
        $result = docker $command $params 2>&1
    }
    else
    {
        &'docker' $command $params 2>&1 | Tee-Object -Variable result -ErrorAction SilentlyContinue | Out-String -Stream -ErrorAction SilentlyContinue | Write-Host -ErrorAction SilentlyContinue
    }

    $dockerExitCode = $LASTEXITCODE
    if($PassThru.IsPresent)
    {
        Write-Verbose "passing through docker result$($result.length)..." -Verbose
        return $result
    }
    elseif($dockerExitCode -ne 0 -and $FailureAction -eq 'error')
    {
        Write-Error "docker $command failed with: $result" -ErrorAction Stop
        return $false
    }
    elseif($dockerExitCode -ne 0 -and $FailureAction -eq 'warning')
    {
        Write-Warning "docker $command failed with: $result"
        return $false
    }
    elseif($dockerExitCode -ne 0)
    {
        return $false
    }

    return $true
}

# Return a list of Linux Container Test Cases
function Get-LinuxContainer
{
    foreach($os in 'centos7','ubuntu16.04')
    {
        Write-Output @{
            Name = $os
            Path = "$psscriptroot/../release/$os"
        }
    }

}

# Return a list of Windows Container Test Cases
function Get-WindowsContainer
{
    foreach($os in 'windowsservercore','nanoserver')
    {
        Write-Output @{
            Name = $os
            Path = "$psscriptroot/../release/$os"
        }
    }
}

$script:repoName = 'microsoft/powershell'
function Get-RepoName
{
    return $script:repoName
}

function Set-RepoName
{
    param([string]$RepoName)

    $script:repoName = $RepoName
    $script:forcePull = $false
}

function Test-SkipWindows
{
    [bool] $canRunWindows = (Get-DockerEngineOs) -like 'Windows*'
    return ($IsLinux -or $IsMacOS -or !$canRunWindows)
}

function Test-SkipLinux
{
    $os = Get-DockerEngineOs

    switch -wildcard ($os)
    {
        '*Linux*' {
            return $false
        }
        '*Mac' {
            return $false
        }
        # Docker for Windows means we are running the linux kernel
        'Docker for Windows' {
            return $false
        }
        'Windows*' {
            return $true
        }
        default {
            throw "Unknow docker os '$os'"
        }
    }
}

function Get-TestContext
{
    param(
        [ValidateSet('Linux','Windows','macOS')]
        [string]$Type
    )

    $resultFileName = 'results.xml'
    $logFileName = 'results.log'
    $containerTestDrive = '/test'

    # Return a windows context if the Context in Windows *AND*
    # the current system is windows, otherwise Join-path will fail.
    if($Type -eq 'Windows' -and $IsWindows)
    {
        $ContainerTestDrive = 'C:\test'
    }
    $resolvedTestDrive = (Resolve-Path "Testdrive:\").providerPath

    return @{
        ResolvedTestDrive = $resolvedTestDrive
        ResolvedXmlPath = Join-Path $resolvedTestDrive -ChildPath $resultFileName
        ResolvedLogPath = Join-Path $resolvedTestDrive -ChildPath $logFileName
        ContainerTestDrive = $ContainerTestDrive
        ContainerXmlPath = Join-Path $containerTestDrive -ChildPath $resultFileName
        ContainerLogPath = Join-Path $containerTestDrive -ChildPath $logFileName
        Type = $Type
        ForcePull = $script:forcePull
    }
}

function Get-ContainerPowerShellVersion
{
    param(
        [HashTable] $TestContext,
        [string] $RepoName,
        [string] $Name
    )

    $imageTag = "${script:repoName}:${Name}"

    if($TestContext.ForcePull)
    {
        $null=Invoke-Docker -Command 'image', 'pull' -Params $imageTag -SuppressHostOutput
    }

    $runParams = @()
    $localVolumeName = $testContext.resolvedTestDrive
    $runParams += '--rm'
    if($TestContext.Type -ne 'Windows' -and $isWindows)
    {
        # use a container volume on windows because host volumes are not automatic
        $volumeName = "test-volume-" + (Get-Random -Minimum 100 -Maximum 999)

        # using alpine because it's tiny
        $null=Invoke-Docker -Command create -Params '-v', '/test', '--name', $volumeName, 'alpine' -SuppressHostOutput
        $runParams += '--volumes-from'
        $runParams += $volumeName
    }
    else {
        $runParams += '-v'
        $runParams += "${localVolumeName}:$($testContext.containerTestDrive)"
    }

    $runParams += $imageTag
    $runParams += 'pwsh'
    $runParams += '-c'
    $runParams += ('$PSVersionTable.PSVersion.ToString() | out-string | out-file -encoding ascii -FilePath '+$testContext.containerLogPath)

    $null = Invoke-Docker -Command run -Params $runParams -SuppressHostOutput
    if($TestContext.Type -ne 'Windows' -and $isWindows)
    {
        $null = Invoke-Docker -Command cp -Params "${volumeName}:$($testContext.containerLogPath)", $TestContext.ResolvedLogPath
        $null = Invoke-Docker -Command container, rm -Params $volumeName, '--force' -SuppressHostOutput
    }
    return (Get-Content -Encoding Ascii $testContext.resolvedLogPath)[0]
}

# Function defines a config mapping for testing Preview packages.
# The list of supported OS for each release can be found here:
# https://github.com/PowerShell/PowerShell-Docs/blob/staging/reference/docs-conceptual/PowerShell-Core-Support.md#supported-platforms
function Get-DefaultPreviewConfigForPackageValidation
{
    # format: <DockerfileFolderName>=<PartOfPackageFilename>
    @{  'centos7'='rhel.7';
        'debian.9'='debian.9';
        'fedora28'='rhel.7';
        'opensuse42.3'='linux-x64.tar.gz';
        'ubuntu16.04'='ubuntu.16.04';
        'ubuntu18.04'='ubuntu.18.04';
        'fxdependent-centos7'='linux-x64-fxdependent.tar.gz';
        'fxdependent-debian.9'='linux-x64-fxdependent.tar.gz';
        'fxdependent-fedora28'='linux-x64-fxdependent.tar.gz';
        'fxdependent-opensuse42.3'='linux-x64-fxdependent.tar.gz';
        'fxdependent-ubuntu16.04'='linux-x64-fxdependent.tar.gz';
        'fxdependent-ubuntu18.04'='linux-x64-fxdependent.tar.gz';
        'fxdependent-dotnetsdk-latest'='linux-x64-fxd-dotnetsdk.tar.gz'
    }
}

# Function defines a config mapping for testing Stable packages.
# The list of supported OS for each release can be found here:
# https://github.com/PowerShell/PowerShell-Docs/blob/staging/reference/docs-conceptual/PowerShell-Core-Support.md#supported-platforms
function Get-DefaultStableConfigForPackageValidation
{
    # format: <DockerfileFolderName>=<PartOfPackageFilename>
    @{  'centos7'='rhel.7';
        'debian.9'='debian.9';
        'opensuse42.3'='linux-x64.tar.gz';
        'ubuntu16.04'='ubuntu.16.04';
        'fxdependent-centos7'='linux-x64-fxdependent.tar.gz';
        'fxdependent-debian.9'='linux-x64-fxdependent.tar.gz';
        'fxdependent-opensuse42.3'='linux-x64-fxdependent.tar.gz';
        'fxdependent-ubuntu16.04'='linux-x64-fxdependent.tar.gz'
    }
}

# Returns a list of files in a specified Azure container.
function Get-PackageNamesOnAzureBlob
{
    param(
        [string]
        $ContainerUrl,

        # $SAS (shared access signature) param should include beginning '?' and trailing '&'
        [string]
        $SAS
    )


    $response = Invoke-RestMethod -Method Get -Uri $($ContainerUrl + $SAS + 'restype=container&comp=list')

    $xmlResponce = [xml]$response.Substring($response.IndexOf('<EnumerationResults')) # remove some bad chars in the beginning that break XML parsing
    ($xmlResponce.EnumerationResults.Blobs.Blob).Name
}

# This function is used for basic validation of PS packages during a release;
# During the process Docker files are filled out and executed with Docker build;
# During the build PS packages are downloaded onto Docker containers, installed and selected Pester tests from PowerShell Github repo are executed.
# This function must be run on a Docker host machine in 'Linux containers' mode, such as Windows 10 server with Hyper-V role installed.
function Test-PSPackage
{
    param(
        [string]
        [Parameter(Mandatory=$true)]
        $PSPackageLocation, # e.g. Azure container storage url
        [string]
        $SAS,# $SAS (shared access signature) param should include beginning '?' and trailing '&'
        [Hashtable]
        $Config, # hashtable that maps packages to dockerfiles; for example see Get-DefaultConfigForPackageValidation
        [string]
        $TestList = "/PowerShell/test/powershell/Modules/PackageManagement/PackageManagement.Tests.ps1,/PowerShell/test/powershell/engine/Module",
        [string]
        $TestDownloadCommand = "git clone --recursive https://github.com/PowerShell/PowerShell.git",
        [switch]
        $Preview = $false
    )

    $PSPackageLocation = $PSPackageLocation.TrimEnd('/','\') # code below assumes there is no trailing separator in PSPackageLocation url
    $RootFolder = Join-Path $PSScriptRoot 'Templates'


    $packageList = Get-PackageNamesOnAzureBlob -ContainerUrl $PSPackageLocation -SAS $SAS
    if (!$Config)
    {
        if ($Preview)
        {
            $Config = Get-DefaultPreviewConfigForPackageValidation
        }
        else
        {
            $Config = Get-DefaultStableConfigForPackageValidation
        }
    }

    # pre-process $Config: verify build directories and packages exist
    $map = @{}
    foreach($kp in $Config.GetEnumerator())
    {
        $buildDir = Join-Path $RootFolder $kp.Key
        $packageName = $packageList | Where-Object {$_ -like $('*'+$kp.Value+'*')}

        if (-not (Test-Path $buildDir))
        {
            Write-Error "Directory does Not exist - $buildDir; Check `$Config parameter and '$RootFolder' folder"
        }
        elseif (-not ($packageName))
        {
            Write-Error "Can not find package that matches filter *$($kp.Value)*; Check `$Config parameter and '$PSPackageLocation'"
        }
        else
        {
            $map.Add($buildDir, $packageName)
        }
    }

    Write-Verbose "Using configuration:" -Verbose
    Write-Verbose ($map | Format-List | Out-String) -Verbose

    $results = @{}
    $returnValue = $true

    # run builds sequentially, but don't block for errors so that configs after failed one can run
    foreach($kp in $map.GetEnumerator())
    {
        $dockerDirPath = $kp.Key
        $packageFileName = $kp.Value

        $buildArgs = @()

        $buildArgs += "--build-arg","PACKAGENAME=$packageFileName"
        $buildArgs += "--build-arg","PACKAGELOCATION=$PSPackageLocation"
        if ($Preview)
        {
            $buildArgs += "--build-arg","PREVIEWSUFFIX=-preview"
        }
        $buildArgs += "--build-arg","TESTLIST=$TestList"
        $buildArgs += "--build-arg","TESTDOWNLOADCOMMAND=$TestDownloadCommand"
        $buildArgs += "--no-cache"
        $buildArgs += $dockerDirPath

        $dockerResult = Invoke-Docker -Command 'build' -Params $buildArgs -FailureAction warning

        $confName = Split-Path -Leaf $dockerDirPath
        $results.Add($confName, $dockerResult)
        if (-not $dockerResult) {$returnValue = $false}
    }

    # in the end print results for all configurations
    Write-Verbose "Package validation results:" -Verbose
    $results

    return $returnValue
}
