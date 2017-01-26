﻿param(
    [Parameter(Mandatory = $true, Position = 0)] $coverallsToken,
    [Parameter(Mandatory = $true, Position = 1)] $codecovToken,
    [Parameter(Position = 2)] $azureLogDrive = "L:\"
)

function Write-LogPassThru
{
    Param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, Position = 0)]
        [string] $Message,
        $Path = "$env:Temp\CodeCoverageRunLogs.txt"
    )

    $message = "{0:d} - {0:t} : {1}" -f ([datetime]::now),$message
    Add-Content -Path $Path -Value $Message -PassThru -Force
}

function Write-BashInvokerScript
{
    param($path)

    $scriptContent =
    @'
    @echo off
    setlocal

    if not exist "%~dpn0.sh" echo Script "%~dpn0.sh" not found & exit 2

    set _CYGBIN=C:\cygwin64\bin
    if not exist "%_CYGBIN%" echo Couldn't find Cygwin at "%_CYGBIN%" & exit 3

    :: Resolve ___.sh to /cygdrive based *nix path and store in %_CYGSCRIPT%
    for /f "delims=" %%A in ('%_CYGBIN%\cygpath.exe "%~dpn0.sh"') do set _CYGSCRIPT=%%A

    :: Throw away temporary env vars and invoke script, passing any args that were passed to us
    endlocal & %_CYGBIN%\bash --login "%_CYGSCRIPT%" %*
'@

    $scriptContent | Out-File $path -Force -Encoding ascii
}

Write-LogPassThru -Message "***** New Run *****"

$codeCoverageZip = 'https://ci.appveyor.com/api/projects/PowerShell/powershell-f975h/artifacts/CodeCoverage.zip'
$testContentZip = 'https://ci.appveyor.com/api/projects/PowerShell/powershell-f975h/artifacts/tests.zip'
$openCoverZip = 'https://ci.appveyor.com/api/projects/PowerShell/powershell-f975h/artifacts/OpenCover.zip'

Write-LogPassThru -Message "codeCoverageZip: $codeCoverageZip"
Write-LogPassThru -Message "testcontentZip: $testContentZip"
Write-LogPassThru -Message "openCoverZip: $openCoverZip"

$outputBaseFolder = "$env:Temp\CC"
$null = New-Item -ItemType Directory -Path $outputBaseFolder -Force

$openCoverPath = "$outputBaseFolder\OpenCover"
$testPath = "$outputBaseFolder\tests"
$psBinPath = "$outputBaseFolder\PSCodeCoverage"
$openCoverTargetDirectory = "$outputBaseFolder\OpenCoverToolset"
$outputLog = "$outputBaseFolder\CodeCoverageOutput.xml"
$psCodeZip = "$outputBaseFolder\PSCode.zip"
$psCodePath = "$outputBaseFolder\PSCode"
$elevatedLogs = "$outputBaseFolder\TestResults_Elevated.xml"
$unelevatedLogs = "$outputBaseFolder\TestResults_Unelevated.xml"

try
{
    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Stop'
    Write-LogPassThru -Message "Starting downloads."
    Invoke-WebRequest -uri $codeCoverageZip -outfile "$outputBaseFolder\PSCodeCoverage.zip"
    Invoke-WebRequest -uri $testContentZip -outfile "$outputBaseFolder\tests.zip"
    Invoke-WebRequest -uri $openCoverZip -outfile "$outputBaseFolder\OpenCover.zip"
    Write-LogPassThru -Message "Downloads complete. Starting expansion"

    Expand-Archive -path "$outputBaseFolder\PSCodeCoverage.zip" -destinationpath "$psBinPath" -Force
    Expand-Archive -path "$outputBaseFolder\tests.zip" -destinationpath $testPath -Force
    Expand-Archive -path "$outputBaseFolder\OpenCover.zip" -destinationpath $openCoverPath -Force

    ## Download Coveralls.net uploader
    $coverallsToolsUrl = 'https://github.com/csMACnz/coveralls.net/releases/download/0.7.0/coveralls.net.0.7.0.nupkg'
    $coverallsPath = "$outputBaseFolder\coveralls"

    ## Saving the nupkg as zip so we can expand it.
    Invoke-WebRequest -uri $coverallsToolsUrl -outfile "$outputBaseFolder\coveralls.zip"
    Expand-Archive -Path "$outputBaseFolder\coveralls.zip" -DestinationPath $coverallsPath -Force

    Write-LogPassThru -Message "Expansion complete."

    Import-Module "$openCoverPath\OpenCover" -Force
    Install-OpenCover -TargetDirectory $openCoverTargetDirectory -force
    Write-LogPassThru -Message "OpenCover installed."

    Write-LogPassThru -Message "TestDirectory : $testPath"
    Write-LogPassThru -Message "openCoverPath : $openCoverTargetDirectory\OpenCover"
    Write-LogPassThru -Message "psbinpath : $psBinPath"
    Write-LogPassThru -Message "elevatedLog : $elevatedLogs"
    Write-LogPassThru -Message "unelevatedLog : $unelevatedLogs"

    $openCoverParams = @{outputlog = $outputLog;
        TestDirectory = $testPath;
        OpenCoverPath = "$openCoverTargetDirectory\OpenCover";
        PowerShellExeDirectory = "$psBinPath\publish";
        PesterLogElevated = $elevatedLogs;
        PesterLogUnelevated = $unelevatedLogs;
    }

    $openCoverParams | Out-String | Write-LogPassThru
    Write-LogPassThru -Message "Starting test run."

    if(Test-Path $outputLog)
    {
        Remove-Item $outputLog -Force -ErrorAction SilentlyContinue
    }

    Invoke-OpenCover @openCoverParams

    if(Test-Path $outputLog)
    {
        Write-LogPassThru -Message (get-childitem $outputLog).FullName
    }

    Write-LogPassThru -Message "Test run done."

    $gitCommitId = & "$psBinPath\publish\powershell.exe" -noprofile -command { $PSVersiontable.GitCommitId }
    $commitId = $gitCommitId.substring($gitCommitId.LastIndexOf('-g') + 2)

    Write-LogPassThru -Message $commitId

    $coverallsPath = "$outputBaseFolder\coveralls"

    $commitInfo = Invoke-RestMethod -Method Get "https://api.github.com/repos/powershell/powershell/git/commits/$commitId"
    $message = ($commitInfo.message).replace("`n", " ")
    $author = $commitInfo.author.name
    $email = $commitInfo.author.email

    $coverallsExe = Join-Path $coverallsPath "tools\csmacnz.Coveralls.exe"
    $coverallsParams = @("--opencover",
        "-i $outputLog",
        "--repoToken $coverallsToken",
        "--commitId $commitId",
        "--commitBranch master",
        "--commitAuthor `"$author`"",
        "--commitEmail $email",
        "--commitMessage `"$message`""
    )

    $coverallsParams | ForEach-Object { Write-LogPassThru -Message $_ }

    Write-LogPassThru -Message "Uploading to CoverAlls"
    & $coverallsExe """$coverallsParams"""

    $bashScriptInvoker = "$PSScriptRoot\CodecovUploader.cmd"
    $bashScript = "$PSScriptRoot\CodecovUploader.sh"
    $cygwinLocation = "$env:SystemDrive\cygwin*"

    if($bashScript)
    {
        Remove-Item $bashScript -Force -ErrorAction SilentlyContinue
    }

    Invoke-RestMethod 'https://codecov.io/bash' -OutFile $bashScript
    Write-BashInvokerScript -path $bashScriptInvoker

    if((Test-Path $bashScriptInvoker) -and
        (Test-Path $bashScript) -and
        (Test-Path $cygwinLocation)
    )
    {
        Write-LogPassThru -Message "Uploading to CodeCov"
        $cygwinPath = "/cygdrive/" + $outputLog.Replace("\", "/").Replace(":","")

        $codecovParmeters = @(
            "-f $cygwinPath"
            "-X gcov",
            "-B master",
            "-C $commitId",
            "-X network")

        $codecovParmetersString = $codecovParmeters -join ' '

        & $bashScriptInvoker $codecovParmetersString
    }
    else
    {
        Write-LogPassThru -Message "BashScript: $bashScript"
        Write-LogPassThru -Message "BashScriptInvoke: $bashScriptInvoker"
        Write-LogPassThru -Message "CygwinPath : $cygwinPath"
        Write-LogPassThru -Message "Cannot upload to codecov as some paths are not existent"
    }

    Write-LogPassThru -Message "Upload complete."
}
catch
{
    $_
}
finally
{
    ## See if Azure log directory is mounted
    if(Test-Path $azureLogDrive)
    {
        ##Create yyyy-dd folder
        $monthFolder = "{0:yyyy-mm}" -f [datetime]::Now
        $monthFolderFullPath = New-Item -Path (Join-Path $azureLogDrive $monthFolder) -ItemType Directory -Force
        $windowsFolderPath = New-Item (Join-Path $monthFolderFullPath "Windows") -ItemType Directory -Force

        $destinationPath = Join-Path $env:Temp ("CodeCoverageLogs-{0:yyyy_MM_dd}-{0:hh_mm_ss}.zip" -f [datetime]::Now)
        Compress-Archive -Path $elevatedLogs,$unelevatedLogs,$outputLog -DestinationPath $destinationPath
        Copy-Item $destinationPath $windowsFolderPath -Force -ErrorAction SilentlyContinue
    }

    ## Disable the cleanup till we stabilize.
    #Remove-Item -recurse -force -path $outputBaseFolder
    $ErrorActionPreference = $oldErrorActionPreference
}
