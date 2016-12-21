$codeCoverageZip = 'https://ci.appveyor.com/api/projects/PowerShell/powershell-f975h/artifacts/CodeCoverage.zip'
$testContentZip = 'https://ci.appveyor.com/api/projects/PowerShell/powershell-f975h/artifacts/tests.zip'
$openCoverZip = 'https://ci.appveyor.com/api/projects/PowerShell/powershell-f975h/artifacts/OpenCover.zip'


New-Item "$PSScriptRoot\logs.txt"  -Force
"codeCoverageZip: $codeCoverageZip" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append
"testcontentZip: $testContentZip" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append
"openCoverZip: $openCoverZip" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

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
    "Starting downloads." | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    Invoke-WebRequest -uri $codeCoverageZip -outfile "$outputBaseFolder\PSCodeCoverage.zip"
    Invoke-WebRequest $testContentZip -outfile "$outputBaseFolder\tests.zip"
    Invoke-WebRequest $openCoverZip -outfile "$outputBaseFolder\OpenCover.zip"

    "Downloads complete." | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    "Starting expansion." | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    Expand-Archive -path "$outputBaseFolder\PSCodeCoverage.zip" -destinationpath "$psBinPath"
    Expand-Archive -path "$outputBaseFolder\tests.zip" -destinationpath $testPath
    Expand-Archive -path "$outputBaseFolder\OpenCover.zip" -destinationpath $openCoverPath

    ## Download Coveralls.net uploader
    $coverallsToolsUrl = 'https://github.com/csMACnz/coveralls.net/releases/download/0.7.0/coveralls.net.0.7.0.nupkg'
    $coverallsPath = "$outputBaseFolder\coveralls"

    ## Saving the nupkg as zip so we can expand it.
    Invoke-WebRequest $coverallsToolsUrl -outfile "$outputBaseFolder\coveralls.zip"
    Expand-Archive -Path "$outputBaseFolder\coveralls.zip" -DestinationPath $coverallsPath

    "Expansion complete." | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    Import-Module "$openCoverPath\OpenCover" -force
    Install-OpenCover -TargetDirectory $openCoverTargetDirectory -force
    "OpenCover installed." | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    "TestDirectory : $testPath" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append
    "openCoverPath : $openCoverTargetDirectory\OpenCover" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append
    "psbinpath : $psBinPath" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    $openCoverParams = @{outputlog = $outputLog;
                         TestDirectory = $testPath;
                         OpenCoverPath = "$openCoverTargetDirectory\OpenCover";
                         PowerShellExeDirectory = "$psBinPath\publish"
                        }

    $openCoverParams | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    "Starting test run." | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    Invoke-OpenCover @openCoverParams

    if(Test-Path $outputLog)
    {
        get-childitem $outputLog | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append
    }

    "Test run done.!!" | Tee-Object -FilePath "$PSScriptRoot\logs.txt" -Append

    $powershellVersionString = (Get-Content "$psBinPath\publish\powershell.version")
    $commitId = $powershellVersionString.Substring($powershellVersionString.LastIndexOf('-g') + 2)
    $commitId | Add-Content -Path "$PSScriptRoot\logs.txt"

    $coverallsPath = "$outputBaseFolder\coveralls"
    $coverallsToken = 'SaRLabTUDNqaO2JT9uuYcR78a7kia044D'

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

    $coverallsParams | Add-Content -Path "$PSScriptRoot\logs.txt"

    & $coverallsExe """$coverallsParams"""
}
catch
{
    $_
}
finally
{
    Remove-Item -recurse -force -path $outputBaseFolder
}
