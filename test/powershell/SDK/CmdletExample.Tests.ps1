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
        $enlistmentRoot = git rev-parse --show-toplevel
        $docLocation = Join-Path -Path $enlistmentRoot -ChildPath '\docs\cmdlet-example'
        $testResultPath = Join-Path $TestDrive 'sendgreetingresults.xml'
        $sampleCopy = Join-Path $TestDrive 'sendgreeting'
        $fullSampleCopyPath = Join-Path $sampleCopy 'cmdlet-example'
        if(!(Test-Path $sampleCopy)) 
        {
            New-Item -ItemType Directory -Path $sampleCopy
        }

        Copy-Item -Recurse -Path $docLocation -Destination $sampleCopy -Force
        dir -Recurse $sampleCopy | %{ Write-Verbose "sc: $($_.FullName)"}
         
$pesterCommand = "Invoke-Pester $sampleCopy -PassThru"
if($global:__PesterTags)
{
    $pesterCommand += " -Tag $(@($global:__PesterTags) -join ',')"
}

if($global:__PesterExcludeTags)
{
    $pesterCommand += " -ExcludeTag $(@($global:__PesterExcludeTags) -join ',')"
} 
 
        $command = @"
Push-Location -Path $fullSampleCopyPath
Import-module $(Join-path $env:PSModulePath pester)
$pesterCommand | Export-Clixml -Path $testResultPath
"@

        Write-Verbose -Message "command: '$command'" -Verbose
        $bytes = [System.Text.Encoding]::Unicode.GetBytes($command)
        $encodedCommand = [Convert]::ToBase64String($bytes)
        &"$PSHOME/Powershell.exe" -encodedCommand $encodedCommand
        
        it "Should have test results file" {
            $testResultPath | should exist
            $script:results = Import-Clixml $testResultPath
        }
        #$host.EnterNestedPrompt();
        it "Should have test results" {
            $script:results | should not be BeNullOrEmpty
            $script:results.TotalCount | should not BeNullOrEmpty
            $script:results.TestResult.Count | should not BeNullOrEmpty
        }

        foreach($testResult in $script:results.TestResult){
            it "Test $($testResult.Name) should not fail" {
                $testResult.FailureMessage + $testResult.StackTrace | should BeNullOrEmpty
                $testResult.ErrorRecord | should BeNullOrEmpty
                Write-Verbose "Result: $($testResult.Result)"
                $testResult.Result | should not be Failed
            }
        }

    } finally {
        Pop-Location
    }

}
