$ErrorActionPreference = 'Stop'
$repoRoot = Join-Path $PSScriptRoot '..'

Import-Module (Join-Path $repoRoot 'build.psm1')

# Emulates running all of AppVeyor but locally
# should not be used on AppVeyor
function Invoke-AppVeyorFull
{
    Invoke-AppVeyorInstall
    Invoke-AppVeyorBuild
    Invoke-AppVeyorTest
    Invoke-AppveyorFinish
}

# Implements the AppVeyor 'build_script' step
function Invoke-AppVeyorBuild
{
      # check to be sure our test tags are correct
      $result = Get-PesterTag
      if ( $result.Result -ne "Pass" ) {
        $result.Warnings
        throw "Tags must be CI, Feature, Scenario, or Slow"
      }
      Start-PSBuild -CrossGen -Configuration $buildConfiguration
      Start-PSBuild -FullCLR
}

# Implements the AppVeyor 'install' step
function Invoke-AppVeyorInstall
{
    Start-PSBootstrap -Force
}

# A wrapper to ensure that we upload test results
# and that if we are not able to that it does not fail
# the CI build
function Update-AppVeyorTestResults
{
    param(
        [string] $resultsFile
    )

    if($env:Appveyor)
    {
        $retryCount = 0 
        $pushedResults = $false
        $pushedArtifacts = $false 
        while( (!$pushedResults -or !$pushedResults) -and $retryCount -lt 3)
        {
            if($retryCount -gt 0)
            {
                Write-Verbose "Retrying updating test artifacts..."
            }

            $retryCount++
            $resolvedResultsPath = (Resolve-Path $resultsFile)
            try {
                (New-Object 'System.Net.WebClient').UploadFile("https://ci.appveyor.com/api/testresults/nunit/$($env:APPVEYOR_JOB_ID)", $resolvedResultsPath)
                $pushedResults = $true
            }
            catch {
                Write-Warning "Pushing test result failed..."
            }

            try {
                Push-AppveyorArtifact $resolvedResultsPath
                $pushedArtifacts = $true
            }
            catch {
                Write-Warning "Pushing test Artifact failed..."
            }
        }

        if(!$pushedResults -or !$pushedResults)
        {
            Write-Warning "Failed to push all artifacts for $resultsFile"
        }
    }
    else 
    {
        Write-Warning "Not running in appveyor, skipping upload of test results: $resultsFile"
    }
}

# Implement AppVeyor 'Test_script'
function Invoke-AppVeyorTest 
{
    #
    # CoreCLR
    $env:CoreOutput = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Publish -Configuration $buildConfiguration))
    Write-Host -Foreground Green 'Run CoreCLR tests'
    $testResultsFile = "$pwd\TestsResults.xml"
    if(!(Test-Path "$env:CoreOutput\powershell.exe"))
    {
        throw "CoreCL PowerShell.exe was not built"
    }
    
    & ("$env:CoreOutput\powershell.exe") -noprofile -noninteractive -c "Set-ExecutionPolicy -Scope Process Unrestricted; Invoke-Pester test/powershell -Tag 'CI' -ExcludeTag 'Slow' -OutputFormat NUnitXml -OutputFile $testResultsFile"
    Update-AppVeyorTestResults -resultsFile $testResultsFile
    
    #
    # FullCLR
    $env:FullOutput = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -FullCLR))
    Write-Host -Foreground Green 'Run FullCLR tests'
    $testResultsFileFullCLR = "$pwd\TestsResults.FullCLR.xml"
    Start-DevPowerShell -FullCLR -NoNewWindow -ArgumentList '-noprofile', '-noninteractive' -Command "Set-ExecutionPolicy -Scope Process Unrestricted; Invoke-Pester test/fullCLR -ExcludeTag 'Slow' -OutputFormat NUnitXml -OutputFile $testResultsFileFullCLR"
    Update-AppVeyorTestResults -resultsFile $testResultsFileFullCLR
 

    #
    # Fail the build, if tests failed
    Write-Host -Foreground Green 'Upload CoreCLR test results'
    $x = [xml](cat -raw $testResultsFile)
    if ([int]$x.'test-results'.failures -gt 0)
    {
    throw "$($x.'test-results'.failures) tests in test/powershell failed"
    }

    Write-Host -Foreground Green 'Upload FullCLR test results'
    $x = [xml](cat -raw $testResultsFileFullCLR)

    if ([int]$x.'test-results'.failures -gt 0)
    {
        throw "$($x.'test-results'.failures) tests in test/fullCLR failed"
    }
}

# Implements AppVeyor 'on_finish' step
function Invoke-AppveyorFinish
{
      try {
        # Build packages
        $packages = Start-PSPackage
        
        # Creating project artifact
        $name = git describe

        # Remove 'v' from version, append 'PowerShell' - to be consistent with other package names
        $name = $name -replace 'v',''        
        $name = 'PowerShell_' + $name

        $zipFilePath = Join-Path $pwd "$name.zip"
        $zipFileFullPath = Join-Path $pwd "$name.FullCLR.zip"
        Add-Type -assemblyname System.IO.Compression.FileSystem
        Write-Verbose "Zipping ${env:CoreOutput} into $zipFilePath" -verbose
        [System.IO.Compression.ZipFile]::CreateFromDirectory($env:CoreOutput, $zipFilePath)
        Write-Verbose "Zipping ${env:FullOutput} into $zipFileFullPath" -verbose
        [System.IO.Compression.ZipFile]::CreateFromDirectory($env:FullOutput, $zipFileFullPath)
        
        $artifacts = New-Object System.Collections.ArrayList
        foreach ($package in $packages) {
            $artifacts.Add($package)
        }
        
        $artifacts.Add($zipFilePath)
        $artifacts.Add($zipFileFullPath)

        if ($env:APPVEYOR_REPO_TAG_NAME)
        {
          # ignore the first part of semver, use the preview part
          $preReleaseVersion = ($env:APPVEYOR_REPO_TAG_NAME).Split('-')[1]
        }
        else
        {
          $previewLabel = (git describe --abbrev=0).Split('-')[1]
          $preReleaseVersion = "$previewLabel.$($env:APPVEYOR_BUILD_NUMBER)"
        }

        # only publish to nuget feed if it is not a pull request
        if(!$env:APPVEYOR_PULL_REQUEST_NUMBER)
        {
            Publish-NuGetFeed -OutputPath .\nuget-artifacts -VersionSuffix $preReleaseVersion
        }

        $artifacts += (ls .\nuget-artifacts | % {$_.FullName})

        $pushedAllArtifacts = $true
        $artifacts | % { 
            Write-Host "Pushing $_ as Appveyor artifact"
            if(Test-Path $_)
            {
                if($env:Appveyor)
                {
                    Push-AppveyorArtifact $_
                }
            }
            else
            {
              $pushedAllArtifacts = $false
              Write-Warning "Artifact $_ does not exist."
            }
          }
        if(!$pushedAllArtifacts)
        {
          throw "Some artifacts did not exist!"
        }
      } catch {
        Write-Host -Foreground Red $_
      }
}