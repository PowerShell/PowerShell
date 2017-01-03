param(
    [Parameter(Mandatory = $true, Position = 0)] $coverallsToken
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

    Import-Module "$openCoverPath\OpenCover" -force
    Install-OpenCover -TargetDirectory $openCoverTargetDirectory -force
    Write-LogPassThru -Message "OpenCover installed."

    Write-LogPassThru -Message "TestDirectory : $testPath"
    Write-LogPassThru -Message "openCoverPath : $openCoverTargetDirectory\OpenCover"
    Write-LogPassThru -Message "psbinpath : $psBinPath"

    $openCoverParams = @{outputlog = $outputLog;
                         TestDirectory = $testPath;
                         OpenCoverPath = "$openCoverTargetDirectory\OpenCover";
                         PowerShellExeDirectory = "$psBinPath\publish"
                        }

    $openCoverParams | Out-String | Write-LogPassThru
    Write-LogPassThru -Message "Starting test run."

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

    $coverallsParams | % { Write-LogPassThru -Message $_ }

    & $coverallsExe """$coverallsParams"""

    Write-LogPassThru -Message "Upload complete."
}
catch
{
    $_
}
finally
{
    Remove-Item -recurse -force -path $outputBaseFolder
    $ErrorActionPreference = $oldErrorActionPreference
}