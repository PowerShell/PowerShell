# Looking at pester internal to get tag filter and ExcludeTagFilter
# This seems like the most stable way to do this
# other options like testing for tags seems more likely to break

InModuleScope Pester {
    Describe 'Getting Tag Filters' -Tag CI {
        $global:__PesterTags = $pester.TagFilter
        $global:__PesterExcludeTags = $pester.ExcludeTagFilter
    }
}
Describe 'SDK Send Greeting Sample Tests' -Tag CI {

    try {
        $enlistmentRoot = Join-Path $PSScriptRoot "../../../"
        $enlistmentRoot = Resolve-Path $enlistmentRoot | % Path
        $docLocation = Join-Path -Path $enlistmentRoot -ChildPath '\docs\cmdlet-example'
        $testResultPath = Join-Path $TestDrive 'sendgreetingresults.xml'
        $sampleCopy = Join-Path $TestDrive 'sendgreeting'
        $fullSampleCopyPath = Join-Path $sampleCopy 'cmdlet-example'
        $powershell = (Get-Process -id $PID).MainModule.FileName
        if(!(Test-Path $sampleCopy))
        {
            New-Item -ItemType Directory -Path $sampleCopy
        }

        Copy-Item -Recurse -Path $docLocation -Destination $sampleCopy -Force
        Get-ChildItem -Recurse $sampleCopy | %{ Write-Verbose "sc: $($_.FullName)"}

        $pesterCommand = "Invoke-Pester $sampleCopy -PassThru"
        if($global:__PesterTags)
        {
            $pesterCommand += " -Tag $(@($global:__PesterTags) -join ',')"
        }

        if($global:__PesterExcludeTags)
        {
            $pesterCommand += " -ExcludeTag $(@($global:__PesterExcludeTags) -join ',')"
        }

        $importPesterCommand = 'Import-module Pester'
        if($IsCoreCLR)
        {
            $importPesterCommand = "Import-Module $(Join-Path -path $PSHOME -child '/Modules/Pester')"
        }

        $command = @"
Push-Location -Path $fullSampleCopyPath
$importPesterCommand
$pesterCommand | Export-Clixml -Path $testResultPath
"@

        Write-Verbose -Message "command: '$command'"
        $bytes = [System.Text.Encoding]::Unicode.GetBytes($command)
        $encodedCommand = [Convert]::ToBase64String($bytes)
        &$powershell -encodedCommand $encodedCommand

        it "Should have test results file" {
            $testResultPath | should exist
            $script:results = Import-Clixml $testResultPath
        }

        it "Should have test results" {
            $script:results | should not be BeNullOrEmpty
            $script:results.TotalCount | should not BeNullOrEmpty
            $script:results.TestResult.Count | should not BeNullOrEmpty
        }

        it "Should have no failures" {
            $script:results.FailedCount | should be 0
        }

        foreach($testResult in $script:results.TestResult){
            Context "Test $($testResult.Name)" {
                it "should have no failure message" {
                    $testResult.FailureMessage | should BeNullOrEmpty
                }
                it "should have no stack trace" {
                    $testResult.StackTrace | should BeNullOrEmpty
                }
                it "should have no error record" {
                    $testResult.ErrorRecord | should BeNullOrEmpty
                }
                it "should have not failed" {
                    Write-Verbose "Result: $($testResult.Result)"
                    $testResult.Result | should not be Failed
                }
            }
        }

    } finally {
        Pop-Location
    }

}
