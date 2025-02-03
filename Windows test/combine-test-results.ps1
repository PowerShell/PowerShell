param (
    [string]$outputFile = "$PSscriptroot\CombinedTestResults.xml"
)
$ErrorActionPreference = "Stop"

$null = new-item -Path $outputFile -ItemType File -Force | Out-Null
$outputFile = (Resolve-Path $outputFile).ProviderPath

# Function to combine test result files
function Combine-TestResults {
    param (
        [string]$outputFile
    )

    $testResults = @{}
    $folderPattern = '^([^-]+)-([^-]+)-([^-]+)-([^-]+)$'
    $folders = Get-ChildItem -Directory -Path $PSscriptroot | Where-Object { $_.Name -match $folderPattern }

    foreach ($folder in $folders) {
        $null = $folder.Name -match $folderPattern
        $priority = $matches[4]
        $purpose = $matches[3]
        $testFiles = Get-ChildItem -Path $folder.FullName -Filter *.xml
        foreach ($file in $testFiles) {
            [xml]$xml = Get-Content -Path $file.FullName
            $testResults["$purpose-$priority"] += $xml.testsuites.testsuite.testcase
        }
    }

    $combinedXml = [xml]@"
<testsuites>
</testsuites>
"@

    foreach($key in $testResults.Keys) {
        $purpose, $priority = $key -split '-'
        foreach ($testResult in $testResults.$key) {
            if($null -eq $testResult) {
                continue
            }
            $importedNode = $combinedXml.ImportNode($testResult, $true)
            $importedNode.SetAttribute('purpose', $purpose)
            $importedNode.SetAttribute('priority', $priority)
            $testSuites = $combinedXml.GetElementsByTagName('testsuites')

            $testSuites.AppendChild($importedNode) | Out-Null
        }
    }

    $combinedXml.Save($outputFile)
}

Combine-TestResults -outputFile $outputFile
