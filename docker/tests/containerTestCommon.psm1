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
    foreach($os in 'amazonlinux','centos7','opensuse42.2','ubuntu14.04','ubuntu16.04')
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
    return !((Get-DockerEngineOs) -like 'Alpine Linux*')
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
