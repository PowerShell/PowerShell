# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '')]
param()

Set-StrictMode -Version 3.0

$ErrorActionPreference = 'continue'
$repoRoot = Join-Path $PSScriptRoot '..'
$script:administratorsGroupSID = "S-1-5-32-544"
$script:usersGroupSID = "S-1-5-32-545"

# set .NET path
$dotNetPath = "$env:USERPROFILE\Appdata\Local\Microsoft\dotnet"
if(Test-Path $dotNetPath)
{
    $env:PATH = $dotNetPath + ';' + $env:PATH
}

# import build into the global scope so it can be used by packaging
# argumentList $true says ignore tha we may not be able to build
Write-Verbose "Importing build.psm1" -Verbose
Import-Module (Join-Path $repoRoot 'build.psm1') -Scope Global -ArgumentList $true
$buildCommands = Get-Command -Module build
Write-Verbose "Imported build.psm1 commands: $($buildCommands.Count)" -Verbose

Write-Verbose "Importing packaging.psm1" -Verbose
Import-Module (Join-Path $repoRoot 'tools\packaging') -Scope Global
$packagingCommands = Get-Command -Module packaging
Write-Verbose "Imported packaging.psm1 commands: $($packagingCommands.Count)" -Verbose

# import the windows specific functcion only in Windows PowerShell or on Windows
if($PSVersionTable.PSEdition -eq 'Desktop' -or $IsWindows)
{
    Import-Module (Join-Path $PSScriptRoot 'WindowsCI.psm1') -Scope Global
}

# tests if we should run a daily build
# returns true if the build is scheduled
# or is a pushed tag
Function Test-DailyBuild
{
    $trueString = 'True'
    if(($env:PS_DAILY_BUILD -eq $trueString) -or $env:BUILD_REASON -eq 'Schedule')
    {
        return $true
    }
    return $false
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
        $Value,

        [switch]
        $IsOutput
    )

    $IsOutputString = if ($IsOutput) { 'true' } else { 'false' }
    $command = "vso[task.setvariable variable=$Name;isOutput=$IsOutputString]$Value"

    # always log command to make local debugging easier
    Write-Verbose -Message "sending command: $command" -Verbose

    if ($env:TF_BUILD) {
        # In VSTS
        Write-Host "##$command"
        # The variable will not show up until the next task.
    }

    # Setting in the current session for the same behavior as the CI and to make it show up in the same task
    Set-Item env:/$name -Value $Value
}

# Emulates running all of CI but locally
function Invoke-CIFull
{
    param(
        [switch] $CleanRepo
    )
    if($CleanRepo)
    {
        Clear-PSRepo
    }

    Invoke-CIInstall
    Invoke-CIBuild
    Invoke-CITest -ErrorAction Continue
    Invoke-CIFinish
}

# Implements the CI 'build_script' step
function Invoke-CIBuild
{
    param(
        [ValidateSet('Debug', 'Release', 'CodeCoverage', 'StaticAnalysis')]
        [string]$Configuration = 'Release'
    )

    $releaseTag = Get-ReleaseTag
    # check to be sure our test tags are correct
    $result = Get-PesterTag
    if ( $result.Result -ne "Pass" )
    {
        $result.Warnings
        throw "Tags must be CI, Feature, Scenario, or Slow"
    }

    if(Test-DailyBuild)
    {
        Start-PSBuild -Configuration 'CodeCoverage' -PSModuleRestore -CI -ReleaseTag $releaseTag
    }

    Start-PSBuild -PSModuleRestore -Configuration $Configuration -CI -ReleaseTag $releaseTag -UseNuGetOrg
    Save-PSOptions

    $options = (Get-PSOptions)

    $path = Split-Path -Path $options.Output

    $psOptionsPath = (Join-Path -Path $PSScriptRoot -ChildPath '../psoptions.json')
    $buildZipPath = (Join-Path -Path $PSScriptRoot -ChildPath '../build.zip')

    Compress-Archive -Path $path -DestinationPath $buildZipPath

    Push-Artifact -Path $psOptionsPath -Name 'build'
    Push-Artifact -Path $buildZipPath -Name 'build'
}

# Implements the CI 'install' step
function Invoke-CIInstall
{
    param(
        [switch]
        $SkipUser
    )

    # Switch to public sources in CI
    Switch-PSNugetConfig -Source Public

    # Make sure we have all the tags
    Sync-PSTags -AddRemoteIfMissing

    if(Test-DailyBuild)
    {
        if ($env:BUILD_REASON -eq 'Schedule')
        {
            Write-Host "##vso[build.updatebuildnumber]Daily-$env:BUILD_SOURCEBRANCHNAME-$env:BUILD_SOURCEVERSION-$((Get-Date).ToString("yyyyMMddhhss"))"
        }
    }

    if ($env:TF_BUILD -and !$SkipUser.IsPresent)
    {
        # Generate new credential for CI (only) remoting tests.
        Write-Verbose "Creating account for remoting tests in CI." -Verbose

        # Password
        $randomObj = [System.Random]::new()
        $password = ""
        1..(Get-Random -Minimum 15 -Maximum 126) | ForEach-Object { $password = $password + [char]$randomObj.next(45,126) }

        # Account
        $userName = 'ciRemote'
        New-LocalUser -username $userName -Password $password
        Add-UserToGroup -username $userName -groupSid $script:administratorsGroupSID

        # Provide credentials globally for remote tests.
        $ss = ConvertTo-SecureString -String $password -AsPlainText -Force
        $ciRemoteCredential = [PSCredential]::new("$env:COMPUTERNAME\$userName", $ss)
        $ciRemoteCredential | Export-Clixml -Path "$env:TEMP\CIRemoteCred.xml" -Force

        # Check that LocalAccountTokenFilterPolicy policy is set, since it is needed for remoting
        # using above local admin account.
        Write-Log -Message "Checking for LocalAccountTokenFilterPolicy in the CI."
        $haveLocalAccountTokenFilterPolicy = $false
        try
        {
            $haveLocalAccountTokenFilterPolicy = ((Get-ItemPropertyValue -Path HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System -Name LocalAccountTokenFilterPolicy) -eq 1)
        }
        # ignore if anything is caught:
        catch {}
        if (!$haveLocalAccountTokenFilterPolicy)
        {
            Write-Verbose "Setting the LocalAccountTokenFilterPolicy for remoting tests"
            Set-ItemProperty -Path HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System -Name LocalAccountTokenFilterPolicy -Value 1
        }
    }

    Set-BuildVariable -Name TestPassed -Value False
}

function Invoke-CIxUnit
{
    param(
        [switch]
        $SkipFailing
    )
    $env:CoreOutput = Split-Path -Parent (Get-PSOutput -Options (Get-PSOptions))
    $path = "$env:CoreOutput\pwsh.exe"
    if($IsMacOS -or $IsLinux)
    {
        $path = "$env:CoreOutput\pwsh"
    }

    if(!(Test-Path $path))
    {
        throw "CoreCLR pwsh.exe was not built"
    }

    $xUnitTestResultsFile = "xUnitTestResults.xml"

    Start-PSxUnit -xUnitTestResultsFile $xUnitTestResultsFile
    Push-Artifact -Path $xUnitTestResultsFile -name xunit

    if(!$SkipFailing.IsPresent)
    {
        # Fail the build, if tests failed
        Test-XUnitTestResults -TestResultsFile $xUnitTestResultsFile
    }
}

# Install Pester module if not already installed with a compatible version
function Install-CIPester
{
    [CmdletBinding()]
    param(
        [string]$MinimumVersion = '5.0.0',
        [string]$MaximumVersion = '5.99.99',
        [switch]$Force
    )

    Write-Verbose "Checking for Pester module (required: $MinimumVersion - $MaximumVersion)" -Verbose

    # Check if a compatible version of Pester is already installed
    $installedPester = Get-Module -Name Pester -ListAvailable | 
        Where-Object { $_.Version -ge $MinimumVersion -and $_.Version -le $MaximumVersion } |
        Sort-Object -Property Version -Descending |
        Select-Object -First 1

    if ($installedPester -and -not $Force) {
        Write-Host "Pester version $($installedPester.Version) is already installed and meets requirements" -ForegroundColor Green
        return
    }

    if ($Force) {
        Write-Host "Installing Pester module (forced)" -ForegroundColor Yellow
    } else {
        Write-Host "Installing Pester module" -ForegroundColor Yellow
    }

    try {
        Install-Module -Name Pester -Force -SkipPublisherCheck -MaximumVersion $MaximumVersion -ErrorAction Stop
        Write-Host "Successfully installed Pester module" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Pester module: $_"
        throw
    }
}

# Implement CI 'Test_script'
function Invoke-CITest
{
    [CmdletBinding()]
    param(
        [ValidateSet('UnelevatedPesterTests', 'ElevatedPesterTests')]
        [string] $Purpose,
        [ValidateSet('CI', 'Others')]
        [string] $TagSet,
        [string] $TitlePrefix,
        [string] $OutputFormat = "NUnitXml"
    )

    Write-Verbose -Verbose "CI test: OutputFormat: $OutputFormat"

    # Set locale correctly for Linux CIs
    Set-CorrectLocale

    # Pester doesn't allow Invoke-Pester -TagAll@('CI', 'RequireAdminOnWindows') currently
    # https://github.com/pester/Pester/issues/608
    # To work-around it, we exlude all categories, but 'CI' from the list
    switch ($TagSet) {
        'CI' {
            Write-Host -Foreground Green 'Running "CI" CoreCLR tests..'
            $ExcludeTag = @('Slow', 'Feature', 'Scenario')
        }
        'Others' {
            Write-Host -Foreground Green 'Running non-CI CoreCLR tests..'
            $ExcludeTag = @('CI')
        }
        Default {
            throw "Unknown TagSet: '$TagSet'"
        }
    }

    if($IsLinux -or $IsMacOS)
    {
        return Invoke-LinuxTestsCore -Purpose $Purpose -ExcludeTag $ExcludeTag -TagSet $TagSet -TitlePrefix $TitlePrefix -OutputFormat $OutputFormat
    }

    # CoreCLR

    $env:CoreOutput = Split-Path -Parent (Get-PSOutput -Options (Get-PSOptions))
    Write-Host -Foreground Green 'Run CoreCLR tests'
    $testResultsNonAdminFile = "$PWD\TestsResultsNonAdmin-$TagSet.xml"
    $testResultsAdminFile = "$PWD\TestsResultsAdmin-$TagSet.xml"
    if(!(Test-Path "$env:CoreOutput\pwsh.exe"))
    {
        throw "CoreCLR pwsh.exe was not built"
    }

    # Get the experimental feature names and the tests associated with them
    $ExperimentalFeatureTests = Get-ExperimentalFeatureTests

    if ($Purpose -eq 'UnelevatedPesterTests') {
        $unelevate = $true
        $environment = Get-EnvironmentInformation
        if ($environment.OSArchitecture -eq 'arm64') {
            Write-Verbose -Verbose "running on arm64, running unelevated tests as elevated"
            $unelevate = $false
        }

        $arguments = @{
            Bindir = $env:CoreOutput
            OutputFile = $testResultsNonAdminFile
            Unelevate = $unelevate
            Terse = $true
            Tag = @()
            ExcludeTag = $ExcludeTag + 'RequireAdminOnWindows'
            OutputFormat = $OutputFormat
        }

        $title = "Pester Unelevated - $TagSet"
        if ($TitlePrefix) {
            $title = "$TitlePrefix - $title"
        }
        Write-Verbose -Verbose "Starting Pester with output format $($arguments.OutputFormat)"
        Start-PSPester @arguments -Title $title

        # Fail the build, if tests failed
        Test-PSPesterResults -TestResultsFile $testResultsNonAdminFile

        # Run tests with specified experimental features enabled
        foreach ($entry in $ExperimentalFeatureTests.GetEnumerator())
        {
            $featureName = $entry.Key
            $testFiles = $entry.Value

            $expFeatureTestResultFile = "$PWD\TestsResultsNonAdmin.$featureName.xml"
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

            $title = "Pester Experimental Unelevated - $featureName"
            if ($TitlePrefix) {
                $title = "$TitlePrefix - $title"
            }

            # We just built the test tools, we don't need to rebuild them
            Write-Verbose -Verbose "Starting Pester with output format $($arguments.OutputFormat)"
            Start-PSPester @arguments -Title $title -SkipTestToolBuild

            # Fail the build, if tests failed
            Test-PSPesterResults -TestResultsFile $expFeatureTestResultFile
        }
    }

    if ($Purpose -eq 'ElevatedPesterTests') {
        $arguments = @{
            Terse = $true
            Bindir = $env:CoreOutput
            OutputFile = $testResultsAdminFile
            Tag = @('RequireAdminOnWindows')
            ExcludeTag = $ExcludeTag
            OutputFormat = $OutputFormat
        }

        $title = "Pester Elevated - $TagSet"
        if ($TitlePrefix) {
            $title = "$TitlePrefix - $title"
        }

        Write-Verbose -Verbose "Starting Pester with output format $($arguments.OutputFormat)"
        Start-PSPester @arguments -Title $title

        # Fail the build, if tests failed
        Test-PSPesterResults -TestResultsFile $testResultsAdminFile

        # Run tests with specified experimental features enabled
        foreach ($entry in $ExperimentalFeatureTests.GetEnumerator())
        {
            $featureName = $entry.Key
            $testFiles = $entry.Value

            $expFeatureTestResultFile = "$PWD\TestsResultsAdmin.$featureName.xml"
            $arguments['OutputFile'] = $expFeatureTestResultFile
            $arguments['ExperimentalFeatureName'] = $featureName
            if ($testFiles.Count -eq 0)
            {
                # If an empty array is specified for the feature name, we run all tests with the feature enabled.
                # This allows us to prevent regressions to a critical engine experimental feature.
                $arguments.Remove('Path')
            }
            else
            {
                # If a non-empty string or array is specified for the feature name, we only run those test files.
                $arguments['Path'] = $testFiles
            }

            $title = "Pester Experimental >levated - $featureName"
            if ($TitlePrefix) {
                $title = "$TitlePrefix - $title"
            }

            Write-Verbose -Verbose "Starting Pester with output format $($arguments.OutputFormat)"
            # We just built the test tools, we don't need to rebuild them
            Start-PSPester @arguments -Title $title -SkipTestToolBuild

            # Fail the build, if tests failed
            Test-PSPesterResults -TestResultsFile $expFeatureTestResultFile
        }
    }

    Set-BuildVariable -Name TestPassed -Value True
}

function New-CodeCoverageAndTestPackage
{
    [CmdletBinding()]
    param()

    if (Test-DailyBuild)
    {
        Start-PSBuild -Configuration 'CodeCoverage' -Clean

        $codeCoverageOutput = Split-Path -Parent (Get-PSOutput)
        $codeCoverageArtifacts = Compress-CoverageArtifacts -CodeCoverageOutput $codeCoverageOutput

        $destBasePath = if ($env:TF_BUILD) {
            $env:BUILD_ARTIFACTSTAGINGDIRECTORY
        } else {
            Join-Path (Get-Location).Path "out"
        }

        if (-not (Test-Path $destBasePath))
        {
            $null = New-Item -ItemType Directory -Path $destBasePath
        }

        Write-Host -ForegroundColor Green 'Upload CodeCoverage artifacts'
        $codeCoverageArtifacts | ForEach-Object {
            Copy-Item -Path $_ -Destination $destBasePath
            $newPath = Join-Path $destBasePath (Split-Path $_ -Leaf)
            Push-Artifact -Path $newPath -Name 'CodeCoverage'
        }

        New-TestPackage -Destination $destBasePath
        $testPackageFullName = Join-Path $destBasePath 'TestPackage.zip'
        Write-Verbose "Created TestPackage.zip" -Verbose
        Write-Host -ForegroundColor Green 'Upload test package'
        Push-Artifact $testPackageFullName -Name 'CodeCoverage'
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
        # In Azure DevOps
        Write-Host "##vso[artifact.upload containerfolder=$artifactName;artifactname=$artifactName;]$Path"
    } elseif ($env:GITHUB_WORKFLOW -and $env:RUNNER_WORKSPACE) {
        # In GitHub Actions
        $destinationPath = Join-Path -Path $env:RUNNER_WORKSPACE -ChildPath $artifactName

        # Create the folder if it does not exist
        if (!(Test-Path -Path $destinationPath)) {
            $null = New-Item -ItemType Directory -Path $destinationPath -Force
        }

        Copy-Item -Path $Path -Destination $destinationPath -Force -Verbose
    } else {
        Write-Warning "Push-Artifact is not supported in this environment."
    }
}

function Compress-CoverageArtifacts
{
    param([string] $CodeCoverageOutput)

    # Create archive for test content, OpenCover module and CodeCoverage build
    $artifacts = New-Object System.Collections.ArrayList

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath((Join-Path $PSScriptRoot '..\test\tools\OpenCover'))
    $zipOpenCoverPath = Join-Path $PWD 'OpenCover.zip'
    [System.IO.Compression.ZipFile]::CreateFromDirectory($resolvedPath, $zipOpenCoverPath)
    $null = $artifacts.Add($zipOpenCoverPath)

    $zipCodeCoveragePath = Join-Path $PWD "CodeCoverage.zip"
    Write-Verbose "Zipping ${CodeCoverageOutput} into $zipCodeCoveragePath" -Verbose
    [System.IO.Compression.ZipFile]::CreateFromDirectory($CodeCoverageOutput, $zipCodeCoveragePath)
    $null = $artifacts.Add($zipCodeCoveragePath)

    return $artifacts
}

function Get-ReleaseTag
{
    $metaDataPath = Join-Path -Path $PSScriptRoot -ChildPath 'metadata.json'
    $metaData = Get-Content $metaDataPath | ConvertFrom-Json
    $releaseTag = $metadata.NextReleaseTag
    if($env:BUILD_BUILID)
    {
        $releaseTag = $releaseTag.split('.')[0..2] -join '.'
        $releaseTag = $releaseTag + '.' + $env:BUILD_BUILID
    }
    return $releaseTag
}

# Implements CI 'on_finish' step
function Invoke-CIFinish
{
    param(
        [string] $Runtime = 'win7-x64',
        [string] $Channel = 'preview',
        [Validateset('Build','Package')]
        [string[]] $Stage = ('Build','Package')
    )

    # Switch to public sources in CI
    Switch-PSNugetConfig -Source Public

    if ($PSEdition -eq 'Core' -and ($IsLinux -or $IsMacOS) -and $Stage -contains 'Build') {
        return New-LinuxPackage
    }

    $artifacts = New-Object System.Collections.ArrayList
    try {
        $buildFolder = "${env:SYSTEM_ARTIFACTSDIRECTORY}/mainBuild"

        if ($Stage -contains "Build") {
            if ($Channel -eq 'preview') {
                $releaseTag = Get-ReleaseTag

                $previewVersion = $releaseTag.Split('-')
                $previewPrefix = $previewVersion[0]
                $previewLabel = $previewVersion[1].replace('.','')

                if (Test-DailyBuild) {
                    $previewLabel = "daily{0}" -f $previewLabel
                }

                $prereleaseIteration = (get-date).Day
                $preReleaseVersion = "$previewPrefix-$previewLabel.$prereleaseIteration"
                # Build clean before backing to remove files from testing
                Start-PSBuild -PSModuleRestore -Configuration 'Release' -ReleaseTag $preReleaseVersion -Clean -Runtime $Runtime -output $buildFolder -PSOptionsPath "${buildFolder}/psoptions.json"
                $options = Get-PSOptions
                # Remove symbol files.
                $filter = Join-Path -Path (Split-Path $options.Output) -ChildPath '*.pdb'
                Write-Verbose "Removing symbol files from $filter" -Verbose
                Remove-Item $filter -Force -Recurse
            } else {
                $releaseTag = Get-ReleaseTag
                $releaseTagParts = $releaseTag.split('.')
                $preReleaseVersion = $releaseTagParts[0]+ ".9.9"
                Write-Verbose "newPSReleaseTag: $preReleaseVersion" -Verbose
                Start-PSBuild -PSModuleRestore -Configuration 'Release' -ReleaseTag $preReleaseVersion -Clean -Runtime $Runtime -output $buildFolder -PSOptionsPath "${buildFolder}/psoptions.json"
                $options = Get-PSOptions
                # Remove symbol files.
                $filter = Join-Path -Path (Split-Path $options.Output) -ChildPath '*.pdb'
                Write-Verbose "Removing symbol files from $filter" -Verbose
                Remove-Item $filter -Force -Recurse
            }

            # Set a variable, both in the current process and in AzDevOps for the packaging stage to get the release tag
            $env:CI_FINISH_RELASETAG=$preReleaseVersion
            $vstsCommandString = "vso[task.setvariable variable=CI_FINISH_RELASETAG]$preReleaseVersion"
            Write-Verbose -Message "$vstsCommandString" -Verbose
            Write-Host -Object "##$vstsCommandString"
        }

        if ($Stage -contains "Package") {
            Restore-PSOptions -PSOptionsPath "${buildFolder}/psoptions.json"
            $preReleaseVersion = $env:CI_FINISH_RELASETAG

            # Build packages	            $preReleaseVersion = "$previewPrefix-$previewLabel.$prereleaseIteration"
            switch -regex ($Runtime){
                default {
                    $runPackageTest = $true
                    $packageTypes = 'msi', 'zip', 'zip-pdb', 'msix'
                }
                'win-arm.*' {
                    $runPackageTest = $false
                    $packageTypes = 'msi', 'zip', 'zip-pdb', 'msix'
                }
            }

            Import-Module "$PSScriptRoot\wix\wix.psm1"
            Install-Wix -arm64:$true
            $packages = Start-PSPackage -Type $packageTypes -ReleaseTag $preReleaseVersion -SkipReleaseChecks -WindowsRuntime $Runtime

            foreach ($package in $packages) {
                if (Test-Path $package -ErrorAction Ignore) {
                    Write-Log "Package found: $package"
                } else {
                    Write-Warning -Message "Package NOT found: $package"
                }

                if ($package -is [string]) {
                    $null = $artifacts.Add($package)
                } elseif ($package -is [pscustomobject] -and $package.psobject.Properties['msi']) {
                    $null = $artifacts.Add($package.msi)
                    $null = $artifacts.Add($package.wixpdb)
                }
            }

            if ($runPackageTest) {
                # the packaging tests find the MSI package using env:PSMsiX64Path
                $env:PSMsiX64Path = $artifacts | Where-Object { $_.EndsWith(".msi")}
                $architechture = $Runtime.Split('-')[1]
                $exePath = New-ExePackage -ProductVersion ($preReleaseVersion -replace '^v') -ProductTargetArchitecture $architechture -MsiLocationPath $env:PSMsiX64Path
                Write-Verbose "exe Path: $exePath" -Verbose
                $artifacts.Add($exePath)
                $env:PSExePath = $exePath
                $env:PSMsiChannel = $Channel
                $env:PSMsiRuntime = $Runtime

                # Install the latest Pester and import it
                $maximumPesterVersion = '4.99'
                Install-CIPester -MinimumVersion '4.0.0' -MaximumVersion $maximumPesterVersion -Force
                Import-Module Pester -Force -MaximumVersion $maximumPesterVersion

                $testResultPath = Join-Path -Path $env:TEMP -ChildPath "win-package-$channel-$runtime.xml"

                # start the packaging tests and get the results
                $packagingTestResult = Invoke-Pester -Script (Join-Path $repoRoot '.\test\packaging\windows\') -PassThru -OutputFormat NUnitXml -OutputFile $testResultPath

                Publish-TestResults -Title "win-package-$channel-$runtime" -Path $testResultPath

                # fail the CI job if the tests failed, or nothing passed
                if (-not $packagingTestResult -is [pscustomobject] -or $packagingTestResult.FailedCount -ne 0 -or $packagingTestResult.PassedCount -eq 0) {
                    throw "Packaging tests failed ($($packagingTestResult.FailedCount) failed/$($packagingTestResult.PassedCount) passed)"
                }
            }
        }
    } catch {
        Get-Error -InputObject $_
        throw
    } finally {
        $pushedAllArtifacts = $true

        $artifacts | ForEach-Object {
            Write-Log -Message "Pushing $_ as CI artifact"
            if (Test-Path $_) {
                Push-Artifact -Path $_ -Name 'artifacts'
            } else {
                $pushedAllArtifacts = $false
                Write-Warning "Artifact $_ does not exist."
            }
        }

        if (!$pushedAllArtifacts) {
            throw "Some artifacts did not exist!"
        }
    }
}

function Set-Path
{
    param
    (
        [Parameter(Mandatory)]
        [string]
        $Path,

        [Parameter(Mandatory)]
        [switch]
        $Append
    )

    $machinePathString = [System.Environment]::GetEnvironmentVariable('path',[System.EnvironmentVariableTarget]::Machine)
    $machinePath = $machinePathString -split ';'

    if($machinePath -inotcontains $path)
    {
        $newPath = "$machinePathString;$path"
        Write-Verbose "Adding $path to path..." -Verbose
        [System.Environment]::SetEnvironmentVariable('path',$newPath,[System.EnvironmentVariableTarget]::Machine)
        Write-Verbose "Added $path to path." -Verbose
    }
    else
    {
        Write-Verbose "$path already in path." -Verbose
    }
}

# Display environment variables in a log group for GitHub Actions
function Show-Environment
{
    Write-LogGroupStart -Title 'Environment'
    Get-ChildItem -Path env: | Out-String -width 9999 -Stream | Write-Verbose -Verbose
    Write-LogGroupEnd -Title 'Environment'
}

# Bootstrap script for Linux and macOS
function Invoke-BootstrapStage
{
    $createPackages = Test-DailyBuild
    Write-Log -Message "Executing ci.psm1 Bootstrap Stage"
    # Make sure we have all the tags
    Sync-PSTags -AddRemoteIfMissing
    Start-PSBootstrap -Scenario Package:$createPackages
}

# Run pester tests for Linux and macOS
function Invoke-LinuxTestsCore
{
    [CmdletBinding()]
    param(
        [ValidateSet('UnelevatedPesterTests', 'ElevatedPesterTests', 'All')]
        [string] $Purpose = 'All',
        [string[]] $ExcludeTag = @('Slow', 'Feature', 'Scenario'),
        [string] $TagSet = 'CI',
        [string] $TitlePrefix,
        [string] $OutputFormat = "NUnitXml"
    )

    $output = Split-Path -Parent (Get-PSOutput -Options (Get-PSOptions))
    $testResultsNoSudo = "$PWD/TestResultsNoSudo.xml"
    $testResultsSudo = "$PWD/TestResultsSudo.xml"
    $testExcludeTag = $ExcludeTag + 'RequireSudoOnUnix'
    $pesterPassThruNoSudoObject = $null
    $pesterPassThruSudoObject = $null
    $noSudoResultsWithExpFeatures = $null
    $sudoResultsWithExpFeatures = $null

    $noSudoPesterParam = @{
        'BinDir'       = $output
        'PassThru'     = $true
        'Terse'        = $true
        'Tag'          = @()
        'ExcludeTag'   = $testExcludeTag
        'OutputFile'   = $testResultsNoSudo
        'OutputFormat' = $OutputFormat
    }

    # Get the experimental feature names and the tests associated with them
    $ExperimentalFeatureTests = Get-ExperimentalFeatureTests

    # Running tests which do not require sudo.
    if($Purpose -eq 'UnelevatedPesterTests' -or $Purpose -eq 'All')
    {
        $title = "Pester No Sudo - $TagSet"
        if ($TitlePrefix) {
            $title = "$TitlePrefix - $title"
        }
        $pesterPassThruNoSudoObject = Start-PSPester @noSudoPesterParam -Title $title

        # Running tests that do not require sudo, with specified experimental features enabled
        $noSudoResultsWithExpFeatures = @()
        foreach ($entry in $ExperimentalFeatureTests.GetEnumerator()) {
            $featureName = $entry.Key
            $testFiles = $entry.Value

            $expFeatureTestResultFile = "$PWD\TestResultsNoSudo.$featureName.xml"
            $noSudoPesterParam['OutputFile'] = $expFeatureTestResultFile
            $noSudoPesterParam['ExperimentalFeatureName'] = $featureName
            if ($testFiles.Count -eq 0) {
                # If an empty array is specified for the feature name, we run all tests with the feature enabled.
                # This allows us to prevent regressions to a critical engine experimental feature.
                $noSudoPesterParam.Remove('Path')
            }
            else
            {
                # If a non-empty string or array is specified for the feature name, we only run those test files.
                $noSudoPesterParam['Path'] = $testFiles
            }
            $title = "Pester Experimental No Sudo - $featureName - $TagSet"
            if ($TitlePrefix) {
                $title = "$TitlePrefix - $title"
            }
            $passThruResult = Start-PSPester @noSudoPesterParam -Title $title -SkipTestToolBuild

            $noSudoResultsWithExpFeatures += $passThruResult
        }
    }

    # Running tests, which require sudo.
    if($Purpose -eq 'ElevatedPesterTests' -or $Purpose -eq 'All')
    {
        $sudoPesterParam = $noSudoPesterParam.Clone()
        $sudoPesterParam.Remove('Path')
        $sudoPesterParam['Tag'] = @('RequireSudoOnUnix')
        $sudoPesterParam['ExcludeTag'] = $ExcludeTag
        $sudoPesterParam['Sudo'] = $true
        $sudoPesterParam['OutputFile'] = $testResultsSudo
        $sudoPesterParam['OutputFormat'] = $OutputFormat

        $title = "Pester Sudo - $TagSet"
        if ($TitlePrefix) {
            $title = "$TitlePrefix - $title"
        }
        $pesterPassThruSudoObject = Start-PSPester @sudoPesterParam -Title $title

        # Running tests that require sudo, with specified experimental features enabled
        $sudoResultsWithExpFeatures = @()
        foreach ($entry in $ExperimentalFeatureTests.GetEnumerator()) {
            $featureName = $entry.Key
            $testFiles = $entry.Value

            $expFeatureTestResultFile = "$PWD\TestResultsSudo.$featureName.xml"
            $sudoPesterParam['OutputFile'] = $expFeatureTestResultFile
            $sudoPesterParam['ExperimentalFeatureName'] = $featureName
            if ($testFiles.Count -eq 0)
            {
                # If an empty array is specified for the feature name, we run all tests with the feature enabled.
                # This allows us to prevent regressions to a critical engine experimental feature.
                $sudoPesterParam.Remove('Path')
            }
            else
            {
                # If a non-empty string or array is specified for the feature name, we only run those test files.
                $sudoPesterParam['Path'] = $testFiles
            }

            $title = "Pester Experimental Sudo - $featureName - $TagSet"
            if ($TitlePrefix) {
                $title = "$TitlePrefix - $title"
            }

            # We just built the test tools for the main test run, we don't need to rebuild them
            $passThruResult = Start-PSPester @sudoPesterParam -Title $title -SkipTestToolBuild

            $sudoResultsWithExpFeatures += $passThruResult
        }
    }

    # Determine whether the build passed
    try {
        $allTestResultsWithNoExpFeature = @($pesterPassThruNoSudoObject, $pesterPassThruSudoObject)
        $allTestResultsWithExpFeatures = @($noSudoResultsWithExpFeatures, $sudoResultsWithExpFeatures)
        # This throws if there was an error:
        $allTestResultsWithNoExpFeature | Where-Object {$null -ne $_} | ForEach-Object { Test-PSPesterResults -ResultObject $_ }
        $allTestResultsWithExpFeatures  | Where-Object {$null -ne $_} | ForEach-Object { Test-PSPesterResults -ResultObject $_ -CanHaveNoResult }
        $result = "PASS"
    } catch {
        # The build failed, set the result:
        $resultError = $_
        $result = "FAIL"
    }

    # If the tests did not pass, throw the reason why
    if ( $result -eq "FAIL" )
    {
        Write-Warning "Tests failed. See the issue below."
        Throw $resultError
    }
    else
    {
        Write-Verbose "Tests did not fail! Nice job!"
    }
}

function New-LinuxPackage
{

    $isFullBuild = Test-DailyBuild
    $releaseTag = Get-ReleaseTag
    $packageParams = @{}
    $packageParams += @{ReleaseTag=$releaseTag}

    # Only build packages for PowerShell/PowerShell repository
    # branches, not pull requests
    $packages = @(Start-PSPackage @packageParams -SkipReleaseChecks -Type deb, rpm, rpm-fxdependent-arm64, tar)
    foreach($package in $packages)
    {
        if (Test-Path $package)
        {
            Write-Log "Package found: $package"
        }
        else
        {
            Write-Error -Message "Package NOT found: $package"
        }

        if ($package -isnot [System.IO.FileInfo])
        {
            $packageObj = Get-Item $package
            Write-Error -Message "The PACKAGE is not a FileInfo object"
        }
        else
        {
            $packageObj = $package
        }

        # Determine artifacts directory (GitHub Actions or Azure DevOps)
        $artifactsDir = if ($env:GITHUB_ACTIONS -eq 'true') {
            "${env:GITHUB_WORKSPACE}/../packages"
        } else {
            "${env:BUILD_ARTIFACTSTAGINGDIRECTORY}"
        }
        
        # Ensure artifacts directory exists
        if (-not (Test-Path $artifactsDir)) {
            New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
        }
        
        Write-Log -message "Artifacts directory: $artifactsDir"
        Copy-Item $packageObj.FullName -Destination $artifactsDir -Force
    }

    if ($IsLinux)
    {
        # Determine artifacts directory (GitHub Actions or Azure DevOps)
        $artifactsDir = if ($env:GITHUB_ACTIONS -eq 'true') {
            "${env:GITHUB_WORKSPACE}/../packages"
        } else {
            "${env:BUILD_ARTIFACTSTAGINGDIRECTORY}"
        }
        
        # Create and package Raspbian .tgz
        # Build must be clean for Raspbian
        Start-PSBuild -PSModuleRestore -Clean -Runtime linux-arm -Configuration 'Release'
        $armPackage = Start-PSPackage @packageParams -Type tar-arm -SkipReleaseChecks
        Copy-Item $armPackage -Destination $artifactsDir -Force
    }
}

function Invoke-InitializeContainerStage {
    param(
        [string]
        $ContainerPattern = '.'
    )

    Write-Verbose "Invoking InitializeContainerStage with ContainerPattern: ${ContainerPattern}" -Verbose

    $fallbackSeed = (get-date).DayOfYear
    Write-Verbose "Fall back seed: $fallbackSeed" -Verbose

    # For PRs set the seed to the PR number so that the image is always the same
    $seed = $env:SYSTEM_PULLREQUEST_PULLREQUESTID

    if(!$seed) {
      # for non-PRs use the integer identifier of the build as the seed.
      $seed = $fallbackSeed
    }

    # cut down to 32 bits and keep the most varying parts, which are lower bits
    if ($seed -ge [Int32]::MaxValue) {
        $seed = [int]($seed -band [int]::MaxValue)
    }

    Write-Verbose "Seed: $seed" -Verbose

    # Get the latest image matrix JSON for preview
    $matrix = ./PowerShell-Docker/build.ps1 -GenerateMatrixJson -FullJson -Channel preview | ConvertFrom-Json

    # Chose images that are validated or validating, Linux and can be used in CI.
    $linuxImages = $matrix.preview |
      Where-Object {$_.IsLinux -and $_.UseInCi -and $_.DistributionState -match 'Validat.*' -and $_.JobName -match $ContainerPattern -and $_.JobName -notlike "*arm*"} |
      Select-Object JobName, Taglist |
      Sort-Object -property JobName

    # Use the selected seed to pick a container
    $index = Get-Random -Minimum 0 -Maximum $linuxImages.Count -SetSeed $seed
    $selectedImage = $linuxImages[$index]

    # Filter to the first test-deps compatible tag
    $tag = $selectedImage.Taglist -split ';' | Where-Object {$_ -match 'preview-\D+'} | Select-Object -First 1

    # Calculate the container name
    $containerName = "mcr.microsoft.com/powershell/test-deps:$tag"

    Set-BuildVariable -Name containerName -Value $containerName -IsOutput
    Set-BuildVariable -Name containerBuildName -Value $selectedImage.JobName -IsOutput

    if($env:BUILD_REASON -eq 'PullRequest') {
      Write-Host "##vso[build.updatebuildnumber]PR-${env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER}-$($selectedImage.JobName)-$((get-date).ToString("yyyyMMddhhmmss"))"
    } else {
      Write-Host "##vso[build.updatebuildnumber]${env:BUILD_SOURCEBRANCHNAME}-$($selectedImage.JobName)-${env:BUILD_SOURCEVERSION}-$((get-date).ToString("yyyyMMddhhmmss"))"

      # Cannot do this for a PR
      Write-Host "##vso[build.addbuildtag]$($selectedImage.JobName)"
    }
}

Function Test-MergeConflictMarker
{
    <#
    .SYNOPSIS
        Checks files for Git merge conflict markers and outputs results for GitHub Actions.
    .DESCRIPTION
        Scans the specified files for Git merge conflict markers (<<<<<<<, =======, >>>>>>>)
        and generates console output, GitHub Actions outputs, and job summary.
        Designed for use in GitHub Actions workflows.
    .PARAMETER File
        Array of file paths (relative or absolute) to check for merge conflict markers.
    .PARAMETER WorkspacePath
        Base workspace path for resolving relative paths. Defaults to current directory.
    .PARAMETER OutputPath
        Path to write GitHub Actions outputs. Defaults to $env:GITHUB_OUTPUT.
    .PARAMETER SummaryPath
        Path to write GitHub Actions job summary. Defaults to $env:GITHUB_STEP_SUMMARY.
    .EXAMPLE
        Test-MergeConflictMarker -File @('file1.txt', 'file2.cs') -WorkspacePath $env:GITHUB_WORKSPACE
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [AllowEmptyCollection()]
        [string[]] $File = @(),

        [Parameter()]
        [string] $WorkspacePath = $PWD,

        [Parameter()]
        [string] $OutputPath = $env:GITHUB_OUTPUT,

        [Parameter()]
        [string] $SummaryPath = $env:GITHUB_STEP_SUMMARY
    )

    Write-Host "Starting merge conflict marker check..." -ForegroundColor Cyan

    # Helper function to write outputs when no files to check
    function Write-NoFilesOutput {
        param(
            [string]$Message,
            [string]$OutputPath,
            [string]$SummaryPath
        )
        
        # Output results to GitHub Actions
        if ($OutputPath) {
            "files-checked=0" | Out-File -FilePath $OutputPath -Append -Encoding utf8
            "conflicts-found=0" | Out-File -FilePath $OutputPath -Append -Encoding utf8
        }
        
        # Create GitHub Actions job summary
        if ($SummaryPath) {
            $summaryContent = @"
# Merge Conflict Marker Check Results

## Summary
- **Files Checked:** 0
- **Files with Conflicts:** 0

## ℹ️ No Files to Check

$Message

"@
            $summaryContent | Out-File -FilePath $SummaryPath -Encoding utf8
        }
    }

    # Handle empty file list (e.g., when PR only deletes files)
    if ($File.Count -eq 0) {
        Write-Host "No files to check (empty file list)" -ForegroundColor Yellow
        Write-NoFilesOutput -Message "No files were provided for checking (this can happen when a PR only deletes files)." -OutputPath $OutputPath -SummaryPath $SummaryPath
        return
    }

    # Filter out *.cs files from merge conflict checking
    $filesToCheck = @($File | Where-Object { $_ -notlike "*.cs" })
    $filteredCount = $File.Count - $filesToCheck.Count
    
    if ($filteredCount -gt 0) {
        Write-Host "Filtered out $filteredCount *.cs file(s) from merge conflict checking" -ForegroundColor Yellow
    }
    
    if ($filesToCheck.Count -eq 0) {
        Write-Host "No files to check after filtering (all files were *.cs)" -ForegroundColor Yellow
        Write-NoFilesOutput -Message "All $filteredCount file(s) were filtered out (*.cs files are excluded from merge conflict checking)." -OutputPath $OutputPath -SummaryPath $SummaryPath
        return
    }

    Write-Host "Checking $($filesToCheck.Count) changed files for merge conflict markers" -ForegroundColor Cyan

    # Convert relative paths to absolute paths for processing
    $absolutePaths = $filesToCheck | ForEach-Object {
        if ([System.IO.Path]::IsPathRooted($_)) {
            $_
        } else {
            Join-Path $WorkspacePath $_
        }
    }

    $filesWithConflicts = @()
    $filesChecked = 0

    foreach ($filePath in $absolutePaths) {
        # Check if file exists (might be deleted)
        if (-not (Test-Path $filePath)) {
            Write-Verbose "  Skipping deleted file: $filePath"
            continue
        }

        # Skip binary files and directories
        if ((Get-Item $filePath) -is [System.IO.DirectoryInfo]) {
            continue
        }

        $filesChecked++
        
        # Get relative path for display
        $relativePath = if ($WorkspacePath -and $filePath.StartsWith($WorkspacePath)) {
            $filePath.Substring($WorkspacePath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        } else {
            $filePath
        }
        
        Write-Host "  Checking: $relativePath" -ForegroundColor Gray

        # Search for conflict markers using Select-String
        try {
            # Git conflict markers are 7 characters followed by a space or end of line
            # Regex pattern breakdown:
            #   ^            - Matches the start of a line
            #   (<{7}|={7}|>{7}) - Matches exactly 7 consecutive '<', '=', or '>' characters (Git conflict markers)
            #   (\s|$)       - Ensures the marker is followed by whitespace or end of line
            $pattern = '^(<{7}|={7}|>{7})(\s|$)'
            $matchedLines = Select-String -Path $filePath -Pattern $pattern -AllMatches -ErrorAction Stop

            if ($matchedLines) {
                # Collect marker details with line numbers (Select-String provides LineNumber automatically)
                $markerDetails = @()

                foreach ($match in $matchedLines) {
                    $markerDetails += [PSCustomObject]@{
                        Marker = $match.Matches[0].Groups[1].Value
                        Line = $match.LineNumber
                    }
                }

                $filesWithConflicts += [PSCustomObject]@{
                    File = $relativePath
                    MarkerDetails = $markerDetails
                }

                Write-Host "  ❌ CONFLICT MARKERS FOUND in $relativePath" -ForegroundColor Red
                foreach ($detail in $markerDetails) {
                    Write-Host "     Line $($detail.Line): $($detail.Marker)" -ForegroundColor Red
                }
            }
        }
        catch {
            # Skip files that can't be read (likely binary)
            Write-Verbose "  Skipping unreadable file: $relativePath"
        }
    }

    # Output results to GitHub Actions
    if ($OutputPath) {
        "files-checked=$filesChecked" | Out-File -FilePath $OutputPath -Append -Encoding utf8
        "conflicts-found=$($filesWithConflicts.Count)" | Out-File -FilePath $OutputPath -Append -Encoding utf8
    }

    Write-Host "`nSummary:" -ForegroundColor Cyan
    Write-Host "  Files checked: $filesChecked" -ForegroundColor Cyan
    Write-Host "  Files with conflicts: $($filesWithConflicts.Count)" -ForegroundColor Cyan

    # Create GitHub Actions job summary
    if ($SummaryPath) {
        $summaryContent = @"
# Merge Conflict Marker Check Results

## Summary
- **Files Checked:** $filesChecked
- **Files with Conflicts:** $($filesWithConflicts.Count)

"@

        if ($filesWithConflicts.Count -gt 0) {
            Write-Host "`n❌ Merge conflict markers detected in the following files:" -ForegroundColor Red

            $summaryContent += "`n## ❌ Conflicts Detected`n`n"
            $summaryContent += "The following files contain merge conflict markers:`n`n"

            foreach ($fileInfo in $filesWithConflicts) {
                Write-Host "  - $($fileInfo.File)" -ForegroundColor Red

                $summaryContent += "### 📄 ``$($fileInfo.File)```n`n"
                $summaryContent += "| Line | Marker |`n"
                $summaryContent += "|------|--------|`n"

                foreach ($detail in $fileInfo.MarkerDetails) {
                    Write-Host "     Line $($detail.Line): $($detail.Marker)" -ForegroundColor Red
                    $summaryContent += "| $($detail.Line) | ``$($detail.Marker)`` |`n"
                }
                $summaryContent += "`n"
            }

            $summaryContent += "`n**Action Required:** Please resolve these conflicts before merging.`n"
            Write-Host "`nPlease resolve these conflicts before merging." -ForegroundColor Red
        } else {
            Write-Host "`n✅ No merge conflict markers found" -ForegroundColor Green
            $summaryContent += "`n## ✅ No Conflicts Found`n`nAll checked files are free of merge conflict markers.`n"
        }

        $summaryContent | Out-File -FilePath $SummaryPath -Encoding utf8
    }

    # Exit with error if conflicts found
    if ($filesWithConflicts.Count -gt 0) {
        throw "Merge conflict markers detected in $($filesWithConflicts.Count) file(s)"
    }
}
